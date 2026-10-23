namespace Kepler7.Core;

// Tipos de acción que puede hacer una nave en su turno.
public enum TipoAccion { Atacar, Escudo, Reparar }

// Cualquier nave que participa de la batalla (aliada o enemiga).
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

// Las naves enemigas son unidades que además tienen una estrategia de IA.
// No deciden solas: le preguntan a su estrategia qué hacer (patrón Strategy).
public sealed class Enemigo : UnidadBatalla
{
    public IEstrategiaIA Estrategia { get; set; } = new IAAgresiva();
}

// Resultado de una decisión de IA: qué hace, sobre quién y con qué intensidad.
public sealed record AccionIA(TipoAccion Tipo, UnidadBatalla? Objetivo, int Valor, string Descripcion);

// Información que la estrategia necesita para decidir:
// quiénes son los aliados, quiénes los enemigos, qué ronda es, etc.
public sealed class Contexto
{
    public List<UnidadBatalla> Aliados { get; } = new();
    public List<Enemigo> Enemigos { get; } = new();
    public UnidadBatalla NaveCarga { get; set; } = new();
    public int Ronda { get; set; } = 1;
    public Random Azar { get; init; } = new();
    public IEnumerable<UnidadBatalla> AliadosVivos => Aliados.Where(a => a.Viva);
    public IEnumerable<Enemigo> EnemigosVivos => Enemigos.Where(e => e.Viva);
}

// ---------------------------------------------------------------------
//  STRATEGY · Inteligencia artificial intercambiable
// ---------------------------------------------------------------------

// El "contrato" que toda IA tiene que cumplir.
public interface IEstrategiaIA
{
    string Nombre { get; }
    string Descripcion { get; }
    // Indica si el radar puede anticipar la jugada (la aleatoria no).
    bool Predecible { get; }
    AccionIA DecidirAccion(Enemigo enemigo, Contexto contexto);
}

// Ataca fuerte a la nave aliada con menos vida para rematarla.
public sealed class IAAgresiva : IEstrategiaIA
{
    public string Nombre => "Agresiva";
    public string Descripcion => "Remata a la nave aliada más dañada.";
    public bool Predecible => true;
    public const int Potencia = 30;

    public AccionIA DecidirAccion(Enemigo enemigo, Contexto contexto)
    {
        var objetivo = contexto.AliadosVivos.MinBy(a => a.Vida)!;
        return new(TipoAccion.Atacar, objetivo, Potencia, "ataca a " + objetivo.Nombre);
    }
}

// Si está dañada se protege; si no, dispara suave a la primera nave de la formación.
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

// Elige al azar objetivo, acción y potencia.
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

// Calcula el golpe más útil: si puede destruir una nave, lo hace;
// si no, apunta a la Nave de Carga, cuya pérdida significa la derrota.
public sealed class IACalculadora : IEstrategiaIA
{
    public string Nombre => "Calculadora";
    public string Descripcion => "Busca destruir la Nave de Carga o rematar.";
    public bool Predecible => true;
    public const int Potencia = 26;

    public AccionIA DecidirAccion(Enemigo enemigo, Contexto contexto)
    {
        var rematable = contexto.AliadosVivos
            .Where(a => (a.Escudo ? Potencia / 2 : Potencia) >= a.Vida)
            .MinBy(a => a.Vida);
        var objetivo = rematable ?? (contexto.NaveCarga.Viva ? contexto.NaveCarga : contexto.AliadosVivos.MinBy(a => a.Vida)!);
        return new(TipoAccion.Atacar, objetivo, Potencia, "calcula y apunta a " + objetivo.Nombre);
    }
}

// Lista de todas las IA disponibles, para el selector de la interfaz.
public static class Estrategias
{
    public static IEstrategiaIA[] Todas => new IEstrategiaIA[] { new IAAgresiva(), new IADefensiva(), new IAAleatoria(), new IACalculadora() };
    public static IEstrategiaIA Por(string nombre) => Todas.First(e => e.Nombre == nombre);
}

