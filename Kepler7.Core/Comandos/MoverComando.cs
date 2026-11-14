namespace Kepler7.Core.Comandos;

public interface IPosicionable
{
    string Nombre { get; }
    int X { get; set; }
    int Y { get; set; }
}

// [PATRÓN COMMAND · Comando concreto] Mueve algo en la grilla y recuerda de dónde salió.
public sealed class MoverComando : IComando
{
    private readonly IPosicionable sujeto;
    private readonly int dx, dy;
    private readonly Func<int, int, bool>? posicionValida;
    private int xAntes, yAntes;

    public MoverComando(IPosicionable sujeto, int dx, int dy, Func<int, int, bool>? posicionValida = null)
    {
        this.sujeto = sujeto; this.dx = dx; this.dy = dy; this.posicionValida = posicionValida;
    }

    public string Descripcion => "Mover " + sujeto.Nombre + " (" + dx + ", " + dy + ")";

    public bool Ejecutar()
    {
        int nx = sujeto.X + dx, ny = sujeto.Y + dy;
        if (posicionValida != null && !posicionValida(nx, ny)) return false;
        xAntes = sujeto.X; yAntes = sujeto.Y;
        sujeto.X = nx; sujeto.Y = ny;
        return true;
    }

    public void Deshacer() { sujeto.X = xAntes; sujeto.Y = yAntes; }
}
