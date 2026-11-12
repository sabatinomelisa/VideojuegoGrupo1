using System.Drawing.Drawing2D;
using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;

// Nivel 2  Escolta orbital: pantalla de la batalla por turnos.
//   - STRATEGY: botones "HACKEAR IA" del Comandante (DrawBattleCommand) = opción para cambiar
//     la estrategia durante la partida; color de la cúpula (IAColor) y líneas punteadas del
//     radar (DrawTelegraphs) = efecto observable del cambio.
//   - TEMPLATE METHOD: la bitácora (DrawBattleLog) muestra cada etapa del turno; el botón de
//     acción (ExecuteAllyTurn) dispara el método plantilla EjecutarTurno().
public sealed partial class GameForm
{
    // ---------- Estado de la pantalla ----------
    private BatallaOrbital? battle;
    private UnidadBatalla? battleTarget;     
    private IEstrategiaIA? battleHack;       
    private TurnoUnidad? battleTurnShown;    
    private float enemyDelay;                
    private float battleShake;               
    private float battleBanner;              
    private string battleBannerText = "";
    private Color battleBannerColor;
    private bool battleLogOpen;              
    private bool battleBriefing;             
    private bool battleRewarded;             
    private string battleRewardText = "";    

    // Puntería  y bloqueo (turno mutante)
    private bool aiming;                     
    private float aimClock;                  
    private Enemigo? chargingEnemy;          
    private float chargeTime;                
    private float blockFactor = -1;          
    private bool spaceHeld;                  
    private string battleCall = "";          
    private Color battleCallColor;
    private float battleCallTime;
    private const float VelocidadAguja = 1.25f, DuracionCarga = 1.3f, RadioBloqueo = 30;

    // Furia y eventos sorpresa
    private readonly HashSet<Enemigo> furiasMostradas = new();   
    private int eventoMostradoEnRonda;                           
    private bool meteorActive;                                   
    private float meteorDelay;                                   
    private float capsuleClock;                                 

    // Efectos visuales
    private sealed class EfectoBatalla { public PointF Desde, Hasta; public Color Color; public float Tiempo = .6f; }
    private sealed class TextoFlotante { public PointF Pos; public string Texto = ""; public Color Color; public float Tiempo = 1.3f; }
    private sealed class Particula { public PointF Pos, Vel; public Color Color; public float Tiempo; }
    private readonly List<EfectoBatalla> battleEffects = new();
    private readonly List<TextoFlotante> battleTexts = new();
    private readonly List<Particula> battleParticles = new();
    private readonly Dictionary<UnidadBatalla, float> battleHits = new();
    private readonly Random battleFx = new();

    private static readonly Color Violeta = Color.FromArgb(200, 160, 255);
    private static readonly Color[] ColoresAliados = { Red, Blue, Mint, Gold };
    private static readonly Componente[][] PartesAliados =
    {
        new RecetaCombate().Componentes,
        new RecetaExploradora().Componentes,
        new RecetaCarga().Componentes,
        new[] { Componente.Casco, Componente.Motor, Componente.Escudo, Componente.Radar, Componente.InteligenciaArtificial }
    };

    private static readonly PointF[] PosAliados = { new(300, 195), new(130, 305), new(300, 415), new(130, 525) };
    private static readonly PointF[] PosEnemigos = { new(900, 215), new(1030, 365), new(900, 515) };

    private void StartBattle()
    {
        battle = BatallaOrbital.Crear(progress);
        battleTarget = null; battleHack = null; battleTurnShown = null;
        battleEffects.Clear(); battleTexts.Clear(); battleParticles.Clear(); battleHits.Clear();
        enemyDelay = 0; battleShake = 0; battleBanner = 0; battleLogOpen = false;
        aiming = false; chargingEnemy = null; blockFactor = -1; battleCallTime = 0;
        battleBriefing = true;  
        battleRewarded = false; battleRewardText = "";
        furiasMostradas.Clear(); eventoMostradoEnRonda = 0; meteorActive = false; capsuleClock = 0;
        Change(Pantalla.Batalla);
    }

    // [PATRÓN STRATEGY  Efecto observable] Color con el que se muestra cada IA.
    private static Color IAColor(IEstrategiaIA ia) => ia switch
    {
        IAAgresiva => Danger,
        IADefensiva => Blue,
        IAAleatoria => Violeta,
        _ => Gold
    };

    private PointF BattlePos(UnidadBatalla u)
    {
        int i = battle!.Contexto.Aliados.IndexOf(u);
        if (i >= 0) return PosAliados[i];
        return PosEnemigos[battle.Contexto.Enemigos.FindIndex(e => e == u)];
    }

    
    private void SyncBattleTurn()
    {
        var turno = battle!.TurnoActual;
        if (!ReferenceEquals(turno, battleTurnShown))
        {
            battleTurnShown = turno; battleHack = null;
            if (turno is TurnoMedico) battleTarget = battle.Contexto.AliadosVivos.MinBy(a => (float)a.Vida / a.VidaMax);
        }
        bool valido = turno is TurnoMedico
            ? battleTarget is { Viva: true } && battle.Contexto.Aliados.Contains(battleTarget)
            : battleTarget is Enemigo { Viva: true };
        if (!valido) battleTarget = turno is TurnoMedico ? battle.Contexto.AliadosVivos.FirstOrDefault() : battle.Contexto.EnemigosVivos.FirstOrDefault();
    }

    // ---------- Efectos ----------

    private void Float(UnidadBatalla u, string texto, Color color)
    {
        var p = BattlePos(u);
        battleTexts.Add(new TextoFlotante { Pos = new(p.X, p.Y - 55), Texto = texto, Color = color });
    }

    private void Explode(PointF p)
    {
        for (int k = 0; k < 28; k++)
        {
            double ang = battleFx.NextDouble() * Math.PI * 2, vel = 60 + battleFx.NextDouble() * 160;
            battleParticles.Add(new Particula
            {
                Pos = p,
                Vel = new((float)(Math.Cos(ang) * vel), (float)(Math.Sin(ang) * vel)),
                Color = k % 3 == 0 ? White : k % 2 == 0 ? Gold : Danger,
                Tiempo = .5f + (float)battleFx.NextDouble() * .5f
            });
        }
    }

    // Foto de la vida y el escudo de todas las naves, para comparar antes y después de una acción.
    private Dictionary<UnidadBatalla, (int Vida, bool Escudo)> BattleSnapshot() =>
        battle!.Contexto.Aliados.Concat(battle.Contexto.Enemigos).ToDictionary(u => u, u => (u.Vida, u.Escudo));

