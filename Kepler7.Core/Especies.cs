using System.Text.Json.Serialization;
namespace Kepler7.Core;

// Estado intrínseco: inmutable y compartido por todos los individuos de la especie.
public interface IEspecie
{
    string Nombre { get; }
    string Color { get; }
    string PlanetaOrigen { get; }
    string Dieta { get; }
}
public sealed class Especie : IEspecie
{
    public string Nombre { get; }
    public string Color { get; }
    public string PlanetaOrigen { get; }
    public string Dieta { get; }
    public Especie(string nombre,string color,string planeta,string dieta)
    { Nombre=nombre;Color=color;PlanetaOrigen=planeta;Dieta=dieta; }
}
public sealed class FabricaEspecies
{
    private readonly Dictionary<string,IEspecie> especies = new(StringComparer.Ordinal);
    public static FabricaEspecies Compartida { get; } = new();
    public static string[] Tipos => new[] { "Atigrado", "Negro", "Siamés", "Calicó", "Blanco" };
    public int ObjetosCreados { get; private set; }
    public int Cantidad => especies.Count;
    public IEspecie Obtener(string nombre)
    {
        lock(especies)
        {
            if(especies.TryGetValue(nombre,out var especie))return especie;
            especie=CrearSinCompartir(nombre);especies.Add(nombre,especie);ObjetosCreados++;return especie;
        }
    }
    public static IEspecie CrearSinCompartir(string nombre)
    {
        int n=Array.IndexOf(Tipos,nombre);if(n<0)throw new ArgumentException("Especie desconocida.");
        string[] colores={"#E6A053","#3F4253","#E5D6BA","#F0EBDF","#EEF2F6"};
        string[] planetas={"Kepler-7","Umbra","Siamia","Calisto","Nivalis"};
        return new Especie(nombre,colores[n],planetas[n],n==4?"Pescado y croquetas polares":"Atún y croquetas");
    }
}
public sealed class BibliotecaTripulantes
{
    public FabricaEspecies Fabrica { get; } = FabricaEspecies.Compartida;
    public List<Tripulante> ConFlyweight { get; } = new(10000);
    public List<Tripulante> SinFlyweight { get; } = new(10000);
    public int ObjetosSinCompartir { get; }
    public int ObjetosCompartidos => ConFlyweight.Select(t=>t.Especie).Distinct(ReferenceEqualityComparer.Instance).Count();
    public double ReduccionPorcentaje => ObjetosSinCompartir==0?0:100d*(1d-(double)ObjetosCompartidos/ObjetosSinCompartir);
    public static BibliotecaTripulantes DesdeProgreso(Progreso progreso) => new(new CensoPoblacion(progreso).Registros.Select(r=>r.Tripulante).ToList());
    public static List<Tripulante> CrearHabitantes()
    {
        var habitantes=new List<Tripulante>(10000);
        for(int i=0;i<10000;i++)
        {
            var t=new Tripulante { Nombre="Michi "+(i+1),Identificador="K7-"+(i+1).ToString("D5"),Rango=i%20==0?"Capitán":"Tripulante",Rol="Habitante",Especialidad="Población" };
            t.UsarEspecie(FabricaEspecies.Compartida.Obtener(FabricaEspecies.Tipos[i%5]));habitantes.Add(t);
        }
        return habitantes;
    }
    public BibliotecaTripulantes(List<Tripulante>? habitantes=null)
    {
        ConFlyweight=habitantes??CrearHabitantes();
        foreach(var con in ConFlyweight)
        {
            var sin=new Tripulante { Nombre=con.Nombre,Identificador=con.Identificador,Rango=con.Rango,Rol=con.Rol };
            sin.UsarEspecie(FabricaEspecies.CrearSinCompartir(con.Especie.Nombre));SinFlyweight.Add(sin);
        }
        ObjetosSinCompartir=SinFlyweight.Select(t=>t.Especie).Distinct(ReferenceEqualityComparer.Instance).Count();
    }
}
