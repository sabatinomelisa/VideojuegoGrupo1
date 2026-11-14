using System.Numerics;
using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;

// [PATRÓN STATE] Cómo se ve cada estado del gato en pantalla.
public sealed partial class GameForm
{
    // Color del gato según su estado actual.
    private Color ColorEstado() => corridor.Gato.Estado switch
    {
        Envenenado => Color.FromArgb(140, 220, 90),
        Aturdido => Gold,
        Berserk => Danger,
        _ => Mint
    };

    // Aro que late alrededor del gato y cartel con el estado y los segundos que quedan.
    private void DrawEstadoGato(Graphics g)
    {
        var estado = corridor.Gato.Estado;
        if (estado is Normal) return;
        var color = ColorEstado();
        var p = corridor.Jugador;
        float pulso = (MathF.Sin(time * 6) + 1) / 2;
        using (var aro = new Pen(Color.FromArgb(120 + (int)(pulso * 120), color), 3))
            g.DrawEllipse(aro, p.X - 38 - pulso * 4, p.Y - 38 - pulso * 4, 76 + pulso * 8, 76 + pulso * 8);
        TextAt(g, estado.Nombre.ToUpper() + " " + corridor.Gato.TiempoRestante.ToString("0.0") + " s", p.X - 52, p.Y - 72, 12, color, true);
    }

    // Línea debajo de la barra de salud que explica el efecto del estado.
    private void DrawEstadoHud(Graphics g)
    {
        var estado = corridor.Gato.Estado;
        string efecto = estado switch
        {
            Envenenado => "pierde vida · más lento",
            Aturdido => "¡no puede disparar!",
            Berserk => "ráfaga y velocidad · recibe +50 % de daño",
            _ => "sin efectos"
        };
        TextAt(g, "ESTADO: " + estado.Nombre.ToUpper() + " · " + efecto, 610, 82, 12, ColorEstado(), true);
    }

    // Power-up rojo que activa el estado Berserk al tocarlo.
    private void DrawFuria(Graphics g, Vector2 p)
    {
        float pulso = (MathF.Sin(time * 5) + 1) / 2;
        for (int i = 4; i >= 1; i--)
        {
            float r = 14 + i * 6 + pulso * 4;
            Ellipse(g, Color.FromArgb(18, Danger), p.X - r, p.Y - r, r * 2, r * 2);
        }
        Ellipse(g, Danger, p.X - 14, p.Y - 14, 28, 28);
        Ellipse(g, Gold, p.X - 6, p.Y - 6, 12, 12);
        TextAt(g, "BERSERK", p.X - 30, p.Y - 42, 11, Danger, true);
    }
}