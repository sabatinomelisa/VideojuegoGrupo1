namespace Kepler7.Core;

// Tipos de acción que puede hacer una nave en su turno.
public enum TipoAccion { Atacar, Escudo, Reparar }

// Cualquier nave que participa de la batalla (aliada o enemiga).
public class UnidadBatalla
{
    public string Nombre { get; init; } = "";
    public string Rol { get; init; } = "";
    public string Piloto { get; init; } = "";
    public int VidaMax { get; init; }
    public int Vida { get; set; }
    public bool Escudo { get; set; }
    public bool Viva => Vida > 0;
    public bool Herida => Viva && Vida < VidaMax;

    // El escudo absorbe la mitad de un único impacto y después se apaga.
    public int RecibirDanio(int danio)
    {
        if (!Viva || danio <= 0) return 0;
        int real = Escudo ? danio / 2 : danio;
        Escudo = false;
        Vida = Math.Max(0, Vida - real);
        return real;
    }

    // Suma vida sin pasarse del máximo. Devuelve cuánto reparó de verdad.
    public int Reparar(int cantidad)
    {
        if (!Viva || cantidad <= 0) return 0;
        int antes = Vida;
        Vida = Math.Min(VidaMax, Vida + cantidad);
        return Vida - antes;
    }
}

// Las naves enemigas son unidades que además tienen una estrategia de IA.
// No deciden solas: le preguntan a su estrategia qué hacer (patrón Strategy).
public sealed class Enemigo : UnidadBatalla
{
    public IEstrategiaIA Estrategia { get; set; } = new IAAgresiva();
}

// Resultado de una decisión de IA: qué hace, sobre quién y con qué intensidad.
public sealed record AccionIA(TipoAccion Tipo, UnidadBatalla? Objetivo, int Valor, string Descripcion);

// Información que la estrategia necesita para decidir:
// quiénes son los aliados, quiénes los enemigos, qué ronda es, etc.
public sealed class Contexto
{
    public List<UnidadBatalla> Aliados { get; } = new();
    public List<Enemigo> Enemigos { get; } = new();
    public UnidadBatalla NaveCarga { get; set; } = new();
    public int Ronda { get; set; } = 1;
    public Random Azar { get; init; } = new();
    public IEnumerable<UnidadBatalla> AliadosVivos => Aliados.Where(a => a.Viva);
    public IEnumerable<Enemigo> EnemigosVivos => Enemigos.Where(e => e.Viva);
}

// ---------------------------------------------------------------------
//  STRATEGY · Inteligencia artificial intercambiable
// ---------------------------------------------------------------------

// El "contrato" que toda IA tiene que cumplir.
public interface IEstrategiaIA
{
    string Nombre { get; }
    string Descripcion { get; }
    // Indica si el radar puede anticipar la jugada (la aleatoria no).
    bool Predecible { get; }
    AccionIA DecidirAccion(Enemigo enemigo, Contexto contexto);
}

// Ataca fuerte a la nave aliada con menos vida para rematarla.
public sealed class IAAgresiva : IEstrategiaIA
{
    public string Nombre => "Agresiva";
    public string Descripcion => "Remata a la nave aliada más dañada.";
    public bool Predecible => true;
    public const int Potencia = 30;

    public AccionIA DecidirAccion(Enemigo enemigo, Contexto contexto)
    {
        var objetivo = contexto.AliadosVivos.MinBy(a => a.Vida)!;
        return new(TipoAccion.Atacar, objetivo, Potencia, "ataca a " + objetivo.Nombre);
    }
}

// Si está dañada se protege; si no, dispara suave a la primera nave de la formación.
public sealed class IADefensiva : IEstrategiaIA
{
    public string Nombre => "Defensiva";
    public string Descripcion => "Se escuda cuando está dañada y dispara poco.";
    public bool Predecible => true;
    public const int Potencia = 16;

    public AccionIA DecidirAccion(Enemigo enemigo, Contexto contexto)
    {
        if (enemigo.Vida <= enemigo.VidaMax / 2 && !enemigo.Escudo)
            return new(TipoAccion.Escudo, enemigo, 0, "levanta el escudo");
        var objetivo = contexto.AliadosVivos.First();
        return new(TipoAccion.Atacar, objetivo, Potencia, "dispara a " + objetivo.Nombre);
    }
}

// Elige al azar objetivo, acción y potencia.
public sealed class IAAleatoria : IEstrategiaIA
{
    public string Nombre => "Aleatoria";
    public string Descripcion => "Impredecible: objetivo y potencia al azar.";
    public bool Predecible => false;

    public AccionIA DecidirAccion(Enemigo enemigo, Contexto contexto)
    {
        if (contexto.Azar.Next(4) == 0 && !enemigo.Escudo)
            return new(TipoAccion.Escudo, enemigo, 0, "levanta el escudo");
        var vivos = contexto.AliadosVivos.ToList();
        var objetivo = vivos[contexto.Azar.Next(vivos.Count)];
        return new(TipoAccion.Atacar, objetivo, contexto.Azar.Next(8, 37), "ataca a " + objetivo.Nombre);
    }
}

// Calcula el golpe más útil: si puede destruir una nave, lo hace;
// si no, apunta a la Nave de Carga, cuya pérdida significa la derrota.
public sealed class IACalculadora : IEstrategiaIA
{
    public string Nombre => "Calculadora";
    public string Descripcion => "Busca destruir la Nave de Carga o rematar.";
    public bool Predecible => true;
    public const int Potencia = 26;

    public AccionIA DecidirAccion(Enemigo enemigo, Contexto contexto)
    {
        var rematable = contexto.AliadosVivos
            .Where(a => (a.Escudo ? Potencia / 2 : Potencia) >= a.Vida)
            .MinBy(a => a.Vida);
        var objetivo = rematable ?? (contexto.NaveCarga.Viva ? contexto.NaveCarga : contexto.AliadosVivos.MinBy(a => a.Vida)!);
        return new(TipoAccion.Atacar, objetivo, Potencia, "calcula y apunta a " + objetivo.Nombre);
    }
}

// Lista de todas las IA disponibles, para el selector de la interfaz.
public static class Estrategias
{
    public static IEstrategiaIA[] Todas => new IEstrategiaIA[] { new IAAgresiva(), new IADefensiva(), new IAAleatoria(), new IACalculadora() };
    public static IEstrategiaIA Por(string nombre) => Todas.First(e => e.Nombre == nombre);
}
