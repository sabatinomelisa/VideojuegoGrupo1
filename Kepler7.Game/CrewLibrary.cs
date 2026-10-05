using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private BibliotecaTripulantes? crewLibrary;
    private BibliotecaTripulantes? libraryComparison;
    private int crewPage,librarySpecies,libraryFilter;
    private string librarySearch="",libraryQueryKey="";
    private CensoPoblacion? libraryCensus;
    private RegistroPoblacion[]? libraryRows;
    private Pantalla libraryReturn=Pantalla.Laboratorio;
    private void OpenLibrary(Pantalla origin){libraryReturn=origin;crewLibrary=BibliotecaTripulantes.DesdeProgreso(progress);crewPage=0;libraryCensus=new CensoPoblacion(progress);libraryRows=null;librarySearch="";librarySpecies=libraryFilter=0;if(progress.RegistrarCenso()){Save();Say("¡Censo recuperado! Insignia Flyweight +100 XP");}Change(Pantalla.Biblioteca);}
    private void CloseLibrary()=>Change(libraryReturn);
    private void DrawLibrary(Graphics g)
    {
        crewLibrary ??= BibliotecaTripulantes.DesdeProgreso(progress);
        var culture=System.Globalization.CultureInfo.GetCultureInfo("es-AR");
        int total=crewLibrary.ConFlyweight.Count;
        libraryCensus??=new CensoPoblacion(progress);
        string query=string.Join("|",librarySearch,librarySpecies,libraryFilter);
        if(libraryRows==null || query!=libraryQueryKey)
        {
            libraryQueryKey=query;
            libraryRows=libraryCensus.Filtrar(populationFilters[libraryFilter],librarySearch,speciesFilters[librarySpecies]).Registros;
            // Rebuild the comparison only when the filters change, never on every frame.
            libraryComparison=libraryRows.Length==total?crewLibrary:new BibliotecaTripulantes(libraryRows.Select(r=>r.Tripulante).ToList());
        }
        var comparison=libraryComparison!;
        var shown=libraryRows;
        int pages=Math.Max(1,(shown.Length+4)/5);
        crewPage=Math.Clamp(crewPage,0,pages-1);
        Header(g,"Biblioteca / Flyweight",shown.Length.ToString("N0",culture)+(shown.Length==1?" persona filtrada · ":" personas filtradas · ")+comparison.ObjetosCompartidos+(comparison.ObjetosCompartidos==1?" especie compartida":" especies compartidas"));
        Button(g,libraryReturn==Pantalla.Poblacion?"← Volver al censo":"← Volver a clonar",40,127,240,40,CloseLibrary);
        TextAt(g,"Estación completa: "+total.ToString("N0",culture)+" personas · "+crewLibrary.ObjetosCompartidos+" especies · "+progress.Pilotos.Count.ToString("N0",culture)+" pilotos",310,140,14,Gold,true);
        var figures=new[]{("CON FLYWEIGHT",comparison.ObjetosCompartidos.ToString(),"objetos compartidos por este grupo",Mint),
            ("SIN FLYWEIGHT",comparison.ObjetosSinCompartir.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("es-AR")),"un objeto de especie por persona filtrada",Red),
            ("REDUCCIÓN", shown.Length==0?"—":comparison.ReduccionPorcentaje.ToString("N4",culture)+" %","menos objetos de especie",Gold)};
        for(int i=0;i<figures.Length;i++)
        {
            float x=40+i*380;Box(g,x,187,360,110,PanelColor);
            TextAt(g,figures[i].Item1,x+20,201,14,figures[i].Item4,true);
            TextAt(g,figures[i].Item2,x+20,224,31,figures[i].Item4,true);
            TextAt(g,figures[i].Item3,x+20,268,13,Muted);
        }
        for(int i=0;i<populationFilters.Length;i++)
        {
            int group=i;
            Button(g,populationFilters[i],40+i*161,307,154,34,()=>{libraryFilter=group;librarySearch="";librarySpecies=crewPage=0;libraryRows=null;},libraryFilter==i);
        }
        Button(g,"Buscar: "+ShortLabel(librarySearch.Length==0?"nombre":librarySearch,25),40,351,380,34,()=>AskText("Buscar por nombre",librarySearch,60,value=>{librarySearch=value;crewPage=0;return true;}));
        Button(g,"Especie: "+speciesFilters[librarySpecies],432,351,270,34,()=>{librarySpecies=(librarySpecies+1)%speciesFilters.Length;crewPage=0;});
        Button(g,"Limpiar filtros",714,351,165,34,()=>{librarySearch="";librarySpecies=crewPage=0;libraryRows=null;});
        TextAt(g,shown.Length+" resultados · "+populationFilters[libraryFilter],895,361,13,Muted);
        for(int i=0;i<5 && crewPage*5+i<shown.Length;i++)
        {
            var person=shown[crewPage*5+i].Tripulante;float x=40+i*226;
            Box(g,x,395,214,229,PanelColor);Cat(g,x+107,444,.65f,Uniforme(person.Uniforme.Color),false,MathF.PI/2,person.Aspecto.Tipo,time);
            TextAt(g,ShortLabel(person.Nombre,21),x+14,479,17,White,true);
            TextAt(g,ShortLabel(person.Identificador,24),x+14,502,11,Muted);
            TextAt(g,person.Rol+" · "+person.Rango,x+14,519,11,Muted);
            TextAt(g,person.Especie.Nombre,x+14,540,16,Gold,true);
            Ellipse(g,ColorTranslator.FromHtml(person.Especie.Color),x+182,541,16,16);
            TextAt(g,person.Especie.PlanetaOrigen,x+14,565,13,Muted);
            Wrap(g,person.Especie.Dieta,x+14,587,190,31,12,Muted);
        }
        if(shown.Length==0)Wrap(g,"No hay coincidencias en "+populationFilters[libraryFilter]+" con esos filtros. Elegí otra especie o tocá Limpiar filtros.",65,450,1050,90,22,Muted);
        Button(g,"‹",40,632,60,37,()=>crewPage=Math.Max(0,crewPage-1),false,crewPage>0);
        Button(g,(crewPage+1)+" / "+pages+" · Ir",108,632,191,37,()=>JumpPage(pages,n=>crewPage=n));
        Button(g,"›",307,632,60,37,()=>crewPage=Math.Min(pages-1,crewPage+1),false,crewPage<pages-1);
        TextAt(g,"Comparación del grupo filtrado completo, no sólo de esta página.",395,642,13,Mint);
        TextAt(g,"Cada clon es un gato nuevo; su especie se comparte. Clones creados: "+(progress.Pilotos.Count+(progress.Ingeniero==null?0:1))+" · Especies nuevas por clon: 0",40,684,14,Mint);
        TextAt(g,"Grupo, especie y nombre actualizan los tres contadores. El total de la estación aparece arriba.",40,712,13,Muted);
    }
}
