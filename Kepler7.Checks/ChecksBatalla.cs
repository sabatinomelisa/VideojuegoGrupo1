using Kepler7.Core;

// Verificaciones del Nivel 2  Escolta orbital (Template Method y Strategy).
// Se llaman desde Program.cs con: ChecksBatalla.Ejecutar(Check);
public static class ChecksBatalla
{
    public static void Ejecutar(Action<bool, string> Check)
    {
        // Template Method 
        var batalla = BatallaOrbital.Crear(semilla: 7, eventos: false);
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

        // Strategy 
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

        // Reglas del juego 
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

        //Puntería, bloqueo y límite de rondas
        var punteria = BatallaOrbital.Crear(semilla: 5);
        var alfa = punteria.Contexto.Enemigos[0];
        punteria.JugarTurnoAliado(alfa, null, 2f);
        Check(alfa.Vida == alfa.VidaMax - TurnoSoldado.Danio * 2, "Puntería: un crítico duplica el daño");
        punteria.JugarTurnoAliado(alfa, null, .5f);
        Check(alfa.Vida == alfa.VidaMax - TurnoSoldado.Danio * 2 - (int)Math.Round(TurnoFrancotirador.Danio * .5), "Puntería: un tiro débil hace la mitad");
        punteria.JugarTurnoAliado(null); punteria.JugarTurnoAliado(null);
        Check(punteria.ProximoEnemigo == alfa, "La pantalla puede anticipar qué mutante dispara");

        var c3 = BatallaOrbital.Crear(semilla: 5).Contexto;
        var turnoNodriza = new TurnoEnemigo(c3.Enemigos[2], c3, new List<string>());
        c3.Bloqueo = .5f;
        turnoNodriza.EjecutarTurno();
        Check(c3.NaveCarga.Vida == c3.NaveCarga.VidaMax - IACalculadora.Potencia / 2, "Bloqueo: el disparo mutante bloqueado hace la mitad");

        var limite = BatallaOrbital.Crear(semilla: 5);
        limite.Contexto.Ronda = BatallaOrbital.RondasMaximas + 1;
        Check(limite.Derrota, "Derrota si se superan las rondas máximas (llegan refuerzos)");

        //Furia de la Nodriza 
        var furia = BatallaOrbital.Crear(semilla: 9, eventos: false);
        var nod = furia.Contexto.Enemigos[2];
        nod.Vida = nod.VidaMax / 2 + 5;
        furia.JugarTurnoAliado(nod);                                   // el Soldado la baja de la mitad
        Check(nod.Furiosa && nod.Estrategia is IAAgresiva, "Furia: la Nodriza herida cambia sola su IA a Agresiva");
        Check(furia.Historial.Any(h => h.Contains("se enfurece")), "Furia: queda registrada en el historial");
        for (int i = 0; i < 3; i++) furia.JugarTurnoAliado(furia.Contexto.Enemigos[0]);
        int disparos = 0;
        while (furia.FaseEnemiga && disparos < 10) { disparos++; furia.EjecutarSiguienteEnemigo(); }
        Check(disparos == 4, "Furia: la Nodriza furiosa dispara dos veces por ronda");

        //Eventos sorpresa
        var lluvia = BatallaOrbital.Crear(semilla: 4, eventos: false);
        lluvia.IniciarEvento(EventoEspacial.Meteoritos);
        Check(lluvia.MeteoritosPendientes == BatallaOrbital.MeteoritosPorLluvia && lluvia.TurnoActual == null, "Meteoritos: tu flota espera hasta que terminen de caer");
        var golpeada = lluvia.MeteoritoApunta!; int vidaAntes = golpeada.Vida;
        lluvia.ImpactarMeteorito(.5f);
        Check(golpeada.Vida == vidaAntes - BatallaOrbital.DanioMeteorito / 2, "Meteoritos: el bloqueo reduce el impacto");
        lluvia.ImpactarMeteorito(); lluvia.ImpactarMeteorito();
        Check(lluvia.MeteoritosPendientes == 0 && lluvia.TurnoActual is TurnoSoldado, "Meteoritos: después de la lluvia vuelve tu turno");

        var capsula = BatallaOrbital.Crear(semilla: 4, eventos: false);
        capsula.Contexto.NaveCarga.Vida = 40;
        capsula.IniciarEvento(EventoEspacial.Capsula);
        Check(capsula.RecogerCapsula() == capsula.Contexto.NaveCarga && capsula.Contexto.NaveCarga.Vida == 40 + BatallaOrbital.ReparacionCapsula, "Cápsula: repara a la nave más dañada");
        Check(capsula.RecogerCapsula() == null, "Cápsula: se puede agarrar una sola vez");

        var tormenta = BatallaOrbital.Crear(semilla: 4, eventos: false);
        tormenta.IniciarEvento(EventoEspacial.Tormenta);
        Check(tormenta.Contexto.Enemigos.All(e => e.Estrategia is IAAleatoria), "Tormenta: todas las IA pasan a Aleatoria (Strategy)");
        for (int i = 0; i < 4; i++) tormenta.JugarTurnoAliado(null);
        while (tormenta.EjecutarSiguienteEnemigo()) { }
        Check(tormenta.Contexto.Enemigos[0].Estrategia is IAAgresiva && tormenta.Contexto.Enemigos[2].Estrategia is IACalculadora, "Tormenta: al terminar la ronda vuelven las IA originales");

        //Insignias y guardado del nivel 2
        var prog = new Progreso();
        int xp0 = prog.XP;
        Check(prog.RegistrarHackeo() && !prog.RegistrarHackeo() && prog.XP == xp0 + 100, "Insignia Strategy otorga XP una sola vez");
        Check(prog.RegistrarVictoriaNivel2(new MedallaBatalla(8, "Plata")) && !prog.RegistrarVictoriaNivel2(new MedallaBatalla(6, "Oro")) && prog.XP == xp0 + 300 && prog.MedallaBatalla!.Nivel == "Oro", "Insignia Template Method otorga XP una vez y guarda la mejor medalla");
        Check(Logros.Obtener(prog).Count(l => l.Obtenida) == 2 && Logros.Obtener(prog).Any(l => l.Patron == "Strategy") && Logros.Obtener(prog).Any(l => l.Patron == "Template Method"), "El panel de logros incluye las insignias del nivel 2");
        var ruta = Path.Combine(Path.GetTempPath(), "kepler7-nivel2-" + Guid.NewGuid().ToString("N") + ".json");
        Guardado.Guardar(prog, ruta);
        var cargado = Guardado.Cargar(ruta);
        File.Delete(ruta);
        Check(cargado.StrategyDominado && cargado.TemplateMethodDominado && cargado.MedallaBatalla?.Nivel == "Oro", "El guardado conserva las insignias y la medalla del nivel 2");

        //Conexión con el Nivel 1
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
