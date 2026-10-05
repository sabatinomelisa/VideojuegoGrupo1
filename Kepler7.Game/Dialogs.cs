using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private string? inputTitle;
    private string inputValue="",inputError="";
    private int inputMax=80;
    private bool inputNumeric,inputSelected;
    private Func<string,bool>? inputAccept;
    private ResumenOperacion? operationSummary;
    private bool DialogOpen=>inputTitle!=null || operationSummary!=null;
    private void AskText(string title,string initial,int max,Func<string,bool> accept,bool numeric=false)
    {
        inputTitle=title;inputValue=initial;inputMax=max;inputAccept=accept;inputNumeric=numeric;inputSelected=true;inputError="";
        keys.Clear();firing=false;buttons.Clear();
    }
    private void InputCharacter(char c)
    {
        if(char.IsControl(c) || inputNumeric && !char.IsDigit(c))return;
        if(inputSelected){inputValue="";inputSelected=false;}
        if(inputValue.Length<inputMax)inputValue+=c;
    }
    private void AcceptInput()
    {
        if(inputAccept?.Invoke(inputValue)==true){inputTitle=null;inputAccept=null;buttons.Clear();}
        else inputError="Revisá el valor y volvé a intentar.";
    }
    private void HandleDialogKey(KeyEventArgs e)
    {
        if(inputTitle!=null)
        {
            if(e.KeyCode==Keys.Escape){inputTitle=null;inputAccept=null;buttons.Clear();}
            else if(e.KeyCode==Keys.Enter)AcceptInput();
            else if(e.Control && e.KeyCode==Keys.A)inputSelected=true;
            else if(e.KeyCode==Keys.Back){inputValue=inputSelected?"":inputValue.Length>0?inputValue[..^1]:"";inputSelected=false;}
            else if(e.Control && e.KeyCode==Keys.V){try{foreach(char c in Clipboard.GetText())InputCharacter(c);}catch(System.Runtime.InteropServices.ExternalException){}}
        }
        else if(e.KeyCode is Keys.Enter or Keys.Escape){operationSummary=null;buttons.Clear();}
        e.Handled=true;if(e.KeyCode is Keys.Enter or Keys.Escape or Keys.Back || e.Control)e.SuppressKeyPress=true;
        Invalidate();
    }
    private void ShowSummary(ResumenOperacion summary,bool record=true)
    {
        operationSummary=summary;keys.Clear();firing=false;buttons.Clear();
        if(record){progress.RegistrarOperacion(summary);Save();}
    }
    private void DrawDialogs(Graphics g)
    {
        if(!DialogOpen)return;
        buttons.Clear();Box(g,0,0,1200,760,Color.FromArgb(225,Navy),0);
        Box(g,210,140,780,480,PanelColor,22);
        if(inputTitle!=null)
        {
            TextAt(g,inputTitle,246,175,27,Mint,true);
            TextAt(g,inputNumeric?"Escribí el número y presioná Enter":"Escribí para buscar o editar · Ctrl+A selecciona todo",246,229,16,Muted);
            Box(g,246,278,708,142,inputSelected?Line:Navy,10);
            Wrap(g,inputValue+((int)(time*2)%2==0?"|":""),265,297,670,112,21,White);
            TextAt(g,inputError,246,444,16,Red);
            Button(g,"Cancelar",510,531,200,49,()=>{inputTitle=null;inputAccept=null;buttons.Clear();});
            Button(g,"Aceptar",730,531,224,49,AcceptInput,true);
        }
        else if(operationSummary!=null)
        {
            Paw(g,922,191,.65f,Mint);TextAt(g,operationSummary.Titulo,246,178,27,Mint,true);
            float rowY=245;
            int count=Math.Min(6,operationSummary.Detalles.Count);
            for(int i=0;i<count;i++)
            {
                string detail=operationSummary.Detalles[i];
                float height=Math.Min(Math.Max(41, (float)Math.Ceiling(detail.Length/62f)*25), 535-rowY-(count-i-1)*43);
                Wrap(g,detail,250,rowY,685,height,18,White);rowY+=height+2;
            }
            Button(g,"¡A seguir!",730,551,224,45,()=>{operationSummary=null;buttons.Clear();},true);
        }
    }
}
