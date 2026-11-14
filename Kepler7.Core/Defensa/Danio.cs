namespace Kepler7.Core.Defensa;

// Objeto que viaja por la cadena de responsabilidad. Cada eslabón lo reduce y deja registro de lo que hizo.
public sealed class Danio
{
    private readonly List<string> recorrido = new();

    public Danio(int cantidad, string origen = "Ataque")
    {
        CantidadOriginal = cantidad;
        Cantidad = cantidad;
        Origen = origen;
    }

    public string Origen { get; }
    public int CantidadOriginal { get; }
    // Daño que todavía no fue absorbido por ningún eslabón.
    public int Cantidad { get; set; }
    // Resultado del minijuego de bloqueo: 1 = sin bloqueo, .5 = bloqueo, .25 = bloqueo perfecto.
    public float FactorBloqueo { get; set; } = 1f;
    // Azar de la batalla (para que las partidas con semilla sean reproducibles).
    public Random? Azar { get; set; }

    public bool Esquivado { get; set; }
    public int AbsorbidoPorEscudo { get; set; }
    // Daño que llegó al último eslabón (Vida), aunque la nave tuviera menos vida que eso.
    public int LlegoALaVida { get; set; }
    public bool ObjetivoDerrotado { get; set; }

    // Paso a paso, para mostrarlo en pantalla y en las verificaciones.
    public IReadOnlyList<string> Recorrido => recorrido;
    public void Registrar(string paso) => recorrido.Add(paso);
}
