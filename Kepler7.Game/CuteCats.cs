using System.Drawing.Drawing2D;
using Kepler7.Core;
namespace Kepler7.Game;
internal static partial class Art
{
    public static Color MixFur(Color a,Color b,float amount)=>Color.FromArgb(
        (int)(a.R+(b.R-a.R)*amount),(int)(a.G+(b.G-a.G)*amount),(int)(a.B+(b.B-a.B)*amount));
    private static void SoftEar(Graphics g,float direction,bool back,Color fur,Color outline,bool hissing)
    {
        var state=g.Save();g.ScaleTransform(direction,1);
        if(hissing){g.TranslateTransform(0,-5);g.RotateTransform(26);}
        using var ear=new GraphicsPath();
        ear.AddBezier(8,-26,13,-33,21,-43,24,-40);
        ear.AddBezier(24,-40,27,-37,27,-23,25,-15);
        ear.CloseFigure();using(var brush=new SolidBrush(outline))g.FillPath(brush,ear);
        using var inner=new GraphicsPath();
        inner.AddBezier(12,-26,16,-32,21,-38,23,-37);
        inner.AddBezier(23,-37,25,-33,24,-24,23,-20);
        inner.CloseFigure();using(var brush=new SolidBrush(fur))g.FillPath(brush,inner);
        if(!back)
        {
            using var pink=new GraphicsPath();
            pink.AddBezier(16,-26,19,-30,21,-34,22,-33);
            pink.AddBezier(22,-33,23,-30,22,-26,22,-24);
            pink.CloseFigure();using var brush=new SolidBrush(Color.FromArgb(242,171,183));g.FillPath(brush,pink);
        }
        g.Restore(state);
    }
    private static void CuteCat(Graphics g,float x,float y,float scale,Color suit,float aim,string kind,float clock,float stride,bool hero,bool hissing)
    {
        var state=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(scale,scale);
        if(hero)kind="Blanco";
        Color fur=ColorTranslator.FromHtml(FabricaEspecies.Compartida.Obtener(kind).Color);
        Color outline=MixFur(fur,Color.FromArgb(91,104,128),.36f);
        Color soft=MixFur(fur,White,.17f),mark=MixFur(fur,Color.FromArgb(89,60,47),.55f);
        if(kind=="Siamés")mark=Color.FromArgb(112,85,79);
        float side=MathF.Cos(aim),vertical=MathF.Sin(aim),breathe=MathF.Sin(clock*2.6f)*.55f;
        bool back=vertical<-.55f,profile=Math.Abs(side)>.72f;
        float step=stride==0?0:MathF.Sin(stride)*3;
        Ellipse(g,Color.FromArgb(65,0,0,0),-24,30,49,10);
        // Soft curled tail, moving independently of the walking cycle.
        var a=new PointF(15,25);var b=new PointF(35,30);var c=new PointF(39,5);var d=new PointF(30,2+MathF.Sin(clock*2)*3);
        using(var p=new Pen(outline,8){StartCap=LineCap.Round,EndCap=LineCap.Round})g.DrawBezier(p,a,b,c,d);
        using(var p=new Pen(kind=="Siamés"?mark:soft,5.5f){StartCap=LineCap.Round,EndCap=LineCap.Round})g.DrawBezier(p,a,b,c,d);
        // Short plush paws and a compact, rounded uniform.
        for(int i=0;i<2;i++)
        {
            float px=i==0?-17:3,py=28+(i==0?step:-step);
            Ellipse(g,outline,px,py,15,12);Ellipse(g,kind=="Siamés"?mark:soft,px+.7f,py,13.6f,10);
            LineTo(g,outline,px+5,py+7,px+5,py+9,.6f);LineTo(g,outline,px+9,py+7,px+9,py+9,.6f);
        }
        Ellipse(g,Shade(suit,.76f),-20,0+breathe,40,35);Ellipse(g,suit,-18,0+breathe,36,31);
        Ellipse(g,Color.FromArgb(34,White),-13,8+breathe,25,16);
        for(int i=0;i<2;i++)
        {
            float px=i==0?-26:17,py=13+(i==0?-step:step)*.45f;
            Box(g,px,py,10,16,i==0?suit:Shade(suit,.89f),5);
            Ellipse(g,outline,px-.3f,py+9,10.6f,10);Ellipse(g,kind=="Siamés"?mark:soft,px+.3f,py+9,9.4f,8.6f);
        }
        if(back){Box(g,-9,13,18,16,Shade(suit,.85f),6);LineTo(g,MixFur(suit,White,.2f),-4,16,4,16,1);}
        else{Box(g,-5,22,10,6,Navy,2);Ellipse(g,Gold,-1.5f,23,3,3);}
        if(hero)
        {
            Box(g,-17,12+breathe,34,8,Color.FromArgb(68,131,196),4);
            Poly(g,Blue,new PointF(-16,15+breathe),new PointF(-26,24+breathe),new PointF(-24,14+breathe));
            Ellipse(g,Gold,-3,15+breathe,6,6);
        }
        var head=g.Save();g.TranslateTransform(side*2,breathe);
        SoftEar(g,-1,back,kind=="Siamés"?mark:soft,outline,hissing);
        SoftEar(g,1,back,kind=="Siamés"?mark:soft,outline,hissing);
        Ellipse(g,outline,-28,-31,56,49);Ellipse(g,fur,-27,-30.5f,54,47);
        Ellipse(g,Color.FromArgb(23,White),-24,-29,46,35);
        // Small forehead tufts keep the silhouette soft at portrait and sprite sizes.
        using(var tuft=new Pen(soft,2){StartCap=LineCap.Round,EndCap=LineCap.Round})
        {g.DrawBezier(tuft,new PointF(-4,-29),new PointF(-5,-34),new PointF(0,-33),new PointF(1,-29));g.DrawBezier(tuft,new PointF(1,-29),new PointF(2,-33),new PointF(5,-32),new PointF(6,-28));}
        if(kind=="Atigrado")
        {
            for(int i=-1;i<=1;i++)
                using(var p=new Pen(mark,2.6f){StartCap=LineCap.Round,EndCap=LineCap.Round})g.DrawBezier(p,new PointF(i*7,-28),new PointF(i*7,-26),new PointF(i*6,-24),new PointF(i*6,-23));
            LineTo(g,mark,-25,-1,-21,0,1.7f);LineTo(g,mark,21,0,25,-1,1.7f);
        }
        if(kind=="Calicó"){Ellipse(g,Color.FromArgb(205,137,79),-25,-28,22,26);Ellipse(g,Color.FromArgb(88,84,96),11,-24,15,23);}
        if(back)
        {using var p=new Pen(outline,1);g.DrawBezier(p,new PointF(-7,-16),new PointF(-4,-11),new PointF(4,-11),new PointF(7,-16));}
        else
        {
            float offset=side*3;
            if(kind=="Siamés")Ellipse(g,mark,-23+offset,-23,46,34);
            Color cheek=kind=="Negro"?Color.FromArgb(105,226,151,177):Color.FromArgb(83,247,152,174);
            Ellipse(g,cheek,-24+offset,1,10,5);Ellipse(g,cheek,14+offset,1,10,5);
            Color muzzle=kind=="Negro"?Color.FromArgb(125,132,153):MixFur(fur,Color.FromArgb(255,250,244),.72f);
            Ellipse(g,muzzle,-8+offset,2,9,7);Ellipse(g,muzzle,-1+offset,2,9,7);
            bool blink=!hissing && clock%5.3f>5.13f;
            foreach(float eye in new[]{-11f,11f})
            {
                float ex=eye+offset,width=profile && Math.Sign(eye)!=Math.Sign(side)?10.5f:13;
                if(blink)
                {using var p=new Pen(Navy,1.6f){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawBezier(p,new PointF(ex-5,-7),new PointF(ex-3,-4),new PointF(ex+3,-4),new PointF(ex+5,-7));}
                else
                {
                    Color iris=hero?(eye<0?Color.FromArgb(64,161,241):Color.FromArgb(86,192,125)):kind=="Siamés"?Blue:Gold;
                    Ellipse(g,outline,ex-width/2-.6f,-16.8f,width+1.2f,16.4f);
                    Ellipse(g,iris,ex-width/2,-16.3f,width,15.4f);
                    Ellipse(g,Color.FromArgb(29,31,49),ex-width*.34f+side*.6f,-15.8f,width*.68f,12.8f);
                    Ellipse(g,MixFur(iris,White,.38f),ex-width*.3f,-5.5f,width*.6f,3.4f);
                    Ellipse(g,White,ex-3.8f,-14.4f,4.1f,4.6f);Ellipse(g,White,ex+1.8f,-8.1f,1.7f,1.7f);
                }
            }
            Color nose=Color.FromArgb(195,127,145),mouth=kind=="Negro"?Color.FromArgb(208,211,222):Color.FromArgb(124,105,119);
            CatMouth(g,offset,2,.85f,mouth,nose);
            for(int i=0;i<2;i++)
            {LineTo(g,outline,offset-20,4+i*3,offset-30,3+i*5,.65f);LineTo(g,outline,offset+20,4+i*3,offset+30,3+i*5,.65f);}
            if(hissing)
            {
                Ellipse(g,Navy,offset-5,5,10,8);Poly(g,White,new PointF(offset-4,6),new PointF(offset-1,6),new PointF(offset-2.5f,10));Poly(g,White,new PointF(offset+1,6),new PointF(offset+4,6),new PointF(offset+2.5f,10));
                LineTo(g,mouth,offset-17,-17,offset-9,-15,1);LineTo(g,mouth,offset+9,-15,offset+17,-17,1);
            }
        }
        g.Restore(head);g.Restore(state);
    }
}
