namespace Kepler7.Core;

//  NIVEL 2 ESCOLTA ORBITAL 
//  Template Method define CUÁNDO se decide; Strategy define QUÉ se decide.
public enum TipoAccion { Atacar, Escudo, Reparar }
public enum EventoEspacial { Ninguno, Meteoritos, Capsula, Tormenta }

public class UnidadBatalla
{
    public string Nombre { get; init; } = "";
    public string Rol { get; init; } = "";
    public string Piloto { get; init; } = "";
    public int VidaMax { get; init; }
    public int Vida { get; set; }
    public bool Escudo { get; set; }
    public bool Viva => Vida > 0;
    public bool Herida => Viva && Vida < VidaMax;

    // El escudo absorbe la mitad de un único impacto y después se apaga.
    public int RecibirDanio(int danio)
    {
        if (!Viva || danio <= 0) return 0;
        int real = Escudo ? danio / 2 : danio;
        Escudo = false;
        Vida = Math.Max(0, Vida - real);
        return real;
    }

    // Suma vida sin pasarse del máximo. Devuelve cuánto reparó de verdad.
    public int Reparar(int cantidad)
    {
        if (!Viva || cantidad <= 0) return 0;
        int antes = Vida;
        Vida = Math.Min(VidaMax, Vida + cantidad);
        return Vida - antes;
    }
}

// [PATRÓN STRATEGY] Las naves enemigas no deciden solas: guardan una IEstrategiaIA intercambiable y le preguntan qué hacer.
public sealed class Enemigo : UnidadBatalla
{
    public IEstrategiaIA Estrategia { get; set; } = new IAAgresiva();
    // La Nodriza se enfurece al quedar con la mitad de vida: cambia sola su IA y dispara dos veces.
    public bool PuedeEnfurecerse { get; init; }
    public bool Furiosa { get; set; }
}

// Resultado de una decisión de IA: qué hace, sobre quién y con qué intensidad.
public sealed record AccionIA(TipoAccion Tipo, UnidadBatalla? Objetivo, int Valor, string Descripcion);

// Estado de la batalla que recibe la estrategia en DecidirAccion.
public sealed class Contexto
{
    public List<UnidadBatalla> Aliados { get; } = new();
    public List<Enemigo> Enemigos { get; } = new();
    public UnidadBatalla NaveCarga { get; set; } = new();
    public int Ronda { get; set; } = 1;
    public Random Azar { get; init; } = new();
    // Factor de daño de los disparos mutantes: 1 = sin bloquear, 0.5 = bloqueo, 0.25 = bloqueo perfecto.
    public float Bloqueo { get; set; } = 1f;
    public IEnumerable<UnidadBatalla> AliadosVivos => Aliados.Where(a => a.Viva);
    public IEnumerable<Enemigo> EnemigosVivos => Enemigos.Where(e => e.Viva);
}

//  [PATRÓN STRATEGY] Inteligencia artificial intercambiable

public interface IEstrategiaIA
{
    string Nombre { get; }
    string Descripcion { get; }
    bool Predecible { get; }
    AccionIA DecidirAccion(Enemigo enemigo, Contexto contexto);
}

// [PATRÓN STRATEGY  Estrategia concreta] Ataca fuerte a la nave aliada con menos vida para rematarla.
public sealed class IAAgresiva : IEstrategiaIA
{
    public string Nombre => "Agresiva";
    public string Descripcion => "Remata a la nave aliada más dañada.";
    public bool Predecible => true;
    public const int Potencia = 34;

    public AccionIA DecidirAccion(Enemigo enemigo, Contexto contexto)
    {
        var objetivo = contexto.AliadosVivos.MinBy(a => a.Vida)!;
        return new(TipoAccion.Atacar, objetivo, Potencia, "ataca a " + objetivo.Nombre);
    }
}

// [PATRÓN STRATEGY  Estrategia concreta] Si está dañada se protege; si no, dispara suave a la primera nave.
public sealed class IADefensiva : IEstrategiaIA
{
    public string Nombre => "Defensiva";
    public string Descripcion => "Se escuda cuando está dañada y dispara poco.";
    public bool Predecible => true;
    public const int Potencia = 16;

