namespace Kepler7.Core;

public interface IPrototipo { IPrototipo Clonar(); }

// Prototype: la lista y el objeto anidado se copian; ninguna referencia mutable se comparte.
public sealed class Tripulante : IPrototipo
{
    public string Identificador { get; set; } = Guid.NewGuid().ToString("N");
    public string Rango { get; set; } = "Cadete";
    private IEspecie? especie;
    [System.Text.Json.Serialization.JsonIgnore]
    public IEspecie Especie => especie != null && especie.Nombre == Aspecto.Tipo ? especie : especie = FabricaEspecies.Compartida.Obtener(Aspecto.Tipo);
    public void UsarEspecie(IEspecie valor) { especie=valor;Aspecto.Tipo=valor.Nombre; }
    public string Nombre { get; set; } = "";
    public string Rol { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public List<string> Habilidades { get; set; } = new();
    public Uniforme Uniforme { get; set; } = new();
    public AspectoFelino Aspecto { get; set; } = new();
    public IPrototipo Clonar() => new Tripulante
    {
        Nombre = Nombre, Rol = Rol, Especialidad = Especialidad, Rango = Rango, especie = Especie,
        Habilidades = new List<string>(Habilidades),
        Uniforme = new Uniforme { Color = Uniforme.Color },
        Aspecto = new AspectoFelino { Tipo = Aspecto.Tipo, Marcas = new(Aspecto.Marcas) }
    };
}
public sealed class AspectoFelino
{
    public string Tipo { get; set; } = "Atigrado";
    public List<string> Marcas { get; set; } = new() { "Rayas" };
}
public sealed class Uniforme { public string Color { get; set; } = "Menta"; }
public static class Prototipos
{
    public static string[] Tipos => FabricaEspecies.Tipos;
    public static Tripulante ConAspecto(Tripulante baseOriginal, string tipo)
    {
        if (!Tipos.Contains(tipo)) throw new ArgumentException("Pelaje desconocido.");
        var prototipo = (Tripulante)baseOriginal.Clonar();
        prototipo.Aspecto = new AspectoFelino { Tipo = tipo, Marcas = new() { tipo == "Atigrado" ? "Rayas" : tipo == "Calicó" ? "Manchas" : tipo == "Siamés" ? "Máscara" : "Liso" } };
        return prototipo;
    }
    // Cada consulta devuelve una base nueva para evitar mutaciones globales accidentales.
    public static Tripulante Ingeniero => new() { Nombre = "Michi-07", Rol = "Ingeniero", Habilidades = new() { "Diagnóstico", "Mecánica" } };
    public static Tripulante Piloto => new() { Nombre = "Piloto base", Rol = "Piloto", Habilidades = new() { "Pilotaje básico" } };
}
public enum Componente { Casco, Motor, Escudo, Armas, Radar, InteligenciaArtificial, Tanques }
public sealed class Nave
{
    public string Tipo { get; set; } = "";
    public List<Pieza> Piezas { get; set; } = new();
    public List<Componente> Componentes { get; set; } = new();
    public Tripulante? Piloto { get; set; }
}
public interface INaveBuilder
{
    void Reiniciar(string tipo);
    void ConstruirCasco();
    void ConstruirMotor();
    void ConstruirEscudo();
    void ConstruirArmas();
    void ConstruirRadar();
    void ConstruirInteligenciaArtificial();
    void ConstruirTanques();
    Nave ObtenerNave();
}
public sealed class NaveBuilder : INaveBuilder
{
    private Nave nave = new();
    public void Reiniciar(string tipo) => nave = new Nave { Tipo = tipo };
    private void Agregar(Componente c) { if (!nave.Componentes.Contains(c)) nave.Componentes.Add(c); }
    public void ConstruirCasco() => Agregar(Componente.Casco);
    public void ConstruirMotor() => Agregar(Componente.Motor);
    public void ConstruirEscudo() => Agregar(Componente.Escudo);
    public void ConstruirArmas() => Agregar(Componente.Armas);
    public void ConstruirRadar() => Agregar(Componente.Radar);
    public void ConstruirInteligenciaArtificial() => Agregar(Componente.InteligenciaArtificial);
    public void ConstruirTanques() => Agregar(Componente.Tanques);
    public Nave ObtenerNave() => new() { Tipo = nave.Tipo, Componentes = new(nave.Componentes) };
}
public abstract class RecetaNave
{
    public abstract string Nombre { get; }
    public abstract Componente[] Componentes { get; }
    public abstract string HabilidadPiloto { get; }
    public static RecetaNave[] Todas => new RecetaNave[] { new RecetaExploradora(), new RecetaCombate(), new RecetaCarga() };
}
public sealed class RecetaExploradora : RecetaNave
{
    public override string Nombre => "Exploradora";
    public override string HabilidadPiloto => "Exploración";
    public override Componente[] Componentes => new[] { Componente.Casco, Componente.Motor, Componente.Radar, Componente.InteligenciaArtificial };
}
public sealed class RecetaCombate : RecetaNave
{
    public override string Nombre => "Combate";
    public override string HabilidadPiloto => "Combate espacial";
    public override Componente[] Componentes => new[] { Componente.Casco, Componente.Motor, Componente.Escudo, Componente.Armas };
}
public sealed class RecetaCarga : RecetaNave
{
    public override string Nombre => "Carga";
    public override string HabilidadPiloto => "Transporte";
    public override Componente[] Componentes => new[] { Componente.Casco, Componente.Motor, Componente.Escudo, Componente.Tanques };
}
// Director de Builder. Decide el orden de construcción, sin referencias a la interfaz.
public sealed class IngenieroJefe
{
    private readonly INaveBuilder builder;
    public IngenieroJefe(INaveBuilder builder) => this.builder = builder;
    public Nave Construir(RecetaNave receta)
    {
        builder.Reiniciar(receta.Nombre);
        foreach (var c in receta.Componentes)
            switch (c)
            {
                case Componente.Casco: builder.ConstruirCasco(); break;
                case Componente.Motor: builder.ConstruirMotor(); break;
                case Componente.Escudo: builder.ConstruirEscudo(); break;
                case Componente.Armas: builder.ConstruirArmas(); break;
                case Componente.Radar: builder.ConstruirRadar(); break;
                case Componente.InteligenciaArtificial: builder.ConstruirInteligenciaArtificial(); break;
                case Componente.Tanques: builder.ConstruirTanques(); break;
            }
        return builder.ObtenerNave();
    }
}
public sealed class Taller
{
    public int Vidas { get; private set; } = 3;
    public Nave? UltimaNave { get; private set; }
    public string Mensaje { get; private set; } = "";
    public List<string> Pasos { get; } = new();
    public void Reiniciar() { Vidas = 3; UltimaNave = null; Mensaje = ""; Pasos.Clear(); }
    public List<Nave> EnsamblarLote(RecetaNave receta, IReadOnlyList<Pieza> modelo, IReadOnlyList<Pieza> seleccion, int cantidad)
    {
        if(cantidad<1 || cantidad>10000)throw new ArgumentOutOfRangeException(nameof(cantidad));
        var lote=new List<Nave>();
        if(!EnsamblarModelo(receta,modelo,seleccion))return lote;
        lote.Add(UltimaNave!);
        var director=new IngenieroJefe(new NaveBuilder());
        for(int i=1;i<cantidad;i++){var nave=director.Construir(receta);nave.Piezas=seleccion.ToList();lote.Add(nave);}
        return lote;
    }
    public bool EnsamblarModelo(RecetaNave receta, IReadOnlyList<Pieza> modelo, IReadOnlyList<Pieza> seleccion)
    {
        if (Vidas <= 0) return false;
        if (modelo.Count != seleccion.Count || modelo.Any(p => seleccion.Count(s => s == p) != 1))
        {
            Vidas--; UltimaNave = null; Pasos.Clear();
            Mensaje = "¡Explosión! Revisá piezas, formas y colores del modelo."; return false;
        }
        if (!Ensamblar(receta, seleccion.Select(p => p.Componente))) return false;
        UltimaNave!.Piezas = seleccion.ToList(); return true;
    }
    public bool Ensamblar(RecetaNave receta, IEnumerable<Componente> seleccion)
    {
        if (Vidas == 0) { Mensaje = "Sin vidas. Reiniciá el desafío."; return false; }
        var piezas = seleccion.ToHashSet();
        var faltan = receta.Componentes.Except(piezas).ToArray();
        var sobran = piezas.Except(receta.Componentes).ToArray();
        UltimaNave = null; Pasos.Clear();
        if (faltan.Length > 0 || sobran.Length > 0)
        {
            Vidas--;
            Mensaje = (faltan.Length > 0 ? "Falta: " + string.Join(", ", faltan.Select(Nombres.Componente)) + ". " : "")
                + (sobran.Length > 0 ? "Sobra: " + string.Join(", ", sobran.Select(Nombres.Componente)) + "." : "");
            return false;
        }
        UltimaNave = new IngenieroJefe(new NaveBuilder()).Construir(receta);
        Pasos.AddRange(UltimaNave.Componentes.Select(c => "Instalado: " + Nombres.Componente(c)));
        Mensaje = "Ensamblaje correcto. Nave lista para recibir un piloto.";
        return true;
    }
}
public sealed record Pieza(Componente Componente, int Variante, string Color);
public static class Modelos
{
    public static List<Pieza> Aleatorio(int modelo, Random random, IReadOnlyList<Pieza>? anterior = null)
    {
        string[] colores = { "Menta", "Coral", "Azul", "Dorado" };
        var piezas = RecetaNave.Todas[modelo].Componentes.Select(c => new Pieza(c, random.Next(2), colores[random.Next(4)])).ToList();
        // Evita un modelo monocromático y una repetición exacta del desafío anterior.
        if (piezas.Select(p => p.Color).Distinct().Count() == 1)
            piezas[1] = piezas[1] with { Color = colores[(Array.IndexOf(colores, piezas[0].Color) + 1 + random.Next(3)) % 4] };
        if (anterior != null && piezas.SequenceEqual(anterior)) piezas[0] = piezas[0] with { Variante = 1-piezas[0].Variante };
        return piezas;
    }
    public static List<Pieza> Para(int modelo) => RecetaNave.Todas[modelo].Componentes
        .Select((c, i) => new Pieza(c, (modelo + i) % 2, new[] { "Azul", "Coral", "Dorado" }[modelo])).ToList();
}
public static class Nombres
{
    public static string Componente(Componente c) => c switch
    { Core.Componente.InteligenciaArtificial => "IA", Core.Componente.Tanques => "Tanques", _ => c.ToString() };
}