    // Muestra lo que cambió: daño, reparación, escudos y naves destruidas.
    private void BattleFeedback(Dictionary<UnidadBatalla, (int Vida, bool Escudo)> antes)
    {
        foreach (var (u, previo) in antes)
        {
            int diferencia = u.Vida - previo.Vida;
            if (diferencia < 0)
            {
                Float(u, diferencia.ToString(), Danger);
                battleHits[u] = .35f;
                if (battle!.Contexto.Aliados.Contains(u)) battleShake = .25f;
            }
            if (diferencia > 0) Float(u, "+" + diferencia, Mint);
            if (u.Escudo && !previo.Escudo) Float(u, "ESCUDO", Blue);
            if (previo.Vida > 0 && !u.Viva) Explode(BattlePos(u));
        }
    }

    private void Call(string texto, Color color) { battleCall = texto; battleCallColor = color; battleCallTime = 1.1f; }
    private void ShowBanner(string texto, Color color, float duracion = 1.2f) { battleBannerText = texto; battleBannerColor = color; battleBanner = duracion; }


    private PointF CapsulePos => new(600 + 150 * MathF.Sin(capsuleClock * 1.1f), 330 + 105 * MathF.Sin(capsuleClock * 1.7f));
    private float AimPos => 1 - Math.Abs(aimClock * VelocidadAguja % 2 - 1);
    private float ChargeRadius => 120 - 110 * Math.Min(1, chargeTime / DuracionCarga);
    private bool NeedsAim(TurnoUnidad turno) =>
        !(turno is TurnoFrancotirador f && !f.Cargado) && !(turno is TurnoComandante && battleHack != null);

    // ---------- Acciones ----------

    private void OnActionButton()
    {
        var turno = battle?.TurnoActual;
        if (turno == null || battleEffects.Count > 0) return;
        if (NeedsAim(turno)) { aiming = true; aimClock = 0; }
        else ExecuteAllyTurn(1f);
    }

    // Frena la aguja: según dónde quedó, el disparo es débil, bueno o crítico.
    private void StopAim()
    {
        if (!aiming) return;
        aiming = false;
        float distancia = Math.Abs(AimPos - .5f);
        float precision = distancia <= .06f ? 2f : distancia <= .2f ? 1f : .5f;
        if (precision == 2f) Call("¡CRÍTICO!", Mint);
        else if (precision == 1f) Call("¡Bien!", Gold);
        else Call("Débil…", Danger);
        ExecuteAllyTurn(precision);
    }

    // Intento de bloqueo: según que tan cerca está el anillo del círculo blanco.
    private void TryBlock()
    {
        if ((chargingEnemy == null && !meteorActive) || blockFactor >= 0) return;
        float distancia = Math.Abs(ChargeRadius - RadioBloqueo);
        blockFactor = distancia <= 9 ? .25f : distancia <= 22 ? .5f : 1f;
        if (blockFactor == .25f) Call("¡BLOQUEO PERFECTO!", Mint);
        else if (blockFactor == .5f) Call("¡Bloqueo!", Blue);
        else Call(ChargeRadius > RadioBloqueo ? "Muy pronto…" : "Tarde…", Danger);
    }

    // Clic en la cápsula: repara a la nave más dañada.
    private void GrabCapsule()
    {
        var desde = CapsulePos;
        var antes = BattleSnapshot();
        var herida = battle?.RecogerCapsula();
        if (herida == null) return;
        battleEffects.Add(new EfectoBatalla { Desde = desde, Hasta = BattlePos(herida), Color = Mint });
        BattleFeedback(antes);
        Call("¡Cápsula recuperada!", Mint);
    }

    // Le pasa al núcleo lo que eligió el jugador y la puntería que logró.
    private void ExecuteAllyTurn(float precision)
    {
        var turno = battle?.TurnoActual;
        if (turno == null || battleEffects.Count > 0) return;
        var actor = turno.Unidad;
        var hackeo = turno is TurnoComandante ? battleHack : null;
        var antes = BattleSnapshot();
        battle!.JugarTurnoAliado(battleTarget, hackeo, precision);   // [PATRÓN TEMPLATE METHOD] adentro se llama a EjecutarTurno()

        if (turno.Objetivo != null)
        {
            var color = turno is TurnoMedico ? Mint : hackeo != null ? IAColor(hackeo) : ColoresAliados[battle.Contexto.Aliados.IndexOf(actor)];
            battleEffects.Add(new EfectoBatalla { Desde = BattlePos(actor), Hasta = BattlePos(turno.Objetivo), Color = color });
            if (hackeo != null) Float(turno.Objetivo, "IA " + hackeo.Nombre.ToUpperInvariant(), IAColor(hackeo));
        }
        else Float(actor, "RECARGANDO", Gold);
        BattleFeedback(antes);

        battleHack = null;
        enemyDelay = 1.1f;
        if (battle.FaseEnemiga) ShowBanner("TURNO MUTANTE", Danger, 1f);
    }

