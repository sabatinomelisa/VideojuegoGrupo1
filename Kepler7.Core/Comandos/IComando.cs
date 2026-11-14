namespace Kepler7.Core.Comandos;

// [PATRÓN COMMAND] Cada acción del jugador es un objeto que sabe ejecutarse y deshacerse.
public interface IComando
{
    // Texto para el historial que se ve en pantalla.
    string Descripcion { get; }
    // Devuelve false si la acción no pudo realizarse (no entra al historial de deshacer).
    bool Ejecutar();
    void Deshacer();
    // Algunas acciones dejan de poder deshacerse (por ejemplo, una nave que ya tiene piloto).
    bool PuedeDeshacer => true;
}
