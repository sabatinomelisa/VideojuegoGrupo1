using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;

//Pantalla del inventario (del patron visitor): el jugador elige un visitante
// y ve el resultado de aplicarlo sobre todos los ítems.
public sealed partial class GameForm
{
    private readonly Inventario inventario = new Inventario();
    private string resultadoVisitante = "Elegí un analizador para recorrer el inventario.";

    private void AbrirInventario()
    {
        resultadoVisitante = "Elegí un analizador para recorrer el inventario.";
        Change(Pantalla.Inventario);
    }

    private void VolverDelInventario()
    {
        Change(Pantalla.Estacion);
    }

    // Cada botón crea un visitante distinto y se lo pasa al inventario.
    private void UsarCalculadorPrecio()
    {
        resultadoVisitante = inventario.Aplicar(new CalculadorPrecio());
        DarInsigniaVisitor();
    }

    private void UsarContadorArmas()
    {
        resultadoVisitante = inventario.Aplicar(new ContadorArmas());
        DarInsigniaVisitor();
    }

    private void UsarAplicadorVeneno()
    {
        resultadoVisitante = inventario.Aplicar(new AplicadorVeneno());
        DarInsigniaVisitor();
    }

    private void DarInsigniaVisitor()
    {
        if (progress.RegistrarVisitor())
        {
            Say("¡Insignia Visitor conseguida! +100 XP");
            Save();
        }
    }

    private void DrawInventario(Graphics g)
    {
        Header(g, "Analizador de inventario", "Inventario de la tripulación");
        Button(g, "← Volver", 40, 125, 170, 40, VolverDelInventario);

        // Columna izquierda: los ítems
        TextAt(g, "ÍTEMS", 40, 185, 14, Mint, true);
        float y = 210;
        foreach (IItem item in inventario.Items)
        {
            Color color = White;
            if (item is Arma arma && arma.Envenenada)
            {
                color = Color.FromArgb(140, 220, 90);
            }
            Box(g, 40, y, 560, 52, PanelColor, 10);
            TextAt(g, item.Nombre, 60, y + 6, 17, White, true);
            TextAt(g, item.Descripcion(), 60, y + 29, 13, color);
            TextAt(g, item.Precio + " cr", 515, y + 15, 15, Gold, true);
            y = y + 60;
        }

        // Columna derecha: los visitantes y el resultado
        TextAt(g, "ANALIZADORES (VISITANTES)", 640, 185, 14, Mint, true);
        Button(g, "Calcular precio total", 640, 210, 520, 50, UsarCalculadorPrecio, true);
        Button(g, "Contar armas", 640, 270, 520, 50, UsarContadorArmas, true);
        Button(g, "Aplicar veneno a las armas", 640, 330, 520, 50, UsarAplicadorVeneno, true);
        Box(g, 640, 400, 520, 230, Navy, 12);
        TextAt(g, "RESULTADO", 660, 415, 13, Gold, true);
        Wrap(g, resultadoVisitante, 660, 445, 480, 170, 18, White);
    }
}