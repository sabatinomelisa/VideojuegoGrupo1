using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private sealed record RoomGuide(string Key,string Title,string Instructions);
    private RoomGuide? activeGuide;
    private RoomGuide? CurrentRoomGuide() => screen switch
    {
        Pantalla.Pasillo => new("combate-emergencia","¡CLONADORA COMPROMETIDA!",""),
        Pantalla.Reparacion => new("reparacion","¡Una chispa de esperanza!","La tripulación cuenta con vos. ¡Devolvé la energía a la clonadora antes de que se agote el tiempo!\n\nEncastrá los dos módulos. Mantené clic y girá alrededor de cada tornillo: una vuelta y media en sentido horario.\n\nCon los cuatro tornillos firmes, uní los cables del mismo color y número. ¡Ya casi está!"),
        Pantalla.SalaLab when !progress.LaboratorioReparado => new("laboratorio-roto","¡Llegaste! Ahora salvá la clonadora","Los mutantes quedaron atrás, pero la emergencia sigue. ¡Todavía podés salvar la estación!\n\nAcercate a la máquina del centro y presioná E para empezar la reparación.\n\nCuando vuelva la energía, podrás reunir a tu nueva tripulación."),
        Pantalla.SalaLab or Pantalla.Laboratorio => new("clonacion","¡La tripulación te espera!","¡La clonadora vuelve a la vida! Tu próxima misión: darle a Kepler-7 una tripulación llena de bigotes.\n\nAcercate a la máquina y presioná E. Empezá por el Ingeniero Jefe: te ayudará a poner el hangar en marcha.\n\nDespués, elegí el nombre, pelaje y remera de tus pilotos. ¡Prepará especialistas en exploración, combate y carga para tu flota!"),
        Pantalla.SalaHangar or Pantalla.Hangar => new("ensamblaje","¡Tu próxima nave empieza acá!","Un casco de pescado, motores de atún… ¡Hasta los juguetes pueden llevarte a las estrellas!\n\nAcercate a la mesa y presioná E. Arrastrá las piezas que coincidan con la forma y el color del modelo.\n\nCada error cuesta una vida del taller. Cuando tu nave esté lista, caminá hacia la puerta Sector abordaje de la derecha para elegir a su piloto."),
        Pantalla.Abordaje => new("abordaje","¡Tripulación, a bordo!","Tu nave está lista para despegar. ¡Solo falta un michi que tome los mandos!\n\nElegí una nave y un piloto con la misma especialidad: exploración, combate o carga. Cada piloto puede ocuparse de una nave.\n\nSi forman el equipo perfecto, verás a tu michi subir a bordo. ¡El próximo destino los espera!"),
        Pantalla.Poblacion => new("poblacion","Conocé a tu tripulación","Detrás de cada nave hay un michi con una misión. ¡Acá podés conocer a todo tu equipo!\n\nMirá quién está a bordo, quién espera su nave y qué especialistas necesita tu flota.\n\nElegí un tripulante para abrir su ficha. Usá los filtros y las flechas para encontrar a tus próximos pilotos."),
        Pantalla.Biblioteca => new("biblioteca","Mil historias entre las estrellas","Explorá el archivo de Kepler-7: habitantes y clones de cinco familias felinas con mundos por descubrir.\n\nPasá las páginas para conocer sus nombres, planetas de origen y comidas favoritas.\n\nEste es el archivo de la estación. Cada nuevo clon se suma al registro. Consultá sus grupos en el control de población."),
        Pantalla.SalaComunicaciones or Pantalla.Comunicaciones => new("bridge-historia","¡Una señal de esperanza!","La Tierra no sabe qué pasó en Kepler-7. ¡Hacé que escuche a tu tripulación!\n\nAcercate a la consola y presioná E. Arrastrá las tres antenas hacia sus marcas verdes hasta que la señal quede estable.\n\nCada avance de tu misión habilita un informe y una respuesta de la Tierra. Elegí el cifrado, alineá la señal y transmití. Las antenas se reinician después de cada envío."),
        Pantalla.Estacion => new("estacion","¡La estación vuelve a despertar!","Las puertas están abiertas y la aventura sigue. ¿Cuál será tu próximo paso?\n\nA la izquierda, el laboratorio espera a tus nuevos michis. A la derecha, el hangar aguarda al Ingeniero Jefe. La puerta central lleva a comunicaciones.\n\nMovete con WASD o flechas: las puertas se abren al caminar hacia ellas. ¡Tu flota no se va a construir sola!"),
        _ => null
    };
    private void OfferRoomGuide(bool force=false)
    {
        activeGuide=null;var guide=CurrentRoomGuide();
        progress.GuiasVistas ??=new();
        if(guide!=null && (force || !progress.GuiasVistas.Contains(guide.Key)))
        {activeGuide=guide;keys.Clear();firing=false;dragging=null;CancelRepairDrag();Capture=false;buttons.Clear();}
    }
    private void CloseRoomGuide()
    {
        if(activeGuide==null)return;
        progress.GuiasVistas.Add(activeGuide.Key);activeGuide=null;keys.Clear();buttons.Clear();firing=false;Save();SyncEmergencyAudio();Invalidate();
    }
    private void DrawRoomGuide(Graphics g)
    {
        if(activeGuide?.Key=="combate-emergencia"){DrawCombatGuide(g);return;}
        buttons.Clear();Box(g,0,0,1200,760,Color.FromArgb(220,Navy),0);
        Box(g,120,105,960,550,PanelColor,20);
        TextAt(g,"TU PRÓXIMA MISIÓN",165,142,14,Mint,true);
        TextAt(g,activeGuide!.Title,165,183,28,White,true);
        Paw(g,998,164,.6f,Mint);
        var paragraphs=activeGuide.Instructions.Split("\n\n");
        for(int i=0;i<paragraphs.Length;i++)
        {
            float y=243+i*99;Box(g,165,y,870,86,Navy,10);
            Box(g,165,y+15,4,56,i==0?Gold:Mint,0);
            Wrap(g,paragraphs[i],185,y+12,824,66,19,i==0?Gold:White);
        }
        TextAt(g,"F1: volver a consultar · El juego se pausa mientras leés",165,600,14,Muted);
        Button(g,"¡Vamos!",800,580,236,49,CloseRoomGuide,true);
    }
    private void DrawCombatGuide(Graphics g)
    {
        buttons.Clear();Box(g,0,0,1200,760,Color.FromArgb(230,Navy),0);
        Box(g,120,80,960,600,PanelColor,20);
        TextAt(g,"ALERTA DE CONTENCIÓN",165,110,14,Red,true);
        TextAt(g,"¡CLONADORA COMPROMETIDA!",165,144,31,White,true);
        TextAt(g,"Los mutantes se están clonando sin control.",165,196,21,White);
        TextAt(g,"¡Detené la amenaza antes de que sea demasiado tarde!",165,225,20,Gold);
        Box(g,165,270,870,73,Navy,10);
        TextAt(g,"TENÉS 2 MINUTOS",185,281,22,Gold,true);
        TextAt(g,"Eliminá 15 mutantes  →  Entrá a LAB BIO-01  →  Repará la clonadora",185,313,17,White);
        var controls=new[]{("WASD / FLECHAS","Moverte"),("MOUSE","Apuntar"),("CLIC / ESPACIO","Disparar"),("E","Interactuar"),("P","Pausar"),("M","Activar / silenciar música")};
        for(int i=0;i<controls.Length;i++)
        {
            float x=165+(i%3)*295,y=365+(i/3)*76;
            Box(g,x,y,280,64,Navy,8);
            TextAt(g,controls[i].Item1,x+15,y+9,16,Mint,true);
            TextAt(g,controls[i].Item2,x+15,y+34,17,White);
        }
        TextAt(g,"Cada mutante resiste 2 impactos.",165,529,17,White);
        TextAt(g,"Botiquines brillantes: +30 de salud.",610,529,17,Mint);
        TextAt(g,"El reloj comienza al entrar. F1 permite volver a ver esta ayuda.",165,570,15,Muted);
        Button(g,"¡Vamos! · Entrar",765,609,270,46,CloseRoomGuide,true);
    }

}
