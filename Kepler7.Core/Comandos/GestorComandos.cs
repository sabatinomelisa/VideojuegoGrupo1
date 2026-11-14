namespace Kepler7.Core.Comandos;

// [PATRÓN COMMAND · Invocador] Ejecuta comandos y mantiene las pilas de deshacer y rehacer, más el historial visible.
public sealed class GestorComandos
{
    private readonly Stack<IComando> deshacer = new();
    private readonly Stack<IComando> rehacer = new();
    private readonly List<string> historial = new();
    private int numero;

    public IReadOnlyList<string> Historial => historial;
    public bool PuedeDeshacer => deshacer.Count > 0 && deshacer.Peek().PuedeDeshacer;
    public bool PuedeRehacer => rehacer.Count > 0;
    public int Ejecutados => deshacer.Count;
    public int Rehacibles => rehacer.Count;
    public string? ProximoDeshacer => deshacer.Count > 0 ? deshacer.Peek().Descripcion : null;
    public string? ProximoRehacer => rehacer.Count > 0 ? rehacer.Peek().Descripcion : null;

    public bool Ejecutar(IComando comando)
    {
        if (!comando.Ejecutar()) { Anotar("RECHAZADO", comando.Descripcion); return false; }
        deshacer.Push(comando);
        rehacer.Clear();                 // una acción nueva borra el "futuro"
        Anotar("EJECUTADO", comando.Descripcion);
        return true;
    }

    public bool Deshacer()
    {
        if (!PuedeDeshacer) return false;
        var c = deshacer.Pop();
        c.Deshacer();
        rehacer.Push(c);
        Anotar("DESHECHO", c.Descripcion);
        return true;
    }

    public bool Rehacer()
    {
        if (!PuedeRehacer) return false;
        var c = rehacer.Pop();
        if (!c.Ejecutar()) { rehacer.Push(c); return false; }
        deshacer.Push(c);
        Anotar("REHECHO", c.Descripcion);
        return true;
    }

    // Cuando algo irreversible cambia el mundo (por ejemplo, actúan los mutantes) las pilas se vacían; el historial queda.
    public void Cerrar(string motivo)
    {
        if (deshacer.Count == 0 && rehacer.Count == 0) return;
        deshacer.Clear(); rehacer.Clear();
        historial.Add("— " + motivo + " —");
    }

    public void Reiniciar() { deshacer.Clear(); rehacer.Clear(); historial.Clear(); numero = 0; }

    private void Anotar(string estado, string texto) => historial.Add("#" + (++numero) + " " + estado + " · " + texto);
}
