using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private int boardingPilotIndex, boardingFilter,boardingSpecies;
    private int boardingBatch=1;
    private string boardingResult="";
    private ResumenOperacion? boardingSummary;
    private readonly List<Nave> boardingGroup=new();
    private readonly string[] boardingFilters={"Para esta nave","Todos","Exploradora","Combate","Carga"};
    private System.Media.SoundPlayer? catHiss;
    private string? BoardingScope(Nave ship)=>boardingFilter==1?null:boardingFilter==0?ship.Tipo:boardingFilters[boardingFilter];
    private Tripulante[] BoardingPilots(Nave ship)=>progress.PilotosDisponibles(BoardingScope(ship),boardingSpecies==0?null:speciesFilters[boardingSpecies]);
    private void DrawBoardingStats(Graphics g)
    {
        var stats=GetFleetStats();
        for(int i=0;i<stats.Length;i++)
        {
            float x=40+i*379;var s=stats[i];Box(g,x,179,362,70,PanelColor,12);
            TextAt(g,s.Tipo,x+14,186,16,Gold,true);
            TextAt(g,"Naves: "+s.NavesConPiloto+" con piloto · "+s.NavesSinPiloto+" sin piloto",x+14,211,13,White);
            TextAt(g,"Pilotos disponibles: "+s.PilotosDisponibles,x+14,231,12,Mint,true);
        }
    }

    private void Hiss()
    {
        try{catHiss ??=new System.Media.SoundPlayer(Path.Combine(AppContext.BaseDirectory,"Audio","bufido.wav"));catHiss.Play();}
        catch(Exception ex) when(ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception){}
    }
    private float boardingTime,boardingError;
    private const float BoardingDuration=3.2f;
    private Tripulante? boardingCat;
    private Nave? boardingShip;
    private void TickBoarding(float dt)
    {
        boardingError=Math.Max(0,boardingError-dt);
        if(boardingTime<=0)return;
        boardingTime=Math.Max(0,boardingTime-dt);
        if(boardingTime==0){if(boardingSummary!=null){ShowSummary(boardingSummary);boardingSummary=null;}else Say("¡"+boardingCat!.Nombre+" a bordo! Nave lista para el lanzamiento.");}
    }
    private void DrawBoarding(Graphics g)
    {
        Header(g,"Hangar / Sector de abordaje","Elegí quién estará al mando");
        Button(g,"← Volver al hangar",40,127,225,40,()=>{roomCat=new(1030,375);Change(Pantalla.SalaHangar);},false,boardingTime==0);
        DrawBoardingStats(g);
        if(progress.Flota.Count==0){TextAt(g,"Construí tu primera nave en el hangar.",70,260,23);return;}
        bool idle=boardingTime==0;
        fleetIndex=Math.Clamp(fleetIndex,0,progress.Flota.Count-1);
        var ship=progress.Flota[fleetIndex];
        for(int i=0;i<boardingFilters.Length;i++)
        {
            int n=i;Button(g,boardingFilters[i],390+i*155,127,150,40,()=>{boardingFilter=n;boardingPilotIndex=0;boardingError=0;if(n>=2){int next=progress.Flota.FindIndex(s=>s.Tipo==boardingFilters[n] && s.Piloto==null);if(next>=0)fleetIndex=next;}},boardingFilter==i,idle);
        }
        var pilots=BoardingPilots(ship);
        var shipParts=ship.Piezas.Count>0?ship.Piezas:Modelos.Para(Array.FindIndex(RecetaNave.Todas,r=>r.Nombre==ship.Tipo));
        Box(g,40,260,310,400,PanelColor);Box(g,369,260,444,400,PanelColor);Box(g,832,260,328,400,PanelColor);
        TextAt(g,"NAVE "+(fleetIndex+1)+" / "+progress.Flota.Count,64,276,15,Gold,true);
        DrawCraft(g,194,389,.65f,shipParts);
        TextAt(g,ship.Tipo,65,474,26,White,true);
        Wrap(g,ship.Piloto==null?"Requiere un piloto de "+ship.Tipo:"Piloto a bordo: "+ship.Piloto.Nombre,65,520,255,55,16,ship.Piloto==null?Gold:Mint);
        Button(g,"‹ Nave",62,590,123,42,()=>{fleetIndex=(fleetIndex+progress.Flota.Count-1)%progress.Flota.Count;boardingPilotIndex=0;boardingError=0;},false,idle);
        Button(g,"Nave ›",200,590,123,42,()=>{fleetIndex=(fleetIndex+1)%progress.Flota.Count;boardingPilotIndex=0;boardingError=0;},false,idle);
        TextAt(g,"PASARELA DE ACCESO",400,276,16,Muted,true);
        for(int i=0;i<6;i++)LineTo(g,Line,397,323+i*50,783,323+i*50);
        Poly(g,Line,new(448,588),new(506,607),new(665,438),new(622,422));
        for(int i=0;i<7;i++){float t=i/7f;LineTo(g,Muted,448+174*t,588-166*t,506+159*t,607-169*t,2);}
        var parts=boardingTime>0 && boardingShip!=null && boardingShip.Piezas.Count>0?boardingShip.Piezas:shipParts;
        DrawCraft(g,650,396,.6f,parts);
        Ellipse(g,Navy,625,403,50,23);LineTo(g,boardingError>0?Red:Mint,626,423,674,423,3);
        Tripulante? pilot=null;
        if(pilots.Length>0)
        {
            boardingPilotIndex=Math.Clamp(boardingPilotIndex,0,pilots.Length-1);pilot=pilots[boardingPilotIndex];
            TextAt(g,"LIBRES · "+(boardingPilotIndex+1)+" / "+pilots.Length,855,276,15,Mint,true);
            Cat(g,993,368,1.1f,Uniforme(pilot.Uniforme.Color),false,MathF.PI/2,pilot.Aspecto.Tipo,time,hissing:boardingError>0);
            TextAt(g,ShortLabel(pilot.Nombre,24),856,418,19,White,true);
            TextAt(g,"Especialidad: "+pilot.Especialidad,856,457,16,Gold);
            TextAt(g,progress.PilotoOcupado(pilot)?"Ya asignado":"Disponible",856,490,15,progress.PilotoOcupado(pilot)?Muted:Mint);
            Button(g,"‹ Piloto",853,536,130,38,()=>boardingPilotIndex=(boardingPilotIndex+pilots.Length-1)%pilots.Length,false,idle);
            Button(g,"Piloto ›",999,536,139,38,()=>boardingPilotIndex=(boardingPilotIndex+1)%pilots.Length,false,idle);
            var selectedPilot=pilot;
            Button(g,idle?"Asignar y abordar":"Abordando…",853,590,285,42,()=>
            {
                string error=progress.ErrorAsignacion(ship,selectedPilot);
                if(!progress.AsignarPiloto(ship,selectedPilot)){boardingError=1.5f;Hiss();Say(error);return;}
                boardingGroup.Clear();boardingSummary=null;boardingResult="";boardingCat=selectedPilot;boardingShip=ship;boardingTime=BoardingDuration;Save();
            },true,idle && ship.Piloto==null);
        }
        else Wrap(g,progress.Pilotos.Count==0 ? "Todavía no creaste pilotos. Volvé al laboratorio para clonar tu tripulación." : "No hay pilotos libres con esos filtros. Revisá los contadores, elegí otro pelaje o cloná nuevos pilotos.",859,324,264,170,22,Gold);
        if(boardingTime>0 && boardingCat!=null)
        {
            float t=1-boardingTime/BoardingDuration;
            if(t<.94f){float u=Math.Clamp(t/.87f,0,1);Cat(g,472+178*u,564-150*u,.92f*(1-.68f*u),Uniforme(boardingCat.Uniforme.Color),false,-.8f,boardingCat.Aspecto.Tipo,time,time*12);}
            if(boardingGroup.Count>1)
            {
                for(int i=1;i<Math.Min(5,boardingGroup.Count);i++)
                {
                    var cat=boardingGroup[i].Piloto!;float u=Math.Clamp((t-i*.1f)/.6f,0,1);
                    if(u<1)Cat(g,426+224*u-i*8,583-169*u+i*5,.7f*(1-.65f*u),Uniforme(cat.Uniforme.Color),false,-.8f,cat.Aspecto.Tipo,time,time*12);
                }
            }
            TextAt(g,boardingGroup.Count>1 ? "¡"+boardingGroup.Count+" pilotos rumbo a sus naves!" : "Abordando: "+boardingCat.Nombre,406,626,15,Mint,true);
        }
        else if(ship.Piloto==null && pilot!=null)
        {
            float recoil=boardingError>0?MathF.Sin(Math.Clamp((1.5f-boardingError)/.45f,0,1)*MathF.PI/2)*16:0;
            Cat(g,472-recoil,564+recoil*.4f,.92f,Uniforme(pilot.Uniforme.Color),false,boardingError>0?MathF.PI/2:-.8f,pilot.Aspecto.Tipo,time,hissing:boardingError>0);
            if(boardingError>0)TextAt(g,"¡Fssss!",408,510,20,White,true);
        }
        else if(ship.Piloto!=null){Paw(g,587,542,.9f,Mint);TextAt(g,"TRIPULACIÓN A BORDO",451,594,17,Mint,true);}
        if(boardingError>0){using var pen=new Pen(Red,4);g.DrawRectangle(pen,374,306,434,343);TextAt(g,"ACCESO RECHAZADO",451,320,21,Red,true);}
        DrawLotQuantity(g,"PILOTOS",40,710,boardingBatch,n=>boardingBatch=n,idle,true);
        string? scope=BoardingScope(ship);
        string? species=boardingSpecies==0?null:speciesFilters[boardingSpecies];
        int waiting=progress.Flota.Count(n=>n.Piloto==null && (scope==null || n.Tipo==scope));
        int capacity=RecetaNave.Todas.Sum(r=>Math.Min(progress.Flota.Count(n=>n.Piloto==null && n.Tipo==r.Nombre && (scope==null || n.Tipo==scope)),pilots.Count(p=>p.Especialidad==r.Nombre)));
        int projected=Math.Min(boardingBatch,capacity);
        TextAt(g,(scope??"Todas")+" · Se asignarán "+projected+" de "+boardingBatch,512,717,14,projected>0?Mint:Gold);
        Button(g,"Asignar lote ×"+boardingBatch,896,705,264,36,()=>
        {
            boardingGroup.Clear();boardingGroup.AddRange(progress.AsignarLote(boardingBatch,scope,species));
            int remaining=waiting-boardingGroup.Count;
            boardingResult="¡"+boardingGroup.Count+" "+(boardingGroup.Count==1?"nave con tripulación":"naves con tripulación")+"! "+remaining+" "+(remaining==1?"sigue esperando piloto.":"siguen esperando piloto.");
            boardingSummary=new ResumenOperacion{Titulo="Abordaje de la flota",Detalles=new List<string>{"Solicitaste hasta "+boardingBatch+" asignaciones · Completadas: "+boardingGroup.Count,"Grupo: "+(scope??"Todas las especialidades")+" · Pelaje: "+(species??"Todos")}.Concat(FleetWaitingLines()).ToList()};
            if(boardingGroup.Count==0){boardingSummary.Detalles.Add("Cloná pilotos compatibles o construí naves disponibles.");ShowSummary(boardingSummary);boardingSummary=null;return;}
            boardingShip=boardingGroup[0];boardingCat=boardingShip.Piloto;fleetIndex=progress.Flota.IndexOf(boardingShip);
            boardingTime=BoardingDuration;boardingError=0;Save();
        },true,idle && capacity>0);
        Button(g,"Especie: "+speciesFilters[boardingSpecies],40,669,250,33,()=>{boardingSpecies=(boardingSpecies+1)%speciesFilters.Length;boardingPilotIndex=0;boardingError=0;},false,idle);
        TextAt(g,"Elegí especialidad, pelaje y cantidad. Un intento rechazado deja al piloto libre.",309,678,14,Gold);
    }
}
