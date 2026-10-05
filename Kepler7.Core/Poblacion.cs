namespace Kepler7.Core;

// Censo de habitantes fundacionales y clones creados durante la partida.
public sealed record RegistroPoblacion(Tripulante Tripulante, int? NumeroNave, string TipoNave)
{
    public string Destino => NumeroNave.HasValue ? "Nave " + NumeroNave + " · " + TipoNave : Tripulante.Rol == "Ingeniero" ? "Laboratorio / hangar" : Tripulante.Rol=="Piloto" ? "Disponible" : "Estación Kepler-7";
}
public sealed class CensoPoblacion
{
    public IReadOnlyList<RegistroPoblacion> Registros { get; }
    public int Total => Registros.Count;
    public int Pilotos => Registros.Count(r => r.Tripulante.Rol == "Piloto");
    public int Asignados => Registros.Count(r => r.NumeroNave.HasValue);
    public int Disponibles => Registros.Count(r => r.Tripulante.Rol == "Piloto" && !r.NumeroNave.HasValue);
    private readonly (string Tipo, bool Libre)[] flota;
    public CensoFiltrado Filtrar(string filtro,string busqueda="",string especie="Todas",string rol="Todos",string estado="Todos")
    {
        bool todos=filtro=="Todos", libres=filtro=="Disponibles";
        var filas=Registros.Where(r=>filtro switch
        {
            "Todos"=>true,
            "Población" or "Habitantes"=>r.Tripulante.Rol=="Habitante",
            "Tripulación" or "Tripulantes" or "No pilotos"=>r.Tripulante.Rol!="Piloto" && r.Tripulante.Rol!="Habitante",
            "Disponibles"=>r.Tripulante.Rol=="Piloto" && !r.NumeroNave.HasValue,
            _=>r.Tripulante.Rol=="Piloto" && r.Tripulante.Especialidad==filtro
        }).Where(r=>(string.IsNullOrWhiteSpace(busqueda) || System.Globalization.CultureInfo.InvariantCulture.CompareInfo.IndexOf(r.Tripulante.Nombre+" "+r.Tripulante.Identificador,busqueda.Trim(),System.Globalization.CompareOptions.IgnoreCase|System.Globalization.CompareOptions.IgnoreNonSpace)>=0)
            && (especie=="Todas" || r.Tripulante.Especie.Nombre==especie)
            && (rol=="Todos" || r.Tripulante.Rol==rol)
            && (estado=="Todos" || estado=="A bordo" && r.NumeroNave.HasValue || estado=="Libres" && r.Tripulante.Rol=="Piloto" && !r.NumeroNave.HasValue)).ToArray();
        var naves=flota.Where(n=>todos || (libres ? n.Libre : n.Tipo==filtro)).ToArray();
        return new(filas,naves.Length,naves.Count(n=>n.Libre));
    }
    public int Naves { get; }
    public int NavesSinPiloto { get; }
    public CensoPoblacion(Progreso progreso)
    {
        flota=progreso.Flota.Select(n=>(n.Tipo,n.Piloto==null)).ToArray();
        Naves=progreso.Flota.Count; NavesSinPiloto=progreso.Flota.Count(n=>n.Piloto==null);
        var porId=new Dictionary<string,(int Numero,string Tipo)>();
        var porNombre=new Dictionary<(string,string),(int Numero,string Tipo)>();
        for(int i=0;i<progreso.Flota.Count;i++)
        {
            var nave=progreso.Flota[i]; if(nave.Piloto==null)continue;
            porId.TryAdd(nave.Piloto.Identificador,(i+1,nave.Tipo));
            // Compatibilidad con guardados anteriores sin identificadores persistidos.
            porNombre.TryAdd((nave.Piloto.Nombre,nave.Piloto.Especialidad),(i+1,nave.Tipo));
        }
        var individuos=new List<Tripulante>(progreso.Habitantes);
        if(progreso.Ingeniero!=null)individuos.Add(progreso.Ingeniero);
        individuos.AddRange(progreso.Pilotos);
        Registros=individuos.Select(t =>
        {
            if(t.Rol=="Piloto" && (porId.TryGetValue(t.Identificador,out var nave) || porNombre.TryGetValue((t.Nombre,t.Especialidad),out nave)))
                return new RegistroPoblacion(t,nave.Numero,nave.Tipo);
            return new RegistroPoblacion(t,null,"");
        }).ToArray();
    }
}

public sealed record CensoFiltrado(RegistroPoblacion[] Registros, int Naves, int NavesSinPiloto)
{
    public int Total => Registros.Length;
    public int Disponibles => Registros.Count(r=>r.Tripulante.Rol=="Piloto" && !r.NumeroNave.HasValue);
    public int Asignados => Registros.Count(r=>r.NumeroNave.HasValue);
}
