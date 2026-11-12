using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private void DrawLotQuantity(Graphics g,string label,float x,float y,int value,Action<int> set,bool enabled,bool editable=false)
    {
        if(editable)Button(g,label+" ×"+value,x,y,127,31,()=>AskText("Cantidad de pilotos · 1 a 10.000",value.ToString(),5,text=>{if(!int.TryParse(text,out int quantity) || quantity<1 || quantity>10000)return false;set(quantity);return true;},true),false,enabled);
        else TextAt(g,label+" ×"+value,x,y+7,16,Gold,true);
        Button(g,"−10",x+135,y,61,31,()=>set(Math.Max(1,value-10)),false,enabled);
        Button(g,"+10",x+204,y,61,31,()=>set(value==1?10:Math.Min(10000,value+10)),false,enabled);
        Button(g,"+100",x+273,y,72,31,()=>set(value==1?100:Math.Min(10000,value+100)),false,enabled);
        Button(g,"Solo 1",x+353,y,82,31,()=>set(1),false,enabled);
    }
}
