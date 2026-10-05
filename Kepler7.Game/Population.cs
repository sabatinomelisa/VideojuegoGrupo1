using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private CensoPoblacion? population;
    private int populationPage, populationFilter, populationSelection;
    private string populationSearch="";
    private int populationSpecies,populationStatus;
    private readonly string[] speciesFilters={"Todas","Atigrado","Negro","Siamés","Calicó","Blanco"};
    private readonly string[] statusFilters={"Todos","Libres","A bordo"};
    private string populationQueryKey="";
    private CensoFiltrado? populationView;
    private void ResetPopulationPage(){populationPage=populationSelection=0;populationView=null;}
    private void JumpPage(int pages,Action<int> set)
    {
        AskText("Ir a página · 1 a "+pages,"",9,value=>{if(!int.TryParse(value,out int n) || n<1 || n>pages)return false;set(n-1);return true;},true);
    }
    private readonly string[] populationFilters = { "Todos", "Exploradora", "Combate", "Carga", "Disponibles", "Habitantes", "Tripulantes" };
    private void OpenPopulation()
    {
        if(progress.RegistrarCenso()){Save();Say("¡Cada michi cuenta! Censo recuperado · Insignia Flyweight +100 XP");}
        population=new CensoPoblacion(progress);populationPage=populationFilter=populationSelection=0;populationSearch="";populationSpecies=populationStatus=0;populationView=null;
        Change(Pantalla.Poblacion);
    }
    private static string ShortLabel(string value,int max) => value.Length>max?value[..(max-1)]+"…":value;
    private void DrawPopulationConsole(Graphics g)
    {
        bool online=progress.LaboratorioReparado;
        Color light=online?Mint:Red;
        int total=progress.Habitantes.Count+progress.Pilotos.Count+(progress.Ingeniero==null?0:1);
        TextAt(g,"CENTRO DE COMANDOS",916,250,14,Mint,true);
        // Monitor on a pedestal, with a separate desk, keyboard and mouse.
        Ellipse(g,Color.FromArgb(45,0,0,0),924,381,211,16);
        Box(g,1005,346,45,23,Line,5);
        Box(g,985,363,85,8,Muted,3);
        Box(g,923,273,211,85,MixFur(Line,Blue,.22f),9);
        Box(g,930,280,197,68,Navy,5);
        Box(g,935,285,187,58,Color.FromArgb(30,light),4);
        LineTo(g,Color.FromArgb(70,light),942,304,1114,304,1);
        TextAt(g,"POBLACIÓN",946,288,11,light,true);
        Ellipse(g,light,1100,290,5,5);
        if(online)
        {
            for(int i=0;i<4;i++)Box(g,945+i*7,330-i*4,4,7+i*4,light,1);
            TextAt(g,total.ToString("N0"),981,309,16,White,true);
            TextAt(g,"habitantes y personal",981,328,9,Muted);
            float scan=285+(time*12%56);
            LineTo(g,Color.FromArgb(24,light),937,scan,1119,scan,1);
        }
        else TextAt(g,"SIN CONEXIÓN",955,316,13,Red,true);
        Ellipse(g,light,1026,350,4,4);
        Box(g,923,366,211,24,Line,6);
        Box(g,937,369,133,16,Navy,3);
        for(int row=0;row<2;row++)for(int key=0;key<12;key++)
            Box(g,942+key*10,372+row*5,7,3,Muted,1);
        Ellipse(g,Navy,1089,369,16,15);LineTo(g,light,1097,371,1097,375,1);
        TextAt(g,online?"E · Consultar población":"Restaurá la clonadora",931,398,13,online?Gold:Muted);

    }
    private void DrawPopulation(Graphics g)
    {
        population ??=new CensoPoblacion(progress);
        var filtro=populationFilters[populationFilter];
        string queryKey=string.Join("|",filtro,populationSearch,populationSpecies,populationStatus);
        if(populationView==null || queryKey!=populationQueryKey){populationQueryKey=queryKey;populationView=population.Filtrar(filtro,populationSearch,speciesFilters[populationSpecies],"Todos",statusFilters[populationStatus]);}
        var census=populationView;
        bool residents=filtro=="Habitantes", crew=filtro=="Tripulantes";
        bool nonPilots=residents || crew;
        Header(g,"Control de población","Habitantes · Personal y pilotos");
        Button(g,"← Volver al laboratorio",40,122,263,39,ExitConsole);
        TextAt(g,populationFilter==0 ? "Toda tu tripulación" : residents ? "Habitantes de Kepler-7" : crew ? "Tripulación · Personal no piloto" : "Pilotos · "+filtro,329,134,14,Muted);
        Button(g,"Con Flyweight / Sin Flyweight",780,122,380,39,()=>OpenLibrary(Pantalla.Poblacion));
        var metrics=new[]{(populationFilter==0 ? "TOTAL DE PERSONAS" : "PILOTOS · "+filtro.ToUpperInvariant(),census.Total.ToString(),populationFilter==0 ? "Habitantes + ingeniero + pilotos" : "Especialistas de tu flota"),
            ("PILOTOS DISPONIBLES",census.Disponibles.ToString(),"Sin asignación a una nave"),
            ("PILOTOS A BORDO",census.Asignados.ToString(),"Asignados en el sector de abordaje"),
            (populationFilter==4 ? "NAVES SIN PILOTO" : "NAVES CONSTRUIDAS",census.Naves.ToString(),census.NavesSinPiloto+" esperan piloto")};
        if(nonPilots)metrics=new[]{
            ("TRIPULACIÓN NO PILOTO",census.Total.ToString(),"Personal de la estación"),
            ("INGENIERÍA",census.Registros.Count(r=>r.Tripulante.Rol=="Ingeniero").ToString(),"Construcción y mantenimiento"),
            ("OTROS ROLES",census.Registros.Count(r=>r.Tripulante.Rol!="Ingeniero").ToString(),"Personal con otras funciones"),
            ("ESPECIES PRESENTES",census.Registros.Select(r=>r.Tripulante.Especie.Nombre).Distinct().Count().ToString(),"Dentro de este grupo")};
        if(residents)metrics=new[]{
            ("POBLACIÓN",census.Total.ToString(),"Habitantes fundacionales de Kepler-7"),
            ("ESPECIES",census.Registros.Select(r=>r.Tripulante.Especie.Nombre).Distinct().Count().ToString(),"Datos compartidos con Flyweight"),
            ("CAPITANES",census.Registros.Count(r=>r.Tripulante.Rango=="Capitán").ToString(),"Rango del registro de habitantes"),
            ("OTROS RANGOS",census.Registros.Count(r=>r.Tripulante.Rango!="Capitán").ToString(),"Resto de la población")};
        for(int i=0;i<metrics.Length;i++)
        {
            float x=40+i*284;Box(g,x,177,268,112,PanelColor,9);
            TextAt(g,metrics[i].Item1,x+14,191,13,Muted,true);
            TextAt(g,metrics[i].Item2,x+14,213,31,i==2?Gold:Mint,true);
            TextAt(g,metrics[i].Item3,x+14,262,11,Muted);
        }
        for(int i=0;i<populationFilters.Length;i++)
        {
            int n=i;Button(g,populationFilters[i],40+i*161,307,154,34,()=>{populationFilter=n;populationSearch="";populationSpecies=populationStatus=0;ResetPopulationPage();},populationFilter==i);
        }
        Button(g,"Buscar: "+ShortLabel(populationSearch.Length==0?"nombre":populationSearch,19),40,352,270,34,()=>AskText("Buscar tripulante",populationSearch,60,value=>{populationSearch=value;ResetPopulationPage();return true;}));
        Button(g,"Especie: "+speciesFilters[populationSpecies],318,352,224,34,()=>{populationSpecies=(populationSpecies+1)%speciesFilters.Length;ResetPopulationPage();});
        Button(g,"Estado: "+statusFilters[populationStatus],550,352,210,34,()=>{populationStatus=(populationStatus+1)%statusFilters.Length;ResetPopulationPage();});
        var rows=census.Registros;
        int pages=Math.Max(1,(rows.Length+4)/5);populationPage=Math.Clamp(populationPage,0,pages-1);
        populationSelection=Math.Clamp(populationSelection,0,Math.Max(0,rows.Length-1));
        Box(g,40,398,720,246,PanelColor,9);
        TextAt(g,"TRIPULANTE",60,410,12,Muted,true);TextAt(g,"ROL / ESPECIALIDAD",281,410,12,Muted,true);TextAt(g,"ASIGNACIÓN",446,410,12,Muted,true);
        for(int i=0;i<5 && populationPage*5+i<rows.Length;i++)
        {
            int index=populationPage*5+i;var row=rows[index];var t=row.Tripulante;float y=436+i*38;
            Button(g,"",50,y,699,36,()=>populationSelection=index);
            if(index==populationSelection)Box(g,50,y,699,36,Line,9);
            TextAt(g,ShortLabel(t.Nombre,23),62,y+10,14,White,true);
            TextAt(g,t.Rol=="Ingeniero"?"Ingeniería":t.Rol=="Piloto"?t.Especialidad:t.Rol,281,y+10,13,White);
            TextAt(g,ShortLabel(row.Destino,31),446,y+10,13,row.NumeroNave.HasValue?Gold:Mint);
        }
        if(rows.Length==0)Wrap(g,"No hay coincidencias en "+filtro+" con esos filtros. Probá otra especie o tocá Limpiar filtros.",68,470,640,65,18,Muted);
        Box(g,780,352,380,292,PanelColor,9);
        if(rows.Length>0)
        {
            var row=rows[populationSelection];var t=row.Tripulante;
            Cat(g,1106,413,.65f,Uniforme(t.Uniforme.Color),false,MathF.PI/2,t.Aspecto.Tipo,time);
            TextAt(g,"FICHA DEL TRIPULANTE",799,365,13,Mint,true);
            TextAt(g,ShortLabel(t.Nombre,26),799,389,18,White,true);
            TextAt(g,t.Rol+" · "+t.Rango,799,418,13,Gold);
            TextAt(g,"Pelaje: "+t.Especie.Nombre,799,443,14,White);
            TextAt(g,"Especialidad: "+(t.Rol=="Ingeniero"?"Ingeniería":t.Rol=="Piloto"?t.Especialidad:t.Rol),799,468,14,White);
            TextAt(g,row.Destino,799,493,14,Mint,true);
            TextAt(g,"HABILIDADES",799,519,11,Muted,true);
            Wrap(g,string.Join(" · ",t.Habilidades),799,538,338,43,13,White);
            TextAt(g,"ID: "+ShortLabel(t.Identificador,34),799,587,11,Muted);
            TextAt(g,"Remera: "+t.Uniforme.Color,799,612,12,Muted);
        }
        else Wrap(g,nonPilots?"Todavía no hay personal en este grupo.":"Cloná pilotos para ver sus fichas.",800,394,330,70,16,Muted);
        Button(g,"‹",40,654,48,33,()=>{populationPage--;populationSelection=populationPage*5;},false,populationPage>0);
        Button(g,(populationPage+1)+" / "+pages+" · Ir a página",100,654,224,33,()=>JumpPage(pages,n=>{populationPage=n;populationSelection=n*5;}));
        Button(g,"›",332,654,48,33,()=>{populationPage++;populationSelection=populationPage*5;},false,populationPage<pages-1);
        TextAt(g,nonPilots ? filtro+": "+census.Total+(residents?" habitantes":" tripulantes no pilotos") : populationFilter>0 && populationFilter<4 ? filtro+": "+census.Total+" pilotos · "+census.Disponibles+" disponibles · "+census.Asignados+" a bordo" : "Pilotos: "+string.Join("   ·   ",RecetaNave.Todas.Select(r=>r.Nombre+" "+census.Registros.Count(t=>t.Tripulante.Rol=="Piloto" && t.Tripulante.Especialidad==r.Nombre))),400,662,14,Gold);
        TextAt(g,progress.FlyweightDominado?"★ FLYWEIGHT · Censo recuperado":"Censo por recuperar",780,704,14,Gold,true);
        Button(g,"Limpiar filtros",40,700,160,33,()=>{populationSearch="";populationSpecies=populationStatus=0;ResetPopulationPage();});
        TextAt(g,rows.Length+" resultados · "+filtro+" · "+speciesFilters[populationSpecies],216,709,13,Muted);
    }
}