    public AccionIA DecidirAccion(Enemigo enemigo, Contexto contexto)
    {
        if (enemigo.Vida <= enemigo.VidaMax / 2 && !enemigo.Escudo)
            return new(TipoAccion.Escudo, enemigo, 0, "levanta el escudo");
        var objetivo = contexto.AliadosVivos.First();
        return new(TipoAccion.Atacar, objetivo, Potencia, "dispara a " + objetivo.Nombre);
    }
}

// [PATRÓN STRATEGY  Estrategia concreta] Elige al azar objetivo, acción y potencia.
public sealed class IAAleatoria : IEstrategiaIA
{
    public string Nombre => "Aleatoria";
    public string Descripcion => "Impredecible: objetivo y potencia al azar.";
    public bool Predecible => false;

    public AccionIA DecidirAccion(Enemigo enemigo, Contexto contexto)
    {
        if (contexto.Azar.Next(4) == 0 && !enemigo.Escudo)
            return new(TipoAccion.Escudo, enemigo, 0, "levanta el escudo");
        var vivos = contexto.AliadosVivos.ToList();
        var objetivo = vivos[contexto.Azar.Next(vivos.Count)];
        return new(TipoAccion.Atacar, objetivo, contexto.Azar.Next(8, 37), "ataca a " + objetivo.Nombre);
    }
}

// [PATRÓN STRATEGY  Estrategia concreta] Calcula el golpe más útil: si puede destruir una nave, lo hace;si no, apunta a la Nave de Carga, cuya pérdida significa la derrota.
public sealed class IACalculadora : IEstrategiaIA
{
    public string Nombre => "Calculadora";
    public string Descripcion => "Busca destruir la Nave de Carga o rematar.";
    public bool Predecible => true;
    public const int Potencia = 48;

    public AccionIA DecidirAccion(Enemigo enemigo, Contexto contexto)
    {
        var rematable = contexto.AliadosVivos
            .Where(a => (a.Escudo ? Potencia / 2 : Potencia) >= a.Vida)
            .MinBy(a => a.Vida);
        var objetivo = rematable ?? (contexto.NaveCarga.Viva ? contexto.NaveCarga : contexto.AliadosVivos.MinBy(a => a.Vida)!);
        return new(TipoAccion.Atacar, objetivo, Potencia, "calcula y apunta a " + objetivo.Nombre);
    }
}

// [PATRÓN STRATEGY] Catálogo de las estrategias concretas, para el selector de la interfaz.
public static class Estrategias
{
    public static IEstrategiaIA[] Todas => new IEstrategiaIA[] { new IAAgresiva(), new IADefensiva(), new IAAleatoria(), new IACalculadora() };
    public static IEstrategiaIA Por(string nombre) => Todas.First(e => e.Nombre == nombre);
}

//  [PATRÓN TEMPLATE METHOD] Turno de unidad

// Todos los turnos siguen la misma estructura: inicio, decisión, acción y finalización. Cada tipo de nave redefine los pasos que le tocan, pero no puede cambiar el orden.
public abstract class TurnoUnidad
{
    protected readonly UnidadBatalla unidad;
    protected readonly Contexto contexto;
    private readonly List<string> historial;

    protected TurnoUnidad(UnidadBatalla unidad, Contexto contexto, List<string> historial)
    {
        this.unidad = unidad;
        this.contexto = contexto;
        this.historial = historial;
    }

    public UnidadBatalla Unidad => unidad;

    public UnidadBatalla? Objetivo { get; set; }

    public string UltimoResultado { get; protected set; } = "";

    public float Precision { get; set; } = 1f;

    protected int ConPrecision(int valor) => (int)Math.Round(valor * Precision);
    protected string Calidad => Precision >= 2 ? "¡crítico! " : Precision < 1 ? "débil: " : "";

    public void EjecutarTurno()
    {
        if (!unidad.Viva) return;
        Iniciar();
        Decidir();
        Actuar();
        Finalizar();
    }

