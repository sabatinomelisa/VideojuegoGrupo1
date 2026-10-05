using System.Drawing.Drawing2D;
namespace Kepler7.Game;
internal static partial class Art
{
    public static void Sparkle(Graphics g,float x,float y,float size,Color color)
    {
        using var p=new Pen(color,1.2f){StartCap=LineCap.Round,EndCap=LineCap.Round};
        g.DrawLine(p,x-size,y,x+size,y);g.DrawLine(p,x,y-size,x,y+size);
        Ellipse(g,color,x-1.5f,y-1.5f,3,3);
    }
    public static void CozyStars(Graphics g,float clock)
    {
        for(int i=0;i<54;i++)
        {
            float x=(i*137+23)%1200,y=(i*97+17)%760;
            Color c=Color.FromArgb(35+(int)(22*(1+MathF.Sin(clock*.7f+i))),i%3==0?Gold:Blue);
            if(i%9==0)Sparkle(g,x,y,3,c);else Ellipse(g,c,x,y,2,2);
        }
    }
    public static void CozyRoom(Graphics g,Color accent)
    {
        Box(g,40,150,1120,500,PanelColor,23);
        Box(g,57,167,1086,141,Navy,24);
        // Quiet, wide floor tiles leave the character and prompts easy to see.
        for(int row=0;row<4;row++)for(int col=0;col<8;col++)
            Box(g,62+col*135,320+row*76,128,69,Color.FromArgb((row+col)%2==0?14:8,White),14);
        LineTo(g,Color.FromArgb(95,accent),76,309,1124,309,3);
        Ellipse(g,Color.FromArgb(25,accent),400,431,400,146);
        using(var edge=new Pen(Color.FromArgb(38,accent),2))g.DrawEllipse(edge,413,441,374,124);
        Paw(g,600,503,.9f,Color.FromArgb(24,accent));
        // Small plush wall lights, safely outside the interactive consoles.
        for(int i=0;i<2;i++)
        {
            float x=i==0?81:1118;
            Ellipse(g,Color.FromArgb(14,accent),x-13,203,26,80);
            Box(g,x-4,214,8,52,accent,4);
        }
    }
    public static void CozyDoor(Graphics g,float x,float y,Color accent)
    {
        Box(g,x-97,y,194,132,MixFur(Line,accent,.2f),25);
        Box(g,x-86,y+10,172,121,Navy,23);
        Box(g,x-75,y+19,70,104,Color.FromArgb(33,accent),16);
        Box(g,x+5,y+19,70,104,Color.FromArgb(33,accent),16);
        LineTo(g,accent,x,y+33,x,y+119,2);
        Paw(g,x,y+39,.34f,accent);
        Box(g,x-76,y+125,152,6,accent,3);
        Ellipse(g,Color.FromArgb(23,accent),x-76,y+137,152,19);
    }
    public static void BoardingDoor(Graphics g,float x,float y,Color accent)
    {
        // Full-height sliding door: jambs, inset panels, windows and a floor sill.
        Box(g,x-96,y,192,158,MixFur(Line,accent,.24f),17);
        Box(g,x-83,y+10,166,143,Navy,10);
        Box(g,x-76,y+16,70,132,MixFur(PanelColor,accent,.18f),7);
        Box(g,x+6,y+16,70,132,MixFur(PanelColor,accent,.18f),7);
        Box(g,x-57,y+34,29,47,Navy,7);Box(g,x+28,y+34,29,47,Navy,7);
        LineTo(g,accent,x-53,y+39,x-34,y+39,2);LineTo(g,accent,x+32,y+39,x+51,y+39,2);
        LineTo(g,accent,x-15,y+90,x-15,y+115,3);LineTo(g,accent,x+15,y+90,x+15,y+115,3);
        Box(g,x-91,y+149,182,9,accent,3);
        for(int i=0;i<8;i++)LineTo(g,Navy,x-80+i*22,y+150,x-74+i*22,y+156,3);
        Ellipse(g,accent,x+84,y+81,6,6);
    }
    public static void PlushFace(Graphics g,float x,float y,float size,Color eye)
    {
        Ellipse(g,eye,x-11*size,y-6*size,6*size,8*size);Ellipse(g,eye,x+5*size,y-6*size,6*size,8*size);
        Ellipse(g,White,x-10*size,y-5*size,2*size,2*size);Ellipse(g,White,x+6*size,y-5*size,2*size,2*size);
        Ellipse(g,Color.FromArgb(110,Red),x-16*size,y+2*size,7*size,3*size);Ellipse(g,Color.FromArgb(110,Red),x+9*size,y+2*size,7*size,3*size);
        CatMouth(g,x,y+2*size,1.18f*size,eye,Red);
        for(int i=0;i<2;i++)
        {
            LineTo(g,eye,x-12*size,y+(4+i*3)*size,x-20*size,y+(2+i*6)*size,.65f*size);
            LineTo(g,eye,x+12*size,y+(4+i*3)*size,x+20*size,y+(2+i*6)*size,.65f*size);
        }
    }
    public static void CatMouth(Graphics g,float x,float y,float size,Color ink,Color nose)
    {
        Poly(g,nose,new PointF(x-2.5f*size,y),new PointF(x+2.5f*size,y),new PointF(x,y+2.5f*size));
        using var p=new Pen(ink,1.15f*size){StartCap=LineCap.Round,EndCap=LineCap.Round};
        g.DrawLine(p,x,y+2*size,x,y+4*size);
        g.DrawBezier(p,new PointF(x,y+4*size),new PointF(x-1*size,y+8*size),new PointF(x-5*size,y+8*size),new PointF(x-6*size,y+4*size));
        g.DrawBezier(p,new PointF(x,y+4*size),new PointF(x+1*size,y+8*size),new PointF(x+5*size,y+8*size),new PointF(x+6*size,y+4*size));
    }
}
