namespace Kepler7.Core.Comandos;

// [PATRÓN COMMAND · Comando concreto] Base de las acciones de la flota durante la escolta orbital.
// Antes de ejecutar saca una foto de la batalla (Memento): Deshacer vuelve a ella; Rehacer reaplica
// la foto del "después", así no se vuelve a tirar el azar (esquiva, etc.) y el resultado es el mismo.
public abstract class ComandoBatalla : IComando
{
    private readonly BatallaOrbital batalla;
    private readonly UnidadBatalla? objetivo;
    private readonly IEstrategiaIA? hackeo;
    private readonly float precision;
    private EstadoBatalla? antes, despues;
    private string resumen = "";

    protected ComandoBatalla(BatallaOrbital batalla, UnidadBatalla? objetivo, IEstrategiaIA? hackeo, float precision)
    {
        this.batalla = batalla; this.objetivo = objetivo; this.hackeo = hackeo; this.precision = precision;
    }

    protected abstract string Verbo { get; }
    public string Descripcion => resumen.Length > 0 ? resumen : Verbo;

    public bool Ejecutar()
    {
        if (despues != null) { batalla.Restaurar(despues); return true; }          // rehacer
        var turno = batalla.TurnoActual;
        if (turno == null || batalla.Terminada) return false;
        string rol = turno.Unidad.Rol;
        antes = batalla.Capturar();
        batalla.JugarTurnoAliado(objetivo, hackeo, precision);
        despues = batalla.Capturar();
        resumen = rol + " · " + Verbo + ": " + batalla.UltimoResultado;
        return true;
    }

    public void Deshacer() { if (antes != null) batalla.Restaurar(antes); }
}

public sealed class AtacarComando : ComandoBatalla
{
    public AtacarComando(BatallaOrbital b, UnidadBatalla? objetivo, float precision = 1f) : base(b, objetivo, null, precision) { }
    protected override string Verbo => "atacar";
}

public sealed class CurarComando : ComandoBatalla
{
    public CurarComando(BatallaOrbital b, UnidadBatalla? objetivo, float precision = 1f) : base(b, objetivo, null, precision) { }
    protected override string Verbo => "reparar";
}

public sealed class HackearComando : ComandoBatalla
{
    public HackearComando(BatallaOrbital b, UnidadBatalla? objetivo, IEstrategiaIA ia) : base(b, objetivo, ia, 1f) { }
    protected override string Verbo => "hackear IA";
}

public static class ComandosBatalla
{
    // Elige el comando que corresponde al turno que está jugando la flota.
    public static IComando Para(BatallaOrbital batalla, UnidadBatalla? objetivo, IEstrategiaIA? hackeo, float precision) =>
        batalla.TurnoActual switch
        {
            TurnoMedico => new CurarComando(batalla, objetivo, precision),
            TurnoComandante when hackeo != null => new HackearComando(batalla, objetivo, hackeo),
            _ => new AtacarComando(batalla, objetivo, precision)
        };
}