    // [PATRÓN TEMPLATE METHOD  Pasos] Los "virtual" tienen un comportamiento por defecto que se puede redefinir; los "abstract" no tienen código y cada subclase está obligada a escribirlos.
    protected virtual void Iniciar() => Registrar("Inicio de turno");
    protected abstract void Decidir();
    protected abstract void Actuar();
    protected virtual void Finalizar()
    {
        if (Objetivo != null && !Objetivo.Viva) Registrar("Fin · ¡" + Objetivo.Nombre + " fue destruida!");
        else Registrar("Fin de turno");
    }

    // [PATRÓN TEMPLATE METHOD  Historial visible] Anota cada etapa en el historial que se ve en pantalla. 
    protected void Registrar(string paso) => historial.Add("[R" + contexto.Ronda + "] " + unidad.Rol + " · " + paso);
    protected UnidadBatalla EnemigoObjetivo() =>
        Objetivo is { Viva: true } o && contexto.Enemigos.Contains(o) ? o : contexto.EnemigosVivos.First();
}

// [PATRÓN TEMPLATE METHOD Subclase concreta] 
public sealed class TurnoSoldado : TurnoUnidad
{
    public const int Danio = 22;

    public TurnoSoldado(UnidadBatalla u, Contexto c, List<string> h) : base(u, c, h) { }

    protected override void Decidir()
    {
        Objetivo = EnemigoObjetivo();
        Registrar("Decide: disparar a " + Objetivo.Nombre);
    }

    protected override void Actuar()
    {
        int real = Objetivo!.RecibirDanio(ConPrecision(Danio));
        UltimoResultado = Calidad + real + " de daño a " + Objetivo.Nombre;
        Registrar("Actúa: " + UltimoResultado);
    }
}

// [PATRÓN TEMPLATE METHOD · Subclase concreta] 
public sealed class TurnoFrancotirador : TurnoUnidad
{
    public const int Danio = 38;
    public bool Cargado { get; private set; } = true;
    private bool disparaEsteTurno;

    public TurnoFrancotirador(UnidadBatalla u, Contexto c, List<string> h) : base(u, c, h) { }
    protected override void Iniciar()
    {
        disparaEsteTurno = Cargado;
        Registrar(Cargado ? "Inicio · arma cargada" : "Inicio · arma recargando");
    }

    protected override void Decidir()
    {
        if (!disparaEsteTurno)
        {
            Objetivo = null;
            Registrar("Decide: recargar el cañón de precisión");
            return;
        }
        Objetivo = EnemigoObjetivo();
        Registrar("Decide: apuntar a " + Objetivo.Nombre);
    }

    protected override void Actuar()
    {
        if (!disparaEsteTurno)
        {
            Cargado = true;
            UltimoResultado = "recargó el cañón";
            Registrar("Actúa: " + UltimoResultado);
            return;
        }
        int real = Objetivo!.RecibirDanio(ConPrecision(Danio));
        Cargado = false;
        UltimoResultado = "disparo de precisión: " + Calidad + real + " de daño a " + Objetivo.Nombre;
        Registrar("Actúa: " + UltimoResultado);
    }
}

// [PATRÓN TEMPLATE METHOD  Subclase concreta] Nave de Carga: repara a un aliado (puede repararse a sí misma).
public sealed class TurnoMedico : TurnoUnidad
{
    public const int Reparacion = 25;

    public TurnoMedico(UnidadBatalla u, Contexto c, List<string> h) : base(u, c, h) { }

    protected override void Decidir()
    {
        // Si el jugador eligió un aliado vivo, repara a ese, si no, elige al que tenga menor porcentaje de vida.
        Objetivo = Objetivo is { Viva: true } o && contexto.Aliados.Contains(o)
            ? o
            : contexto.AliadosVivos.MinBy(a => (float)a.Vida / a.VidaMax)!;
        Registrar("Decide: reparar a " + Objetivo.Nombre);
    }

    protected override void Actuar()
    {
        int real = Objetivo!.Reparar(ConPrecision(Reparacion));
        UltimoResultado = real > 0
            ? Calidad + "reparó " + real + " de casco a " + Objetivo.Nombre
            : Objetivo.Nombre + " ya estaba intacta";
        Registrar("Actúa: " + UltimoResultado);
    }

    protected override void Finalizar() => Registrar("Fin de turno");
}