    private void TickBattle(float dt)
    {
        if (screen != Pantalla.Batalla || battle == null) return;
        // Se guardan en el progreso y aparecen en el panel de Logros.
        if (battle.CambiosDeEstrategia > 0 && progress.RegistrarHackeo()) { Save(); Call("¡Nueva insignia! +100 XP", Violeta); }
        if (battle.Victoria && !battleRewarded)
        {
            battleRewarded = true;
            bool primera = progress.RegistrarVictoriaNivel2(battle.Medalla!);
            battleRewardText = primera ? "¡Nueva insignia en Logros! +200 XP" : "Tu mejor medalla: " + progress.MedallaBatalla!.Nivel;
            Save();
        }
        foreach (var e in battleEffects) e.Tiempo -= dt;
        battleEffects.RemoveAll(e => e.Tiempo <= 0);
        foreach (var t in battleTexts) { t.Tiempo -= dt; t.Pos = new(t.Pos.X, t.Pos.Y - 35 * dt); }
        battleTexts.RemoveAll(t => t.Tiempo <= 0);
        foreach (var p in battleParticles) { p.Tiempo -= dt; p.Pos = new(p.Pos.X + p.Vel.X * dt, p.Pos.Y + p.Vel.Y * dt); p.Vel = new(p.Vel.X * .95f, p.Vel.Y * .95f); }
        battleParticles.RemoveAll(p => p.Tiempo <= 0);
        foreach (var u in battleHits.Keys.ToList()) { battleHits[u] -= dt; if (battleHits[u] <= 0) battleHits.Remove(u); }
        battleShake = Math.Max(0, battleShake - dt);
        battleBanner = Math.Max(0, battleBanner - dt);

        battleCallTime = Math.Max(0, battleCallTime - dt);
        bool espacio = keys.Contains(Keys.Space), apretado = espacio && !spaceHeld;
        spaceHeld = espacio;
        if (battleBriefing) { if (apretado || keys.Contains(Keys.Enter)) battleBriefing = false; return; }
        if (aiming) { aimClock += dt; if (apretado) StopAim(); return; }
        if (battle.CapsulaDisponible) capsuleClock += dt;

        // Se anuncia la primera vez que una nave se enfurece.
        foreach (var e in battle.Contexto.Enemigos.Where(e => e.Furiosa && furiasMostradas.Add(e)))
        {
            ShowBanner("¡" + e.Nombre.ToUpperInvariant() + " SE ENFURECE!", Danger, 1.6f);
            Call("IA → AGRESIVA · DISPARA ×2", Danger);
            battleShake = .5f;
        }

        // Evento sorpresa
        if (battle.EventoRonda != EventoEspacial.Ninguno && eventoMostradoEnRonda != battle.Ronda && !battle.FaseEnemiga)
        {
            eventoMostradoEnRonda = battle.Ronda;
            if (battle.EventoRonda == EventoEspacial.Meteoritos) { ShowBanner("¡LLUVIA DE METEORITOS!", Gold, 1.4f); meteorDelay = 1.4f; }
            if (battle.EventoRonda == EventoEspacial.Capsula) { ShowBanner("CÁPSULA DE REPARACIÓN A LA DERIVA", Mint, 1.4f); capsuleClock = 0; }
            if (battle.EventoRonda == EventoEspacial.Tormenta) ShowBanner("¡TORMENTA DE IONES! TODAS LAS IA EN ALEATORIA", Violeta, 1.8f);
        }

        // Lluvia de meteoritos
        if (battle.MeteoritosPendientes > 0 && !battle.Terminada)
        {
            if (!meteorActive)
            {
                meteorDelay -= dt;
                if (meteorDelay > 0) return;
                meteorActive = true; chargeTime = 0; blockFactor = -1;
                return;
            }
            chargeTime += dt;
            if (apretado) TryBlock();
            if (chargeTime < DuracionCarga) return;
            var golpeada = battle.MeteoritoApunta!;
            var previo = BattleSnapshot();
            battle.ImpactarMeteorito(blockFactor < 0 ? 1f : blockFactor);
            battleEffects.Add(new EfectoBatalla { Desde = BattlePos(golpeada), Hasta = BattlePos(golpeada), Color = Gold });
            BattleFeedback(previo);
            meteorActive = false; meteorDelay = .45f;
            return;
        }

        if (!battle.FaseEnemiga || battle.Terminada) return;
        if (chargingEnemy == null)
        {
            enemyDelay -= dt;
            if (enemyDelay > 0) return;
            chargingEnemy = battle.ProximoEnemigo;   
            chargeTime = 0; blockFactor = -1;
            if (chargingEnemy == null) battle.EjecutarSiguienteEnemigo();
            return;
        }
        chargeTime += dt;
        if (apretado) TryBlock();
        if (chargeTime < DuracionCarga) return;

        
        var acciones = battle.TurnosEnemigos.Select(t => t.UltimaAccion).ToArray();
        var antes = BattleSnapshot();
        battle.EjecutarSiguienteEnemigo(blockFactor < 0 ? 1f : blockFactor);
        for (int i = 0; i < acciones.Length; i++)
        {
            var accion = battle.TurnosEnemigos[i].UltimaAccion;
            if (accion == null || ReferenceEquals(accion, acciones[i])) continue;
            var enemigo = (Enemigo)battle.TurnosEnemigos[i].Unidad;
            var desde = BattlePos(enemigo);
            battleEffects.Add(new EfectoBatalla { Desde = desde, Hasta = accion.Tipo == TipoAccion.Escudo ? desde : BattlePos(accion.Objetivo!), Color = accion.Tipo == TipoAccion.Escudo ? Blue : IAColor(enemigo.Estrategia) });
        }
        BattleFeedback(antes);
        chargingEnemy = null;
        enemyDelay = .6f;
    }

    // ---------- Dibujo ----------

    private void DrawBattle(Graphics g)
    {
        battle ??= BatallaOrbital.Crear(progress);
        SyncBattleTurn();
        DrawSpace(g);
        if (battle.EventoRonda == EventoEspacial.Tormenta) DrawStorm(g);

        // Todo el campo de batalla tiembla un poco cuando golpean a tu flota.
        var estado = g.Save();
        if (battleShake > 0) g.TranslateTransform(MathF.Sin(time * 90) * 24 * battleShake, MathF.Cos(time * 70) * 16 * battleShake);
        DrawTelegraphs(g);
        for (int i = 0; i < battle.Contexto.Aliados.Count; i++) DrawBattleAlly(g, i);
        for (int j = 0; j < battle.Contexto.Enemigos.Count; j++) DrawBattleEnemy(g, j);
        if (battle.CapsulaDisponible && !aiming) DrawCapsule(g);
        DrawBattleEffects(g);
        DrawCharge(g);
        g.Restore(estado);

        DrawBattleHud(g);
        DrawTurnTracker(g);
        DrawBattleLog(g);
        DrawBattleCommand(g);
        if (battleBanner > 0) DrawBanner(g);
        if (battleCallTime > 0) DrawCall(g);
        // Mientras apuntás o bloqueás, un clic en cualquier lado cuenta como ESPACIO.
        var pantalla = new RectangleF(0, 0, 1200, 760);
        if (aiming) { buttons.Clear(); buttons.Add((pantalla, StopAim)); }
        else if ((chargingEnemy != null || meteorActive) && !battle.Terminada) { buttons.Clear(); buttons.Add((pantalla, TryBlock)); }
        if (battleBriefing) { DrawBriefing(g); return; }
        if (battle.Terminada) DrawBattleEnd(g);
    }

    // Fondo
    private void DrawSpace(Graphics g)
    {
        Ellipse(g, Color.FromArgb(22, Violeta), -160, 60, 700, 420);
        Ellipse(g, Color.FromArgb(18, Blue), 640, 140, 700, 460);
        Ellipse(g, Color.FromArgb(70, 60, 110), 330, 440, 540, 540);
        Ellipse(g, Color.FromArgb(85, 74, 132), 360, 452, 480, 480);
        for (int k = 0; k < 4; k++) Ellipse(g, Color.FromArgb(25, Gold), 380 + k * 20, 520 + k * 34, 440 - k * 40, 16);
        using (var anillo = new Pen(Color.FromArgb(70, Gold), 3)) g.DrawEllipse(anillo, 250, 560, 700, 90);
        for (int k = 0; k < 70; k++)
        {
            float velocidad = 20 + k % 5 * 18;
            float x = ((k * 197.3f) - time * velocidad) % 1240; if (x < 0) x += 1240;
            float y = (k * 83.7f) % 600 + 20;
            LineTo(g, Color.FromArgb(40 + k % 4 * 30, White), x - 20, y, x - 20 + velocidad * .12f, y, 1.2f);
        }
    }

    private void CenterText(Graphics g, string texto, float cx, float y, float size, Color color, bool bold = false)
    {
        var medida = g.MeasureString(texto, FontAt(size, bold));
        TextAt(g, texto, cx - medida.Width / 2, y, size, color, bold);
    }

