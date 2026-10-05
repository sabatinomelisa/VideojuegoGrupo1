using System.Drawing.Drawing2D;
using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;

public sealed partial class GameForm
{
    private ReparacionClonadora repair = new();
    private int repairModule = -1, repairCable = -1, repairScrew = -1;
    private float screwAngle, repairSpark;
    private bool screwTracking;
    private readonly RectangleF[] moduleSlots = { new(389, 258, 191, 153), new(634, 258, 130, 153) };
    private readonly RectangleF[] moduleTrays = { new(71, 243, 234, 150), new(71, 432, 234, 153) };
    private readonly PointF[] screwPoints = { new(409, 278), new(560, 391), new(654, 278), new(744, 391) };
    private readonly Color[] wireColors = { Red, Blue, Gold, Mint };
    private PointF WireStart(int i) => new(422, 465 + i * 49);
    private PointF WireEnd(int i) => new(1030, 465 + new[] { 2, 0, 3, 1 }[i] * 49);
    private static float Distance(PointF a, PointF b) => MathF.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
    private void CancelRepairDrag() { repairModule = repairCable = repairScrew = -1; screwTracking = false; }
    private bool BeginRepairDrag()
    {
        if (repair.Completa) return false;
        for (int i = 0; i < 2; i++)
            if (!repair.Modulos[i] && moduleTrays[i].Contains(mouse)) { repairModule = i; return true; }
        for (int i = 0; i < 4; i++)
            if (repair.Modulos[i / 2] && repair.Tornillos[i] < 1 && Distance(mouse, screwPoints[i]) < 33)
            { repairScrew = i; screwTracking = false; return true; }
        if (repair.Fijada)
            for (int i = 0; i < 4; i++)
                if (!repair.Cables[i] && Distance(mouse, WireStart(i)) < 26) { repairCable = i; return true; }
        return false;
    }
    private void MoveRepair()
    {
        if (screen != Pantalla.Reparacion || repairScrew < 0) return;
        PointF p = screwPoints[repairScrew]; float radius = Distance(mouse, p);
        if (radius < 12 || radius > 68) { screwTracking = false; return; }
        float angle = MathF.Atan2(mouse.Y - p.Y, mouse.X - p.X);
        if (screwTracking)
        {
            float delta = angle - screwAngle;
            while (delta > MathF.PI) delta -= 2 * MathF.PI;
            while (delta < -MathF.PI) delta += 2 * MathF.PI;
            repair.Atornillar(repairScrew, delta);
        }
        screwAngle = angle; screwTracking = true;
        if (repair.Tornillos[repairScrew] >= 1) { repairScrew = -1; Say(repair.Fijada ? "Módulos firmes. Ahora conectá los cuatro cables." : "Tornillo ajustado. Seguí con los demás."); }
    }
    private void EndRepairDrag()
    {
        if (screen != Pantalla.Reparacion) { CancelRepairDrag(); return; }
        if (repairModule >= 0)
        {
            int target = Array.FindIndex(moduleSlots, r => r.Contains(mouse));
            if (target >= 0 && repair.Colocar(repairModule, target)) Say("Pieza encastrada. Ajustá sus dos tornillos girando el mouse ↻.");
            else if (target >= 0) { repairSpark = .55f; Say("Esa pieza no encaja en ese hueco. Mirá su forma."); }
        }
        if (repairCable >= 0)
        {
            int target = Enumerable.Range(0, 4).FirstOrDefault(i => Distance(mouse, WireEnd(i)) < 28, -1);
            if (target >= 0 && !repair.Conectar(repairCable, target)) { repairSpark = .65f; Say("¡Chispa! Uní el mismo color y número. El cable volvió a su origen."); }
            if (repair.Completa && !progress.LaboratorioReparado)
            {
                CompleteRescue();
                Say("¡Contención restablecida! Detuviste la clonación mutante a tiempo.");
            }
        }
        CancelRepairDrag();
    }
    private void DrawRepairModule(Graphics g, int i, RectangleF r, bool ghost = false)
    {
        Color c = ghost ? Line : i == 0 ? Mint : Gold;
        Box(g, r.X, r.Y, r.Width, r.Height, ghost ? Navy : Color.FromArgb(25, 57, 63), i == 0 ? 7 : 24);
        using (var pen = new Pen(c, 3) { DashStyle = ghost ? DashStyle.Dash : DashStyle.Solid }) g.DrawRectangle(pen, r.X + 7, r.Y + 7, r.Width - 14, r.Height - 14);
        if (i == 0)
        {
            for (int j = 0; j < 4; j++)
            {
                LineTo(g, c, r.X + 30, r.Y + 45 + j * 14, r.X + r.Width - 27, r.Y + 45 + j * 14, 2);
                Ellipse(g, c, r.X + 24, r.Y + 42 + j * 14, 6, 6);
            }
            Box(g, r.X + r.Width/2 - 24, r.Y + 44, 48, 57, ghost ? Line : Navy, 5);
            TextAt(g, "ADN", r.X + r.Width/2 - 19, r.Y + 63, 16, c, true);
        }
        else
        {
            for (int j = 0; j < 3; j++) Box(g, r.X + 23 + j*(r.Width-46)/3, r.Y+43, (r.Width-53)/3, 64, c, 6);
            Poly(g, Navy, new(r.X+r.Width/2+6,r.Y+51),new(r.X+r.Width/2-14,r.Y+79),new(r.X+r.Width/2,r.Y+79),new(r.X+r.Width/2-5,r.Y+101),new(r.X+r.Width/2+17,r.Y+69),new(r.X+r.Width/2+3,r.Y+69));
        }
    }
    private void DrawWire(Graphics g, PointF start, PointF end, Color color)
    {
        using var shadow = new Pen(Navy, 12) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var pen = new Pen(color, 6) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        var a = new PointF(start.X + 140, start.Y + 25); var b = new PointF(end.X - 140, end.Y - 25);
        g.DrawBezier(shadow, start, a, b, end); g.DrawBezier(pen, start, a, b, end);
    }
    private void DrawRepair(Graphics g)
    {
        DrawLabEnvironment(g);
        Box(g,40,150,1120,510,Color.FromArgb(230,Navy),0);
        Header(g, "LAB BIO-01 / Panel de reparación", "Manos a la clonadora");
        string step = repair.Completa ? "SISTEMA RESTAURADO" : !repair.Modulos.All(x => x) ? "1 · Encastrá los dos módulos en sus huecos" : !repair.Fijada ? "2 · Ajustá los cuatro tornillos: mantené clic y girá ↻" : "3 · Arrastrá cada cable hasta su color y número";
        TextAt(g, step, 40, 128, 19, repair.Completa ? Mint : Gold, true);
        Box(g, 40, 181, 288, 485, PanelColor); Box(g, 350, 181, 810, 485, PanelColor);
        TextAt(g, "BANDEJA DE REPUESTOS", 65, 201, 15, Muted, true);
        TextAt(g, "INTERIOR DE LA CLONADORA", 376, 201, 15, Muted, true);
        for (int i = 0; i < 2; i++)
        {
            var r = moduleTrays[i];
            if (!repair.Modulos[i] && repairModule != i) DrawRepairModule(g, i, new(r.X+22, r.Y+10, r.Width-44, r.Height-30));
            else TextAt(g, repair.Modulos[i] ? "✓ Instalado" : "Colocando…", r.X+60, r.Y+55, 18, Mint);
            TextAt(g, i == 0 ? "Placa de control ADN" : "Celda de energía", r.X+20, r.Bottom-10, 15, Muted);
            DrawRepairModule(g, i, moduleSlots[i], !repair.Modulos[i]);
        }
        for (int i = 0; i < 4; i++)
        {
            PointF p = screwPoints[i]; float done = repair.Tornillos[i];
            if (!repair.Modulos[i/2]) continue;
            Ellipse(g, Navy, p.X-19, p.Y-19, 38, 38); Ellipse(g, done >= 1 ? Mint : Muted, p.X-10, p.Y-10, 20, 20);
            float angle = done * 3 * MathF.PI;
            LineTo(g, Navy, p.X-MathF.Cos(angle)*8, p.Y-MathF.Sin(angle)*8, p.X+MathF.Cos(angle)*8, p.Y+MathF.Sin(angle)*8, 3);
            using var ring = new Pen(done >= 1 ? Mint : Gold, 4);
            if (done > 0) g.DrawArc(ring, p.X-24, p.Y-24, 48, 48, -90, Math.Max(1, done*360));
            if (repairScrew == i) { using var guide = new Pen(Gold, 1) { DashStyle = DashStyle.Dash }; g.DrawEllipse(guide, p.X-42, p.Y-42, 84, 84); }
        }
        Box(g, 812, 248, 320, 167, Navy, 9);
        TextAt(g, repair.Completa ? "ADN ESTABLE" : "DIAGNÓSTICO", 833, 269, 17, repair.Completa ? Mint : Gold, true);
        TextAt(g, "Módulos: " + repair.Modulos.Count(x => x) + "/2", 833, 303, 16);
        TextAt(g, "Tornillos: " + repair.Tornillos.Count(x => x >= 1) + "/4", 833, 332, 16);
        TextAt(g, "Cables: " + repair.Cables.Count(x => x) + "/4", 833, 361, 16);
        foreach (int i in Enumerable.Range(0, 4)) if (repair.Cables[i]) DrawWire(g, WireStart(i), WireEnd(i), wireColors[i]);
        for (int i = 0; i < 4; i++)
        {
            PointF a = WireStart(i), b = WireEnd(i); Color c = repair.Fijada ? wireColors[i] : Muted;
            Ellipse(g, Line, a.X-20, a.Y-20, 40, 40); Ellipse(g, c, a.X-14, a.Y-14, 28, 28);
            Ellipse(g, c, b.X-20, b.Y-20, 40, 40); Ellipse(g, Navy, b.X-13, b.Y-13, 26, 26);
            TextAt(g, (i+1).ToString(), a.X-5, a.Y-10, 16, Navy, true); TextAt(g, (i+1).ToString(), b.X-5, b.Y-10, 16, c, true);
        }
        if (repairCable >= 0) { DrawWire(g, WireStart(repairCable), mouse, wireColors[repairCable]); Box(g, mouse.X-10, mouse.Y-8, 20, 16, White, 3); }
        if (repairModule >= 0) { var r = moduleSlots[repairModule]; DrawRepairModule(g, repairModule, new(mouse.X-r.Width/2, mouse.Y-r.Height/2,r.Width,r.Height)); }
        if (repairSpark > 0)
            for (int i = 0; i < 12; i++) { float angle=i*MathF.PI/6, r=(.7f-repairSpark)*150; LineTo(g, i%2==0 ? Gold : Red,mouse.X+MathF.Cos(angle)*r,mouse.Y+MathF.Sin(angle)*r,mouse.X+MathF.Cos(angle)*(r+20),mouse.Y+MathF.Sin(angle)*(r+20),3); }
        if (repair.Completa) Button(g, "Cerrar panel · Volver a la sala →", 800, 670, 360, 38, () => { cloneName = "Jefa Misha"; roomCat = new(600, 410); Change(Pantalla.SalaLab); }, true);
        else TextAt(g, !repair.Fijada ? "Tornillos: arrastrá en círculos ↻ alrededor de cada cabeza (1½ vueltas)." : "Mantené clic en un conector izquierdo y soltalo en su pareja derecha.", 40, 677, 15, Muted);
    }
}