// [PATRÓN TEMPLATE METHOD  Subclase concreta]
public sealed class TurnoComandante : TurnoUnidad
{
    public const int Danio = 12;
    public IEstrategiaIA? NuevaEstrategia { get; set; }

    public TurnoComandante(UnidadBatalla u, Contexto c, List<string> h) : base(u, c, h) { }

    protected override void Decidir()
    {
        Objetivo = EnemigoObjetivo();
        Registrar(NuevaEstrategia != null
            ? "Decide: hackear la IA de " + Objetivo.Nombre
            : "Decide: disparar el láser a " + Objetivo.Nombre);
    }

    protected override void Actuar()
    {
        if (NuevaEstrategia != null && Objetivo is Enemigo enemigo)
        {
            string anterior = enemigo.Estrategia.Nombre;
            enemigo.Estrategia = NuevaEstrategia;      // [PATRÓN STRATEGY  Cambio de estrategia en plena partida: hackeo]
            UltimoResultado = enemigo.Nombre + ": IA " + anterior + " → " + NuevaEstrategia.Nombre;
        }
        else
        {
            int real = Objetivo!.RecibirDanio(ConPrecision(Danio));
            UltimoResultado = Calidad + real + " de daño a " + Objetivo.Nombre;
        }
        NuevaEstrategia = null;   
        Registrar("Actúa: " + UltimoResultado);
    }
}

// [PATRÓN TEMPLATE METHOD · Subclase concreta] + [PATRÓN STRATEGY · Uso de la estrategia]. Turno de las naves mutantes: el paso Decidir le pregunta a la estrategia qué hacer.
// Template Method define cuándo se decide; Strategy define qué se decide.
public sealed class TurnoEnemigo : TurnoUnidad
{
    private AccionIA? accion;
    public AccionIA? UltimaAccion => accion;

    public TurnoEnemigo(Enemigo e, Contexto c, List<string> h) : base(e, c, h) { }

    protected override void Iniciar() => Registrar("Inicio de turno · " + unidad.Nombre);

    protected override void Decidir()
    {
        var enemigo = (Enemigo)unidad;
        accion = enemigo.Estrategia.DecidirAccion(enemigo, contexto);   // [PATRÓN STRATEGY] acá se cruzan los dos patrones
        Objetivo = accion.Objetivo;
        Registrar("Decide (IA " + enemigo.Estrategia.Nombre + "): " + accion.Descripcion);
    }

    protected override void Actuar()
    {
        if (accion!.Tipo == TipoAccion.Escudo)
        {
            unidad.Escudo = true;
            UltimoResultado = unidad.Nombre + " levantó el escudo";
        }
        else
        {
            int real = accion.Objetivo!.RecibirDanio((int)Math.Round(accion.Valor * contexto.Bloqueo));
            UltimoResultado = unidad.Nombre + " hizo " + real + " de daño a " + accion.Objetivo.Nombre + (contexto.Bloqueo < 1 ? " (bloqueado)" : "");
        }
        Registrar("Actúa: " + UltimoResultado);
    }
}
//  Coordinador de la batalla (reglas de rondas, victoria y derrota)
// Medalla según cuántas rondas tardó el jugador en ganar.
public sealed record MedallaBatalla(int Rondas, string Nivel)
{
    public static MedallaBatalla Obtener(int rondas) =>
        new(rondas, rondas <= 6 ? "Oro" : rondas <= 8 ? "Plata" : "Bronce");
}

// Arma la batalla y maneja de quién es el turno, las rondas y el final de la partida.
public sealed class BatallaOrbital
{
    public Contexto Contexto { get; }
    public List<string> Historial { get; } = new();
    public List<TurnoUnidad> TurnosAliados { get; } = new();
    public List<TurnoEnemigo> TurnosEnemigos { get; } = new();
    public bool FaseEnemiga { get; private set; }
    public int CambiosDeEstrategia { get; private set; }
    public string UltimoResultado { get; private set; } = "";
    private int indiceAliado;
   
    private readonly Queue<TurnoEnemigo> colaEnemiga = new();

    // Si pasan más rondas que éstas sin destruir a los mutantes, llegan refuerzos y se pierde.
    public const int RondasMaximas = 10;
    public const int DanioMeteorito = 18, MeteoritosPorLluvia = 3, ReparacionCapsula = 30;

