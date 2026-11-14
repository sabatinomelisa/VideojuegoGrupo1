namespace Kepler7.Core.Defensa;

// Arma y mantiene la cadena de defensas de una nave. Se puede cambiar en plena partida:
// agregar, quitar y reordenar eslabones. Después de cada cambio se vuelve a enlazar "Siguiente".
// Regla: Vida siempre queda al final y no se puede quitar.
public sealed class CadenaDefensa
{
    private readonly List<IManejadorDanio> eslabones = new();
    private readonly UnidadBatalla unidad;

    public CadenaDefensa(UnidadBatalla unidad) { this.unidad = unidad; }

    public IReadOnlyList<IManejadorDanio> Eslabones => eslabones;

    // Cadena de fábrica: Esquiva → Bloqueo → Armadura → Escudo → Vida.
    public static CadenaDefensa Estandar(UnidadBatalla u)
    {
        var c = new CadenaDefensa(u);
        c.Agregar(new Esquiva(u)); c.Agregar(new Bloqueo(u)); c.Agregar(new Armadura(u)); c.Agregar(new Escudo(u)); c.Agregar(new Vida(u));
        return c;
    }

    public bool Contiene(string nombre) => eslabones.Any(e => e.Nombre == nombre);

    public void Agregar(IManejadorDanio eslabon) { eslabones.Add(eslabon); Reenlazar(); }

    // Agrega un eslabón por nombre (sirve para el panel de la interfaz). Si ya existe, no hace nada.
    public bool Agregar(string nombre)
    {
        if (Contiene(nombre)) return false;
        IManejadorDanio? nuevo = nombre switch
        {
            "Esquiva" => new Esquiva(unidad), "Bloqueo" => new Bloqueo(unidad), "Armadura" => new Armadura(unidad),
            "Escudo" => new Escudo(unidad), _ => null
        };
        if (nuevo == null) return false;
        Agregar(nuevo);
        return true;
    }

    public bool Quitar(string nombre)
    {
        if (nombre == "Vida") return false;
        var e = eslabones.FirstOrDefault(x => x.Nombre == nombre);
        if (e == null) return false;
        eslabones.Remove(e); e.Siguiente = null; Reenlazar();
        return true;
    }

    // Mueve un eslabón una posición hacia adelante (-1) o hacia atrás (+1).
    public bool Mover(string nombre, int delta)
    {
        int i = eslabones.FindIndex(x => x.Nombre == nombre);
        int j = i + delta;
        if (i < 0 || nombre == "Vida" || j < 0 || j >= eslabones.Count) return false;
        (eslabones[i], eslabones[j]) = (eslabones[j], eslabones[i]);
        Reenlazar();
        return true;
    }

    public Danio Procesar(Danio danio)
    {
        danio.Registrar("Ataque de " + danio.Origen + ": " + danio.CantidadOriginal);
        if (eslabones.Count > 0) eslabones[0].Manejar(danio);
        danio.Registrar(danio.Esquivado ? "Resultado: ataque esquivado"
            : "Resultado: " + danio.LlegoALaVida + " al casco" + (danio.AbsorbidoPorEscudo > 0 ? ", " + danio.AbsorbidoPorEscudo + " absorbidos por el escudo" : ""));
        return danio;
    }

    // Prueba la cadena sin dejar secuelas: guarda el estado, procesa el golpe y lo restaura.
    public Danio Simular(int cantidad, Random? azar = null)
    {
        int vida = unidad.Vida; bool escudo = unidad.Escudo;
        var d = Procesar(new Danio(cantidad, "Simulación") { Azar = azar ?? new Random() });
        unidad.Vida = vida; unidad.Escudo = escudo;
        return d;
    }

    public string Descripcion() => string.Join(" → ", eslabones.Select(e => e.Nombre));

    private void Reenlazar()
    {
        var vidas = eslabones.OfType<Vida>().ToList();     // Vida siempre al final
        foreach (var v in vidas) eslabones.Remove(v);
        eslabones.AddRange(vidas);
        for (int i = 0; i < eslabones.Count; i++) eslabones[i].Siguiente = i + 1 < eslabones.Count ? eslabones[i + 1] : null;
    }
}