    // Barra de vida chica
    private void DrawMiniBar(Graphics g, UnidadBatalla u, float cx, float y)
    {
        float pct = (float)u.Vida / u.VidaMax;
        Box(g, cx - 50, y, 100, 9, Color.FromArgb(200, Navy), 4);
        Box(g, cx - 50, y, Math.Max(8, 100 * pct), 9, pct > .5f ? Mint : pct > .25f ? Gold : Danger, 4);
        TextAt(g, u.Vida.ToString(), cx + 56, y - 4, 12, u.Escudo ? Blue : Muted, true);
    }

    private void DrawReticle(Graphics g, PointF p, float r, Color color)
    {
        using var pen = new Pen(color, 2.5f);
        var estado = g.Save();
        g.TranslateTransform(p.X, p.Y); g.RotateTransform(time * 70);
        g.DrawEllipse(pen, -r, -r, r * 2, r * 2);
        for (int k = 0; k < 4; k++) { g.RotateTransform(90); g.DrawLine(pen, r - 8, 0, r + 10, 0); }
        g.Restore(estado);
    }

    private void DrawBattleAlly(Graphics g, int i)
    {
        var u = battle!.Contexto.Aliados[i];
        var p = PosAliados[i];
        var zona = new RectangleF(p.X - 75, p.Y - 65, 150, 135);
        bool activo = battle.TurnoActual?.Unidad == u;
        bool clickeable = battle.TurnoActual is TurnoMedico && u.Viva && !battle.Terminada;

        if (!u.Viva)
        {
            for (int k = 0; k < 4; k++) Ellipse(g, Line, p.X - 30 + k * 17, p.Y - 8 + (k % 2) * 14, 12, 9);
            CenterText(g, u.Nombre, p.X, p.Y + 40, 13, Muted, true);
            CenterText(g, "DESTRUIDA", p.X, p.Y + 58, 11, Danger, true);
            return;
        }

        float bob = MathF.Sin(time * 1.6f + i) * 4;
        float shake = battleHits.TryGetValue(u, out var h) ? MathF.Sin(time * 70) * 14 * h : 0;
        if (activo)
        {
            Ellipse(g, Color.FromArgb(45 + (int)(25 * MathF.Sin(time * 4)), Mint), p.X - 70, p.Y - 58, 140, 116);
            float salto = MathF.Sin(time * 6) * 5;
            Poly(g, Mint, new PointF(p.X - 11, p.Y - 84 + salto), new PointF(p.X + 11, p.Y - 84 + salto), new PointF(p.X, p.Y - 70 + salto));
        }
        float largo = 30 + 10 * MathF.Sin(time * 22 + i);
        Poly(g, Color.FromArgb(120, Gold), new PointF(p.X - 44 + shake, p.Y - 9 + bob), new PointF(p.X - 44 - largo + shake, p.Y + bob), new PointF(p.X - 44 + shake, p.Y + 9 + bob));
        Poly(g, Color.FromArgb(200, White), new PointF(p.X - 44 + shake, p.Y - 4 + bob), new PointF(p.X - 56 - largo * .4f + shake, p.Y + bob), new PointF(p.X - 44 + shake, p.Y + 4 + bob));
        var estado = g.Save();
        g.TranslateTransform(p.X + shake, p.Y + bob); g.RotateTransform(90);
        Ship(g, 0, 0, .45f, PartesAliados[i], ColoresAliados[i]);
        g.Restore(estado);
        if (u.Escudo) { using var pen = new Pen(Color.FromArgb(170, Blue), 3); g.DrawEllipse(pen, p.X - 62, p.Y - 58, 124, 116); }

        CenterText(g, u.Nombre + (u == battle.Contexto.NaveCarga ? " ★" : ""), p.X, p.Y + 46, 13, White, true);
        DrawMiniBar(g, u, p.X, p.Y + 66);

        if (clickeable && u == battleTarget) DrawReticle(g, p, 62, Gold);
        else if (clickeable && zona.Contains(mouse)) DrawReticle(g, p, 62, Color.FromArgb(120, White));
        if (clickeable) buttons.Add((zona, () => battleTarget = u));
    }

    private void DrawBattleEnemy(Graphics g, int j)
    {
        var e = battle!.Contexto.Enemigos[j];
        var p = PosEnemigos[j];
        var zona = new RectangleF(p.X - 85, p.Y - 85, 170, 170);
        bool clickeable = battle.TurnoActual != null && battle.TurnoActual is not TurnoMedico && e.Viva && !battle.Terminada;

        if (!e.Viva)
        {
            for (int k = 0; k < 5; k++) Ellipse(g, Line, p.X - 40 + k * 18, p.Y + (k % 2) * 12, 14, 10);
            CenterText(g, e.Nombre, p.X, p.Y + 46, 13, Muted, true);
            CenterText(g, "DESTRUIDA", p.X, p.Y + 64, 11, Danger, true);
            return;
        }

        var color = IAColor(e.Estrategia);
        float bob = MathF.Sin(time * 1.3f + j * 2) * 5;
        float shake = battleHits.TryGetValue(e, out var h) ? MathF.Sin(time * 70) * 14 * h : 0;
        if (e.Furiosa) shake += MathF.Sin(time * 45) * 2;          
        float x = p.X + shake, y = p.Y + bob;
        if (e.Furiosa)
        {
            float pulso = 8 * MathF.Sin(time * 6);
            Ellipse(g, Color.FromArgb(60, Danger), x - 100 - pulso, y - 86 - pulso, 200 + pulso * 2, 174 + pulso * 2);
        }

        // [PATRÓN STRATEGY  Efecto observable] 
        Ellipse(g, Color.FromArgb(35 + (int)(20 * MathF.Sin(time * 3 + j)), color), x - 88, y - 74, 176, 150);
        Ellipse(g, Color.FromArgb(70, color), x - 44, y - 62, 88, 88);
        Cat(g, x, y - 14, .55f, Red, true, MathF.PI, "Atigrado", time, 0, decay: j);
        using (var cupula = new Pen(Color.FromArgb(220, color), 2.5f)) g.DrawEllipse(cupula, x - 44, y - 62, 88, 88);
        Ellipse(g, Color.FromArgb(60, White), x - 28, y - 54, 26, 12);
        Ellipse(g, Line, x - 80, y + 6, 160, 38);
        Ellipse(g, PanelColor, x - 72, y + 10, 144, 24);
        for (int k = 0; k < 6; k++)
            Ellipse(g, ((int)(time * 4) + k) % 3 == 0 ? color : Muted, x - 62 + k * 23, y + 18, 8, 8);
        if (e.Escudo) { using var pen = new Pen(Color.FromArgb(180, Blue), 3); g.DrawEllipse(pen, x - 82, y - 78, 164, 140); }

        
        string etiqueta = "IA " + e.Estrategia.Nombre.ToUpperInvariant() + (e.Furiosa ? " · FURIA ×2" : "");
        var medida = g.MeasureString(etiqueta, FontAt(11, true));
        Box(g, p.X - medida.Width / 2 - 8, p.Y - 96, medida.Width + 16, 22, Color.FromArgb(200, Navy), 11);
        CenterText(g, etiqueta, p.X, p.Y - 93, 11, color, true);

        CenterText(g, e.Nombre, p.X, p.Y + 50, 13, White, true);
        DrawMiniBar(g, e, p.X, p.Y + 70);

        if (clickeable && e == battleTarget) DrawReticle(g, new(x, y - 10), 64, Gold);
        else if (clickeable && zona.Contains(mouse)) DrawReticle(g, new(x, y - 10), 64, Color.FromArgb(120, White));
        if (clickeable) buttons.Add((zona, () => battleTarget = e));
    }

