using Kepler7.Core;

// Verificaciones del Nivel 2 · Escolta orbital (Template Method y Strategy).
// Se llaman desde Program.cs con: ChecksBatalla.Ejecutar(Check);
public static class ChecksBatalla
{
    public static void Ejecutar(Action<bool, string> Check)
    {
        // ---------- Template Method ----------
        var batalla = BatallaOrbital.Crear(semilla: 7);
        var ctx = batalla.Contexto;
        Check(batalla.TurnosAliados.Select(t => t.GetType().Name).SequenceEqual(new[] { "TurnoSoldado", "TurnoFrancotirador", "TurnoMedico", "TurnoComandante" }), "Template Method: cuatro subclases de TurnoUnidad");
        Check(typeof(TurnoUnidad).GetMethod("EjecutarTurno")!.IsVirtual == false, "Template Method: EjecutarTurno no es virtual");

        var beta = ctx.Enemigos[1]; var nodriza = ctx.Enemigos[2];
        batalla.JugarTurnoAliado(beta);
        var pasos = batalla.Historial.Skip(1).Take(4).ToList();
        Check(pasos.Count == 4 && pasos[0].Contains("Inicio") && pasos[1].Contains("Decide") && pasos[2].Contains("Actúa") && pasos[3].Contains("Fin"), "Template Method: inicio, decisión, acción y finalización en orden");
        Check(beta.Vida == beta.VidaMax - TurnoSoldado.Danio, "Soldado: aplica su daño al objetivo elegido");

        batalla.JugarTurnoAliado(beta);
        Check(beta.Vida == beta.VidaMax - TurnoSoldado.Danio - TurnoFrancotirador.Danio, "Francotirador: disparo de precisión");
        var franco = (TurnoFrancotirador)batalla.TurnosAliados[1];
        Check(!franco.Cargado, "Francotirador: queda recargando tras disparar");

        ctx.Aliados[1].Vida = 30;
        batalla.JugarTurnoAliado(ctx.Aliados[1]);
        Check(ctx.Aliados[1].Vida == 30 + TurnoMedico.Reparacion, "Médico: repara al aliado elegido");

        // ---------- Strategy ----------
        Check(batalla.Intencion(nodriza).Contains("Nave de Carga"), "Strategy: la Calculadora apunta a la Nave de Carga");
        batalla.JugarTurnoAliado(nodriza, new IAAgresiva());
        Check(nodriza.Estrategia is IAAgresiva && batalla.CambiosDeEstrategia == 1, "Strategy: el Comandante cambia la IA durante la partida");
        Check(batalla.Intencion(nodriza).Contains(ctx.AliadosVivos.MinBy(a => a.Vida)!.Nombre), "Strategy: el cambio modifica la decisión del enemigo");
        Check(batalla.FaseEnemiga && batalla.TurnoActual == null, "Tras los cuatro aliados empieza la fase enemiga");

        while (batalla.EjecutarSiguienteEnemigo()) { }
        Check(!batalla.FaseEnemiga && batalla.Ronda == 2 && batalla.TurnoActual is TurnoSoldado, "Termina la fase enemiga y comienza la ronda 2");
        Check(beta.Escudo, "Strategy: la Defensiva dañada levanta el escudo");
        Check(batalla.Historial.Any(h => h.Contains("IA Agresiva")) && batalla.Historial.Any(h => h.Contains("IA Defensiva")), "Historial muestra la IA que decidió cada turno");

        var c2 = BatallaOrbital.Crear(semilla: 1).Contexto;
        var debil = c2.Aliados[1]; debil.Vida = 10;
        var accionCalc = new IACalculadora().DecidirAccion(c2.Enemigos[2], c2);
        Check(accionCalc.Objetivo == debil, "Strategy: la Calculadora remata si puede destruir una nave");
        var accionAgr = new IAAgresiva().DecidirAccion(c2.Enemigos[0], c2);
        Check(accionAgr.Objetivo == debil && accionAgr.Valor == IAAgresiva.Potencia, "Strategy: la Agresiva ataca a la nave más dañada");
        Check(!new IAAleatoria().Predecible && BatallaOrbital.Crear().Intencion(new Enemigo { Nombre = "X", VidaMax = 10, Vida = 10, Estrategia = new IAAleatoria() }) == "Impredecible", "Strategy: la Aleatoria no se puede anticipar");
        Check(Estrategias.Todas.Length == 4, "Cuatro estrategias disponibles para el selector");

        // ---------- Reglas del juego ----------
        var escudo = new UnidadBatalla { VidaMax = 50, Vida = 50, Escudo = true };
        Check(escudo.RecibirDanio(20) == 10 && !escudo.Escudo && escudo.RecibirDanio(20) == 20, "El escudo absorbe la mitad de un solo impacto");

        var ganar = BatallaOrbital.Crear(semilla: 3);
        foreach (var e in ganar.Contexto.Enemigos) e.Vida = 1;
        for (int i = 0; i < 4 && !ganar.Terminada; i++) ganar.JugarTurnoAliado(ganar.Contexto.EnemigosVivos.First());   // el Médico repara: atacan Soldado, Francotirador y Comandante
        Check(ganar.Victoria && ganar.Medalla!.Nivel == "Oro", "Victoria al destruir a todos los enemigos, con medalla");

        var perder = BatallaOrbital.Crear(semilla: 3);
        perder.Contexto.NaveCarga.Vida = 0;
        Check(perder.Derrota && perder.Terminada, "Derrota si cae la Nave de Carga");
        Check(MedallaBatalla.Obtener(8).Nivel == "Plata" && MedallaBatalla.Obtener(12).Nivel == "Bronce", "Medalla según rondas");

        // ---------- Conexión con el Nivel 1 ----------
        var p = new Progreso { LaboratorioReparado = true };
        p.ClonarIngeniero("Jefa", "Menta");
        foreach (var receta in RecetaNave.Todas)
        {
            p.ClonarPiloto("Piloto " + receta.Nombre, "Azul", receta);
            var nave = new IngenieroJefe(new NaveBuilder()).Construir(receta);
            p.RegistrarNave(nave); p.AsignarPiloto(nave);
        }
        var conFlota = BatallaOrbital.Crear(p);
        Check(conFlota.Contexto.Aliados[0].Piloto == "Piloto Combate" && conFlota.Contexto.Aliados[3].Piloto == "Jefa", "La batalla usa los pilotos clonados en el nivel 1");
    }
}
