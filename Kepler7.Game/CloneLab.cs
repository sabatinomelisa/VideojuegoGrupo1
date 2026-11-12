using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private int furIndex;
    private int batchSize=1;
    private bool batchEditing,batchSelected;
    private string batchText="1";
    private bool ValidBatch => int.TryParse(batchText,out int n) && n>=1 && n<=10000 && (n==1 || n%10==0);
    private void SetBatch(int n){batchSize=Math.Clamp(n,1,10000);batchText=batchSize.ToString();batchEditing=false;}
    private void CommitBatch(){if(ValidBatch){SetBatch(int.Parse(batchText));}else Say("Cantidad: 1 o múltiplos de 10, hasta 10.000 por lote.");}
    private void DrawLab(Graphics g)
    {
        bool engineer=progress.Ingeniero==null;
        DrawLabEnvironment(g);
        Box(g,40,150,1120,510,Color.FromArgb(230,Navy),0);
        Header(g,"LAB BIO-01 / Clonadora",engineer?"Un ingeniero para la estación":"¡Más bigotes para tu tripulación!");Nav(g,true);
        var original=Prototipos.ConAspecto(engineer?progress.IngenieroBase:progress.PilotoBase,Prototipos.Tipos[furIndex]);
        // Esta es una copia real y aislada: nunca se editan el original ni el último clon al elegir colores.
        var preview=(Tripulante)original.Clonar();preview.Uniforme.Color=colors[colorIndex];preview.Nombre=cloneName;
        preview.Habilidades.Add(engineer?"Ensamblaje de naves":RecetaNave.Todas[selectedPilotRecipe].HabilidadPiloto);
        Box(g,40,186,320,468,PanelColor);Box(g,379,186,400,468,PanelColor);Box(g,798,186,362,468,PanelColor);
        TextAt(g,"MATRIZ DE ORIGEN",64,208,15,Muted,true);TextAt(g,"TU PRÓXIMO TRIPULANTE",403,208,15,Mint,true);
        DrawPod(g,200,340,1.05f,Mint);DrawPod(g,579,340,1.05f,Uniforme(preview.Uniforme.Color));
        Cat(g,200,337,1.4f,Uniforme(original.Uniforme.Color),false,MathF.PI/2,original.Aspecto.Tipo,time);
        Cat(g,579,337,1.4f,Uniforme(preview.Uniforme.Color),false,MathF.PI/2,preview.Aspecto.Tipo,time);
        TextAt(g,original.Aspecto.Tipo,65,449,23,White,true);
        TextAt(g,original.Rol+" base · uniforme "+original.Uniforme.Color,65,484,14,Muted);
        for(int i=0;i<original.Habilidades.Count;i++)TextAt(g,"• "+original.Habilidades[i],65,518+i*24,15);
        TextAt(g,"El comienzo de una nueva aventura",65,613,13,Mint);
        TextAt(g,string.IsNullOrWhiteSpace(cloneName)?"Tu nuevo michi":cloneName,404,449,23,White,true);
        TextAt(g,preview.Aspecto.Tipo+" · remera "+preview.Uniforme.Color,404,484,16,Mint);
        for(int i=0;i<preview.Habilidades.Count;i++)TextAt(g,"• "+preview.Habilidades[i],404,516+i*23,15,i==preview.Habilidades.Count-1?Gold:White);
        if(lastClone!=null)TextAt(g,"Último: "+lastClone.Nombre+" · "+lastClone.Uniforme.Color,404,617,13,Muted);
        TextAt(g,engineer?"Preparar ingeniero":"Preparar piloto",820,207,23,White,true);
        TextAt(g,"NOMBRE",820,251,12,Muted,true);
        Box(g,819,274,318,40,naming?Line:Navy,7);TextAt(g,cloneName+(naming&&((int)(time*2)%2==0)?"|":""),832,284,17);
        buttons.Add((new RectangleF(819,274,318,40),()=>{batchEditing=false;naming=true;nameSelected=true;}));
        TextAt(g,"TIPO DE MICHI",820,331,12,Muted,true);
        for(int i=0;i<5;i++){int n=i;Button(g,Prototipos.Tipos[i],820+(i%3)*107,353+(i/3)*40,100,34,()=>{furIndex=n;naming=false;},furIndex==i);}
        TextAt(g,"COLOR DE REMERA",820,441,12,Muted,true);
        for(int i=0;i<4;i++)
        {
            int n=i;float x=827+i*79;Ellipse(g,Uniforme(colors[i]),x,465,31,31);
            if(i==colorIndex){using var pen=new Pen(White,2);g.DrawEllipse(pen,x-4,461,39,39);}
            buttons.Add((new RectangleF(x-5,459,44,44),()=>{colorIndex=n;naming=false;}));
        }
        if(!engineer)
        {
            TextAt(g,"ESPECIALIDAD",820,516,12,Muted,true);
            for(int i=0;i<3;i++){int n=i;Button(g,RecetaNave.Todas[i].Nombre,819+i*108,537,102,34,()=>selectedPilotRecipe=n,selectedPilotRecipe==i);}
        }
        else TextAt(g,"Habilidad: ensamblaje de naves",820,535,15,Gold);
        if(!engineer)
        {
            TextAt(g,"CANTIDAD · 1 O MÚLTIPLOS DE 10",405,564,11,Muted,true);
            Button(g,"−10",405,583,51,29,()=>SetBatch(Math.Max(1,(ValidBatch?int.Parse(batchText):batchSize)-10)));
            Box(g,464,581,115,33,batchEditing?Line:Navy,5);
            TextAt(g,batchText+(batchEditing && (int)(time*2)%2==0?"|":""),478,588,16,ValidBatch?White:Red,true);
            buttons.Add((new RectangleF(464,581,115,33),()=>{batchEditing=true;batchSelected=true;naming=false;}));
            Button(g,"+10",588,583,55,29,()=>SetBatch((ValidBatch?int.Parse(batchText):batchSize)==1?10:Math.Min(10000,(ValidBatch?int.Parse(batchText):batchSize)+10)));
            Button(g,"Solo 1",652,583,100,29,()=>SetBatch(1));
        }
        Button(g,engineer?"Crear ingeniero":ValidBatch?(int.Parse(batchText)==1?"Clonar 1 piloto":"Clonar "+batchText+" pilotos"):"Cantidad inválida",819,593,318,43,()=>
        {
            naming=false;batchEditing=false;if(!engineer){if(!ValidBatch)return;batchSize=int.Parse(batchText);}
            string name=cloneName.Trim();
            if(!engineer && batchSize==1 && name.Length>0 && progress.Pilotos.Any(p=>p.Nombre.Equals(name,StringComparison.OrdinalIgnoreCase))) {Say("Ya existe ese piloto. Elegí otro nombre.");return;}
            if(!engineer && batchSize>1)
            {
                var lote=progress.ClonarPilotos(batchSize,name,colors[colorIndex],RecetaNave.Todas[selectedPilotRecipe],Prototipos.Tipos[furIndex]);
                lastClone=lote[^1];Say("¡"+lote.Count+" nuevos pilotos se sumaron a tu tripulación! Sus naves los esperan.");
                cloneName="Michi "+(progress.Pilotos.Count+1);Save();ShowSummary(new ResumenOperacion{Titulo="¡"+lote.Count+" nuevos pilotos a bordo!",Detalles=new List<string>{"Especialidad: "+RecetaNave.Todas[selectedPilotRecipe].Nombre,"Pelaje: "+Prototipos.Tipos[furIndex]+" · Remera: "+colors[colorIndex]}.Concat(FleetWaitingLines()).ToList()});return;
            }
            lastClone=engineer?progress.ClonarIngeniero(name,colors[colorIndex],Prototipos.Tipos[furIndex]):progress.ClonarPiloto(name,colors[colorIndex],RecetaNave.Todas[selectedPilotRecipe],Prototipos.Tipos[furIndex]);
            Say("¡Bienvenido a bordo, "+lastClone.Nombre+"! "+(engineer?"El hangar te espera.":"Tu próxima aventura está por despegar."));
            cloneName="Michi "+(progress.Pilotos.Count+1);Save();
        },true,engineer || ValidBatch);
        TextAt(g,"Dales tu toque y preparalos para su próxima aventura.",40,670,15,Gold);
        Button(g,"Biblioteca · Censo completo",858,665,302,35,()=>OpenLibrary(Pantalla.Laboratorio));
    }
}