    // [PATRÓN STRATEGY  Efecto observable] 
 
    private void DrawTelegraphs(Graphics g)
    {
        if (battle!.Terminada || battle.FaseEnemiga || !battle.Contexto.Aliados[1].Viva) return;
        for (int j = 0; j < battle.Contexto.Enemigos.Count; j++)
        {
            var e = battle.Contexto.Enemigos[j];
            if (!e.Viva) continue;
            var p = PosEnemigos[j];
            var color = IAColor(e.Estrategia);
            if (!e.Estrategia.Predecible) { CenterText(g, "?", p.X - 105, p.Y - 30, 30, color, true); continue; }
            var accion = e.Estrategia.DecidirAccion(e, battle.Contexto);   
            if (accion.Tipo != TipoAccion.Atacar || accion.Objetivo == null) continue;
            var q = BattlePos(accion.Objetivo);
            using var pen = new Pen(Color.FromArgb(110, color), 2) { DashStyle = DashStyle.Dash };
            float x1 = p.X - 90, x2 = q.X + 70;
            g.DrawLine(pen, x1, p.Y, x2, q.Y);
            if (e.Furiosa) CenterText(g, "×2", (x1 + x2) / 2, (p.Y + q.Y) / 2 - 22, 16, Danger, true);
            var dir = new PointF(x2 - x1, q.Y - p.Y);
            float largo = MathF.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
            float ux = dir.X / largo, uy = dir.Y / largo;
            Poly(g, Color.FromArgb(170, color), new PointF(x2, q.Y), new PointF(x2 - ux * 14 - uy * 7, q.Y - uy * 14 + ux * 7), new PointF(x2 - ux * 14 + uy * 7, q.Y - uy * 14 - ux * 7));
        }
    }

    private void DrawBattleEffects(Graphics g)
    {
        foreach (var ef in battleEffects)
        {
            float t = 1 - ef.Tiempo / .6f;   
            int alpha = (int)(255 * (1 - t));
            if (ef.Desde == ef.Hasta)
            {
                using var pen = new Pen(Color.FromArgb(alpha, ef.Color), 4);
                float r = 40 + 50 * t;
                g.DrawEllipse(pen, ef.Desde.X - r, ef.Desde.Y - r, r * 2, r * 2);
                continue;
            }
            LineTo(g, Color.FromArgb(alpha / 2, ef.Color), ef.Desde.X, ef.Desde.Y, ef.Hasta.X, ef.Hasta.Y, 4);
            float k = Math.Min(1, t * 1.8f);
            float x = ef.Desde.X + (ef.Hasta.X - ef.Desde.X) * k, y = ef.Desde.Y + (ef.Hasta.Y - ef.Desde.Y) * k;
            Ellipse(g, Color.FromArgb(alpha, ef.Color), x - 12, y - 12, 24, 24);
            Ellipse(g, Color.FromArgb(alpha, White), x - 5, y - 5, 10, 10);
            if (k >= 1) Sparkle(g, ef.Hasta.X, ef.Hasta.Y, 18 + 16 * t, Color.FromArgb(alpha, ef.Color));
        }
        foreach (var p in battleParticles) Ellipse(g, Color.FromArgb((int)(255 * Math.Min(1, p.Tiempo * 2)), p.Color), p.Pos.X - 3, p.Pos.Y - 3, 6, 6);
        foreach (var t in battleTexts)
        {
            int alpha = (int)(255 * Math.Min(1, t.Tiempo * 1.5f));
            CenterText(g, t.Texto, t.Pos.X + 2, t.Pos.Y + 2, 22, Color.FromArgb(alpha, Navy), true);
            CenterText(g, t.Texto, t.Pos.X, t.Pos.Y, 22, Color.FromArgb(alpha, t.Color), true);
        }
    }

    // Barra superior
    private void DrawBattleHud(Graphics g)
    {
        TextAt(g, "KEPLER–7", 32, 18, 19, Mint, true);
        TextAt(g, "NIVEL 2 · ESCOLTA ORBITAL", 34, 44, 11, Muted, true);
        Button(g, "Ayuda", 700, 18, 100, 36, () => battleBriefing = true);
        Button(g, "Logros", 812, 18, 100, 36, OpenAchievements);
        TextAt(g, progress.XP + " XP", 930, 26, 16, Gold, true);
        Button(g, "Menú", 1050, 18, 110, 36, () => { Save(); Change(Pantalla.Inicio); });
    }

    // Orden de turnos de la ronda
    private void DrawTurnTracker(Graphics g)
    {
        int actual = battle!.TurnoActual == null ? 4 : battle.TurnosAliados.IndexOf(battle.TurnoActual);
        string[] nombres = { "Ronda " + battle.Ronda + "/" + BatallaOrbital.RondasMaximas, "Soldado", "Francotirador", "Médico", "Comandante", "Mutantes" };
        float ancho = 116, x0 = (1200 - (ancho * 6 + 8 * 5)) / 2;
        for (int k = 0; k < 6; k++)
        {
            float x = x0 + k * (ancho + 8);
            Color fondo = PanelColor, texto = White;
            if (k == 0) { fondo = Color.FromArgb(60, Gold); texto = Gold; }
            else if (k <= 4)
            {
                var u = battle.TurnosAliados[k - 1].Unidad;
                if (!u.Viva) { fondo = Color.FromArgb(120, Line); texto = Line; }
                else if (k - 1 == actual && !battle.Terminada) { fondo = Mint; texto = Navy; }
                else if (k - 1 < actual) texto = Muted;
            }
            else if (battle.FaseEnemiga) { fondo = Danger; texto = White; }
            Box(g, x, 66, ancho, 28, fondo, 14);
            CenterText(g, nombres[k], x + ancho / 2, 72, 12, texto, true);
        }
    }

    // [PATRÓN TEMPLATE METHOD  Historial visible] Bitácora con cada etapa del turno. 
    private void DrawBattleLog(Graphics g)
    {
        int cantidad = battleLogOpen ? 12 : 1;
        var lineas = battle!.Historial.TakeLast(cantidad).ToList();
        float alto = 30 + cantidad * 16;
        var zona = new RectangleF(395, 104, 410, alto);
        Box(g, zona.X, zona.Y, zona.Width, zona.Height, Color.FromArgb(battleLogOpen ? 235 : 150, Navy), 12);
        TextAt(g, "BITÁCORA DE TURNOS", 410, 110, 11, Mint, true);
        TextAt(g, battleLogOpen ? "cerrar" : "ver todo", 745, 110, 11, Muted, true);
        for (int i = 0; i < lineas.Count; i++)
        {
            var l = lineas[i];
            Color c = l.Contains("enfurece") ? Danger : l.Contains("Evento") || l.Contains("tormenta") ? Violeta : l.Contains("Nueva ronda") ? Blue : l.Contains("Decide") ? Gold : l.Contains("Actúa") ? White : l.Contains("Fin") ? Mint : Muted;
            TextAt(g, ShortLabel(l, 62), 410, 128 + i * 16, 11, c);
        }
        buttons.Add((zona, () => battleLogOpen = !battleLogOpen));
    }