// ---------------------------------------------------------------------
//  TEMPLATE METHOD · Turno de unidad
// ---------------------------------------------------------------------

// Todos los turnos siguen la misma estructura: inicio, decisión, acción y finalización.
// Cada tipo de nave redefine los pasos que le tocan, pero NO puede cambiar el orden.
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
    // Objetivo elegido por el jugador desde la interfaz (sólo turnos aliados).
    public UnidadBatalla? Objetivo { get; set; }
    // Resumen del último paso Actuar, para mostrar en pantalla.
    public string UltimoResultado { get; protected set; } = "";

    // MÉTODO PLANTILLA. No es virtual: ninguna subclase puede cambiar el orden.
    public void EjecutarTurno()
    {
        if (!unidad.Viva) return;
        Iniciar();
        Decidir();
        Actuar();
        Finalizar();
    }

    // Pasos del turno. Los "virtual" tienen un comportamiento por defecto que se puede
    // redefinir; los "abstract" no tienen código y cada subclase está obligada a escribirlos.
    protected virtual void Iniciar() => Registrar("Inicio de turno");
    protected abstract void Decidir();
    protected abstract void Actuar();
    protected virtual void Finalizar()
    {
        if (Objetivo != null && !Objetivo.Viva) Registrar("Fin · ¡" + Objetivo.Nombre + " fue destruida!");
        else Registrar("Fin de turno");
    }

    // Anota cada etapa en el historial que se ve en pantalla. Ej: "[R2] Soldado · Actúa: 22 de daño..."
    protected void Registrar(string paso) => historial.Add("[R" + contexto.Ronda + "] " + unidad.Rol + " · " + paso);

    // Si el objetivo elegido ya no sirve (fue destruido o no es enemigo), toma el primer enemigo vivo.
    protected UnidadBatalla EnemigoObjetivo() =>
        Objetivo is { Viva: true } o && contexto.Enemigos.Contains(o) ? o : contexto.EnemigosVivos.First();
}

// Nave de Combate: disparo estándar al enemigo elegido.
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
        int real = Objetivo!.RecibirDanio(Danio);
        UltimoResultado = real + " de daño a " + Objetivo.Nombre;
        Registrar("Actúa: " + UltimoResultado);
    }
}

// Exploradora: disparo de precisión muy fuerte, pero necesita recargar un turno.
public sealed class TurnoFrancotirador : TurnoUnidad
{
    public const int Danio = 38;
    public bool Cargado { get; private set; } = true;
    private bool disparaEsteTurno;

    public TurnoFrancotirador(UnidadBatalla u, Contexto c, List<string> h) : base(u, c, h) { }

    // Redefine Iniciar: revisa si el arma está cargada.
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
        int real = Objetivo!.RecibirDanio(Danio);
        Cargado = false;
        UltimoResultado = "disparo de precisión: " + real + " de daño a " + Objetivo.Nombre;
        Registrar("Actúa: " + UltimoResultado);
    }
}

// Nave de Carga: repara a un aliado (puede repararse a sí misma).
public sealed class TurnoMedico : TurnoUnidad
{
    public const int Reparacion = 25;

    public TurnoMedico(UnidadBatalla u, Contexto c, List<string> h) : base(u, c, h) { }

    protected override void Decidir()
    {
        // Si el jugador eligió un aliado vivo, repara a ese.
        // Si no, elige al que tenga menor porcentaje de vida.
        Objetivo = Objetivo is { Viva: true } o && contexto.Aliados.Contains(o)
            ? o
            : contexto.AliadosVivos.MinBy(a => (float)a.Vida / a.VidaMax)!;
        Registrar("Decide: reparar a " + Objetivo.Nombre);
    }

    protected override void Actuar()
    {
        int real = Objetivo!.Reparar(Reparacion);
        UltimoResultado = real > 0
            ? "reparó " + real + " de casco a " + Objetivo.Nombre
            : Objetivo.Nombre + " ya estaba intacta";
        Registrar("Actúa: " + UltimoResultado);
    }

