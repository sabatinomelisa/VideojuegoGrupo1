using System.Drawing.Drawing2D;
using Kepler7.Core;
namespace Kepler7.Game;
internal static partial class Art
{
    private static Color Shade(Color c,float f)=>Color.FromArgb(c.A,(int)Math.Clamp(c.R*f,0,255),(int)Math.Clamp(c.G*f,0,255),(int)Math.Clamp(c.B*f,0,255));
    public static void Cat(Graphics g,float x,float y,float scale,Color suit,bool mutant=false,float aim=MathF.PI/2,string kind="Atigrado",float clock=0,float stride=0,bool hero=false,int decay=0,bool hissing=false)
    {
        if(!mutant || hero){CuteCat(g,x,y,scale,suit,aim,kind,clock,stride,hero,hissing);return;}
        var state=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(scale,scale);
        if(hero){kind="Blanco";mutant=false;}
        decay=Math.Abs(decay%4);
        float side=MathF.Cos(aim),vertical=MathF.Sin(aim);bool back=vertical<-.55f,profile=Math.Abs(side)>.72f;
        float breathe=MathF.Sin(clock*2.6f)*.7f,walk=stride==0?0:MathF.Sin(stride)*(mutant?6:4);
        if(mutant)g.RotateTransform(MathF.Sin(clock*3+decay)*3+(decay%2==0?-6:6));
        Color fur=ColorTranslator.FromHtml(FabricaEspecies.Compartida.Obtener(kind).Color);
        if(mutant)fur=decay switch {0=>Color.FromArgb(137,161,92),1=>Color.FromArgb(151,133,166),2=>Color.FromArgb(98,149,136),_=>Color.FromArgb(173,155,103)};
        Color dark=kind=="Siamés"?Color.FromArgb(95,72,68):Shade(fur,.60f);
        Ellipse(g,Color.FromArgb(85,0,0,0),-25,28,54,15);
        using(var tail=new Pen(dark,7){StartCap=LineCap.Round,EndCap=LineCap.Round})
            g.DrawBezier(tail,new PointF(13,24),new PointF(39,38),new PointF(38,-3+MathF.Sin(clock*3)*6),new PointF(30,-5+MathF.Sin(clock*3)*6));
        Box(g,-15,24+walk,12,18,dark,5);Box(g,4,24-walk,12,18,dark,5);
        Box(g,-20,1+breathe,40,35,Shade(suit,.60f),11);Box(g,-17,1+breathe,33,29,suit,10);
        Ellipse(g,Color.FromArgb(60,White),-13,4,20,11);
        Box(g,-26,8-walk*.6f,10,23,suit,5);Box(g,17,8+walk*.6f,10,23,Shade(suit,.85f),5);
        Ellipse(g,fur,-26,24-walk*.6f,10,9);Ellipse(g,fur,17,24+walk*.6f,10,9);
        if(back){LineTo(g,Shade(suit,.7f),0,7,0,28,2);Box(g,-10,13,20,15,Shade(suit,.8f),5);}
        else {Box(g,-7,17,14,8,Navy,2);Ellipse(g,Gold,-2,18,4,4);}
        if(hero)
        {
            // Atuendo exclusivo del protagonista, fuera de los prototipos clonables.
            Poly(g,Color.FromArgb(51,96,153),new(-20,2),new(20,2),new(12,12),new(-13,10));
            Poly(g,Blue,new(-17,7),new(-30,19),new(-24,5));
            Ellipse(g,Gold,-5,3,10,10);LineTo(g,White,-2,8,2,8,2);
        }
        if(mutant)
        {
            // Tela rasgada, costillas estilizadas y pierna huesuda, sin sangre realista.
            Poly(g,Shade(fur,.38f),new(-13,7),new(3,11),new(-1,16),new(7,22),new(-12,29));
            for(int rib=0;rib<3;rib++)LineTo(g,Color.FromArgb(223,217,174),-10,13+rib*5,-1,15+rib*5,2);
            Poly(g,fur,new(7,26),new(11,19),new(14,29),new(20,23),new(20,34));
            if(decay%2==0){Box(g,6,29-walk,7,12,Color.FromArgb(223,217,174),2);LineTo(g,dark,6,35-walk,13,35-walk,1);}
            else{LineTo(g,dark,-24,17,-19,21,3);LineTo(g,Gold,-24,18,-20,17,1);}
        }
        var head=g.Save();g.TranslateTransform(side*3,breathe);
        if(hissing)
        {
            Poly(g,dark,new(-19,-16),new(-39,-29),new(-15,-30));Poly(g,dark,new(19,-16),new(39,-29),new(15,-30));
            Poly(g,Red,new(-22,-22),new(-33,-27),new(-18,-27));Poly(g,Red,new(22,-22),new(33,-27),new(18,-27));
        }
        else
        {
        if(!mutant)Poly(g,dark,new(-26,-14),new(-26,-46),new(-6,-28));Poly(g,dark,new(26,-14),new(26,-46),new(6,-28));
        if(!mutant)Poly(g,fur,new(-23,-16),new(-23,-42),new(-6,-26));Poly(g,fur,new(23,-16),new(23,-42),new(6,-26));
        if(mutant)
        {
            // Muesca en la silueta de la oreja, visible incluso de espaldas.
            Poly(g,dark,new(-26,-14),new(-26,-46),new(-19,-37),new(-20,-29),new(-12,-33),new(-6,-28));
            Poly(g,fur,new(-23,-16),new(-23,-39),new(-18,-31),new(-12,-30),new(-6,-26));
        }
        if(!back){if(!mutant)Poly(g,Red,new(-21,-23),new(-21,-35),new(-12,-27));Poly(g,Red,new(21,-23),new(21,-35),new(12,-27));}
        }
        Ellipse(g,dark,-27,-32,54,49);Ellipse(g,fur,-26,-32,51,44);Ellipse(g,Color.FromArgb(36,White),-22,-29,36,23);
        if(kind=="Atigrado")
        {
            for(int i=-1;i<=1;i++)Poly(g,dark,new(i*10-3,-30),new(i*10+3,-30),new(i*8,-19));
            LineTo(g,dark,-24,-8,-18,-5,3);LineTo(g,dark,18,-5,24,-8,3);
        }
        if(kind=="Calicó"){Ellipse(g,Color.FromArgb(198,121,55),-24,-28,22,27);Ellipse(g,Color.FromArgb(70,66,68),10,-24,16,22);}
        if(mutant)
        {
            Ellipse(g,Shade(fur,.55f),decay%2==0?-24:8,-26,17,20);
            for(int spot=0;spot<4;spot++)Ellipse(g,Shade(fur,.42f),-19+(spot*13+decay*7)%38,-24+(spot*11)%24,3+spot%2,4);
            if(decay==1 || decay==3){LineTo(g,dark,-18,-22,-4,-16,3);for(int stitch=0;stitch<3;stitch++)LineTo(g,Gold,-16+stitch*4,-24+stitch*2,-18+stitch*4,-18+stitch*2,1);}
        }
        if(!back)
        {
            if(kind=="Siamés")Ellipse(g,dark,-21,-21,42,32);
            float offset=side*5;Color muzzle=kind=="Negro"?Color.FromArgb(146,148,159):Color.FromArgb(250,237,216);
            Ellipse(g,muzzle,-12+offset,-1,25,16);
            bool blink=!mutant && clock%5.3f>5.13f;
            // La cabeza es tres cuartos, no perfil puro: conservar ambos ojos.
            float[] eyes=new[]{-12f,12f};
            foreach(float eye in eyes)
            {
                float ex=eye+offset;
                float width=profile && Math.Sign(eye)!=Math.Sign(side)?8:10;
                if(blink)LineTo(g,Navy,ex-4,-10,ex+4,-10,2);
                else{if(mutant)Ellipse(g,Shade(fur,.32f),ex-width/2-2,-19,width+4,16);
                    Ellipse(g,hero?(eye<0?Color.FromArgb(68,169,255):Color.FromArgb(92,210,112)):mutant?(decay==2?Gold:Red):kind=="Siamés"?Blue:Gold,ex-width/2,-17,width,12);Ellipse(g,Navy,ex-2+side,-16,4,10);Ellipse(g,White,ex-2,-15,2,3);}
            }
            Poly(g,mutant?Red:Color.FromArgb(160,103,113),new(offset-4,0),new(offset+4,0),new(offset,4));
            LineTo(g,dark,offset,4,offset,7,1);LineTo(g,dark,offset-5,8,offset,7,1);LineTo(g,dark,offset,7,offset+5,8,1);
            if(hissing)
            {
                Ellipse(g,Navy,offset-7,5,14,12);Poly(g,White,new(offset-5,6),new(offset-1,6),new(offset-3,12));Poly(g,White,new(offset+1,6),new(offset+5,6),new(offset+3,12));
                LineTo(g,dark,offset-17,-19,offset-8,-16,2);LineTo(g,dark,offset+8,-16,offset+17,-19,2);
            }
            for(int i=0;i<2;i++){LineTo(g,Muted,offset-15,2+i*4,offset-33,i*8,1);LineTo(g,Muted,offset+15,2+i*4,offset+33,i*8,1);}
            if(mutant){LineTo(g,Shade(fur,.25f),-10,9,11,9,4);LineTo(g,White,6,8,6,13,2);Poly(g,White,new(-9,6),new(-4,6),new(-6,14));LineTo(g,Red,-18,-24,-8,-19,3);}
        }
        else if(kind!="Atigrado"){LineTo(g,Shade(fur,.85f),-9,-27,0,-20,2);LineTo(g,Shade(fur,.85f),9,-27,0,-20,2);}
        g.Restore(head);g.Restore(state);
    }
    public static void Bowl(Graphics g,float x,float y,float scale,bool water=false)
    {
        var st=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(scale,scale);
        Color color=water?Blue:Red;
        Ellipse(g,Color.FromArgb(60,0,0,0),-34,5,68,22);
        Box(g,-32,-4,64,25,MixFur(color,Navy,.2f),12);
        Box(g,-30,-5,60,23,color,11);
        Ellipse(g,MixFur(color,White,.5f),-31,-14,62,26);Ellipse(g,Navy,-25,-10,50,18);
        if(water){Ellipse(g,MixFur(Blue,Navy,.3f),-24,-9,48,16);LineTo(g,White,-12,-3,6,-3,1.5f);}
        else for(int i=0;i<13;i++)Ellipse(g,i%2==0?Gold:Color.FromArgb(174,120,76),-19+(i*13)%36,-7+(i*7)%10,6,5);
        Paw(g,0,11,.25f,White);Ellipse(g,Color.FromArgb(80,White),-24,6,8,4);g.Restore(st);
    }
    public static void DrawPod(Graphics g,float x,float y,float scale,Color light)
    {
        var st=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(scale,scale);
        Ellipse(g,Color.FromArgb(60,0,0,0),-74,52,148,26);
        Color shell=MixFur(Line,light,.4f);
        Box(g,-70,-91,140,165,shell,38);Box(g,-65,-87,130,154,MixFur(shell,White,.18f),34);
        Box(g,-58,-79,116,137,Navy,29);Box(g,-54,-75,108,128,Color.FromArgb(36,light),26);
        for(int i=0;i<3;i++)Ellipse(g,Color.FromArgb(7,light),-47+i*5,-66+i*4,94-i*10,108-i*8);
        Box(g,-47,-58,6,68,Color.FromArgb(75,White),3);
        Ellipse(g,Color.FromArgb(60,White),27,-66,13,24);
        Box(g,-65,47,130,25,shell,12);LineTo(g,light,-44,53,44,53,2);
        Paw(g,0,62,.23f,White);Ellipse(g,light,45,58,6,6);Ellipse(g,Gold,-50,58,6,6);
        g.Restore(st);
    }
}