    // Botón de color para elegir la IA a hackear.
    private void IAChip(Graphics g, string texto, float x, float y, Color color, bool elegida, Action click)
    {
        var zona = new RectangleF(x, y, 108, 30);
        bool hover = zona.Contains(mouse);
        Box(g, x, y, 108, 30, elegida ? color : hover ? Color.FromArgb(80, color) : Color.FromArgb(40, color), 15);
        CenterText(g, texto, x + 54, y + 7, 12, elegida ? Navy : color, true);
        buttons.Add((zona, click));
    }

    // Barra de comando inferior: quién juega, a quién apunta y el botón de acción.
    private void DrawBattleCommand(Graphics g)
    {
        Box(g, 24, 616, 1152, 132, Color.FromArgb(235, PanelColor), 22);
        if (battle!.Terminada) return;
        var turno = battle.TurnoActual;
        if (battle.MeteoritosPendientes > 0)
        {
            TextAt(g, "EVENTO · LLUVIA DE METEORITOS · QUEDAN " + battle.MeteoritosPendientes, 48, 632, 12, Gold, true);
            TextAt(g, "¡Bloqueá con ESPACIO!", 48, 652, 22, White, true);
            if (!string.IsNullOrEmpty(battle.UltimoResultado)) TextAt(g, battle.UltimoResultado, 48, 692, 16, Gold, true);
            return;
        }
        if (turno == null)
        {
            TextAt(g, "TURNO MUTANTE" + (chargingEnemy != null ? " · " + chargingEnemy.Nombre.ToUpperInvariant() + " CARGA SU DISPARO" : ""), 48, 632, 12, Danger, true);
            TextAt(g, chargingEnemy != null ? "¡Bloqueá con ESPACIO!" : "Los mutantes se preparan…", 48, 652, 22, White, true);
            if (!string.IsNullOrEmpty(battle.UltimoResultado)) TextAt(g, battle.UltimoResultado, 48, 692, 16, Gold, true);
            return;
        }

        int i = battle.Contexto.Aliados.IndexOf(turno.Unidad);
      
        Ellipse(g, Color.FromArgb(55, ColoresAliados[i]), 42, 636, 92, 92);
        var estado = g.Save();
        g.TranslateTransform(88, 682); g.RotateTransform(90);
        Ship(g, 0, 0, .3f, PartesAliados[i], ColoresAliados[i]);
        g.Restore(estado);
        TextAt(g, turno.Unidad.Rol.ToUpperInvariant(), 152, 642, 12, ColoresAliados[i], true);
        TextAt(g, turno.Unidad.Nombre, 152, 660, 24, White, true);
        TextAt(g, turno.Unidad.Piloto, 152, 694, 13, Muted);
        bool recargando = turno is TurnoFrancotirador f && !f.Cargado;
        if (aiming) { DrawAimBar(g); return; }

        if (turno is TurnoComandante)
        {
            // [PATRÓN STRATEGY  Opción de la interfaz para cambiar la estrategia durante la partida]
            TextAt(g, "HACKEAR IA", 568, 630, 12, Violeta, true);
            IAChip(g, "Láser", 568, 652, Muted, battleHack == null, () => battleHack = null);
            var todas = Estrategias.Todas;
            for (int k = 0; k < todas.Length; k++)
            {
                var ia = todas[k];
                float x = 568 + (k + 1) % 3 * 116, y = 652 + (k + 1) / 3 * 40;
                IAChip(g, ia.Nombre, x, y, IAColor(ia), battleHack?.Nombre == ia.Nombre, () => battleHack = ia);
            }
        }
        else
        {
            TextAt(g, recargando ? "ESTE TURNO" : "OBJETIVO", 568, 650, 12, Muted, true);
            TextAt(g, recargando ? "Recarga el cañón" : battleTarget?.Nombre ?? "—", 568, 668, 24, Gold, true);
        }

        string verbo = turno switch
        {
            TurnoSoldado => "¡Disparar!",
            TurnoFrancotirador when !recargando => "¡Disparo de precisión!",
            TurnoFrancotirador => "Recargar",
            TurnoMedico => "Reparar",
            _ => battleHack != null ? "Hackear IA" : "Disparar láser"
        };
        Button(g, verbo, 930, 640, 226, 86, OnActionButton, true, battleEffects.Count == 0);
    }

    // Barra de puntería
    private void DrawAimBar(Graphics g)
    {
        TextAt(g, "¡FRENÁ LA AGUJA EN EL VERDE!", 568, 630, 13, Gold, true);
        float x = 568, y = 664, w = 588, h = 34;
        Box(g, x, y, w, h, Color.FromArgb(150, Danger), 10);           
        Box(g, x + w * .30f, y, w * .40f, h, Gold, 0);                 
        Box(g, x + w * .44f, y, w * .12f, h, Mint, 0);                  
        float aguja = x + w * AimPos;
        Box(g, aguja - 4, y - 10, 8, h + 20, White, 3);
        TextAt(g, "débil ×½", x + 6, y + 44, 12, Muted, true);
        CenterText(g, "bien ×1", x + w * .37f, y + 44, 12, Gold, true);
        CenterText(g, "¡crítico! ×2", x + w * .5f, y + 44, 12, Mint, true);
        CenterText(g, "bien ×1", x + w * .63f, y + 44, 12, Gold, true);
        TextAt(g, "débil ×½", x + w - 62, y + 44, 12, Muted, true);
    }

    // Carga del disparo mutante
    private void DrawCharge(Graphics g)
    {
        if (meteorActive && battle!.MeteoritoApunta != null) { DrawMeteor(g, BattlePos(battle.MeteoritoApunta)); return; }
        if (chargingEnemy == null) return;
        var p = BattlePos(chargingEnemy);
        var color = IAColor(chargingEnemy.Estrategia);
        float cx = p.X, cy = p.Y - 10;
        using (var guia = new Pen(Color.FromArgb(230, White), 3) { DashStyle = DashStyle.Dash })
            g.DrawEllipse(guia, cx - RadioBloqueo, cy - RadioBloqueo, RadioBloqueo * 2, RadioBloqueo * 2);
        float r = ChargeRadius;
        var colorAnillo = blockFactor < 0 ? Gold : blockFactor < 1 ? Mint : Danger;   
        using (var anillo = new Pen(colorAnillo, 5))
            g.DrawEllipse(anillo, cx - r, cy - r, r * 2, r * 2);
        float carga = Math.Min(1, chargeTime / DuracionCarga);
        Ellipse(g, Color.FromArgb((int)(200 * carga), color), p.X - 92 - 16 * carga, p.Y + 20 - 16 * carga, 32 * carga, 32 * carga);
    }

