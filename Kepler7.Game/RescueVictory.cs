using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private bool rescueVictory;
    private static Color MedalColor(string nivel)=>nivel=="Oro"?Gold:nivel=="Plata"?Color.FromArgb(196,215,232):Color.FromArgb(202,137,83);
    private static string RescueTime(float seconds){int n=(int)Math.Ceiling(seconds);return (n/60).ToString("00")+":"+(n%60).ToString("00");}
    private void CompleteRescue()
    {
        if(emergency.Agotada || corridor.Bajas!=corridor.Total)return;
        emergency.Resolver();progress.LaboratorioReparado=true;
        progress.MedallaRescate=MedallaRescate.Obtener(emergency.Restante);
        progress.XP+=100;rescueVictory=true;keys.Clear();firing=false;buttons.Clear();SyncEmergencyAudio();Save();
    }
    private void CloseRescueVictory(){rescueVictory=false;noticeTime=0;roomCat=new(600,410);Change(Pantalla.SalaLab);}
    private void DrawRescueVictory(Graphics g)
    {
        if(!rescueVictory || progress.MedallaRescate==null)return;
        var medal=progress.MedallaRescate;var color=MedalColor(medal.Nivel);
        buttons.Clear();Box(g,0,0,1200,760,Color.FromArgb(242,Navy),0);
        Box(g,130,84,940,585,PanelColor,24);
        TextAt(g,"¡FELICIDADES!",430,114,38,color,true);
        TextAt(g,"Eliminaste la amenaza mutante",342,170,29,White,true);
        TextAt(g,"La clonación ha sido habilitada",370,212,25,Mint,true);
        float y=348+MathF.Sin(time*2)*3;
        Poly(g,Red,new(547,y-92),new(581,y-92),new(605,y-19),new(573,y-10));
        Poly(g,Blue,new(619,y-92),new(653,y-92),new(627,y-10),new(595,y-19));
        Ellipse(g,Color.FromArgb(28,color),516,y-74,168,168);
        Ellipse(g,color,537,y-53,126,126);Ellipse(g,Navy,547,y-43,106,106);
        Ellipse(g,color,553,y-37,94,94);Paw(g,600,y+8,1.65f,Navy);
        for(int i=0;i<12;i++){float a=i*MathF.PI/6+time*.1f;float x=600+MathF.Cos(a)*156,sy=y+MathF.Sin(a)*103;LineTo(g,color,x-3,sy,x+3,sy,2);LineTo(g,color,x,sy-3,x,sy+3,2);}
        TextAt(g,"MEDALLA DE "+medal.Nivel.ToUpperInvariant(),423,455,27,color,true);
        TextAt(g,"Tiempo de misión: "+RescueTime(medal.TiempoSegundos),453,500,21,White,true);
        TextAt(g,"La estación está a salvo. Tu tripulación te espera.",361,544,17,Muted);
        Button(g,"Entrar al laboratorio restaurado →",363,591,475,51,CloseRescueVictory,true);
    }
}
