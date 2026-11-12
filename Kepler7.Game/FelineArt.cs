using System.Drawing.Drawing2D;
using Kepler7.Core;
namespace Kepler7.Game;

internal static partial class Art
{
    public static void Paw(Graphics g, float x, float y, float s, Color color)
    {
        var st = g.Save(); g.TranslateTransform(x, y); g.ScaleTransform(s, s);
        Ellipse(g, color, -13, -2, 26, 21);
        Ellipse(g, color, -23, -14, 11, 15); Ellipse(g, color, -12, -25, 11, 16);
        Ellipse(g, color, 2, -25, 11, 16); Ellipse(g, color, 14, -14, 11, 15);
        g.Restore(st);
    }
    public static void Yarn(Graphics g, float x, float y, float s, Color c)
    {
        var st = g.Save(); g.TranslateTransform(x, y); g.ScaleTransform(s, s);
        Ellipse(g, MixFur(c,Navy,.24f), -25, -25, 50, 50); Ellipse(g, c, -23, -23, 46, 46);
        using var strand = new Pen(Color.FromArgb(95, Navy), 1.8f);
        for (int i = 0; i < 4; i++) g.DrawArc(strand, -23+i*5, -22, 44-i*7, 44, -80, 235);
        g.DrawArc(strand, -21, -10, 40, 22, 15, 160);
        for(int i=0;i<3;i++) LineTo(g,Color.FromArgb(85,Navy),-17,-12+i*8,17,-2+i*8,2);
        using var tail = new Pen(c, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawBezier(tail, new PointF(15,19),new PointF(43,12),new PointF(13,43),new PointF(40,34));
        g.Restore(st);
    }
    public static void Fish(Graphics g, float x, float y, float s, Color c)
    {
        var st=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(s,s);
        Color edge=MixFur(c,Navy,.27f);
        using(var fin=new GraphicsPath())
        {
            fin.AddBezier(-18,0,-35,-23,-44,-22,-40,-7);
            fin.AddBezier(-40,-7,-38,-2,-38,2,-40,7);
            fin.AddBezier(-40,7,-44,22,-35,23,-18,0);fin.CloseFigure();
            using var b=new SolidBrush(edge);g.FillPath(b,fin);
        }
        using(var p=new Pen(c,10){StartCap=LineCap.Round,EndCap=LineCap.Round})
        {g.DrawBezier(p,new PointF(-2,-16),new PointF(8,-30),new PointF(12,-29),new PointF(16,-17));g.DrawBezier(p,new PointF(0,15),new PointF(10,26),new PointF(16,26),new PointF(19,14));}
        Ellipse(g,edge,-27,-24,67,48);Ellipse(g,c,-26,-23,65,45);
        Ellipse(g,Color.FromArgb(38,White),-18,-19,39,17);
        Ellipse(g,Navy,18,-12,11,14);Ellipse(g,White,20,-10,4,5);Ellipse(g,White,25,-3,1.8f,1.8f);
        Ellipse(g,Color.FromArgb(115,Red),18,5,11,5);
        using var seam=new Pen(MixFur(c,Navy,.36f),1.5f);
        g.DrawArc(seam,-9,-13,24,28,110,140);
        using var smile=new Pen(Navy,1.1f);
        g.DrawBezier(smile,new PointF(32,5),new PointF(34,8),new PointF(37,8),new PointF(39,5));
        g.Restore(st);
    }
    public static void ToyMouse(Graphics g,float x,float y,float s,Color c)
    {
        var st=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(s,s);
        Color edge=MixFur(c,Navy,.3f);
        using(var tail=new Pen(c,4){StartCap=LineCap.Round,EndCap=LineCap.Round})g.DrawBezier(tail,new PointF(0,32),new PointF(45,70),new PointF(-40,69),new PointF(-13,92));
        Ellipse(g,edge,-35,-48,29,32);Ellipse(g,edge,6,-48,29,32);
        Ellipse(g,c,-33,-47,25,28);Ellipse(g,c,8,-47,25,28);
        Ellipse(g,MixFur(c,Red,.6f),-28,-42,16,19);Ellipse(g,MixFur(c,Red,.6f),13,-42,16,19);
        Ellipse(g,edge,-30,-35,60,84);Ellipse(g,c,-29,-34,58,81);
        Ellipse(g,Color.FromArgb(36,White),-23,-28,39,29);
        PlushFace(g,0,-20,1.1f,Navy);
        Ellipse(g,Red,-3,-27,6,4);
        Ellipse(g,MixFur(c,White,.55f),-15,3,30,29);
        Ellipse(g,Color.FromArgb(28,Navy),-10,9,20,15);
        LineTo(g,White,-6,11,1,11,1.3f);
        g.Restore(st);
    }
    public static void Feather(Graphics g,float x,float y,float s,Color c,bool wide)
    {
        var st=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(s,s);
        LineTo(g,Gold,0,35,0,-23,4);
        using var feather=new GraphicsPath();float w=wide?26:18;
        feather.AddBezier(0,-42,w,-32,w+3,-8,0,21);
        feather.AddBezier(0,21,-w-3,-8,-w,-32,0,-42);feather.CloseFigure();
        using(var b=new SolidBrush(c))g.FillPath(b,feather);
        LineTo(g,MixFur(c,White,.65f),0,-32,0,23,1.7f);
        for(int i=0;i<4;i++){LineTo(g,MixFur(c,Navy,.25f),-11,-22+i*8,0,-13+i*8,1);LineTo(g,MixFur(c,Navy,.25f),11,-22+i*8,0,-13+i*8,1);}
        g.Restore(st);
    }
    public static PointF ToyPosition(Componente c) => c switch
    {
        Componente.Casco => new(0,0), Componente.Motor => new(0,86), Componente.Escudo => new(-88,1),
        Componente.Armas => new(87,12), Componente.Radar => new(-70,-64), Componente.InteligenciaArtificial => new(68,-63), _ => new(-73,54)
    };
    public static void TunaEngine(Graphics g,float x,float y,float scale,Color c)
    {
        var st=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(scale,scale);
        // Metal lid folded back, open rim, tuna flakes and an exhaust nozzle.
        var lid=g.Save();g.TranslateTransform(-4,-24);g.RotateTransform(-38);
        Ellipse(g,White,-24,-14,48,28);Ellipse(g,Muted,-19,-10,38,20);
        using(var ring=new Pen(White,2))g.DrawEllipse(ring,-8,-5,16,10);
        g.Restore(lid);
        Box(g,-11,20,22,16,Line,4);Ellipse(g,Gold,-9,28,18,8);
        Box(g,-27,-10,54,36,c,9);Ellipse(g,c,-27,14,54,19);
        Box(g,-22,-3,5,19,Color.FromArgb(70,White),2);
        LineTo(g,White,-22,21,22,21,2);
        Ellipse(g,White,-28,-20,56,23);Ellipse(g,Navy,-24,-17,48,17);
        Ellipse(g,Color.FromArgb(224,150,140),-21,-14,42,12);
        for(int i=0;i<5;i++)LineTo(g,Color.FromArgb(167,87,94),-16+i*7,-11,-12+i*7,-6,2);
        Fish(g,0,7,.35f,Navy);
        g.Restore(st);
    }
    public static void ToyMount(Graphics g,float x,float y,float scale,Componente part,Color c)
    {
        if(part==Componente.Casco)return;
        var o=ToyPosition(part);
        LineTo(g,Line,x,y+18*scale,x+o.X*scale,y+o.Y*scale,13*scale);
        LineTo(g,c,x,y+18*scale,x+o.X*scale,y+o.Y*scale,5*scale);
        Ellipse(g,White,x+o.X*scale*.68f-3*scale,y+(18+(o.Y-18)*.68f)*scale-3*scale,6*scale,6*scale);
    }
    public static void ToyPart(Graphics g,float x,float y,float scale,Componente part,int variant,Color c)
    {
        var st=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(scale,scale);bool b=variant==1;
        switch(part)
        {
            case Componente.Casco:
                if(b) ToyMouse(g,0,-4,.92f,c);
                else Fish(g,0,0,1.35f,c); break;
            case Componente.Motor:
                if(b){TunaEngine(g,-21,0,.70f,c);TunaEngine(g,21,0,.70f,c);}else TunaEngine(g,0,0,1,c); break;
            case Componente.Escudo:
                if(b){using var shield=new GraphicsPath();shield.AddBezier(-27,-25,-12,-30,12,-30,27,-25);shield.AddBezier(27,-25,31,5,18,28,0,36);shield.AddBezier(0,36,-18,28,-31,5,-27,-25);shield.CloseFigure();using var brush=new SolidBrush(c);g.FillPath(brush,shield);}
                else {Ellipse(g,MixFur(c,Navy,.22f),-30,-30,60,60);Ellipse(g,c,-28,-28,56,55);}
                Ellipse(g,Color.FromArgb(50,White),-18,-23,27,12);
                Paw(g,0,3,.8f,Navy);break;
            case Componente.Armas:
                Feather(g,b?-13:0,0,.85f,c,b);if(b)Feather(g,16,5,.70f,c,false);break;
            case Componente.Radar:
                LineTo(g,c,0,29,0,-14,4);
                SoftEar(g,-1,false,c,MixFur(c,Navy,.2f),false);SoftEar(g,1,false,c,MixFur(c,Navy,.2f),false);
                Ellipse(g,MixFur(c,Navy,.2f),-25,-29,50,42);Ellipse(g,c,-24,-28,48,40);PlushFace(g,0,-11,.9f,Navy);
                if(b){using var wave=new Pen(c,2);g.DrawArc(wave,-36,-48,72,65,195,150);}break;
            case Componente.InteligenciaArtificial:
                SoftEar(g,-1,false,c,MixFur(c,Navy,.2f),false);SoftEar(g,1,false,c,MixFur(c,Navy,.2f),false);
                Box(g,-26,-23,52,49,c,14);
                Box(g,-19,-15,38,30,Navy,5);Ellipse(g,c,-13,-6,7,b?7:3);Ellipse(g,c,6,-6,7,b?7:3);
                CatMouth(g,0,0,1.15f,c,c);if(b)Paw(g,0,23,.25f,Navy);break;
            case Componente.Tanques:
                if(b){Fish(g,-14,-13,.50f,c);Fish(g,12,17,.50f,c);}else{Box(g,-27,-28,54,57,c,9);Ellipse(g,White,-27,-29,54,16);Fish(g,0,4,.5f,Navy);}break;
        }
        g.Restore(st);
    }
}