    // Redefine Finalizar: un médico nunca destruye a nadie, así que siempre cierra igual.
    protected override void Finalizar() => Registrar("Fin de turno");
}

// Nave Insignia: hackea la IA de un enemigo (le cambia la estrategia)
// o, si no se elige estrategia, dispara un láser liviano.
public sealed class TurnoComandante : TurnoUnidad
{
    public const int Danio = 12;
    // La IA que el jugador eligió en el desplegable "Hackear IA" (o null si no eligió).
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
            enemigo.Estrategia = NuevaEstrategia;      // <- cambio de Strategy en plena partida
            UltimoResultado = enemigo.Nombre + ": IA " + anterior + " → " + NuevaEstrategia.Nombre;
        }
        else
        {
            int real = Objetivo!.RecibirDanio(Danio);
            UltimoResultado = real + " de daño a " + Objetivo.Nombre;
        }
        NuevaEstrategia = null;   // se usa una sola vez; el próximo turno hay que volver a elegir
        Registrar("Actúa: " + UltimoResultado);
    }
}

// Turno de las naves mutantes: el paso Decidir le pregunta a la estrategia qué hacer.
// Template Method define CUÁNDO se decide; Strategy define QUÉ se decide.
public sealed class TurnoEnemigo : TurnoUnidad
{
    private AccionIA? accion;
    public AccionIA? UltimaAccion => accion;

    public TurnoEnemigo(Enemigo e, Contexto c, List<string> h) : base(e, c, h) { }

    protected override void Iniciar() => Registrar("Inicio de turno · " + unidad.Nombre);

    protected override void Decidir()
    {
        var enemigo = (Enemigo)unidad;
        accion = enemigo.Estrategia.DecidirAccion(enemigo, contexto);   // <- acá se cruzan los dos patrones
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
            int real = accion.Objetivo!.RecibirDanio(accion.Valor);
            UltimoResultado = unidad.Nombre + " hizo " + real + " de daño a " + accion.Objetivo.Nombre;
        }
        Registrar("Actúa: " + UltimoResultado);
    }
}

// ---------------------------------------------------------------------
//  Coordinador de la batalla (reglas de rondas, victoria y derrota)
// ---------------------------------------------------------------------

// Medalla según cuántas rondas tardó el jugador en ganar.
public sealed record MedallaBatalla(int Rondas, string Nivel)
{
    public static MedallaBatalla Obtener(int rondas) =>
        new(rondas, rondas <= 7 ? "Oro" : rondas <= 9 ? "Plata" : "Bronce");
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
    private int indiceAliado, indiceEnemigo;

    public int Ronda => Contexto.Ronda;
    public bool Victoria => !Contexto.EnemigosVivos.Any();
    public bool Derrota => !Contexto.NaveCarga.Viva || !Contexto.AliadosVivos.Any();
    public bool Terminada => Victoria || Derrota;
    public TurnoUnidad? TurnoActual => Terminada || FaseEnemiga ? null : TurnosAliados[indiceAliado];
    public MedallaBatalla? Medalla => Victoria ? MedallaBatalla.Obtener(Ronda) : null;

    private BatallaOrbital(Contexto contexto) { Contexto = contexto; }

    // Arma la batalla usando los pilotos de la flota del nivel 1 cuando existen.
    public static BatallaOrbital Crear(Progreso? progreso = null, int? semilla = null)
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
        c.Enemigos.Add(new Enemigo { Nombre = "Mutante Alfa", Rol = "Enemigo", VidaMax = 90, Vida = 90, Estrategia = new IAAgresiva() });
        c.Enemigos.Add(new Enemigo { Nombre = "Mutante Beta", Rol = "Enemigo", VidaMax = 90, Vida = 90, Estrategia = new IADefensiva() });
        c.Enemigos.Add(new Enemigo { Nombre = "Nodriza", Rol = "Enemigo", VidaMax = 140, Vida = 140, Estrategia = new IACalculadora() });

