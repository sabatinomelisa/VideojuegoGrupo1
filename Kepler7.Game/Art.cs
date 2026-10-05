using System.Drawing.Drawing2D;
using Kepler7.Core;
namespace Kepler7.Game;

// Arte vectorial propio: se dibuja con GDI+, sin imágenes ni dependencias externas.
internal static partial class Art
{
    public static readonly Color Navy = Color.FromArgb(27, 27, 49), PanelColor = Color.FromArgb(48, 46, 75), Line = Color.FromArgb(87, 81, 115),
        White = Color.FromArgb(246, 244, 251), Muted = Color.FromArgb(187, 193, 217), Mint = Color.FromArgb(157, 235, 206),
        Gold = Color.FromArgb(255, 215, 150), Red = Color.FromArgb(249, 150, 176), Blue = Color.FromArgb(151, 201, 249);
    public static readonly Color Danger=Color.FromArgb(255,91,115);
    public static Color Uniforme(string color) => color switch { "Coral" => Red, "Azul" => Blue, "Dorado" => Gold, _ => Mint };
    public static void Box(Graphics g, float x, float y, float w, float h, Color color, float r = 14)
    {
        using var b = new SolidBrush(color);
        if (r <= 0) { g.FillRectangle(b, x, y, w, h); return; }
        bool themed=color.A==255 && w<1190 && h>=28 && (color==PanelColor || color==Navy || color==Line);
        if(themed)r=Math.Max(r,Math.Min(18,h*.22f));
        r=Math.Min(r,Math.Min(w,h)/2);
        using var p = new GraphicsPath(); float d = r * 2;
        p.AddArc(x, y, d, d, 180, 90); p.AddArc(x + w - d, y, d, d, 270, 90);
        p.AddArc(x + w - d, y + h - d, d, d, 0, 90); p.AddArc(x, y + h - d, d, d, 90, 90); p.CloseFigure(); g.FillPath(b, p);
        if(themed){using var edge=new Pen(Color.FromArgb(color==Navy?35:48,White),1);g.DrawPath(edge,p);}
    }
    public static void Ellipse(Graphics g, Color c, float x, float y, float w, float h) { using var b = new SolidBrush(c); g.FillEllipse(b, x, y, w, h); }
    public static void LineTo(Graphics g, Color c, float x, float y, float x2, float y2, float width = 1) { using var p = new Pen(c, width); g.DrawLine(p, x, y, x2, y2); }
    public static void Poly(Graphics g, Color c, params PointF[] points) { using var b = new SolidBrush(c); g.FillPolygon(b, points); }
    public static void Ship(Graphics g, float x, float y, float scale, IEnumerable<Componente> components, Color color, float unfold = 0)
    {
        var cs = components.ToArray(); int model = cs.Contains(Componente.Armas) ? 1 : cs.Contains(Componente.Tanques) ? 2 : 0;
        var recipe = RecetaNave.Todas[model];
        if(cs.Contains(Componente.Casco)) foreach(var part in cs) ToyMount(g,x,y,scale,part,color);
        foreach (var part in cs.OrderBy(c => c == Componente.Casco ? 0 : 1))
        {
            var o = ToyPosition(part); int i = Array.IndexOf(recipe.Componentes, part);
            ToyPart(g,x+o.X*scale,y+o.Y*scale,scale,part,(model+Math.Max(i,0))%2,color);
        }
    }
}