    // Cartel grande del resultado
    private void DrawCall(Graphics g)
    {
        int alpha = (int)(255 * Math.Min(1, battleCallTime * 2));
        float y = battleBanner > 0 ? 392 : 290;   
        CenterText(g, battleCall, 602, y + 2, 38, Color.FromArgb(alpha, Navy), true);
        CenterText(g, battleCall, 600, y, 38, Color.FromArgb(alpha, battleCallColor), true);
    }

    private void DrawBanner(Graphics g)
    {
        int alpha = (int)(255 * Math.Min(1, battleBanner * 2));
        Box(g, 0, 300, 1200, 76, Color.FromArgb(alpha * 2 / 3, Navy), 0);
        CenterText(g, battleBannerText, 600, 314, 34, Color.FromArgb(alpha, battleBannerColor), true);
    }

    // Meteorito que cae sobre una nave aliada, con el anillo para bloquearlo.
    private void DrawMeteor(Graphics g, PointF destino)
    {
        float t = Math.Min(1, chargeTime / DuracionCarga);
        float x = destino.X + 420 * (1 - t), y = destino.Y - 330 * (1 - t);
        for (int k = 1; k <= 5; k++)
            Ellipse(g, Color.FromArgb(150 - k * 25, k % 2 == 0 ? Gold : Danger), x + k * 14 - 9, y - k * 11 - 9, 18 - k * 2, 18 - k * 2);
        Ellipse(g, Line, x - 17, y - 17, 34, 34);
        Ellipse(g, Muted, x - 12, y - 13, 12, 10);
        Ellipse(g, Navy, x + 2, y - 2, 9, 8);
        using (var guia = new Pen(Color.FromArgb(230, White), 3) { DashStyle = DashStyle.Dash })
            g.DrawEllipse(guia, destino.X - RadioBloqueo, destino.Y - RadioBloqueo, RadioBloqueo * 2, RadioBloqueo * 2);
        float r = ChargeRadius;
        var colorAnillo = blockFactor < 0 ? Gold : blockFactor < 1 ? Mint : Danger;
        using var anillo = new Pen(colorAnillo, 5);
        g.DrawEllipse(anillo, destino.X - r, destino.Y - r, r * 2, r * 2);
    }

    // Cápsula de reparación
    private void DrawCapsule(Graphics g)
    {
        var p = CapsulePos;
        float pulso = 6 * MathF.Sin(time * 5);
        Ellipse(g, Color.FromArgb(50, Mint), p.X - 40 - pulso, p.Y - 40 - pulso, 80 + pulso * 2, 80 + pulso * 2);
        Box(g, p.X - 28, p.Y - 15, 56, 30, Mint, 15);
        Box(g, p.X - 28, p.Y - 15, 28, 30, White, 15);
        Box(g, p.X - 3, p.Y - 9, 6, 18, Danger, 2);
        Box(g, p.X - 9, p.Y - 3, 18, 6, Danger, 2);
        CenterText(g, "+" + BatallaOrbital.ReparacionCapsula + " · ¡clic!", p.X, p.Y + 22, 12, Mint, true);
        var zona = new RectangleF(p.X - 42, p.Y - 42, 84, 84);
        if (zona.Contains(mouse)) DrawReticle(g, p, 40, Mint);
        buttons.Add((zona, GrabCapsule));
    }

    // Tormenta de iones
    private void DrawStorm(Graphics g)
    {
        Box(g, 0, 0, 1200, 760, Color.FromArgb(22, Violeta), 0);
        int destello = (int)(time * 3) % 4;
        if (destello < 3 && battle!.Contexto.Enemigos[destello].Viva && (time * 3) % 1 < .25f)
        {
            var p = PosEnemigos[destello];
            var puntos = new List<PointF>();
            for (int k = 0; k <= 6; k++) puntos.Add(new PointF(p.X + (k % 2 == 0 ? -14 : 14) + MathF.Sin(k * 7 + time) * 6, p.Y - 220 + k * 30));
            using var rayo = new Pen(Color.FromArgb(220, Violeta), 3);
            g.DrawLines(rayo, puntos.ToArray());
        }
    }

