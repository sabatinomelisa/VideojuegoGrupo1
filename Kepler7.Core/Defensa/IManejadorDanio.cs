namespace Kepler7.Core.Defensa;

// [PATRÓN CHAIN OF RESPONSIBILITY] Interfaz de cada eslabón de la cadena de defensas.
public interface IManejadorDanio
{
    string Nombre { get; }
    IManejadorDanio? Siguiente { get; set; }
    void Manejar(Danio danio);
}

// Parte común: procesar y, si todavía queda daño, pasárselo al siguiente eslabón.
public abstract class ManejadorDanioBase : IManejadorDanio
{
    protected readonly UnidadBatalla unidad;
    protected ManejadorDanioBase(UnidadBatalla unidad) { this.unidad = unidad; }

    public abstract string Nombre { get; }
    public IManejadorDanio? Siguiente { get; set; }

    public void Manejar(Danio danio)
    {
        Procesar(danio);
        if (danio.Cantidad > 0 && Siguiente != null)
        {
            danio.Registrar("   → pasa a " + Siguiente.Nombre + " con " + danio.Cantidad);
            Siguiente.Manejar(danio);
        }
    }

    protected abstract void Procesar(Danio danio);
}
