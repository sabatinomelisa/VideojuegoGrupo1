namespace Kepler7.Core;
public sealed record InformeMision(string Id,string Titulo,string Texto,string Respuesta,int Tipo,int Cifrado);
public static class ComunicacionesHistoria
{
    public static IReadOnlyList<InformeMision> Disponibles(Progreso p)
    {
        var reports=new List<InformeMision>();
        if(!p.LaboratorioReparado)return reports;
        reports.Add(new("rescate","Amenaza contenida","Kepler-7 a Tierra: eliminamos la amenaza mutante y restauramos la clonadora. La emergencia está bajo control.","¡Qué alivio escucharlos! Recuperen al Ingeniero Jefe para poner en marcha el hangar.",2,0));
        if(p.Ingeniero!=null)reports.Add(new("ingeniero","Ingeniería en marcha","El Ingeniero Jefe está operativo. El hangar está habilitado y comenzamos a preparar nuestra flota.","Recibido. Construyan su primera nave; desde Tierra prepararemos el apoyo de navegación.",0,1));
        if(p.Flota.Count>0)reports.Add(new("primera-nave","Primera nave construida","Nuestra primera nave está construida. Iniciamos la preparación de pilotos y las asignaciones de abordaje.","¡Un gran paso, Kepler-7! Necesitan una nave de cada tipo con su piloto compatible para despegar.",1,2));
        if(p.Lista)reports.Add(new("flota-lista","Flota lista para despegar","Exploración, combate y carga tienen nave y piloto asignados. La flota está lista para despegar.","Ruta de salida confirmada. Cuando estén listos, activen el lanzamiento desde el hangar. ¡Buen viaje, michis!",0,1));
        if(p.Lanzada)reports.Add(new("despegue","¡Estamos entre las estrellas!","Tierra, la flota de Kepler-7 ha despegado. Nuestra tripulación ya está rumbo a las estrellas.","¡Los vemos en nuestros radares! Misión cumplida. Pueden seguir ampliando la flota desde la estación.",0,2));
        return reports;
    }
    public static InformeMision? Siguiente(Progreso p)
    {
        var actual=Disponibles(p).LastOrDefault();
        return actual!=null && !p.Transmisiones.Any(t=>t.MisionId==actual.Id)?actual:null;
    }
    public static string ProximoObjetivo(Progreso p)=>!p.LaboratorioReparado?"Eliminá a los mutantes y repará la clonadora.":p.Ingeniero==null?"Cloná al Ingeniero Jefe para habilitar el próximo informe.":p.Flota.Count==0?"Construí tu primera nave para informar a la Tierra.":!p.Lista?"Prepará una nave de cada tipo con su piloto compatible.":!p.Lanzada?"Lanzá la flota desde el hangar para confirmar el despegue.":"El despegue ya fue confirmado. ¡Seguí ampliando tu flota!";
}
