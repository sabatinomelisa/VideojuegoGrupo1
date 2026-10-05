using System.Drawing.Drawing2D;
using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;

public sealed partial class GameForm
{
    private const float AssemblyDuration = 5.4f;
    private readonly List<Pieza> failedParts=new();
    private const float FailureDuration=1.6f;
    private static string ToyLabel(Componente c, int v) => c switch
    {
        Componente.Casco => v == 0 ? "Pescado" : "Ratón",
        Componente.Motor => v == 0 ? "Lata abierta" : "Latas x2",
        Componente.Escudo => v == 0 ? "Almohadilla" : "Huella",
        Componente.Armas => v == 0 ? "Plumero" : "Plumas x2",
        Componente.Radar => v == 0 ? "Orejas" : "Sonar felino",
        Componente.InteligenciaArtificial => v == 0 ? "Michi-chip" : "Michi-bot",
        _ => v == 0 ? "Lata de atún" : "Pescados x2"
    };
    private string AssemblyCaption()
    {
        if(explosion>0)return "¡Ensamblaje fallido! Revisá formas y colores.";
        if (animation <= 0) return placed.Count==0?"Mesa libre · Arrastrá las piezas del modelo":"Soltá afuera para quitar · " + placed.Count + "/4";
        float elapsed = AssemblyDuration-animation;
        if (elapsed >= 3.8f) return "Encendiendo motores de atún…";
        int step = Math.Min(3,(int)(elapsed/.95f));
        return "Ensamblando " + Nombres.Componente(pendingShip!.Piezas[step].Componente) + " · " + (step+1) + "/4";
    }
    private void DrawAssembly(Graphics g)
    {
        if(explosion>0){DrawAssemblyFailure(g);return;}
        if (animation <= 0 || pendingShip == null)
        {
            DrawCraft(g,565,411,.82f,placed.Values);
            return;
        }
        var st=g.Save();g.SetClip(new RectangleF(374,286,389,267));
        float elapsed=AssemblyDuration-animation;
        float ignition=Math.Clamp((elapsed-3.8f)/1.6f,0,1);
        float lift=ignition*24;
        Ellipse(g,Color.FromArgb(50,Mint),448,482,235,45);
        var parts=pendingShip.Piezas;
        for(int i=0;i<parts.Count;i++)
            if(elapsed-i*.95f >= .68f) ToyMount(g,565,411-lift,.82f,parts[i].Componente,Uniforme(parts[i].Color));
        for(int i=0;i<parts.Count;i++)
        {
            float local=(elapsed-i*.95f)/.68f;
            if(local<=0)continue;
            var p=parts[i];var dest=PartPosition(p.Componente);
            float t=Math.Clamp(local,0,1), ease=1-MathF.Pow(1-t,3);
            float startX=i%2==0?394:738,startY=i%2==0?320:517;
            float x=startX+(565+dest.X*.82f-startX)*ease,y=startY+(411+dest.Y*.82f-startY)*ease-lift;
            var pieceState=g.Save();g.TranslateTransform(x,y);g.RotateTransform((1-ease)*(i%2==0?-40:40));
            DrawPart(g,0,0,.82f,p);g.Restore(pieceState);
            if(local<1.36f)
            {
                float armX=i%2==0?380:751;
                LineTo(g,Line,armX,302,armX+(x-armX)*.45f,345,12);
                LineTo(g,Muted,armX+(x-armX)*.45f,345,x,y-30,8);
                Paw(g,x,y-25,.45f,Gold);
                if(local>=.9f)
                    for(int k=0;k<9;k++)
                    {float a=k*MathF.PI*2/9+elapsed*8,r=9+(elapsed*80+k*7)%25;LineTo(g,k%2==0?Gold:Mint,x+MathF.Cos(a)*r,y+MathF.Sin(a)*r,x+MathF.Cos(a)*(r+6),y+MathF.Sin(a)*(r+6),2);}
            }
        }
        if(ignition>0)
        {
            float pulse=12+MathF.Sin(time*35)*6;
            bool dual = parts.First(p => p.Componente == Componente.Motor).Variante == 1;
            foreach(float dx in dual ? new[] { -17f, 17f } : new[] { 0f })
            {
                float cx=565+dx, nozzle=dual?500:509;
                Poly(g,Gold,new(cx-7,nozzle-lift),new(cx,nozzle+24-lift+pulse),new(cx+7,nozzle-lift));
                Poly(g,White,new(cx-3,nozzle-lift),new(cx,nozzle+15-lift+pulse*.6f),new(cx+3,nozzle-lift));
            }
            using var halo=new Pen(Color.FromArgb((int)(140*(1-ignition)),Mint),3);
            float radius=65+ignition*70;g.DrawEllipse(halo,565-radius,403-lift-radius,radius*2,radius*2);
        }
        g.Restore(st);
        Box(g,395,548,340,5,Line,2);Box(g,395,548,Math.Max(5,340*elapsed/AssemblyDuration),5,Mint,2);
    }
    private void DrawAssemblyFailure(Graphics g)
    {
        float t=FailureDuration-explosion;
        var state=g.Save();g.SetClip(new RectangleF(374,286,389,267));
        float shake=t<.3f?MathF.Sin(t*85)*(1-t/.3f)*6:0;
        for(int i=0;i<failedParts.Count;i++)
        {
            var part=failedParts[i];var o=PartPosition(part.Componente);
            float fall=Math.Max(0,t-.28f),direction=i%2==0?-1:1;
            var st=g.Save();g.TranslateTransform(565+o.X*.82f+shake+direction*fall*(34+i*12),411+o.Y*.82f-45*fall+155*fall*fall);
            g.RotateTransform(direction*fall*(55+i*15));DrawPart(g,0,0,.82f,part);g.Restore(st);
        }
        if(t<.65f)
            for(int i=0;i<14;i++){float a=i*MathF.PI/7,r=8+t*(60+i%4*22);LineTo(g,i%3==0?White:Gold,565+MathF.Cos(a)*r,410+MathF.Sin(a)*r,565+MathF.Cos(a)*(r+8),410+MathF.Sin(a)*(r+8),2);}
        if(t>.55f)
            for(int i=0;i<4;i++){float age=t-.55f;Ellipse(g,Color.FromArgb((int)(42*Math.Max(0,1-age)),Muted),535+i*17,412-age*60-i*9,30+age*22,24+age*18);}
        g.Restore(state);
        TextAt(g,"FALLO MECÁNICO",470,302,19,Red,true);
    }
    private void DecorateFelineRoom(Graphics g,bool hub,bool lab)
    {
        Bowl(g,315,582,.78f);Bowl(g,383,583,.72f,true);
        // Props stay at the edges; the route to doors and consoles remains clear.
        for(int i=0;i<6;i++)
        {
            Paw(g,440+i*61,510+(i%2==0?0:20),.45f,Color.FromArgb(42,Mint));
        }
        if(hub)
        {
            Yarn(g,104,572,.9f,Red);Yarn(g,1085,564,1.1f,Blue);
            Fish(g,170,589,.7f,Gold);ToyMouse(g,1019,563,.45f,Mint);
            Paw(g,128,237,.6f,Mint);Paw(g,1072,237,.6f,Gold);

        }
        else
        {
            Ellipse(g,Color.FromArgb(50,0,0,0),94,527,170,45);
            Ellipse(g,MixFur(Red,Navy,.25f),95,473,167,91);
            Ellipse(g,Red,97,462,163,89);
            Ellipse(g,MixFur(Red,White,.6f),112,473,133,62);
            Paw(g,179,504,.6f,Color.FromArgb(110,Red));
            TextAt(g,"RINCÓN MICHI",117,582,13,Muted,true);
            Yarn(g,1080,560,1.05f,lab?Mint:Red);Feather(g,1016,552,.7f,Gold,true);
            Fish(g,890,280,.5f,Blue);
            if(!lab)
            {
                Box(g,872,458,232,14,Line,4);
                ToyMouse(g,913,424,.42f,Mint);Yarn(g,977,432,.65f,Gold);Fish(g,1046,434,.56f,Blue);
                TextAt(g,"JUGUETES DE VUELO",894,480,12,Muted,true);
            }
            else { Paw(g,838,267,.4f,Mint);Paw(g,1110,247,.4f,Mint); }
        }
    }
}