    // ---------- Eventos sorpresa ----------
    public bool EventosActivos { get; init; } = true;
    public EventoEspacial EventoRonda { get; private set; }
    public int MeteoritosPendientes { get; private set; }
    public UnidadBatalla? MeteoritoApunta { get; private set; }
    public bool CapsulaDisponible { get; private set; }
    private EventoEspacial ultimoEvento;
    private IEstrategiaIA? iaTormenta;
    private readonly Dictionary<Enemigo, IEstrategiaIA> antesDeLaTormenta = new();

    public int Ronda => Contexto.Ronda;
    public bool Victoria => !Contexto.EnemigosVivos.Any();
    public bool Derrota => !Contexto.NaveCarga.Viva || !Contexto.AliadosVivos.Any() || Contexto.Ronda > RondasMaximas;
    public bool Terminada => Victoria || Derrota;
    // Mientras caen meteoritos, tu flota todavía no puede jugar.
    public TurnoUnidad? TurnoActual => Terminada || FaseEnemiga || MeteoritosPendientes > 0 ? null : TurnosAliados[indiceAliado];
    public MedallaBatalla? Medalla => Victoria ? MedallaBatalla.Obtener(Ronda) : null;
    // El próximo mutante que va a disparar (la pantalla muestra su carga antes del disparo).
    public Enemigo? ProximoEnemigo => FaseEnemiga && !Terminada
        ? colaEnemiga.FirstOrDefault(t => t.Unidad.Viva)?.Unidad as Enemigo
        : null;

    private BatallaOrbital(Contexto contexto) { Contexto = contexto; }

    // Arma la batalla usando los pilotos de la flota del nivel 1 cuando existen.
    public static BatallaOrbital Crear(Progreso? progreso = null, int? semilla = null, bool eventos = true)
    {
        string PilotoDe(string tipo) =>
            progreso?.Flota.FirstOrDefault(n => n.Tipo == tipo && n.Piloto != null)?.Piloto!.Nombre ?? "Piloto " + tipo;

        var c = new Contexto { Azar = semilla.HasValue ? new Random(semilla.Value) : new Random() };

        // Tu flota
        var soldado = new UnidadBatalla { Nombre = "Nave de Combate", Rol = "Soldado", Piloto = PilotoDe("Combate"), VidaMax = 100, Vida = 100 };
        var francotirador = new UnidadBatalla { Nombre = "Exploradora", Rol = "Francotirador", Piloto = PilotoDe("Exploradora"), VidaMax = 70, Vida = 70 };
        var medico = new UnidadBatalla { Nombre = "Nave de Carga", Rol = "Médico", Piloto = PilotoDe("Carga"), VidaMax = 90, Vida = 90 };
        var comandante = new UnidadBatalla { Nombre = "Nave Insignia", Rol = "Comandante", Piloto = progreso?.Ingeniero?.Nombre ?? "Michi-07", VidaMax = 80, Vida = 80 };
        c.Aliados.AddRange(new[] { soldado, francotirador, medico, comandante });
        c.NaveCarga = medico;

        // Naves mutantes, cada una con su IA inicial
        c.Enemigos.Add(new Enemigo { Nombre = "Mutante Alfa", Rol = "Enemigo", VidaMax = 120, Vida = 120, Estrategia = new IAAgresiva() });
        c.Enemigos.Add(new Enemigo { Nombre = "Mutante Beta", Rol = "Enemigo", VidaMax = 120, Vida = 120, Estrategia = new IADefensiva() });
        c.Enemigos.Add(new Enemigo { Nombre = "Nodriza", Rol = "Enemigo", VidaMax = 160, Vida = 160, Estrategia = new IACalculadora(), PuedeEnfurecerse = true });

        // Un turno (Template Method) para cada nave
        var b = new BatallaOrbital(c) { EventosActivos = eventos };
        b.TurnosAliados.Add(new TurnoSoldado(soldado, c, b.Historial));
        b.TurnosAliados.Add(new TurnoFrancotirador(francotirador, c, b.Historial));
        b.TurnosAliados.Add(new TurnoMedico(medico, c, b.Historial));
        b.TurnosAliados.Add(new TurnoComandante(comandante, c, b.Historial));
        foreach (var e in c.Enemigos) b.TurnosEnemigos.Add(new TurnoEnemigo(e, c, b.Historial));

        b.Historial.Add("[R1] Comienza la escolta orbital. ¡Protegé la Nave de Carga!");
        return b;
    }

