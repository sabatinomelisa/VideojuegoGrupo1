using System.Numerics;
using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private float playerStride;
    private void DrawBattleInterior(Graphics g)
    {
        Box(g,0,0,Pasillo.Largo,565,Color.FromArgb(34,38,58),0);
        for(int x=0;x<Pasillo.Largo;x+=128)
        {
            for(int y=80;y<510;y+=86)
            {Box(g,x+3,y,121,80,Color.FromArgb(43+(x/128%2)*4,47,68),12);LineTo(g,Color.FromArgb(61,65,91),x+8,y+3,x+117,y+3);}
            Box(g,x,0,128,79,PanelColor,0);Box(g,x+6,7,115,59,Line,20);Box(g,x+13,12,101,46,Navy,17);
            for(int k=0;k<5;k++)Ellipse(g,Muted,x+22+(k*17)%81,20+(k*11)%29,2,2);
            Ellipse(g,Color.FromArgb(28,Blue),x+35,18,32,24);
            Box(g,x,521,128,44,PanelColor,0);LineTo(g,Muted,x+4,524,x+121,524,2);
        }
        foreach(var pod in corridor.Capsulas)
        {
            float x=pod.X+pod.W/2-70;
            float pulse=.6f+.4f*MathF.Sin(time*2+x);
            for(int layer=4;layer>=1;layer--)Ellipse(g,Color.FromArgb((int)(7*pulse),Mint),x-60-layer*9,63-layer*2,180+layer*18,100+layer*8);
            Box(g,x,70,88,6,Mint,2);LineTo(g,Line,x+44,80,x+44,130,4);
            Paw(g,x+44,285,.9f,Color.FromArgb(25,Mint));
            DrawPod(g,pod.X+pod.W/2,pod.Y+91*.64f,.64f,x%3==0?Red:Mint);
            Cat(g,x+70,189,.55f,Muted,true,MathF.PI/2,"Siamés",time,decay:(int)(x/420)%4);
            TextAt(g,"BIO–"+((int)(x/420)+1),x+30,248,12,Muted,true);
            Bowl(g,x+194,488,.7f);Bowl(g,x+246,492,.65f,true);
            Yarn(g,x+284,501,.4f,Gold);
            // Cracked conduits and spilled coolant: visual detail, no invisible collision.
            LineTo(g,Line,x+150,72,x+150,122,6);LineTo(g,Gold,x+150,122,x+165,135,2);
            Ellipse(g,Color.FromArgb(45,Mint),x+118,421,74,21);
            for(int k=0;k<3;k++)LineTo(g,Color.FromArgb(70,Muted),x+300+k*7,351,x+281+k*7,376,2);
        }
        for(int x=120;x<Pasillo.Largo;x+=270)
        {Box(g,x,544,100,4,Blue,2);TextAt(g,"→ CLONACIÓN",x,313,14,Color.FromArgb(45,Mint),true);}
    }
    private void DrawBattleObstacle(Graphics g,Caja o)
    {
        Box(g,o.X+7,o.Y+11,o.W,o.H,Color.FromArgb(100,0,0,0),8);
        Box(g,o.X,o.Y,o.W,o.H,Line,7);Box(g,o.X+5,o.Y+5,o.W-10,o.H-15,PanelColor,5);
        Box(g,o.X+8,o.Y+8,o.W-16,37,Navy,4);
        LineTo(g,Mint,o.X+15,o.Y+19,o.X+o.W-19,o.Y+19,2);
        for(int j=0;j<4;j++)LineTo(g,Muted,o.X+15,o.Y+65+j*12,o.X+o.W-15,o.Y+65+j*12,2);
        Paw(g,o.X+o.W/2,o.Y+o.H-26,.4f,Gold);
    }
    private void DrawMedkit(Graphics g,Vector2 p)
    {
        float pulse=(MathF.Sin(time*4)+1)/2;
        for(int i=5;i>=1;i--){float r=20+i*7+pulse*5;Ellipse(g,Color.FromArgb(9+(int)(pulse*3),Mint),p.X-r,p.Y-r,r*2,r*2);}
        using(var ring=new Pen(Color.FromArgb(90+(int)(pulse*80),Mint),2))g.DrawEllipse(ring,p.X-30-pulse*5,p.Y-12-pulse*2,60+pulse*10,24+pulse*4);
        float bob=MathF.Sin(time*3)*3;
        Box(g,p.X-18,p.Y-17+bob,36,32,White,7);Box(g,p.X-8,p.Y-23+bob,16,8,Mint,3);
        Box(g,p.X-3,p.Y-12+bob,6,22,Color.FromArgb(37,160,129),0);Box(g,p.X-11,p.Y-4+bob,22,6,Color.FromArgb(37,160,129),0);
        for(int k=0;k<4;k++)
        {float a=time*1.2f+k*MathF.PI/2,r=34+pulse*7;float x=p.X+MathF.Cos(a)*r,y=p.Y+MathF.Sin(a)*r;LineTo(g,White,x-3,y,x+3,y,1.5f);LineTo(g,White,x,y-3,x,y+3,1.5f);}
        TextAt(g,"+30 SALUD",p.X-32,p.Y-53,11,Mint,true);
    }
}
