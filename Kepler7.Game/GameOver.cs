using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private float gameOverTime=-1;
    private const float GameOverDuration=3.2f;
    private void BeginGameOver()
    {
        if(gameOverTime>=0)return;
        gameOverTime=0;keys.Clear();buttons.Clear();firing=false;CancelRepairDrag();Capture=false;SyncEmergencyAudio();
    }
    private void DrawGameOver(Graphics g)
    {
        buttons.Clear();float t=Math.Max(0,gameOverTime);
        float dark=Math.Clamp(t/1.6f,0,1);
        Box(g,0,0,1200,760,Color.FromArgb((int)(90+150*dark),Navy),0);
        if(t<1.9f)
        {
            float shake=MathF.Sin(t*47)*Math.Max(0,1-t/1.9f)*8;
            var st=g.Save();g.TranslateTransform(shake,0);
            DrawPod(g,600,370,1.8f,Red);
            float radius=25+t*88;
            Ellipse(g,Color.FromArgb((int)(130*Math.Max(0,1-t/1.9f)),Red),600-radius,350-radius,radius*2,radius*2);
            for(int i=0;i<24;i++)
            {
                float a=i*MathF.PI/12,reach=25+t*(55+i%5*18);
                LineTo(g,i%2==0?Gold:Red,600+MathF.Cos(a)*reach,354+MathF.Sin(a)*reach,600+MathF.Cos(a)*(reach+14),354+MathF.Sin(a)*(reach+14),3);
            }
            TextAt(g,"SOBRECARGA DE LA CLONADORA",370,548,24,Red,true);g.Restore(st);
        }
        if(t>.9f)
        {
            float close=Math.Clamp((t-.9f)/1.2f,0,1);
            Box(g,-600+600*close,0,600,760,Color.FromArgb(12,14,24),0);
            Box(g,1200-600*close,0,600,760,Color.FromArgb(12,14,24),0);
            LineTo(g,Color.FromArgb(80,Red),600*close,0,600*close,760,4);
            LineTo(g,Color.FromArgb(80,Red),1200-600*close,0,1200-600*close,760,4);
        }
        if(t>1.7f)
        {
            float entry=Math.Clamp((t-1.7f)/.8f,0,1);float scale=1.25f-.25f*entry;
            var st=g.Save();g.TranslateTransform(600,290-22*(1-entry));g.ScaleTransform(scale,scale);
            TextAt(g,"GAME OVER",-213,-45,65,Color.FromArgb((int)(255*entry),Red),true);g.Restore(st);
            if(t>2.3f)
            {
                TextAt(g,"¡Se acabó el tiempo!",444,351,28,White,true);
                TextAt(g,"La amenaza mutante superó la contención.",365,405,21,Muted);
            }
        }
        if(t>=GameOverDuration)
        {
            Button(g,"Reintentar · 02:00",325,497,260,53,RetryEmergency,true);
            Button(g,"Volver al menú",615,497,260,53,()=>{Save();Change(Pantalla.Inicio);});
        }
    }
}