    public IEnumerable<UnidadBatalla> ObjetivosValidos() =>
        TurnoActual is TurnoMedico ? Contexto.AliadosVivos : Contexto.EnemigosVivos;

    // Juega el turno de la nave aliada activa con lo que eligió el jugador.
    public void JugarTurnoAliado(UnidadBatalla? objetivo, IEstrategiaIA? hackeo = null, float precision = 1f)
    {
        var turno = TurnoActual ?? throw new InvalidOperationException("No es el turno de la flota.");
        turno.Objetivo = objetivo;
        turno.Precision = precision;
        if (turno is TurnoComandante comandante)
        {
            comandante.NuevaEstrategia = hackeo;
            if (hackeo != null && objetivo is Enemigo e && e.Estrategia.Nombre != hackeo.Nombre) CambiosDeEstrategia++;
        }
        turno.EjecutarTurno();                       // [PATRÓN TEMPLATE METHOD]
        UltimoResultado = turno.Unidad.Nombre + ": " + turno.UltimoResultado;
        RevisarFuria();
        if (Terminada) return;
        indiceAliado++;
        SaltarAliadosCaidos();
        if (indiceAliado >= TurnosAliados.Count) PrepararFaseEnemiga();
    }
    private void PrepararFaseEnemiga()
    {
        FaseEnemiga = true;
        CapsulaDisponible = false;                  
        colaEnemiga.Clear();
        foreach (var t in TurnosEnemigos)
        {
            if (!t.Unidad.Viva) continue;
            colaEnemiga.Enqueue(t);
            if (((Enemigo)t.Unidad).Furiosa) colaEnemiga.Enqueue(t);   
        }
    }

    public bool EjecutarSiguienteEnemigo(float bloqueo = 1f)
    {
        if (!FaseEnemiga || Terminada) return false;
        while (colaEnemiga.Count > 0 && !colaEnemiga.Peek().Unidad.Viva) colaEnemiga.Dequeue();
        if (colaEnemiga.Count > 0)
        {
            var turno = colaEnemiga.Dequeue();
            Contexto.Bloqueo = bloqueo;              // lo que logró el jugador al bloquear
            turno.EjecutarTurno();                   // [PATRÓN TEMPLATE METHOD] 
            Contexto.Bloqueo = 1f;
            UltimoResultado = turno.UltimoResultado;
            if (Terminada) return false;
        }
        if (colaEnemiga.Any(t => t.Unidad.Viva)) return true;
        NuevaRonda();
        return false;
    }
    public string Intencion(Enemigo enemigo)
    {
        if (!enemigo.Viva) return "Destruida";
        if (Terminada) return "—";
        if (!enemigo.Estrategia.Predecible) return "Impredecible";
        return enemigo.Estrategia.DecidirAccion(enemigo, Contexto).Descripcion;
    }

    // [PATRÓN STRATEGY · Cambio de estrategia en plena partida: furia]. La Nodriza herida se enfurece: el juego le cambia la IA a Agresiva.
    private void RevisarFuria()
    {
        foreach (var e in Contexto.EnemigosVivos)
        {
            if (!e.PuedeEnfurecerse || e.Furiosa || e.Vida > e.VidaMax / 2) continue;
            e.Furiosa = true;
            string anterior = e.Estrategia.Nombre;
            e.Estrategia = new IAAgresiva();
            Historial.Add("[R" + Ronda + "] ¡" + e.Nombre + " se enfurece! IA " + anterior + " → Agresiva · dispara dos veces");
        }
    }

    // ---------- Eventos sorpresa ----------
    private void SortearEvento()
    {
        EventoRonda = EventoEspacial.Ninguno;
        if (!EventosActivos || Contexto.Ronda < 2 || Contexto.Azar.Next(100) >= 40) return;
        var opciones = new[] { EventoEspacial.Meteoritos, EventoEspacial.Capsula, EventoEspacial.Tormenta }.Where(e => e != ultimoEvento).ToArray();
        IniciarEvento(opciones[Contexto.Azar.Next(opciones.Length)]);
    }

