using System.Numerics;
using System.Text.Json;
namespace Kepler7.Core;

public sealed class Mutante
{
    public int VarianteVisual { get; set; }
    public Vector2 Posicion { get; set; }
    public int Vida { get; set; } = 2;
    public float Velocidad { get; set; }
    public float Impacto { get; set; }
}
public sealed class Disparo
{
    public Vector2 Posicion;
    public Vector2 Velocidad;
    public float Tiempo = 1.6f;
}
public sealed class Particula
{
    public Vector2 Posicion;
    public Vector2 Velocidad;
    public float Tiempo;
    public bool Danio;
}
public readonly record struct Caja(float X, float Y, float W, float H)
{
    public bool Toca(Vector2 p, float radio) => p.X + radio > X && p.X - radio < X + W && p.Y + radio > Y && p.Y - radio < Y + H;
}
// Simulación independiente del formulario. Tick recibe segundos, nunca controles de Windows.
public sealed class Pasillo
{
    public const float Largo = 2700;
    public Vector2 Jugador { get; private set; } = new(120, 285);
    public int Vida { get; private set; } = 100;
    public int Bajas { get; private set; }
    public int Total => 15;
    public bool Derrota => Vida <= 0;
    public bool PuedeReparar => !Derrota && Bajas == Total && Jugador.X > 2470;
    public float Invulnerable { get; private set; }
    public float Tiempo { get; private set; }
    public List<Mutante> Mutantes { get; } = new();
    public List<Disparo> Disparos { get; } = new();
    public List<Particula> Particulas { get; } = new();
    public List<Vector2> Botiquines { get; } = new() { new(910, 430), new(1730, 125) };
    public Caja[] Obstaculos { get; } = { new(480, 55, 85, 155), new(960, 365, 90, 150), new(1640, 45, 85, 150), new(2180, 360, 85, 160) };
    // Mismas dimensiones que DrawPod a escala .64; la decoración usa estas cajas.
    public Caja[] Capsulas { get; } = Enumerable.Range(0,6).Select(i => new Caja(240+i*420-44.8f,131.76f,89.6f,105.6f)).ToArray();
    public IEnumerable<Caja> Solidos => Obstaculos.Concat(Capsulas);
    private float recarga;
    private readonly Random random;
    public Pasillo(int semilla = 7)
    {
        random = new Random(semilla);
        for (int i = 0; i < Total; i++)
        {
            var pos = new Vector2(490 + i * 137, 245 + (i % 3 - 1) * 100);
            if (Solidos.Any(o => o.Toca(pos,20))) pos.Y = 285;
            Mutantes.Add(new Mutante { Posicion=pos, Velocidad=65+i%4*10, VarianteVisual=i%4 });
        }
    }
    public void Tick(float dt, Vector2 movimiento, Vector2 apuntar, bool disparar)
    {
        if (Derrota) return;
        dt = Math.Clamp(dt, 0, .05f); Tiempo += dt;
        Invulnerable = Math.Max(0, Invulnerable - dt); recarga -= dt;
        if (movimiento.LengthSquared() > 1) movimiento = Vector2.Normalize(movimiento);
        Jugador = Mover(Jugador, movimiento * (230 * dt), 21);
        var direccion = apuntar - Jugador;
        if (disparar && recarga <= 0 && direccion.LengthSquared() > .01f)
        {
            direccion = Vector2.Normalize(direccion);
            Disparos.Add(new Disparo { Posicion = Jugador + direccion * 31, Velocidad = direccion * 780 });
            recarga = .18f;
        }
        foreach (var m in Mutantes)
        {
            m.Impacto = Math.Max(0, m.Impacto - dt);
            var d = Jugador - m.Posicion;
            if (d.Length() < 670 && d.Length() > .1f)
            {
                var paso = Vector2.Normalize(d) * m.Velocidad * dt;
                var siguiente = Mover(m.Posicion, paso, 20);
                // Si un cajón bloquea el recorrido, rodearlo por el centro del pasillo.
                if (Vector2.DistanceSquared(siguiente, m.Posicion) < paso.LengthSquared() * .2f)
                    siguiente = Mover(m.Posicion, new Vector2(0, Math.Sign(280 - m.Posicion.Y) * m.Velocidad * dt), 20);
                m.Posicion = siguiente;
            }
            if (Vector2.Distance(m.Posicion, Jugador) < 43 && Invulnerable <= 0)
            {
                Vida = Math.Max(0, Vida - 20); Invulnerable = 1.15f; Emitir(Jugador, true);
                var alejamiento = d.LengthSquared() > .01f ? -Vector2.Normalize(d) : Vector2.UnitX;
                m.Posicion = Mover(m.Posicion, alejamiento * 50, 20);
            }
        }
        for (int i = Disparos.Count - 1; i >= 0; i--)
        {
            var b = Disparos[i]; b.Posicion += b.Velocidad * dt; b.Tiempo -= dt;
            bool quitar = b.Tiempo <= 0 || b.Posicion.Y < 30 || b.Posicion.Y > 535 || Solidos.Any(o => o.Toca(b.Posicion, 3));
            if (!quitar)
                for (int j = Mutantes.Count - 1; j >= 0; j--)
                    if (Vector2.Distance(b.Posicion, Mutantes[j].Posicion) < 29)
                    {
                        var m = Mutantes[j]; m.Vida--; m.Impacto = .12f; quitar = true; Emitir(m.Posicion, false);
                        if (m.Vida <= 0) { Mutantes.RemoveAt(j); Bajas++; }
                        break;
                    }
            if (quitar) Disparos.RemoveAt(i);
        }
        for (int i = Botiquines.Count - 1; i >= 0; i--)
            if (Vida < 100 && Vector2.Distance(Botiquines[i], Jugador) < 36)
            { Vida = Math.Min(100, Vida + 30); Botiquines.RemoveAt(i); }
        for (int i = Particulas.Count - 1; i >= 0; i--)
        {
            var p = Particulas[i]; p.Posicion += p.Velocidad * dt; p.Tiempo -= dt;
            if (p.Tiempo <= 0) Particulas.RemoveAt(i);
        }
    }
    private Vector2 Mover(Vector2 p, Vector2 delta, float radio)
    {
        var x = new Vector2(Math.Clamp(p.X + delta.X, 40, Largo - 55), p.Y);
        if (!Solidos.Any(o => o.Toca(x, radio))) p = x;
        var y = new Vector2(p.X, Math.Clamp(p.Y + delta.Y, 48, 515));
        if (!Solidos.Any(o => o.Toca(y, radio))) p = y;
        return p;
    }
    private void Emitir(Vector2 p, bool danio)
    {
        for (int k = 0; k < 9; k++) Particulas.Add(new Particula { Posicion = p, Velocidad = new Vector2(random.Next(-110, 111), random.Next(-110, 111)), Tiempo = .35f, Danio = danio });
    }
}
public sealed class Progreso
{
    public MedallaRescate? MedallaRescate { get; set; }
    public HashSet<string> GuiasVistas { get; set; } = new();
    public Tripulante IngenieroBase { get; set; } = Prototipos.Ingeniero;
    public Tripulante PilotoBase { get; set; } = Prototipos.Piloto;
    public bool LaboratorioReparado { get; set; }
    public Tripulante? Ingeniero { get; set; }
    public List<Tripulante> Pilotos { get; set; } = new();
    public List<Nave> Flota { get; set; } = new();
    public int XP { get; set; }
    public bool Lanzada { get; set; }
    // Registro fundacional estable: se reconstruye con los mismos IDs al cargar.
    private List<Tripulante>? habitantes;
    [System.Text.Json.Serialization.JsonIgnore]
    public List<Tripulante> Habitantes => habitantes ??= BibliotecaTripulantes.CrearHabitantes();
    public bool FlyweightDominado { get; set; }
    public bool BridgeDominado {get;set;}
    public List<Transmision> Transmisiones {get;set;}=new();
    public List<ResumenOperacion> Operaciones {get;set;}=new();
    public void RegistrarOperacion(ResumenOperacion resumen)
    {
        Operaciones.Add(resumen);if(Operaciones.Count>20)Operaciones.RemoveAt(0);
    }
    public bool RegistrarTransmision(Transmision transmision)
    {
        if(!string.IsNullOrEmpty(transmision.MisionId) && Transmisiones.Any(t=>t.MisionId==transmision.MisionId))return false;
        Transmisiones.Add(transmision);
        bool logro=!BridgeDominado && (Transmisiones.Any(t=>!string.IsNullOrEmpty(t.MisionId)) || Transmisiones.Select(t=>t.Tipo).Distinct().Count()>=3 && Transmisiones.Select(t=>t.Cifrado).Distinct().Count()>=3);
        if(logro){BridgeDominado=true;XP+=150;}
        return logro;
    }
    public bool RegistrarCenso()
    {
        if(FlyweightDominado)return false;
        _=Habitantes;FlyweightDominado=true;XP+=100;return true;
    }
    public bool PrototypeDominado => Ingeniero != null;
    public bool BuilderDominado => Flota.Count > 0;
    public bool Lista => RecetaNave.Todas.All(r => Flota.Any(n => n.Tipo == r.Nombre && n.Piloto?.Especialidad == r.Nombre));
    public Tripulante ClonarIngeniero(string nombre, string color, string tipo = "Atigrado")
    {
        if (!LaboratorioReparado) throw new InvalidOperationException("Primero repará el laboratorio.");
        var clon = (Tripulante)Prototipos.ConAspecto(IngenieroBase, tipo).Clonar();
        clon.Nombre = string.IsNullOrWhiteSpace(nombre) ? "Jefa Misha" : nombre.Trim();
        clon.Uniforme.Color = color; clon.Habilidades.Add("Ensamblaje de naves");
        if (Ingeniero == null) XP += 100;
        Ingeniero = clon; return clon;
    }
    public Tripulante ClonarPiloto(string nombre, string color, RecetaNave receta, string tipo = "Atigrado")
    {
        if (Ingeniero == null) throw new InvalidOperationException("Primero cloná al ingeniero.");
        var clon = (Tripulante)Prototipos.ConAspecto(PilotoBase, tipo).Clonar();
        clon.Nombre = string.IsNullOrWhiteSpace(nombre) ? "Michi " + (Pilotos.Count + 1) : nombre.Trim();
        clon.Uniforme.Color = color; clon.Especialidad = receta.Nombre; clon.Habilidades.Add(receta.HabilidadPiloto);
        Pilotos.Add(clon); XP += 25; return clon;
    }
    public List<Tripulante> ClonarPilotos(int cantidad,string prefijo,string color,RecetaNave receta,string tipo)
    {
        if(Ingeniero==null)throw new InvalidOperationException("Primero cloná al ingeniero.");
        if(cantidad<1 || cantidad>10000)throw new ArgumentOutOfRangeException(nameof(cantidad),"El lote admite de 1 a 10.000 pilotos.");
        var prototipo=Prototipos.ConAspecto(PilotoBase,tipo);
        var lote=new List<Tripulante>();
        var usados=new HashSet<string>(Pilotos.Select(p=>p.Nombre),StringComparer.OrdinalIgnoreCase);
        string raiz=string.IsNullOrWhiteSpace(prefijo)?"Michi":prefijo.Trim();int suffix=1;
        for(int i=0;i<cantidad;i++)
        {
            var clon=(Tripulante)prototipo.Clonar();string nombre;
            do {nombre=raiz+" "+suffix++;} while(!usados.Add(nombre));
            clon.Nombre=nombre;clon.Uniforme.Color=color;clon.Especialidad=receta.Nombre;clon.Habilidades.Add(receta.HabilidadPiloto);
            Pilotos.Add(clon);lote.Add(clon);XP+=25;
        }
        return lote;
    }
    public void RegistrarNave(Nave nave)
    {
        if (Ingeniero == null) throw new InvalidOperationException("Se necesita al ingeniero.");
        Flota.Add(nave); XP += 75;
    }
    public bool PilotoOcupado(Tripulante piloto) => Flota.Any(n => n.Piloto != null &&
        (ReferenceEquals(n.Piloto,piloto) || n.Piloto.Identificador == piloto.Identificador || n.Piloto.Nombre == piloto.Nombre && n.Piloto.Especialidad == piloto.Especialidad));
    public string ErrorAsignacion(Nave nave,Tripulante piloto)
    {
        if(!Flota.Contains(nave) || !Pilotos.Contains(piloto)) return "Elegí una nave y un piloto de tu estación.";
        if(nave.Piloto != null) return "Esta nave ya tiene piloto.";
        if(piloto.Especialidad != nave.Tipo) return "Acceso rechazado: el piloto debe especializarse en " + nave.Tipo + ".";
        if(PilotoOcupado(piloto)) return "Este piloto ya está asignado a otra nave.";
        return "";
    }
    public bool AsignarPiloto(Nave nave,Tripulante piloto)
    {
        if(ErrorAsignacion(nave,piloto).Length > 0) return false;
        nave.Piloto=piloto;return true;
    }
    public Tripulante[] PilotosDisponibles(string? especialidad=null,string? especie=null)
    {
        var assigned=Flota.Where(n=>n.Piloto!=null).Select(n=>n.Piloto!).ToArray();
        var ids=assigned.Select(p=>p.Identificador).ToHashSet();
        var names=assigned.Select(p=>(p.Nombre,p.Especialidad)).ToHashSet();
        return Pilotos.Where(p=>p.Rol=="Piloto" && !ids.Contains(p.Identificador) && !names.Contains((p.Nombre,p.Especialidad))
            && (especialidad==null || p.Especialidad==especialidad) && (especie==null || p.Especie.Nombre==especie)).ToArray();
    }
    public List<Nave> AsignarLote(int cantidad,string? especialidad=null,string? especie=null)
    {
        if(cantidad<1 || cantidad>10000)throw new ArgumentOutOfRangeException(nameof(cantidad));
        var ocupados=Flota.Where(n=>n.Piloto!=null).Select(n=>n.Piloto!).ToArray();
        var ids=ocupados.Select(p=>p.Identificador).ToHashSet();
        var nombres=ocupados.Select(p=>(p.Nombre,p.Especialidad)).ToHashSet();
        var candidatos=PilotosDisponibles(especialidad,especie)
            .GroupBy(p=>p.Especialidad).ToDictionary(g=>g.Key,g=>new Queue<Tripulante>(g));
        var asignadas=new List<Nave>();
        foreach(var nave in Flota.Where(n=>n.Piloto==null && (especialidad==null || n.Tipo==especialidad)))
        {
            if(!candidatos.TryGetValue(nave.Tipo,out var cola))continue;
            while(cola.Count>0)
            {
                var piloto=cola.Dequeue();
                if(ids.Contains(piloto.Identificador) || nombres.Contains((piloto.Nombre,piloto.Especialidad)))continue;
                nave.Piloto=piloto;ids.Add(piloto.Identificador);nombres.Add((piloto.Nombre,piloto.Especialidad));asignadas.Add(nave);break;
            }
            if(asignadas.Count==cantidad)break;
        }
        return asignadas;
    }
    public bool AsignarPiloto(Nave nave)
    {
        var piloto=Pilotos.FirstOrDefault(p=>ErrorAsignacion(nave,p).Length==0);
        return piloto!=null && AsignarPiloto(nave,piloto);
    }

}
public static class Guardado
{
    public static string Ruta => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kepler7", "prototipo-gatitos.json");
    public static void Guardar(Progreso progreso, string? ruta = null)
    {
        var destino = ruta ?? Ruta;
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        string temporal = destino + ".tmp";
        File.WriteAllText(temporal, JsonSerializer.Serialize(progreso, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporal, destino, true);
    }
    public static Progreso Cargar(string? ruta = null)
    {
        var destino = ruta ?? Ruta;
        if (!File.Exists(destino)) return new();
        var p = JsonSerializer.Deserialize<Progreso>(File.ReadAllText(destino)) ?? throw new InvalidDataException("Guardado vacío.");
        if (p.Flota == null || p.Pilotos == null || p.Flota.Any(n => n.Componentes == null)) throw new InvalidDataException("Guardado inválido.");
        return p;
    }
}
