using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private EnlaceComunicaciones antennaLink=new();
    private int antennaDrag=-1,messageType,cipherType,transmissionIndex=-1;
    private float transmissionTime;
    private Transmision? pendingTransmission;
    private const string DefaultCommunicationMessage="Kepler-7 solicita asistencia. ¡La tripulación está a salvo!";
    private string communicationMessage=DefaultCommunicationMessage;
    private bool showTransmissionHistory;
    private InformeMision? storyDraft;
    private void EnsureStoryDraft()
    {
        var next=ComunicacionesHistoria.Siguiente(progress);
        if(storyDraft?.Id==next?.Id)return;
        ResetCommunicationsDraft();storyDraft=next;
        if(next==null){communicationMessage="No hay informes pendientes. "+ComunicacionesHistoria.ProximoObjetivo(progress);return;}
        communicationMessage=next.Texto;ChooseMessage(next.Tipo);cipherType=next.Cifrado;outbound.Cifrado=ciphers[cipherType];
    }
    private Mensaje outbound=new MensajeTexto(new SinCifrado());
    private readonly ICifrado[] ciphers={new SinCifrado(),new CifradoAES(),new CifradoCuantico()};
    private void ResetCommunicationsDraft()
    {
        storyDraft=null;antennaLink=new EnlaceComunicaciones();antennaDrag=-1;
        messageType=cipherType=0;outbound=new MensajeTexto(ciphers[0]);
        communicationMessage=DefaultCommunicationMessage;
        pendingTransmission=null;transmissionTime=0;showTransmissionHistory=false;
    }
    private void ChooseMessage(int type)
    {
        showTransmissionHistory=false;messageType=type;ICifrado cipher=outbound.Cifrado;
        outbound=type switch{1=>new MensajeAlerta(cipher),2=>new MensajeEmergencia(cipher),_=>new MensajeTexto(cipher)};
    }
    private bool BeginAntennaDrag()
    {
        if(transmissionTime>0 || storyDraft==null)return false;
        for(int i=0;i<3;i++)if(new RectangleF(84,282+i*98,432,45).Contains(mouse)){antennaDrag=i;MoveAntenna();return true;}
        return false;
    }
    private void MoveAntenna()
    {
        if(antennaDrag>=0 && screen==Pantalla.Comunicaciones && !DialogOpen)antennaLink.Ajustar(antennaDrag,(mouse.X-100)/4);
    }
    private void TickCommunications(float dt)
    {
        if(screen!=Pantalla.Comunicaciones)return;
        EnsureStoryDraft();
        antennaLink.Avanzar(dt);
        if(transmissionTime<=0)return;
        transmissionTime=Math.Max(0,transmissionTime-dt);
        if(transmissionTime==0 && pendingTransmission!=null)
        {
            bool badge=progress.RegistrarTransmision(pendingTransmission);transmissionIndex=progress.Transmisiones.Count-1;
            var details=new List<string>{"La Tierra recibió tu mensaje de "+pendingTransmission.Tipo+".","Canal: "+pendingTransmission.Cifrado,"Tierra: "+pendingTransmission.RespuestaTierra};
            if(badge)details.Add("¡Insignia Bridge conseguida! +150 XP");
            else if(!progress.BridgeDominado)details.Add("Mantené a la Tierra al tanto de tus próximos avances.");
            ResetCommunicationsDraft();
            ShowSummary(new ResumenOperacion{Titulo="¡Contacto con la Tierra!",Detalles=details});
        }
    }
    private void DrawCommunicationsRoom(Graphics g)
    {
        Header(g,"Comunicaciones","Una voz entre las estrellas");
        CozyRoom(g,Blue);
        Ellipse(g,Blue,878,185,85,85);Ellipse(g,Mint,894,196,31,28);Ellipse(g,Mint,927,231,22,23);
        for(int i=0;i<4;i++){using var p=new Pen(Color.FromArgb(70+i*25,Blue),2);g.DrawArc(p,500-i*26,180-i*13,200+i*52,135+i*26,190,160);}
        Box(g,452,274,296,63,Line,12);Box(g,473,284,255,35,Navy,8);
        TextAt(g,ComunicacionesHistoria.Siguiente(progress)!=null?"INFORME LISTO PARA ENVIAR":"SIN INFORMES PENDIENTES",482,294,15,progress.BridgeDominado?Mint:Gold,true);
        TextAt(g,"RED DE ANTENAS",96,190,22,Mint,true);
        Wrap(g,"Alguien allá afuera necesita saber que seguimos acá.",97,231,300,55,17,White);
        Yarn(g,159,563,.8f,Blue);Bowl(g,1065,575,.85f,true);
        Box(g,68,402,289,91,Navy,9);TextAt(g,progress.Transmisiones.Count+" mensajes enviados",88,419,18,Mint,true);TextAt(g,"Texto · Alerta · Emergencia",88,453,15,Muted);
        Box(g,514,620,172,25,Mint,3);TextAt(g,"SALIDA",568,621,15,Navy,true);
        Cat(g,roomCat.X,roomCat.Y,.92f,Mint,false,roomFacing,"Blanco",time,keys.Count>0?roomStride:0,hero:true);
        TextAt(g,RoomAction(),100,670,19,Gold,true);
    }
    private void DrawCommunications(Graphics g)
    {
        EnsureStoryDraft();
        Header(g,"Puente de comunicaciones / Bridge","¡Que la Tierra escuche a Kepler-7!");
        bool idle=transmissionTime<=0;
        Button(g,"← Volver a la sala",40,127,230,40,ExitConsole,false,idle);
        TextAt(g,storyDraft==null?"Volvé cuando completes el próximo objetivo de la misión.":"Arrastrá las tres antenas hasta sus marcas y mantené la señal estable.",298,140,16,Muted);
        Box(g,40,190,520,471,PanelColor,12);Box(g,580,190,580,471,PanelColor,12);
        TextAt(g,"RECUPERAR LA SEÑAL",64,211,18,Mint,true);
        string[] channels={"DIRECCIÓN","FRECUENCIA","FASE"};
        for(int i=0;i<3;i++)
        {
            float y=306+i*98,target=100+antennaLink.Objetivos[i]*4,actual=100+antennaLink.Ajustes[i]*4;
            TextAt(g,channels[i],69,y-56,13,Muted,true);
            Box(g,target-20,y-18,40,36,Color.FromArgb(70,Mint),5);
            LineTo(g,Mint,target,y-19,target,y+19,2);LineTo(g,Muted,100,y,500,y,5);
            Ellipse(g,Math.Abs(actual-target)<=20?Mint:Gold,actual-13,y-13,26,26);
            for(int j=0;j<65;j++)
            {
                float x=85+j*6.5f,phase=(antennaLink.Ajustes[i]-antennaLink.Objetivos[i])*.07f;
                LineTo(g,Blue,x,y+34+MathF.Sin(j*.3f+time*2)*7,x+6.5f,y+34+MathF.Sin((j+1)*.3f+time*2)*7,1);
                LineTo(g,Gold,x,y+34+MathF.Sin(j*.3f+time*2+phase)*7,x+6.5f,y+34+MathF.Sin((j+1)*.3f+time*2+phase)*7,1);
            }
        }
        Box(g,69,567,460,15,Navy,5);if(antennaLink.Estabilidad>0)Box(g,69,567,460*antennaLink.Estabilidad/1.2f,15,Mint,5);
        TextAt(g,storyDraft==null?"No hay informes pendientes":antennaLink.Listo?"¡Señal estable! Lista para transmitir":"Alineá los controles con las marcas verdes",69,603,17,antennaLink.Listo?Mint:Gold,true);
        TextAt(g,"INFORME DE LA MISIÓN",601,204,12,Muted,true);
        Wrap(g,storyDraft?.Titulo??"Esperando el próximo avance",601,229,530,42,21,White);
        string[] types={"Texto","Alerta","Emergencia"},names={"Sin cifrado","AES","Cuántico"};
        for(int i=0;i<3;i++)
        {
            int n=i;
            Button(g,names[i],600+i*182,302,173,35,()=>{showTransmissionHistory=false;cipherType=n;outbound.Cifrado=ciphers[n];},cipherType==i,idle && storyDraft!=null);
        }
        TextAt(g,(cipherType==2?"CANAL SIMULADO":"CANAL")+" · "+(storyDraft==null?"Sin informe":types[messageType]),601,279,12,Muted,true);
        TextAt(g,"TU MENSAJE",601,356,12,Muted,true);

        Wrap(g,storyDraft==null?ComunicacionesHistoria.ProximoObjetivo(progress):communicationMessage,602,382,530,64,16,White);
        var sent=pendingTransmission ?? (showTransmissionHistory && transmissionIndex>=0 && transmissionIndex<progress.Transmisiones.Count?progress.Transmisiones[transmissionIndex]:null);
        TextAt(g,sent==null?"PAQUETE DE SALIDA":(pendingTransmission!=null?"ENVIANDO · ":"HISTORIAL · ")+sent.Tipo+" / "+sent.Cifrado,601,448,12,Mint,true);
        Wrap(g,sent?.Resultado??"El informe aparecerá aquí al transmitir. El historial conserva los envíos anteriores.",602,475,532,99,12,White);
        Button(g,storyDraft==null?"Sin informes pendientes":idle?"Transmitir a la Tierra":"Enviando…",600,600,539,43,()=>
        {
            if(storyDraft==null)return;
            pendingTransmission=antennaLink.Transmitir(outbound,communicationMessage);
            pendingTransmission.MisionId=storyDraft.Id;pendingTransmission.RespuestaTierra=storyDraft.Respuesta;transmissionTime=2.4f;
        },true,idle && storyDraft!=null && antennaLink.Listo && !string.IsNullOrWhiteSpace(communicationMessage));
        if(!idle)
        {
            float t=1-transmissionTime/2.4f;Ellipse(g,Mint,76+t*432,566,13,17);
        }
        int total=progress.Transmisiones.Count;
        Button(g,"‹",40,688,44,36,()=>{transmissionIndex=Math.Max(0,(transmissionIndex<0?total-1:transmissionIndex)-1);showTransmissionHistory=true;},false,idle && total>0);
        TextAt(g,total==0?"Todavía no hay transmisiones":"Historial: "+((transmissionIndex<0?total-1:transmissionIndex)+1)+" / "+total,101,698,16,White);
        Button(g,"›",325,688,44,36,()=>{transmissionIndex=Math.Min(total-1,(transmissionIndex<0?total-1:transmissionIndex)+1);showTransmissionHistory=true;},false,idle && total>0);
        Button(g,"Ver mensaje recibido",390,688,262,36,()=>{var t=progress.Transmisiones[transmissionIndex<0?total-1:transmissionIndex];ShowSummary(new ResumenOperacion{Titulo="La Tierra recibió tu señal",Detalles=new(){t.Tipo+" · "+t.Cifrado,t.Recibido,"Tierra: "+(string.IsNullOrEmpty(t.RespuestaTierra)?"Mensaje recibido.":t.RespuestaTierra)}},false);},false,idle && total>0);
        TextAt(g,"Cada avance tiene su propio informe.",690,700,16,Gold);
    }
}
