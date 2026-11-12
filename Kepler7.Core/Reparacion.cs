namespace Kepler7.Core;

// Estado de la reparación separado de Windows Forms para validar cada interacción.
public sealed class ReparacionClonadora
{
    public bool[] Modulos { get; } = new bool[2];
    public float[] Tornillos { get; } = new float[4];
    public bool[] Cables { get; } = new bool[4];
    public bool Fijada => Modulos.All(x => x) && Tornillos.All(x => x >= 1);
    public bool Completa => Fijada && Cables.All(x => x);
    public float Avance => (Modulos.Count(x => x) + Tornillos.Sum() + Cables.Count(x => x)) / 10f;
    public bool Colocar(int pieza, int hueco)
    {
        if (pieza < 0 || pieza >= 2 || pieza != hueco || Modulos[pieza]) return false;
        Modulos[pieza] = true; return true;
    }
    public void Atornillar(int indice, float radianes)
    {
        if (indice < 0 || indice >= 4 || !Modulos[indice / 2] || !float.IsFinite(radianes) || Math.Abs(radianes) > MathF.PI / 2) return;
        // Una vuelta y media en sentido horario. El giro contrario deshace el ajuste.
        if (Tornillos[indice] < 1) Tornillos[indice] = Math.Clamp(Tornillos[indice] + radianes / (3 * MathF.PI), 0, 1);
    }
    public bool Conectar(int origen, int destino)
    {
        if (!Fijada || origen < 0 || origen >= 4 || origen != destino || Cables[origen]) return false;
        Cables[origen] = true; return true;
    }
}
