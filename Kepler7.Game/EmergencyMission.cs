using System.Media;
using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private readonly Emergencia emergency=new();
    private SoundPlayer? emergencyMusic;
    private bool musicMuted,musicPlaying,audioUnavailable;
    private string audioTrack="";
    private bool EmergencySector => !progress.LaboratorioReparado && screen is Pantalla.Pasillo or Pantalla.SalaLab or Pantalla.Reparacion;
    private bool EmergencyBlocked => EmergencySector && (paused || emergency.Agotada || corridor.Derrota);
    private void SyncEmergencyAudio()
    {
        bool play=EmergencySector && activeGuide==null && !paused && !emergency.Agotada && !corridor.Derrota && (!musicMuted || emergency.Critica) && !audioUnavailable;
        string track=emergency.Critica?(musicMuted?"alarma.wav":"emergencia_critica.wav"):"emergencia.wav";
        if(play==musicPlaying && (!play || track==audioTrack))return;
        try
        {
            if(play){emergencyMusic?.Stop();emergencyMusic?.Dispose();emergencyMusic=new SoundPlayer(Path.Combine(AppContext.BaseDirectory,"Audio",track));audioTrack=track;emergencyMusic.PlayLooping();}
            else emergencyMusic?.Stop();
            musicPlaying=play;
        }
        catch(Exception ex) when(ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {audioUnavailable=true;musicPlaying=false;}
    }
    private void RetryEmergency()
    {
        gameOverTime=-1;emergency.Reiniciar();corridor=new();repair=new();paused=false;CancelRepairDrag();
        progress.LaboratorioReparado=false;Change(Pantalla.Pasillo);
    }
    private void ToggleEmergencyPause(){paused=!paused;keys.Clear();firing=false;CancelRepairDrag();Capture=false;SyncEmergencyAudio();}
    private void DrawEmergency(Graphics g)
    {
        if(!EmergencySector)return;
        if(emergency.Agotada){DrawGameOver(g);return;}
        bool critical=emergency.Critica && !EmergencyBlocked && activeGuide==null;
        float pulse=(MathF.Sin(time*MathF.PI*(critical?3f:1.6f))+1)/2;
        if(critical)
        {
            // Pulso rojo gradual a 1,5 Hz; mantiene legibles la sala y los controles.
            Box(g,40,160,1120,530,Color.FromArgb((int)(12+35*pulse),Danger),0);
            using var border=new Pen(Color.FromArgb(110+(int)(120*pulse),Danger),7);
            g.DrawRectangle(border,44,164,1112,522);
        }
        // Las balizas acompañan el pulso rojo durante los últimos segundos.
        for(int x=20;x<=1180;x+=1160)
        {
            for(int ring=3;ring>=1;ring--)Ellipse(g,Color.FromArgb((int)(6+pulse*8),Danger),x-12-ring*7,170-ring*7,24+ring*14,24+ring*14);
            Ellipse(g,Color.FromArgb(140+(int)(115*pulse),Danger),x-9,173,18,18);
            LineTo(g,Color.FromArgb(50+(int)(90*pulse),Danger),40,150,40,650,3);
            LineTo(g,Color.FromArgb(50+(int)(90*pulse),Danger),1160,150,1160,650,3);
        }
        int seconds=(int)Math.Ceiling(emergency.Restante);
        Box(g,895,103,266,41,Navy,8);
        TextAt(g,"TIEMPO  "+(seconds/60).ToString("00")+":"+(seconds%60).ToString("00"),910,113,19,seconds<=60?Danger:Gold,true);
        if(!EmergencyBlocked && activeGuide==null)Button(g,"P · Pausa",780,104,106,37,ToggleEmergencyPause);
        TextAt(g,critical?"¡SE ESTÁ ACABANDO EL TIEMPO!":audioUnavailable?"Audio no disponible":musicMuted?"M · Música apagada":"M · Música encendida",900,146,12,critical?Danger:Muted,critical);
        if(!EmergencyBlocked)return;
        buttons.Clear();Box(g,0,0,1200,760,Color.FromArgb(215,Navy),0);Box(g,280,221,640,320,PanelColor,18);
        bool failed=emergency.Agotada || corridor.Derrota;
        TextAt(g,emergency.Agotada?"¡CONTENCIÓN PERDIDA!":corridor.Derrota?"SIN SEÑAL VITAL":"EMERGENCIA EN PAUSA",319,258,29,failed?Danger:Gold,true);
        Wrap(g,emergency.Agotada?"La clonadora siguió fuera de control. El protocolo de emergencia selló el sector. Reintentá: tenés 2 minutos para eliminar a los mutantes y reparar la máquina.":corridor.Derrota?"Los mutantes te alcanzaron. Recuperá tus fuerzas y volvé a entrar antes de que la estación caiga.":"El reloj y la música están detenidos. Volvé cuando estés lista para recuperar el laboratorio.",319,320,557,111,19,White);
        Button(g,failed?"Reintentar · 02:00":"Continuar",319,457,258,49,()=>{if(failed)RetryEmergency();else ToggleEmergencyPause();},true);
        Button(g,"Menú",608,457,267,49,()=>{Save();Change(Pantalla.Inicio);});
    }
}
