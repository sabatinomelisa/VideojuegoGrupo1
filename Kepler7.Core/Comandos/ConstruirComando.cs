namespace Kepler7.Core.Comandos;

// [PATRÓN COMMAND · Comando concreto] Registra en la flota las naves ensambladas en el hangar.
// Deshacer las quita y devuelve el XP, mientras ninguna tenga piloto asignado.
public sealed class ConstruirComando : IComando
{
    private readonly Progreso progreso;
    private readonly List<Nave> naves;
    private int xpOtorgado;

    public ConstruirComando(Progreso progreso, IEnumerable<Nave> naves)
    {
        this.progreso = progreso;
        this.naves = naves.ToList();
    }

    public string Descripcion => "Construir " + naves.Count + (naves.Count == 1 ? " nave " : " naves ") + (naves.Count > 0 ? naves[0].Tipo : "");

    public bool PuedeDeshacer => naves.All(n => n.Piloto == null && progreso.Flota.Contains(n));

    public bool Ejecutar()
    {
        if (progreso.Ingeniero == null || naves.Count == 0) return false;
        int antes = progreso.XP;
        foreach (var n in naves) progreso.RegistrarNave(n);
        xpOtorgado = progreso.XP - antes;
        return true;
    }

    public void Deshacer()
    {
        if (!PuedeDeshacer) return;
        foreach (var n in naves) progreso.Flota.Remove(n);
        progreso.XP = Math.Max(0, progreso.XP - xpOtorgado);
    }
}
