namespace Kepler7.Core;
public sealed record EstadoEspecialidad(string Tipo,int PilotosDisponibles,int NavesSinPiloto,int NavesConPiloto=0);
public static class EstadisticasFlota
{
    public static EstadoEspecialidad[] Obtener(Progreso progreso)
    {
        var ocupados=progreso.Flota.Where(n=>n.Piloto!=null).Select(n=>n.Piloto!).ToArray();
        var ids=ocupados.Select(p=>p.Identificador).ToHashSet();
        var nombres=ocupados.Select(p=>(p.Nombre,p.Especialidad)).ToHashSet();
        return RecetaNave.Todas.Select(r=>new EstadoEspecialidad(r.Nombre,
            progreso.Pilotos.Count(p=>p.Especialidad==r.Nombre && !ids.Contains(p.Identificador) && !nombres.Contains((p.Nombre,p.Especialidad))),
            progreso.Flota.Count(n=>n.Tipo==r.Nombre && n.Piloto==null),
            progreso.Flota.Count(n=>n.Tipo==r.Nombre && n.Piloto!=null))).ToArray();
    }
}
public sealed class ResumenOperacion
{
    public string Titulo {get;set;}="";
    public List<string> Detalles {get;set;}=new();
}
public sealed record Insignia(string Patron,string Nombre,string Objetivo,bool Obtenida);
public static class Logros
{
    public static Insignia[] Obtener(Progreso p)=>new[]{
        new Insignia("Builder","Patitas constructoras","Construí tu primera nave.",p.BuilderDominado),
        new Insignia("Prototype","Más bigotes a bordo","Cloná al Ingeniero Jefe.",p.PrototypeDominado),
        new Insignia("Flyweight","Cada michi cuenta","Recuperá el censo de la estación.",p.FlyweightDominado),
        new Insignia("Bridge","Una voz entre las estrellas","Restablecé el contacto y enviá un informe de misión a la Tierra.",p.BridgeDominado)
    };
}