    // Público para poder probar cada evento en las verificaciones.
    public void IniciarEvento(EventoEspacial evento)
    {
        EventoRonda = ultimoEvento = evento;
        switch (evento)
        {
            case EventoEspacial.Meteoritos:
                MeteoritosPendientes = MeteoritosPorLluvia;
                MeteoritoApunta = AliadoAlAzar();
                Historial.Add("[R" + Ronda + "] Evento · ¡Lluvia de meteoritos!");
                break;
            case EventoEspacial.Capsula:
                CapsulaDisponible = true;
                Historial.Add("[R" + Ronda + "] Evento · Una cápsula de reparación flota a la deriva");
                break;
            case EventoEspacial.Tormenta:
                // [PATRÓN STRATEGY · Cambio de estrategia en plena partida: tormenta]
                // Todas las IA se reemplazan por la Aleatoria durante una ronda.
                iaTormenta = new IAAleatoria();
                antesDeLaTormenta.Clear();
                foreach (var e in Contexto.EnemigosVivos) { antesDeLaTormenta[e] = e.Estrategia; e.Estrategia = iaTormenta; }
                Historial.Add("[R" + Ronda + "] Evento · Tormenta de iones: todas las IA pasan a Aleatoria");
                break;
        }
    }

    private UnidadBatalla AliadoAlAzar()
    {
        var vivos = Contexto.AliadosVivos.ToList();
        return vivos[Contexto.Azar.Next(vivos.Count)];
    }

    // Cae el meteorito sobre la nave apuntada; 
    public void ImpactarMeteorito(float bloqueo = 1f)
    {
        if (MeteoritosPendientes <= 0 || MeteoritoApunta == null) return;
        int real = MeteoritoApunta.RecibirDanio((int)Math.Round(DanioMeteorito * bloqueo));
        UltimoResultado = "Meteorito: " + real + " de daño a " + MeteoritoApunta.Nombre + (bloqueo < 1 ? " (bloqueado)" : "");
        Historial.Add("[R" + Ronda + "] Evento · " + UltimoResultado);
        MeteoritosPendientes--;
        if (Terminada) MeteoritosPendientes = 0;
        MeteoritoApunta = MeteoritosPendientes > 0 ? AliadoAlAzar() : null;
        if (MeteoritosPendientes == 0) SaltarAliadosCaidos();
    }

    // Disparar a la cápsula repara a la nave más dañada.
    public UnidadBatalla? RecogerCapsula()
    {
        if (!CapsulaDisponible || Terminada) return null;
        CapsulaDisponible = false;
        var herida = Contexto.AliadosVivos.MinBy(a => (float)a.Vida / a.VidaMax)!;
        int real = herida.Reparar(ReparacionCapsula);
        UltimoResultado = "Cápsula: +" + real + " a " + herida.Nombre;
        Historial.Add("[R" + Ronda + "] Evento · " + UltimoResultado);
        return herida;
    }

    // Al terminar la ronda, cada mutante recupera la IA que tenía (salvo que la hayan cambiado durante la tormenta).
    private void TerminarTormenta()
    {
        if (iaTormenta == null) return;
        foreach (var (e, ia) in antesDeLaTormenta)
            if (ReferenceEquals(e.Estrategia, iaTormenta)) e.Estrategia = ia;
        antesDeLaTormenta.Clear();
        iaTormenta = null;
        Historial.Add("[R" + Ronda + "] La tormenta de iones se disipa");
    }

    private void NuevaRonda()
    {
        FaseEnemiga = false;
        TerminarTormenta();
        Contexto.Ronda++;
        indiceAliado = 0;
        SaltarAliadosCaidos();
        Historial.Add("[R" + Contexto.Ronda + "] ── Nueva ronda ──");
        if (!Terminada) SortearEvento();
    }

    // Si una nave aliada fue destruida, su turno se saltea.
    private void SaltarAliadosCaidos()
    {
        while (indiceAliado < TurnosAliados.Count && !TurnosAliados[indiceAliado].Unidad.Viva) indiceAliado++;
    }
}