        // Un turno (Template Method) para cada nave
        var b = new BatallaOrbital(c);
        b.TurnosAliados.Add(new TurnoSoldado(soldado, c, b.Historial));
        b.TurnosAliados.Add(new TurnoFrancotirador(francotirador, c, b.Historial));
        b.TurnosAliados.Add(new TurnoMedico(medico, c, b.Historial));
        b.TurnosAliados.Add(new TurnoComandante(comandante, c, b.Historial));
        foreach (var e in c.Enemigos) b.TurnosEnemigos.Add(new TurnoEnemigo(e, c, b.Historial));

        b.Historial.Add("[R1] Comienza la escolta orbital. ¡Protegé la Nave de Carga!");
        return b;
    }

    // Objetivos que la pantalla puede ofrecer para la nave que juega ahora:
    // el Médico elige entre aliados; los demás, entre enemigos.
    public IEnumerable<UnidadBatalla> ObjetivosValidos() =>
        TurnoActual is TurnoMedico ? Contexto.AliadosVivos : Contexto.EnemigosVivos;

    // Juega el turno de la nave aliada activa con lo que eligió el jugador.
    public void JugarTurnoAliado(UnidadBatalla? objetivo, IEstrategiaIA? hackeo = null)
    {
        var turno = TurnoActual ?? throw new InvalidOperationException("No es el turno de la flota.");
        turno.Objetivo = objetivo;
        if (turno is TurnoComandante comandante)
        {
            comandante.NuevaEstrategia = hackeo;
            if (hackeo != null && objetivo is Enemigo e && e.Estrategia.Nombre != hackeo.Nombre) CambiosDeEstrategia++;
        }
        turno.EjecutarTurno();                       // <- el método plantilla
        UltimoResultado = turno.Unidad.Nombre + ": " + turno.UltimoResultado;
        if (Terminada) return;
        indiceAliado++;
        SaltarAliadosCaidos();
        if (indiceAliado >= TurnosAliados.Count) { FaseEnemiga = true; indiceEnemigo = 0; }
    }

    // La pantalla lo llama con un Timer para animar a cada enemigo por separado.
    // Devuelve false cuando ya no quedan enemigos por actuar en la ronda.
    public bool EjecutarSiguienteEnemigo()
    {
        if (!FaseEnemiga || Terminada) return false;
        while (indiceEnemigo < TurnosEnemigos.Count && !TurnosEnemigos[indiceEnemigo].Unidad.Viva) indiceEnemigo++;
        if (indiceEnemigo < TurnosEnemigos.Count)
        {
            var turno = TurnosEnemigos[indiceEnemigo++];
            turno.EjecutarTurno();                   // <- el mismo método plantilla
            UltimoResultado = turno.UltimoResultado;
            if (Terminada) return false;
        }
        if (indiceEnemigo < TurnosEnemigos.Count && TurnosEnemigos.Skip(indiceEnemigo).Any(t => t.Unidad.Viva)) return true;
        NuevaRonda();
        return false;
    }

    // Lo que muestra el radar de la Exploradora sobre la próxima jugada de un enemigo.
    public string Intencion(Enemigo enemigo)
    {
        if (!enemigo.Viva) return "Destruida";
        if (Terminada) return "—";
        if (!enemigo.Estrategia.Predecible) return "Impredecible";
        // Le pregunta a la estrategia sin ejecutar nada: es solo una "vista previa".
        return enemigo.Estrategia.DecidirAccion(enemigo, Contexto).Descripcion;
    }

    private void NuevaRonda()
    {
        FaseEnemiga = false;
        Contexto.Ronda++;
        indiceAliado = 0;
        SaltarAliadosCaidos();
        Historial.Add("[R" + Contexto.Ronda + "] ── Nueva ronda ──");
    }

    // Si una nave aliada fue destruida, su turno se saltea.
    private void SaltarAliadosCaidos()
    {
        while (indiceAliado < TurnosAliados.Count && !TurnosAliados[indiceAliado].Unidad.Viva) indiceAliado++;
    }
}