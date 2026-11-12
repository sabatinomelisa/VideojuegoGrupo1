using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private void EnterRecoveredLab(){roomCat=new(600,540);Change(Pantalla.SalaLab);}
    // Una única sala para antes y después de reparar. Los paneles son acercamientos
    // a la máquina central, nunca un segundo laboratorio.
    private void DrawLabEnvironment(Graphics g)
    {
        CozyRoom(g,Mint);
        Color state=progress.LaboratorioReparado?Mint:Red;
        TextAt(g,"RESERVAS DE ADN · 5 ESPECIES",102,177,13,Mint,true);
        string[] reserves={"Atigrado","Negro","Siamés","Calicó","Blanco"};
        for(int i=0;i<reserves.Length;i++)
        {
            float x=125+i*72;
            DrawPod(g,x,237,.45f,Mint);
            Cat(g,x,238,.36f,Muted,kind:reserves[i],clock:time+i*.7f);
            TextAt(g,reserves[i],x-25,279,10,Muted);
        }
        Box(g,475,179,250,130,Line,10);
        DrawPod(g,557,245,.65f,state);DrawPod(g,650,245,.65f,state);
        TextAt(g,"CLONADORA BIO-01",500,158,15,state,true);
        Box(g,501,294,197,31,Navy,5);LineTo(g,state,512,308,687,308,3);
        TextAt(g,progress.LaboratorioReparado?"SISTEMA RESTAURADO":"AVERÍA · REPARAR CON E",484,338,14,state,true);
        if(!progress.LaboratorioReparado){LineTo(g,Red,594,255,604,271,3);LineTo(g,Gold,604,271,590,281,3);}
        if(progress.MedallaRescate is { } medal)TextAt(g,"RESCATE · "+medal.Nivel.ToUpperInvariant()+" · "+RescueTime(medal.TiempoSegundos),799,189,15,MedalColor(medal.Nivel),true);
        else TextAt(g,"BIOTECNOLOGÍA · LAB BIO-01",799,189,12,Muted);

        DecorateFelineRoom(g,false,true);DrawPopulationConsole(g);
        Box(g,514,620,172,25,Mint,3);TextAt(g,"SALIDA",568,621,15,Navy,true);
    }
    private void DrawUnifiedLab(Graphics g)
    {
        DrawLabEnvironment(g);Header(g,"Laboratorio BIO-01",progress.LaboratorioReparado?"Laboratorio de clonación restaurado":"Repará la clonadora");
        if(progress.LaboratorioReparado)DrawRoomIndicators(g,true);
        Cat(g,roomCat.X,roomCat.Y,.92f,Mint,false,roomFacing,"Blanco",time,keys.Count>0?roomStride:0,hero:true);
        TextAt(g,RoomAction(),100,670,19,Gold,true);
    }
}
