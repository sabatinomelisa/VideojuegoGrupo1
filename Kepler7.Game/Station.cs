using System.Numerics;
using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;

public sealed partial class GameForm
{
    private readonly Random modelRandom = new();
    private readonly Dictionary<int,List<Pieza>> blueprints = new();
    private List<Pieza> Blueprint => blueprints.TryGetValue(recipeIndex,out var p) ? p : blueprints[recipeIndex] = Modelos.Aleatorio(recipeIndex,modelRandom);
    private float roomFacing = MathF.PI/2, roomStride;
    private Vector2 roomCat = new(600, 480);
    private Pieza? dragging;
    private readonly Dictionary<Componente, Pieza> placed = new();
    private readonly List<(RectangleF Rect, Pieza Part)> trays = new();
    private int fleetIndex, trayPage;
    private int assemblyBatch=1;
    private readonly List<Nave> pendingFleet=new();
    private bool Walking => screen is Pantalla.Estacion or Pantalla.SalaLab or Pantalla.SalaHangar or Pantalla.SalaComunicaciones;
    private bool WorkshopReady => animation == 0 && explosion == 0 && workshop.Vidas > 0;
    private readonly RectangleF assembly = new(370, 272, 390, 287);
    private void ExitConsole()
    {
        if (animation > 0 || boardingTime > 0 || transmissionTime>0) return;
        if(screen==Pantalla.Comunicaciones){roomCat=new(600,410);Change(Pantalla.SalaComunicaciones);return;}
        roomCat = screen == Pantalla.Abordaje ? new(1030,375) : screen == Pantalla.Poblacion ? new(1030,420) : new(600,410);
        Change(screen is Pantalla.Laboratorio or Pantalla.Poblacion or Pantalla.Reparacion ? Pantalla.SalaLab : Pantalla.SalaHangar);
    }
    private void TickStation(float dt)
    {
        if (!Walking) return;
        var v = new Vector2((keys.Contains(Keys.D) || keys.Contains(Keys.Right) ? 1 : 0) - (keys.Contains(Keys.A) || keys.Contains(Keys.Left) ? 1 : 0),
            (keys.Contains(Keys.S) || keys.Contains(Keys.Down) ? 1 : 0) - (keys.Contains(Keys.W) || keys.Contains(Keys.Up) ? 1 : 0));
        if (v.LengthSquared() > 1) v = Vector2.Normalize(v);
        if(v.LengthSquared()>0) { roomFacing = MathF.Atan2(v.Y,v.X); roomStride += dt*11; }
        var next=Vector2.Clamp(roomCat+v*dt*255,new(85,310),new(1115,610));
        // La consola de población ocupa espacio físico; acercarse desde el frente.
        var console=new Caja(923,291,211,99);
        var machine=new Caja(475,179,250,146);
        var radioConsole=new Caja(452,274,296,63);
        if((screen!=Pantalla.SalaLab || (!console.Toca(new(next.X,roomCat.Y),18) && !machine.Toca(new(next.X,roomCat.Y),18))) && (screen!=Pantalla.SalaComunicaciones || !radioConsole.Toca(new(next.X,roomCat.Y),18)))roomCat.X=next.X;
        if((screen!=Pantalla.SalaLab || (!console.Toca(new(roomCat.X,next.Y),18) && !machine.Toca(new(roomCat.X,next.Y),18))) && (screen!=Pantalla.SalaComunicaciones || !radioConsole.Toca(new(roomCat.X,next.Y),18)))roomCat.Y=next.Y;
        WalkThroughDoor(v);
    }
    private void WalkThroughDoor(Vector2 movement)
    {
        if(screen==Pantalla.Estacion && movement.Y<0 && roomCat.Y<=332)
        {
            if(Math.Abs(roomCat.X-250)<=78){roomCat=new(600,540);Change(Pantalla.SalaLab);}
            else if(Math.Abs(roomCat.X-600)<=78){roomCat=new(600,540);Change(Pantalla.SalaComunicaciones);}
            else if(Math.Abs(roomCat.X-950)<=78 && progress.Ingeniero!=null){roomCat=new(600,540);Change(Pantalla.SalaHangar);}
        }
        else if(screen==Pantalla.SalaHangar && movement.Y<0 && roomCat.Y<=332 && Math.Abs(roomCat.X-1030)<=78 && progress.Flota.Count>0)
        {
            Change(Pantalla.Abordaje);
        }
        else if(screen is Pantalla.SalaLab or Pantalla.SalaHangar or Pantalla.SalaComunicaciones && movement.Y>0 && roomCat.Y>=598 && Math.Abs(roomCat.X-600)<=78)
        {
            if(screen==Pantalla.SalaLab && !progress.LaboratorioReparado){Change(Pantalla.Pasillo);return;}
            float x=screen==Pantalla.SalaLab?250:screen==Pantalla.SalaComunicaciones?600:950;roomCat=new(x,420);Change(Pantalla.Estacion);
        }
    }
    private string RoomAction()
    {
        if (screen == Pantalla.Estacion)
        {
            if (Vector2.Distance(roomCat, new(250, 325)) < 115) return "Caminá hacia la puerta del laboratorio";
            if (Vector2.Distance(roomCat,new(600,325))<115)return "Caminá hacia la puerta de comunicaciones";
            if (Vector2.Distance(roomCat, new(950, 325)) < 115) return progress.Ingeniero == null ? "Falta clonar al ingeniero en el laboratorio" : "Caminá hacia la puerta del hangar";
        }
        else if (Walking)
        {
            if(screen==Pantalla.SalaComunicaciones && Vector2.Distance(roomCat,new(600,325))<110)return "E · Recuperar la señal de la Tierra";
            if(screen==Pantalla.SalaLab && Vector2.Distance(roomCat,new(1030,350))<105) return "E · Centro de comandos · Consultar población";
            if(screen==Pantalla.SalaHangar && Vector2.Distance(roomCat,new(1030,350))<105) return progress.Flota.Count>0 ? "Caminá hacia la puerta del sector abordaje" : "Construí una nave para habilitar el abordaje";
            if (Vector2.Distance(roomCat, new(600, 325)) < 110) return "E · Usar " + (screen == Pantalla.SalaLab ? (progress.LaboratorioReparado ? "clonadora" : "panel de reparación") : "mesa de ensamblaje");
            if (Vector2.Distance(roomCat, new(600, 610)) < 90) return "Caminá hacia la salida";
        }
        return "WASD / flechas: caminar · Puertas automáticas · E: usar máquinas · F1: ayuda";
    }
    private void InteractRoom()
    {
        if (!Walking) return;
        if(screen==Pantalla.Estacion)return;
        if(screen==Pantalla.SalaComunicaciones){if(Vector2.Distance(roomCat,new(600,325))<110)Change(Pantalla.Comunicaciones);return;}
        if(screen==Pantalla.SalaLab && Vector2.Distance(roomCat,new(1030,350))<105) {if(progress.LaboratorioReparado)OpenPopulation();else Say("Restaurá la energía de la clonadora para activar el censo.");}
        else if (Vector2.Distance(roomCat, new(600, 325)) < 110) Change(screen == Pantalla.SalaLab ? (progress.LaboratorioReparado ? Pantalla.Laboratorio : Pantalla.Reparacion) : Pantalla.Hangar);

    }
    private void DrawStation(Graphics g)
    {
        bool hub = screen == Pantalla.Estacion, lab = screen == Pantalla.SalaLab;
        if(lab){DrawUnifiedLab(g);return;}
        Header(g, "Estación transitable", hub ? "Pasillo central · Elegí una habitación" : lab ? "Laboratorio de clones" : "Hangar de la flota");
        CozyRoom(g,hub?Blue:Gold);
        if (hub)
        {
            foreach (var door in new[] { (250, "LABORATORIO", Mint), (600, "COMUNICACIONES", Blue), (950, "HANGAR", Gold) })
            {
                CozyDoor(g,door.Item1,184,door.Item3);
                TextAt(g, door.Item2, door.Item1 - 88, 165, 14, door.Item3, true);
                Ellipse(g, door.Item3, door.Item1 + 64, 253, 8, 8);
            }
            TextAt(g,"↑ CONTACTO CON LA TIERRA",460,355,16,Blue,true);
            TextAt(g, "← CLONACIÓN", 365, 390, 17, Mint, true); TextAt(g, "CONSTRUCCIÓN →", 665, 390, 17, Gold, true);
        }
        else
        {
            Box(g, 463, 196, 274, 106, Line, 12);
            if (lab) { Ellipse(g, Color.FromArgb(45, Mint), 540, 178, 120, 110); Cat(g, 600, 228, .85f, Mint); }
            else DrawCraft(g, 600, 238, .48f, Blueprint);
            TextAt(g, lab ? "MATRIZ DE CLONACIÓN" : "MESA DE ENSAMBLAJE", 493, 171, 15, lab ? Mint : Gold, true);
            for (int i = 0; i < 3; i++)
            {
                Box(g, 110 + i * 65, 201, 44, 77, Line, 5);
                Ellipse(g, lab ? Mint : Gold, 121 + i * 65, 219, 22, 35);
            }
            TextAt(g, lab ? progress.Pilotos.Count + " pilotos creados" : progress.Flota.Count + " naves construidas", 798, 364, 16, White);
            Box(g, 514, 620, 172, 25, Mint, 3); TextAt(g, "SALIDA", 568, 621, 15, Navy, true);
        }
        DecorateFelineRoom(g, hub, lab);
        if(!hub && !lab)
        {
            Color doorColor=progress.Flota.Count>0?Gold:Muted;
            BoardingDoor(g,1030,184,doorColor);
            TextAt(g,"SECTOR ABORDAJE",937,165,14,doorColor,true);
            TextAt(g,progress.Flota.Count>0?"↑ ENTRAR":"CONSTRUÍ UNA NAVE",954,348,13,doorColor,true);
        }
        if(lab) DrawPopulationConsole(g);
        if(!hub)DrawRoomIndicators(g,lab);
        Cat(g, roomCat.X, roomCat.Y, .92f, Mint, false, roomFacing, "Blanco", time, keys.Count > 0 ? roomStride : 0, hero:true);
        TextAt(g, RoomAction(), 100, 670, 19, Gold, true);
    }
    private bool StartDrag()
    {
        if (!WorkshopReady) return false;
        var tile = trays.LastOrDefault(t => t.Rect.Contains(mouse));
        if (tile.Part != null) {  dragging = tile.Part; return true; }
        if (assembly.Contains(mouse))
        {
            foreach (var c in placed.Keys.Reverse().ToArray())
            {
                var p = PartPosition(c); var r = new RectangleF(565 + p.X * .82f - 34, 411 + p.Y * .82f - 34, 68, 68);
                if (r.Contains(mouse)) { dragging = placed[c]; placed.Remove(c); return true; }
            }
        }
        return false;
    }
    private void DropPart()
    {
        if (screen == Pantalla.Hangar && WorkshopReady && dragging != null && assembly.Contains(mouse)) placed[dragging.Componente] = dragging;
        dragging = null;
    }
    private static PointF PartPosition(Componente c) => ToyPosition(c);
    private void DrawCraft(Graphics g, float x, float y, float scale, IEnumerable<Pieza> parts)
    {
        var allParts = parts.ToArray();
        if (allParts.Any(p => p.Componente == Componente.Casco))
            foreach(var part in allParts) ToyMount(g,x,y,scale,part.Componente,Uniforme(part.Color));
        foreach (var p in allParts.OrderBy(p => p.Componente == Componente.Casco ? 0 : 1))
        { var o = PartPosition(p.Componente); DrawPart(g, x + o.X * scale, y + o.Y * scale, scale, p); }
    }
    private void DrawPart(Graphics g, float x, float y, float scale, Pieza p)
        => ToyPart(g, x, y, scale, p.Componente, p.Variante, Uniforme(p.Color));
    private void DrawHangar(Graphics g)
    {
        Header(g, "Hangar de la flota", "¡Construí tu próxima aventura!"); Nav(g, false);
        for (int i = 0; i < 3; i++)
        { int n = i; Button(g, RecetaNave.Todas[i].Nombre, 40 + i * 155, 183, 145, 39, () => { recipeIndex = n; placed.Clear();  }, recipeIndex == i, animation == 0 && explosion == 0); }
        TextAt(g, "VIDAS   " + new string('♥', workshop.Vidas) + new string('♡', 3 - workshop.Vidas), 535, 192, 21, Red, true);
        Box(g, 40, 240, 310, 365, PanelColor); Box(g, 365, 240, 405, 365, PanelColor); Box(g, 785, 183, 375, 422, PanelColor);
        TextAt(g, "MODELO · COLORES MIXTOS", 60, 254, 16, Gold, true);
        DrawCraft(g, 195, 411, .88f, Blueprint);
        TextAt(g, "Misma forma + mismo color", 65, 565, 16, Muted);
        TextAt(g, Blueprint[0].Variante == 1 ? "CASCO RATÓN" : "CASCO PESCADO", 62, 292, 13, Muted, true);
        TextAt(g, "ARRASTRÁ LAS PIEZAS AQUÍ", 400, 254, 15, Mint, true);
        for (int x = 390; x < 760; x += 40) LineTo(g, Line, x, 292, x, 550);
        for (int y = 292; y < 550; y += 40) LineTo(g, Line, 390, y, 750, y);
        DrawAssembly(g);
        TextAt(g, AssemblyCaption(), 391, 565, 14, animation > 0 ? Gold : Muted);
        trays.Clear();
        var component = Enum.GetValues<Componente>()[trayPage];
        TextAt(g, Nombres.Componente(component).ToUpperInvariant() + " · PIEZAS", 865, 198, 14, White, true);
        Button(g, "‹", 801, 190, 43, 32, () => trayPage = (trayPage + 6) % 7, false, WorkshopReady);
        Button(g, "›", 1104, 190, 43, 32, () => trayPage = (trayPage + 1) % 7, false, WorkshopReady);
        for (int variant = 0; variant < 2; variant++)
        for (int ci = 0; ci < 4; ci++)
        {
            var part = new Pieza(component, variant, colors[ci]); float x = 800 + ci % 2 * 174, y = 234 + (variant * 2 + ci / 2) * 88;
            var rect = new RectangleF(x, y, 163, 80); Box(g, x, y, 163, 80, rect.Contains(mouse) ? Line : Navy, 8);
            DrawPart(g, x + 43, y + 40, component == Componente.Casco ? .45f : .63f, part);
            TextAt(g, colors[ci], x + 83, y + 19, 14, Uniforme(colors[ci])); TextAt(g, ToyLabel(component, variant), x + 76, y + 43, 12, Muted);
            trays.Add((rect, part));
        }
        if (workshop.Vidas > 0)
            Button(g, animation > 0 ? "Construyendo…" : "Ensamblar ×"+assemblyBatch, 40, 616, 230, 45, () =>
            {
                pendingFleet.Clear();pendingFleet.AddRange(workshop.EnsamblarLote(Recipe,Blueprint,placed.Values.ToList(),assemblyBatch));
                if(pendingFleet.Count>0) { pendingShip=pendingFleet[0];animation=AssemblyDuration; }
                else { failedParts.Clear();failedParts.AddRange(placed.Values);explosion = FailureDuration; placed.Clear(); Say(workshop.Mensaje); }
            }, true, WorkshopReady);
        else Button(g, "Reintentar · 3 vidas", 40, 616, 230, 45, () => { workshop.Reiniciar(); placed.Clear(); }, true, explosion == 0);
        Button(g, "Vaciar mesa", 280, 616, 145, 45, () => { placed.Clear();  }, false, WorkshopReady);
        if (progress.Flota.Count > 0)
        {
            fleetIndex = Math.Clamp(fleetIndex, 0, progress.Flota.Count - 1); var ship = progress.Flota[fleetIndex];
            Button(g, "‹", 450, 616, 36, 45, () => fleetIndex = (fleetIndex + progress.Flota.Count - 1) % progress.Flota.Count);
            TextAt(g, (fleetIndex + 1) + "/" + progress.Flota.Count + " · " + ship.Tipo, 496, 617, 15, Gold, true);
            TextAt(g, ship.Piloto?.Nombre ?? "Sin piloto", 496, 640, 13, Muted);
            Button(g, "›", 701, 616, 36, 45, () => fleetIndex = (fleetIndex + 1) % progress.Flota.Count);
            TextAt(g,"Pilotos en sector",755,620,14,Muted);TextAt(g,"ABORDAJE →",755,644,14,Gold,true);
        }
        Button(g, "Lanzar flota", 950, 616, 210, 45, () => { if (!progress.Lanzada) progress.XP += 150; progress.Lanzada = true; Save(); Change(Pantalla.Final); }, true, progress.Lista && animation == 0);
        int complete = RecetaNave.Todas.Count(r => progress.Flota.Any(n => n.Tipo == r.Nombre && n.Piloto != null));
        TextAt(g, "Rumbo a las estrellas: " + complete + "/3 tipos listos para despegar · ¡Tu flota puede seguir creciendo!", 40, 676, 15, Gold);
        DrawLotQuantity(g,"NAVES",40,710,assemblyBatch,n=>assemblyBatch=n,WorkshopReady);
        TextAt(g,animation>0 ? "Fabricando "+pendingFleet.Count+" naves del modelo armado…" : "Armá el modelo una vez para fabricar el lote.",505,718,14,Mint);
        if (dragging != null) DrawPart(g, mouse.X, mouse.Y, .8f, dragging);
    }
}