    // Pantalla de misión: explica el juego antes de empezar.
    private void DrawBriefing(Graphics g)
    {
        buttons.Clear();
        Box(g, 0, 0, 1200, 760, Color.FromArgb(245, Navy), 0);
        CenterText(g, "MISIÓN · ESCOLTA ORBITAL", 600, 30, 14, Mint, true);
        CenterText(g, "¡Protegé la Nave de Carga!", 600, 50, 38, White, true);
        CenterText(g, "Las naves mutantes que escaparon de la clonadora interceptan tu flota.", 600, 102, 16, Muted);

        string[] titulos = { "1 · OBJETIVO", "2 · APUNTÁ", "3 · BLOQUEÁ", "4 · HACKEÁ LA IA" };
        Color[] colores = { Gold, Mint, Blue, Violeta };
        string[] textos =
        {
            "Destruí a los 3 mutantes en " + BatallaOrbital.RondasMaximas + " rondas. Si cae la Nave de Carga ★, perdés.",
            "Clic en un objetivo, tocá el botón y frená la aguja en el verde. El centro es ¡crítico!",
            "Cuando un mutante carga su disparo, apretá ESPACIO justo cuando el anillo toca el círculo.",
            ""
        };
        for (int k = 0; k < 4; k++)
        {
            float x = 48 + k * 282, y = 134;
            Box(g, x, y, 264, 300, PanelColor, 18);
            TextAt(g, titulos[k], x + 20, y + 16, 15, colores[k], true);
            float cx = x + 132, cy = y + 104;
            switch (k)
            {
                case 0:   // la Nave de Carga a salvo
                    var estado = g.Save(); g.TranslateTransform(cx, cy); g.RotateTransform(90);
                    Ship(g, 0, 0, .5f, PartesAliados[2], ColoresAliados[2]); g.Restore(estado);
                    CenterText(g, "★", cx + 56, cy - 60, 24, Gold, true);
                    break;
                case 1:   // barra de puntería
                    Box(g, x + 24, cy - 14, 216, 28, Color.FromArgb(150, Danger), 8);
                    Box(g, x + 24 + 216 * .30f, cy - 14, 216 * .40f, 28, Gold, 0);
                    Box(g, x + 24 + 216 * .44f, cy - 14, 216 * .12f, 28, Mint, 0);
                    Box(g, x + 24 + 216 * (.5f + .25f * MathF.Sin(time * 3)) - 3, cy - 24, 6, 48, White, 3);
                    CenterText(g, "¡CRÍTICO! ×2", cx, cy + 32, 14, Mint, true);
                    break;
                case 2:   // anillo que se cierra
                    float r = 28 + 34 * (.5f + .5f * MathF.Sin(time * 2.5f));
                    using (var guia = new Pen(White, 3) { DashStyle = DashStyle.Dash }) g.DrawEllipse(guia, cx - 28, cy - 28, 56, 56);
                    using (var anillo = new Pen(Gold, 5)) g.DrawEllipse(anillo, cx - r, cy - r, r * 2, r * 2);
                    CenterText(g, "ESPACIO", cx, cy - 9, 14, White, true);
                    break;
                case 3:   // qué hace cada IA
                    string[] efectos = { "remata al más débil", "se escuda, pega poco", "ataca al azar", "va por la Carga ★" };
                    var ias = Estrategias.Todas;
                    for (int n = 0; n < ias.Length; n++)
                    {
                        float fy = y + 48 + n * 30;
                        Box(g, x + 18, fy, 98, 24, Color.FromArgb(70, IAColor(ias[n])), 12);
                        CenterText(g, ias[n].Nombre, x + 67, fy + 5, 11, IAColor(ias[n]), true);
                        TextAt(g, efectos[n], x + 124, fy + 4, 12, White);
                    }
                    break;
            }
            if (k < 3) Wrap(g, textos[k], x + 20, y + 178, 226, 110, 16, White);
            else
            {
                Wrap(g, "Cada mutante decide según su IA. Con el Comandante elegí otra IA y hacé clic en un mutante: cambia cómo ataca.", x + 20, y + 178, 226, 80, 14, White);
                TextAt(g, "Tip: pasá la Nodriza a Defensiva.", x + 20, y + 262, 13, Gold, true);
            }
        }

        // Sorpresas que pueden aparecer durante la batalla
        CenterText(g, "SORPRESAS EN EL CAMINO", 600, 446, 13, Gold, true);
        string[] sorpresas = { "Furia de la Nodriza", "Lluvia de meteoritos", "Cápsula de reparación", "Tormenta de iones" };
        string[] detalles =
        {
            "Con media vida pasa sola a IA Agresiva y dispara dos veces.",
            "Caen 3 meteoritos: bloqueá cada uno con ESPACIO.",
            "Flota en tu turno: hacele clic y repara " + BatallaOrbital.ReparacionCapsula + ".",
            "Durante una ronda, todas las IA se vuelven Aleatorias."
        };
        Color[] tonos = { Danger, Gold, Mint, Violeta };
        for (int k = 0; k < 4; k++)
        {
            float x = 48 + k * 282, y = 470;
            Box(g, x, y, 264, 96, PanelColor, 16);
            float ix = x + 36, iy = y + 48;
            Ellipse(g, Color.FromArgb(60, tonos[k]), ix - 24, iy - 24, 48, 48);
            switch (k)
            {
                case 0: CenterText(g, "!", ix, iy - 15, 24, Danger, true); break;
                case 1:
                    for (int t = 1; t <= 3; t++) Ellipse(g, Color.FromArgb(160 - t * 40, Gold), ix + t * 7 - 4, iy - t * 6 - 4, 9 - t, 9 - t);
                    Ellipse(g, Muted, ix - 9, iy - 7, 16, 16); break;
                case 2:
                    Box(g, ix - 16, iy - 8, 32, 16, Mint, 8); Box(g, ix - 16, iy - 8, 16, 16, White, 8);
                    Box(g, ix - 2, iy - 6, 4, 12, Danger, 1); Box(g, ix - 6, iy - 2, 12, 4, Danger, 1); break;
                case 3:
                    using (var rayo = new Pen(Violeta, 3))
                        g.DrawLines(rayo, new[] { new PointF(ix + 4, iy - 16), new PointF(ix - 6, iy), new PointF(ix + 4, iy), new PointF(ix - 4, iy + 16) });
                    break;
            }
            TextAt(g, sorpresas[k], x + 70, y + 14, 14, tonos[k], true);
            Wrap(g, detalles[k], x + 70, y + 36, 184, 56, 13, White);
        }

        Button(g, "¡A jugar!", 470, 590, 260, 58, () => battleBriefing = false, true);
        CenterText(g, "o apretá ESPACIO", 600, 656, 13, Muted);
    }

    // Cartel final de victoria o derrota.
    private void DrawBattleEnd(Graphics g)
    {
        buttons.Clear();
        Box(g, 0, 0, 1200, 760, Color.FromArgb(205, Navy), 0);
        Box(g, 300, 170, 600, 420, PanelColor, 22);
        if (battle!.Victoria)
        {
            var medalla = battle.Medalla!;
            for (int k = 0; k < 10; k++) Sparkle(g, 320 + k * 62, 138 + MathF.Sin(time * 2 + k) * 12, 10 + k % 3 * 4, k % 2 == 0 ? Gold : Mint);
            TextAt(g, "NIVEL 2 COMPLETADO", 345, 205, 14, Mint, true);
            TextAt(g, "¡Victoria!", 345, 230, 44, Gold, true);
            Paw(g, 820, 265, .9f, MedalColor(medalla.Nivel));
            TextAt(g, "Medalla de " + medalla.Nivel + " · " + medalla.Rondas + " rondas", 345, 305, 22, MedalColor(medalla.Nivel), true);
            Wrap(g, "La flota escoltó la Nave de Carga y las reservas de ADN llegaron a salvo. Las naves mutantes fueron neutralizadas.", 345, 350, 510, 70, 17, White);
            TextAt(g, "IA hackeadas: " + battle.CambiosDeEstrategia, 345, 430, 16, Violeta, true);
            TextAt(g, battleRewardText, 345, 458, 16, Gold, true);
            Button(g, "Volver a la estación", 345, 505, 250, 54, () => Change(Pantalla.Estacion), true);
            Button(g, "Jugar de nuevo", 610, 505, 245, 54, StartBattle);
        }
        else
        {
            TextAt(g, "MISIÓN FALLIDA", 345, 205, 14, Danger, true);
            TextAt(g, "La flota cayó", 345, 230, 44, Red, true);
            string motivo = battle.Ronda > BatallaOrbital.RondasMaximas
                ? "Pasaron " + BatallaOrbital.RondasMaximas + " rondas y llegaron los refuerzos mutantes. Apuntá a la zona verde para hacer críticos."
                : !battle.Contexto.NaveCarga.Viva
                    ? "Destruyeron la Nave de Carga con las reservas de ADN. Bloqueá con ESPACIO y hackeá la IA de la Nodriza: la Calculadora siempre va por ella."
                    : "Las naves mutantes destruyeron a toda la flota.";
            Wrap(g, motivo, 345, 310, 510, 90, 17, White);
            Button(g, "Reintentar", 345, 505, 250, 54, StartBattle, true);
            Button(g, "Volver a la estación", 610, 505, 245, 54, () => Change(Pantalla.Estacion));
        }
    }
}
