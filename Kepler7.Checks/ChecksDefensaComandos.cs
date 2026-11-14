using Kepler7.Core;
using Kepler7.Core.Comandos;
using Kepler7.Core.Defensa;

// Verificaciones de Chain of Responsibility y Command (nivel 2).
// Se llaman desde Program.cs con: ChecksDefensaComandos.Ejecutar(Check);
public static class ChecksDefensaComandos
{
    static UnidadBatalla NaveDePrueba(int vida = 100) => new() { Nombre = "Prueba", Rol = "Soldado", VidaMax = vida, Vida = vida };

    public static void Ejecutar(Action<bool, string> Check)
    {
        // ---------- Chain of Responsibility ----------
        var u = NaveDePrueba();
        Check(u.Defensa.Descripcion() == "Esquiva → Bloqueo → Armadura → Escudo → Vida", "Chain: cadena de fábrica Esquiva → Bloqueo → Armadura → Escudo → Vida");
        var a = u.RecibirAtaque(new Danio(30));
        Check(u.Vida == 70 && a.LlegoALaVida == 30 && a.Recorrido.Count > 3, "Chain: sin módulos el daño llega entero a la vida y queda el recorrido");

        u = NaveDePrueba(); u.ReduccionArmadura = 4; u.Escudo = true;
        u.RecibirAtaque(new Danio(20));
        Check(u.Vida == 92 && !u.Escudo, "Chain: Armadura (-4) y después Escudo (mitad): 20 → 16 → 8");
        u = NaveDePrueba(); u.ReduccionArmadura = 4; u.Escudo = true;
        Check(u.Defensa.Mover("Escudo", -1) && u.Defensa.Descripcion() == "Esquiva → Bloqueo → Escudo → Armadura → Vida", "Chain: el escudo se adelanta en la cadena");
        u.RecibirAtaque(new Danio(20));
        Check(u.Vida == 94, "Chain: con otro orden el resultado cambia: Escudo 20 → 10 y Armadura → 6");

        u = NaveDePrueba();
        var bloq = u.RecibirAtaque(new Danio(40) { FactorBloqueo = .5f });
        Check(u.Vida == 80 && bloq.Recorrido.Any(r => r.StartsWith("[Bloqueo] Bloquea 20")), "Chain: el eslabón Bloqueo aplica el factor del minijuego");

        u = NaveDePrueba(); u.ProbEsquiva = 1;
        var esq = u.RecibirAtaque(new Danio(50));
        Check(esq.Esquivado && u.Vida == 100 && !esq.Recorrido.Any(r => r.StartsWith("[Vida]")), "Chain: si Esquiva resuelve el ataque, la cadena se corta");

        u = NaveDePrueba(); u.Defensa.Quitar("Armadura");
        Check(u.Defensa.Descripcion() == "Esquiva → Bloqueo → Escudo → Vida" && !u.Defensa.Quitar("Vida"), "Chain: se puede quitar un eslabón, pero no Vida");
        u.Defensa.Agregar("Armadura");
        Check(u.Defensa.Descripcion() == "Esquiva → Bloqueo → Escudo → Armadura → Vida" && !u.Defensa.Agregar("Armadura"), "Chain: agregar un eslabón en ejecución; Vida sigue al final");

        u = NaveDePrueba(30); u.Escudo = true;
        var sim = u.Defensa.Simular(20);
        Check(u.Vida == 30 && u.Escudo && sim.LlegoALaVida == 10, "Chain: Simular prueba la cadena sin tocar la nave");
        var muerta = NaveDePrueba(10).RecibirAtaque(new Danio(50));
        Check(muerta.ObjetivoDerrotado && muerta.LlegoALaVida == 50, "Chain: Vida informa la destrucción");

        var activa = BatallaOrbital.Crear(semilla: 3, eventos: false, modulosDefensivos: true);
        Check(activa.Contexto.Aliados[0].ReduccionArmadura == 6 && activa.Contexto.Aliados[1].ProbEsquiva > 0, "Chain: la batalla del juego activa Armadura y Esquiva");
        var inactiva = BatallaOrbital.Crear(semilla: 3, eventos: false);
        Check(inactiva.Contexto.Aliados.All(x => x.ReduccionArmadura == 0 && x.ProbEsquiva == 0), "Chain: sin módulos la batalla conserva el balance original");

        // ---------- Command: turnos de la flota ----------
        var b = BatallaOrbital.Crear(semilla: 7, eventos: false);
        var ctx = b.Contexto; var beta = ctx.Enemigos[1]; var nodriza = ctx.Enemigos[2];
        var g = new GestorComandos();
        int lineas0 = b.Historial.Count;
        Check(g.Ejecutar(ComandosBatalla.Para(b, beta, null, 1f)) && beta.Vida == beta.VidaMax - TurnoSoldado.Danio && b.TurnoActual is TurnoFrancotirador, "Command: AtacarComando ejecuta el turno del Soldado");
        Check(g.Deshacer() && beta.Vida == beta.VidaMax && b.TurnoActual is TurnoSoldado && b.Historial.Count == lineas0, "Command: Deshacer devuelve vida, turno y bitácora");
        Check(g.Rehacer() && beta.Vida == beta.VidaMax - TurnoSoldado.Danio && b.TurnoActual is TurnoFrancotirador, "Command: Rehacer reaplica el mismo resultado");
        Check(g.Historial.Count == 3 && g.Historial[1].Contains("DESHECHO"), "Command: el historial anota ejecutar, deshacer y rehacer");

        g.Ejecutar(ComandosBatalla.Para(b, null, null, 1f));     // Francotirador
        ctx.Aliados[0].Vida = 50;                                // el Médico tiene algo para reparar
        int vidaHerida = ctx.Aliados[0].Vida;
        Check(g.Ejecutar(ComandosBatalla.Para(b, ctx.Aliados[0], null, 1f)) && ctx.Aliados[0].Vida == vidaHerida + TurnoMedico.Reparacion, "Command: CurarComando repara con el turno del Médico");
        Check(g.Deshacer() && ctx.Aliados[0].Vida == vidaHerida, "Command: Deshacer una curación devuelve la vida anterior");
        g.Rehacer();

        var cmdHack = ComandosBatalla.Para(b, nodriza, new IAAleatoria(), 1f);
        Check(g.Ejecutar(cmdHack) && nodriza.Estrategia is IAAleatoria && b.CambiosDeEstrategia == 1 && b.FaseEnemiga, "Command: HackearComando cambia la IA (Strategy) y cierra la fase de la flota");
        Check(g.Deshacer() && nodriza.Estrategia is IACalculadora && b.CambiosDeEstrategia == 0 && !b.FaseEnemiga && b.TurnoActual is TurnoComandante, "Command: Deshacer un hackeo restaura la IA y vuelve el turno del Comandante");
        Check(g.Rehacer() && nodriza.Estrategia is IAAleatoria && b.FaseEnemiga, "Command: Rehacer el hackeo reconstruye la fase enemiga");
        g.Cerrar("Los mutantes actuaron");
        Check(!g.PuedeDeshacer && !g.PuedeRehacer, "Command: al actuar los mutantes se vacían las pilas");

        // ---------- Command: mover y construir ----------
        var gm = new GestorComandos();
        var nave = NaveDePrueba();
        Check(gm.Ejecutar(new MoverComando(nave, 2, 1)) && nave.X == 2 && nave.Y == 1, "Command: MoverComando mueve la nave");
        Check(!gm.Ejecutar(new MoverComando(nave, 5, 0, (x, y) => x <= 3)) && nave.X == 2, "Command: un movimiento inválido se rechaza y no entra al historial");
        Check(gm.Deshacer() && nave.X == 0 && nave.Y == 0 && gm.Rehacer() && nave.X == 2, "Command: Deshacer y Rehacer mover");

        var p = new Progreso { LaboratorioReparado = true };
        var receta = new RecetaCombate();
        var construida = new IngenieroJefe(new NaveBuilder()).Construir(receta);
        Check(!new GestorComandos().Ejecutar(new ConstruirComando(p, new[] { construida })), "Command: no se construye sin ingeniero");
        p.ClonarIngeniero("Jefa", "Menta");
        int xp = p.XP;
        var gc = new GestorComandos();
        Check(gc.Ejecutar(new ConstruirComando(p, new[] { construida })) && p.Flota.Count == 1 && p.XP == xp + 75, "Command: ConstruirComando suma la nave y el XP");
        Check(gc.Deshacer() && p.Flota.Count == 0 && p.XP == xp && gc.Rehacer() && p.Flota.Count == 1, "Command: Deshacer una construcción la quita y devuelve el XP");
        p.ClonarPiloto("Piloto", "Azul", receta); p.AsignarPiloto(construida);
        Check(!gc.PuedeDeshacer, "Command: una nave con piloto ya no se puede deshacer");

        // ---------- Insignias ----------
        var prog = new Progreso(); int x0 = prog.XP;
        Check(prog.RegistrarCadena() && !prog.RegistrarCadena() && prog.RegistrarDeshacer() && !prog.RegistrarDeshacer() && prog.XP == x0 + 200, "Insignias Chain y Command otorgan XP una sola vez");
        Check(Logros.Obtener(prog).Count(l => l.Obtenida) == 2 && Logros.Obtener(prog).Any(l => l.Patron == "Chain of Responsibility") && Logros.Obtener(prog).Any(l => l.Patron == "Command"), "El panel de logros incluye Chain y Command");
        var ruta = Path.Combine(Path.GetTempPath(), "kepler7-cc-" + Guid.NewGuid().ToString("N") + ".json");
        Guardado.Guardar(prog, ruta); var cargado = Guardado.Cargar(ruta); File.Delete(ruta);
        Check(cargado.ChainDominado && cargado.CommandDominado, "El guardado conserva las insignias Chain y Command");
    }
}
