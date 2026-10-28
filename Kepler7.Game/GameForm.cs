using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Numerics;
using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;

public sealed partial class GameForm : Form
{
    private enum Pantalla { Inicio, Pasillo, Reparacion, Laboratorio, Hangar, Final, Estacion, SalaLab, SalaHangar, Abordaje, Biblioteca, Poblacion, Logros, SalaComunicaciones, Comunicaciones, Batalla }
    private Pantalla screen;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private double previous;
    private float time, animation, explosion;
    private readonly Dictionary<(float, bool), Font> fonts = new();
    private readonly List<(RectangleF Rect, Action Click)> buttons = new();
    private readonly HashSet<Keys> keys = new();
    private PointF mouse = new(600, 350);
    private bool firing, paused, naming, nameSelected;
    private int recipeIndex, colorIndex, selectedPilotRecipe;
    private Progreso progress = new();
    private Pasillo corridor = new();
    private readonly Taller workshop = new();
    private readonly HashSet<Componente> selected = new();
    private Tripulante? lastClone;
    private Nave? pendingShip;
    private string cloneName = "Jefa Misha", notice = "", saveNotice = "";
    private float noticeTime;
    private readonly string[] colors = { "Menta", "Coral", "Azul", "Dorado" };
    private RecetaNave Recipe => RecetaNave.Todas[recipeIndex];
    private float Camera => Math.Clamp(corridor.Jugador.X - 330, 0, Pasillo.Largo - 1120);
    private float ScaleFactor => Math.Min(ClientSize.Width / 1200f, ClientSize.Height / 760f);
    private PointF Offset => new((ClientSize.Width - 1200 * ScaleFactor) / 2, (ClientSize.Height - 760 * ScaleFactor) / 2);
    public GameForm()
    {
        Text = "Kepler-7 · Operación Bigotes · Flota felina · R28"; ClientSize = new Size(1200, 760); MinimumSize = new Size(960, 640);
        StartPosition = FormStartPosition.CenterScreen; DoubleBuffered = true; KeyPreview = true; BackColor = Navy;
        try { progress = Guardado.Cargar(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidDataException)
        { saveNotice = "No se pudo leer el guardado. Podés iniciar una partida nueva."; }
        KeyDown += OnKeyDown; KeyUp += (_, e) => keys.Remove(e.KeyCode);
        KeyPress += (_, e) => { if (DialogOpen) { if (inputTitle != null) InputCharacter(e.KeyChar); e.Handled = true; Invalidate(); return; } if (batchEditing) { if (char.IsDigit(e.KeyChar) && (batchText.Length < 5 || batchSelected)) { if (batchSelected) { batchText = ""; batchSelected = false; } batchText += e.KeyChar; } e.Handled = true; Invalidate(); return; } if (naming && !char.IsControl(e.KeyChar) && (cloneName.Length < 20 || nameSelected)) { if (nameSelected) { cloneName = ""; nameSelected = false; } cloneName += e.KeyChar; Invalidate(); } };
        MouseMove += (_, e) => { mouse = Logical(e.Location); if (!DialogOpen) MoveAntenna(); MoveRepair(); Invalidate(); };
        MouseDown += (_, e) =>
        {
            mouse = Logical(e.Location);
            if (e.Button != MouseButtons.Left) return;
            if (DialogOpen) { var dialog = buttons.LastOrDefault(b => b.Rect.Contains(mouse)); dialog.Click?.Invoke(); Invalidate(); return; }
            if (rescueVictory) { var victory = buttons.LastOrDefault(b => b.Rect.Contains(mouse)); victory.Click?.Invoke(); Invalidate(); return; }
            if (activeGuide != null) { var help = buttons.LastOrDefault(b => b.Rect.Contains(mouse)); help.Click?.Invoke(); Invalidate(); return; }
            if (EmergencyBlocked) { var blocked = buttons.LastOrDefault(b => b.Rect.Contains(mouse)); blocked.Click?.Invoke(); Invalidate(); return; }
            if (screen == Pantalla.Comunicaciones && BeginAntennaDrag()) { Capture = true; return; }
            if (screen == Pantalla.Reparacion && BeginRepairDrag()) { Capture = true; return; }
            if (screen == Pantalla.Pasillo && !paused && !corridor.Derrota) firing = true;
            if (screen == Pantalla.Hangar && StartDrag()) { Capture = true; return; }
          
            var target = buttons.LastOrDefault(b => b.Rect.Contains(mouse));
            target.Click?.Invoke(); Invalidate();
        };
        MouseUp += (_, e) => { mouse = Logical(e.Location); firing = false; antennaDrag = -1; EndRepairDrag(); if (dragging != null) DropPart(); Capture = false; };
        Deactivate += (_, _) => { keys.Clear(); firing = false; antennaDrag = -1; dragging = null; CancelRepairDrag(); Capture = false; if (EmergencySector) { paused = true; SyncEmergencyAudio(); } };
        FormClosing += (_, _) => Save();
        timer.Tick += (_, _) => TickGame(); timer.Start();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Dispose(); catHiss?.Stop(); catHiss?.Dispose(); emergencyMusic?.Stop(); emergencyMusic?.Dispose(); foreach (var f in fonts.Values) f.Dispose(); }
        base.Dispose(disposing);
    }
    private PointF Logical(Point p) => new((p.X - Offset.X) / ScaleFactor, (p.Y - Offset.Y) / ScaleFactor);
    private void Change(Pantalla next) { screen = next; antennaDrag = -1; inputTitle = null; inputAccept = null; dragging = null; CancelRepairDrag(); Capture = false; buttons.Clear(); firing = false; keys.Clear(); naming = false; batchEditing = false; OfferRoomGuide(); SyncEmergencyAudio(); Invalidate(); }
    private void Say(string message) { notice = message; noticeTime = 6; }
    private void Save()
    {
        try { Guardado.Guardar(progress); saveNotice = "Progreso guardado"; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { saveNotice = "No se pudo guardar. Revisá los permisos de AppData."; }
    }
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DialogOpen) { HandleDialogKey(e); return; }
        if (e.KeyCode == Keys.Escape && screen == Pantalla.Logros) { Change(achievementsReturn); return; }
        if (rescueVictory) { if (e.KeyCode is Keys.Enter or Keys.Escape) CloseRescueVictory(); e.SuppressKeyPress = true; return; }
        if (e.KeyCode == Keys.M && !naming && !batchEditing) { musicMuted = !musicMuted; SyncEmergencyAudio(); e.SuppressKeyPress = true; return; }
        if (activeGuide != null) { if (e.KeyCode is Keys.Enter or Keys.Escape) CloseRoomGuide(); e.Handled = true; e.SuppressKeyPress = true; return; }
        if (EmergencySector && (emergency.Agotada || corridor.Derrota)) { e.SuppressKeyPress = true; return; }
        if (EmergencySector && (e.KeyCode == Keys.P || paused && e.KeyCode == Keys.Escape)) { ToggleEmergencyPause(); e.SuppressKeyPress = true; return; }
        if (EmergencyBlocked) { e.SuppressKeyPress = true; return; }
        if (batchEditing)
        {
            if (e.Control && e.KeyCode == Keys.A) batchSelected = true;
            if (e.KeyCode == Keys.Back) { batchText = batchSelected ? "" : batchText.Length > 0 ? batchText[..^1] : ""; batchSelected = false; }
            if (e.KeyCode == Keys.Enter) CommitBatch();
            if (e.KeyCode == Keys.Escape) SetBatch(batchSize);
            e.Handled = true; if (e.KeyCode is Keys.Enter or Keys.Escape or Keys.Back) e.SuppressKeyPress = true; Invalidate(); return;
        }
        if (e.KeyCode == Keys.F1 && animation == 0 && boardingTime == 0 && !naming) { OfferRoomGuide(true); return; }
        if (naming)
        {
            if (e.Control && e.KeyCode == Keys.A) nameSelected = true;
            if (e.KeyCode == Keys.Back && cloneName.Length > 0) { cloneName = nameSelected ? "" : cloneName[..^1]; nameSelected = false; }
            if (e.KeyCode is Keys.Enter or Keys.Escape) naming = false;
            e.Handled = true; return;
        }
        if (e.KeyCode == Keys.Escape && screen == Pantalla.Biblioteca) { CloseLibrary(); return; }
        bool fresh = keys.Add(e.KeyCode);
        if (fresh && e.KeyCode == Keys.E) InteractRoom();
        if (e.KeyCode == Keys.Escape && screen is Pantalla.Laboratorio or Pantalla.Hangar or Pantalla.Abordaje or Pantalla.Poblacion or Pantalla.Reparacion or Pantalla.Comunicaciones) ExitConsole();
        if (e.KeyCode == Keys.Escape && screen == Pantalla.Pasillo) { paused = !paused; keys.Clear(); firing = false; }
        if (e.KeyCode is Keys.Space or Keys.Up or Keys.Down or Keys.Left or Keys.Right) e.SuppressKeyPress = true;
    }
    private void TickGame()
    {
        var now = clock.Elapsed.TotalSeconds; float dt = Math.Clamp((float)(now - previous), 0, .05f); previous = now; time += dt;
        noticeTime = Math.Max(0, noticeTime - dt); explosion = Math.Max(0, explosion - dt);
        repairSpark = Math.Max(0, repairSpark - dt);
        if (progress.LaboratorioReparado && !emergency.Resuelta) emergency.Resolver();
        SyncEmergencyAudio();
        if (EmergencySector && emergency.Agotada) { BeginGameOver(); gameOverTime = Math.Min(GameOverDuration, gameOverTime + dt); Invalidate(); return; }
        if (DialogOpen || rescueVictory || activeGuide != null || EmergencyBlocked) { Invalidate(); return; }
        if (EmergencySector) { emergency.Avanzar(dt); if (emergency.Agotada) { BeginGameOver(); keys.Clear(); firing = false; CancelRepairDrag(); Capture = false; SyncEmergencyAudio(); Invalidate(); return; } }
        TickCommunications(dt);
        TickStation(dt);
        TickBoarding(dt);
        TickBattle(dt);
        if (screen == Pantalla.Pasillo && !paused)
        {
            float x = (keys.Contains(Keys.D) || keys.Contains(Keys.Right) ? 1 : 0) - (keys.Contains(Keys.A) || keys.Contains(Keys.Left) ? 1 : 0);
            float y = (keys.Contains(Keys.S) || keys.Contains(Keys.Down) ? 1 : 0) - (keys.Contains(Keys.W) || keys.Contains(Keys.Up) ? 1 : 0);
            if (x != 0 || y != 0) playerStride += dt * 12;
            corridor.Tick(dt, new(x, y), new(mouse.X - 40 + Camera, mouse.Y - 145), firing || keys.Contains(Keys.Space));
            if (corridor.PuedeReparar && (x > 0 || x == 0 && y != 0) && corridor.Jugador.X >= 2490 && corridor.Jugador.Y >= 155 && corridor.Jugador.Y <= 405) EnterRecoveredLab();
        }
        if (animation > 0)
        {
            animation = Math.Max(0, animation - dt);
            if (animation == 0 && pendingShip != null)
            { int built = pendingFleet.Count; foreach (var nave in pendingFleet) progress.RegistrarNave(nave); pendingFleet.Clear(); fleetIndex = progress.Flota.Count - 1; pendingShip = null; placed.Clear(); blueprints[recipeIndex] = Modelos.Aleatorio(recipeIndex, modelRandom, Blueprint); Save(); ShowSummary(new ResumenOperacion { Titulo = "¡" + built + " naves listas para despegar!", Detalles = new List<string> { "Modelo: " + Recipe.Nombre, "La mesa quedó libre para tu próximo ensamblaje." }.Concat(FleetWaitingLines()).ToList() }); }
        }
        Invalidate();
    }
    private Font FontAt(float size, bool bold = false)
    {
        if (!fonts.TryGetValue((size, bold), out var f)) fonts[(size, bold)] = f = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
        return f;
    }
    private void TextAt(Graphics g, string text, float x, float y, float size = 16, Color? color = null, bool bold = false)
    { using var b = new SolidBrush(color ?? White); g.DrawString(text, FontAt(size, bold), b, x, y); }
    private void Wrap(Graphics g, string text, float x, float y, float w, float h, float size = 16, Color? color = null)
    { using var b = new SolidBrush(color ?? Muted); g.DrawString(text, FontAt(size), b, new RectangleF(x, y, w, h)); }
    private void Button(Graphics g, string text, float x, float y, float w, float h, Action click, bool primary = false, bool enabled = true)
    {
        var rect = new RectangleF(x, y, w, h); bool hover = enabled && rect.Contains(mouse);
        Color bg = !enabled ? Line : primary ? (hover ? MixFur(Mint, White, .18f) : Mint) : hover ? Color.FromArgb(78, 76, 112) : PanelColor;
        Box(g, x, y + 2, w, h, Color.FromArgb(70, 0, 0, 0), Math.Min(16, h / 2));
        Box(g, x, y, w, h, bg, Math.Min(16, h / 2));
        if (enabled) LineTo(g, Color.FromArgb(primary ? 85 : 35, White), x + 16, y + 2, x + w - 16, y + 2, 1);
        var measured = g.MeasureString(text, FontAt(15, true));
        TextAt(g, text, x + (w - measured.Width) / 2, y + (h - measured.Height) / 2, 15, primary && enabled ? Navy : enabled ? White : Muted, true);
        if (enabled) buttons.Add((rect, click));
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.TranslateTransform(Offset.X, Offset.Y); g.ScaleTransform(ScaleFactor, ScaleFactor); buttons.Clear();
        Box(g, 0, 0, 1200, 760, Navy, 0);
        CozyStars(g, time);
        switch (screen)
        {
            case Pantalla.Inicio: DrawStart(g); break;
            case Pantalla.Pasillo: DrawCorridor(g); break;
            case Pantalla.Reparacion: DrawRepair(g); break;
            case Pantalla.Laboratorio: DrawLab(g); break;
            case Pantalla.Hangar: DrawHangar(g); break;
            case Pantalla.Final: DrawFinal(g); break;
            case Pantalla.Abordaje: DrawBoarding(g); break;
            case Pantalla.Biblioteca: DrawLibrary(g); break;
            case Pantalla.Poblacion: DrawPopulation(g); break;
            case Pantalla.Logros: DrawAchievements(g); break;
            case Pantalla.SalaComunicaciones: DrawCommunicationsRoom(g); break;
            case Pantalla.Comunicaciones: DrawCommunications(g); break;
            case Pantalla.Batalla: DrawBattle(g); break;
            case Pantalla.Estacion: case Pantalla.SalaLab: case Pantalla.SalaHangar: DrawStation(g); break;
        }
        if (noticeTime > 0 && screen != Pantalla.Pasillo)
        { Box(g, 190, 698, 820, 43, Line, 10); TextAt(g, notice, 210, 709, 14, Gold); }
        DrawEmergency(g);
        if (activeGuide != null) DrawRoomGuide(g);
        DrawRescueVictory(g);
        DrawDialogs(g);
    }
    private void Header(Graphics g, string stage, string title)
    {
        LineTo(g, Color.FromArgb(60, Blue), 40, 117, 1160, 117, 1);
        TextAt(g, "KEPLER–7", 40, 26, 19, Mint, true); TextAt(g, stage.ToUpperInvariant(), 181, 31, 12, Muted, true);
        TextAt(g, title, 40, 72, 32, White, true);
        if (screen != Pantalla.Logros && !EmergencySector) Button(g, "Logros", 727, 22, 108, 38, OpenAchievements, false, animation == 0 && boardingTime == 0 && transmissionTime == 0);
        TextAt(g, progress.XP + " XP", 850, 32, 16, Gold, true);
        if (CurrentRoomGuide() != null) Button(g, "Ayuda · F1", 933, 22, 107, 38, () => OfferRoomGuide(true), false, animation == 0 && boardingTime == 0);
        Button(g, "Menú", 1050, 22, 110, 38, () => { Save(); Change(Pantalla.Inicio); }, false, animation == 0 && boardingTime == 0 && transmissionTime == 0);
    }
    private void NewGame()
    {
        if ((progress.LaboratorioReparado || progress.XP > 0) && MessageBox.Show("¿Empezar de nuevo? Se reemplazará el progreso de este prototipo.", "Nueva partida", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        inputTitle = null; operationSummary = null; fleetStats = null; boardingSummary = null; libraryCensus = null; libraryRows = null; populationView = null; transmissionIndex = -1; ResetCommunicationsDraft();
        gameOverTime = -1; rescueVictory = false; emergency.Reiniciar(); SetBatch(1); boardingTime = boardingError = 0; boardingPilotIndex = boardingFilter = boardingSpecies = 0; assemblyBatch = boardingBatch = 1; pendingFleet.Clear(); boardingResult = ""; boardingGroup.Clear(); blueprints.Clear(); placed.Clear(); dragging = null; roomCat = new(600, 480); fleetIndex = 0; explosion = 0; progress = new(); population = null; crewLibrary = null; crewPage = 0; corridor = new(); workshop.Reiniciar(); lastClone = null; pendingShip = null; animation = 0; selected.Clear(); recipeIndex = 0; repair = new(); cloneName = "Jefa Misha"; paused = false; Save(); Change(Pantalla.Pasillo);
    }
    private void DrawStart(Graphics g)
    {
        TextAt(g, "R28 · Reservas de ADN ampliadas", 68, 698, 12, Muted);
        TextAt(g, "UNA MISIÓN DE RESCATE FELINA", 65, 65, 13, Mint, true);
        LineTo(g, Color.FromArgb(60, Blue), 40, 117, 1160, 117, 1);
        TextAt(g, "KEPLER–7", 60, 110, 73, White, true); TextAt(g, "Operación Bigotes", 65, 197, 34, Gold, true);
        Wrap(g, "¡La máquina de clonación ha sido comprometida! Los mutantes se multiplican sin control. Eliminá la amenaza y repará la clonadora antes de que sea demasiado tarde.", 68, 273, 510, 120, 20);
        Button(g, "Comenzar misión  →", 68, 405, 270, 57, NewGame, true);
        Button(g, "Continuar", 355, 405, 180, 57, () =>
        { paused = false; Change(progress.LaboratorioReparado ? Pantalla.Estacion : Pantalla.Pasillo); }, false, progress.LaboratorioReparado || corridor.Tiempo > 0);
        Button(g, "Logros y bitácora", 68, 630, 300, 30, OpenAchievements);
        // El nivel 2 se desbloquea sólo después de ganar el nivel 1 .
        if (progress.Lanzada) Button(g, "Nivel 2 · Escolta orbital →", 385, 630, 270, 30, StartBattle, true);
        TextAt(g, "01  RESCATAR     02  CLONAR     03  CONSTRUIR", 68, 503, 13, Muted, true);
        Wrap(g, "Rescatá la estación. Reuní a tu tripulación.\n¡Llevá a tus michis hasta las estrellas!", 68, 563, 505, 63, 16);
        if (progress.MedallaRescate is { } medal) TextAt(g, "RESCATE · " + medal.Nivel.ToUpperInvariant() + " · " + RescueTime(medal.TiempoSegundos), 68, 660, 17, MedalColor(medal.Nivel), true);
        if (progress.FlyweightDominado) TextAt(g, "★ FLYWEIGHT · Censo recuperado", 775, 669, 15, Gold, true);
        TextAt(g, saveNotice, 68, 690, 12, Muted);
        Ellipse(g, PanelColor, 685, 140, 430, 430);
        Yarn(g, 1090, 612, 1.15f, Red); Fish(g, 680, 538, .95f, Gold); Paw(g, 630, 200, .8f, Mint);
        for (int i = 0; i < 3; i++) { using var p = new Pen(Line, 1); g.DrawEllipse(p, 673 - i * 15, 128 - i * 15, 454 + i * 30, 454 + i * 30); }
        Cat(g, 880, 352 + MathF.Sin(time * 1.4f) * 6, 3.9f, Mint, clock: time, hero: true);
        Cat(g, 1075, 482, 1.2f, Red, true); Ship(g, 750, 185, .54f, new RecetaExploradora().Componentes, Blue);
        Box(g, 775, 592, 275, 55, PanelColor); TextAt(g, "ESTACIÓN EN EMERGENCIA", 794, 609, 14, Gold, true);
    }
    private void DrawCorridor(Graphics g)
    {
        TextAt(g, "KEPLER–7 / CONTENCIÓN · ACCESO A LAB BIO-01", 40, 25, 14, Mint, true);
        TextAt(g, "Recuperá la clonadora", 40, 55, 28, White, true);
        TextAt(g, "SALUD", 610, 35, 12, Muted, true);
        Box(g, 610, 58, 245, 18, PanelColor, 8);
        if (corridor.Vida > 0) Box(g, 610, 58, 245 * corridor.Vida / 100f, 18, corridor.Vida > 50 ? Mint : corridor.Vida > 25 ? Gold : Red, 8);
        TextAt(g, corridor.Vida + " / 100", 869, 55, 18, White, true);
        TextAt(g, corridor.Bajas + " / 15 mutantes", 992, 57, 16, Gold, true);
        TextAt(g, "WASD / flechas: mover     Mouse: apuntar     Clic / Espacio: disparar     Esc: pausa · F1: ayuda", 40, 109, 14, Muted);
        var gs = g.Save(); g.SetClip(new RectangleF(40, 145, 1120, 565)); g.TranslateTransform(40 - Camera, 145);
        DrawBattleInterior(g);
        foreach (var o in corridor.Obstaculos) DrawBattleObstacle(g, o);
        foreach (var med in corridor.Botiquines) DrawMedkit(g, med);
        Box(g, 2510, 155, 135, 250, Line, 16); Box(g, 2521, 171, 113, 216, Navy, 8);
        LineTo(g, Mint, 2578, 182, 2578, 365, 4); Paw(g, 2578, 264, .85f, Gold);
        TextAt(g, "LAB BIO-01", 2516, 374, 13, Gold, true);
        Ellipse(g, MathF.Sin(time * 5) > 0 ? Red : Gold, 2556, 139, 35, 10);
        foreach (var m in corridor.Mutantes)
        {
            Cat(g, m.Posicion.X, m.Posicion.Y, .72f, m.Impacto > 0 ? White : Red, true, MathF.Atan2(corridor.Jugador.Y - m.Posicion.Y, corridor.Jugador.X - m.Posicion.X), "Atigrado", time, time * 7, decay: m.VarianteVisual);
            if (m.Vida == 1) Box(g, m.Posicion.X - 14, m.Posicion.Y - 40, 28, 4, Red, 2);
        }
        foreach (var b in corridor.Disparos)
        { Ellipse(g, Color.FromArgb(50, Mint), b.Posicion.X - 10, b.Posicion.Y - 10, 20, 20); Ellipse(g, Mint, b.Posicion.X - 4, b.Posicion.Y - 4, 8, 8); }
        if (corridor.Invulnerable <= 0 || ((int)(time * 14) % 2 == 0)) Cat(g, corridor.Jugador.X, corridor.Jugador.Y, .82f, Mint, false, MathF.Atan2(mouse.Y - 145 - corridor.Jugador.Y, mouse.X - 40 + Camera - corridor.Jugador.X), "Blanco", time, keys.Any(k => k is Keys.W or Keys.A or Keys.S or Keys.D or Keys.Up or Keys.Down or Keys.Left or Keys.Right) ? playerStride : 0, hero: true);
        var aim = new Vector2(mouse.X - 40 + Camera, mouse.Y - 145) - corridor.Jugador;
        if (aim.LengthSquared() > 0)
        { aim = Vector2.Normalize(aim); LineTo(g, Blue, corridor.Jugador.X + aim.X * 21, corridor.Jugador.Y + aim.Y * 21, corridor.Jugador.X + aim.X * 37, corridor.Jugador.Y + aim.Y * 37, 8); }
        foreach (var p in corridor.Particulas) Ellipse(g, p.Danio ? Red : Mint, p.Posicion.X, p.Posicion.Y, 5, 5);
        g.Restore(gs);
        if (corridor.Invulnerable > .9f) { using var p = new Pen(Red, 6); g.DrawRectangle(p, 42, 147, 1116, 561); }
        TextAt(g, corridor.Bajas == 15 ? "Pasillo despejado · Avanzá a la derecha hasta la puerta del laboratorio." : "Neutralizá a los mutantes y avanzá hacia el laboratorio →", 40, 726, 14, Gold);
        if (corridor.PuedeReparar && !paused) TextAt(g, "Caminá hacia la puerta para entrar →", 420, 651, 19, Gold, true);
        if (!paused && !corridor.Derrota)
        { LineTo(g, White, mouse.X - 9, mouse.Y, mouse.X + 9, mouse.Y); LineTo(g, White, mouse.X, mouse.Y - 9, mouse.X, mouse.Y + 9); }
        if ((paused || corridor.Derrota) && !EmergencySector)
        {
            Box(g, 40, 145, 1120, 565, Color.FromArgb(205, Navy), 0); buttons.Clear();
            Box(g, 360, 250, 480, 290, PanelColor); TextAt(g, corridor.Derrota ? "Sin señal vital" : "Misión en pausa", 410, 286, 32, corridor.Derrota ? Red : White, true);
            Wrap(g, corridor.Derrota ? "Los mutantes te alcanzaron. Reintentá el pasillo con la salud completa." : "Tomate un respiro. La estación puede esperar.", 410, 340, 375, 75, 18);
            Button(g, corridor.Derrota ? "Reintentar" : "Continuar", 405, 434, 175, 49, () => { if (corridor.Derrota) corridor = new(); paused = false; }, true);
            Button(g, "Menú", 600, 434, 175, 49, () => Change(Pantalla.Inicio));
        }
    }
    private void Nav(Graphics g, bool lab)
    {
        Button(g, "← Volver a la habitación", 40, 127, 245, 41, ExitConsole, false, animation == 0);
        TextAt(g, "FLOTA " + progress.Flota.Count + "     PILOTOS " + progress.Pilotos.Count, 803, 140, 13, Muted, true);
    }

    private void DrawFinal(Graphics g)
    {

        void Centrado(string texto, float y, float size, Color color, bool bold = false)
        {
            var medida = g.MeasureString(texto, FontAt(size, bold));
            TextAt(g, texto, (1200 - medida.Width) / 2, y, size, color, bold);
        }
        Centrado("NIVEL 1 COMPLETADO", 50, 15, Mint, true);
        Centrado("¡Ganaste! Kepler-7 vuelve a tener futuro", 86, 40, White, true);
        Centrado("Laboratorio recuperado. Tripulación lista. Flota en marcha.", 158, 18, Muted);
        for (int i = 0; i < Math.Min(3, progress.Flota.Count); i++)
        {
            var n = progress.Flota[i]; float x = 78 + i * 374;
            Box(g, x, 215, 350, 333, PanelColor); DrawCraft(g, x + 175, 357 + MathF.Sin(time * 1.8f + i) * 8, .85f, n.Piezas.Count > 0 ? n.Piezas : Modelos.Para(Array.FindIndex(RecetaNave.Todas, r => r.Nombre == n.Tipo)));
            TextAt(g, n.Tipo, x + 23, 463, 23, White, true); TextAt(g, n.Piloto?.Nombre ?? "", x + 23, 505, 16, Muted);
        }
        Centrado(progress.Flota.Count + " naves · " + progress.Pilotos.Count + " pilotos", 568, 17, Muted);
        Centrado(" Nueva amenaza en el radar: naves mutantes interceptan la flota", 602, 20, Gold, true);
        Button(g, "Pasar al Nivel 2  →", 205, 655, 260, 54, StartBattle, true);
        Button(g, "Volver a la estación", 485, 655, 250, 54, () => Change(Pantalla.Estacion));
        Button(g, "Menú principal", 755, 655, 240, 54, () => Change(Pantalla.Inicio));
    }
}
