namespace Kepler7.Core.Defensa;

// [PATRÓN CHAIN OF RESPONSIBILITY · Eslabón] Probabilidad de evitar el ataque por completo.
public sealed class Esquiva : ManejadorDanioBase
{
    public Esquiva(UnidadBatalla unidad) : base(unidad) { }
    public override string Nombre => "Esquiva";

    protected override void Procesar(Danio danio)
    {
        if (unidad.ProbEsquiva <= 0) { danio.Registrar("[Esquiva] sin módulo de esquiva"); return; }
        var azar = danio.Azar ?? Random.Shared;
        if (azar.NextDouble() < unidad.ProbEsquiva)
        {
            danio.Registrar("[Esquiva] ¡Esquivó el ataque! Se evitan " + danio.Cantidad);
            danio.Cantidad = 0;
            danio.Esquivado = true;
        }
        else danio.Registrar("[Esquiva] No logró esquivar");
    }
}

// [PATRÓN CHAIN OF RESPONSIBILITY · Eslabón] Aplica el resultado del minijuego de bloqueo (ESPACIO).
public sealed class Bloqueo : ManejadorDanioBase
{
    public Bloqueo(UnidadBatalla unidad) : base(unidad) { }
    public override string Nombre => "Bloqueo";

    protected override void Procesar(Danio danio)
    {
        if (danio.FactorBloqueo >= 1f) { danio.Registrar("[Bloqueo] sin bloqueo"); return; }
        int restante = (int)Math.Round(danio.Cantidad * danio.FactorBloqueo);
        danio.Registrar("[Bloqueo] Bloquea " + (danio.Cantidad - restante) + ". Restan " + restante);
        danio.Cantidad = restante;
    }
}

// [PATRÓN CHAIN OF RESPONSIBILITY · Eslabón] Resta una cantidad fija a cada impacto.
public sealed class Armadura : ManejadorDanioBase
{
    public Armadura(UnidadBatalla unidad) : base(unidad) { }
    public override string Nombre => "Armadura";

    protected override void Procesar(Danio danio)
    {
        int absorbido = Math.Min(Math.Max(0, unidad.ReduccionArmadura), danio.Cantidad);
        if (absorbido == 0) { danio.Registrar("[Armadura] sin blindaje"); return; }
        danio.Cantidad -= absorbido;
        danio.Registrar("[Armadura] Reduce " + absorbido + ". Restan " + danio.Cantidad);
    }
}

// [PATRÓN CHAIN OF RESPONSIBILITY · Eslabón] El escudo de la nave absorbe la mitad de un único impacto y se apaga.
public sealed class Escudo : ManejadorDanioBase
{
    public Escudo(UnidadBatalla unidad) : base(unidad) { }
    public override string Nombre => "Escudo";

    protected override void Procesar(Danio danio)
    {
        if (!unidad.Escudo) { danio.Registrar("[Escudo] apagado"); return; }
        int pasa = danio.Cantidad / 2;
        danio.AbsorbidoPorEscudo += danio.Cantidad - pasa;
        danio.Registrar("[Escudo] Absorbe " + (danio.Cantidad - pasa) + " y se apaga. Restan " + pasa);
        danio.Cantidad = pasa;
        unidad.Escudo = false;
    }
}

// [PATRÓN CHAIN OF RESPONSIBILITY · Último eslabón] Lo que llega hasta acá se descuenta del casco.
public sealed class Vida : ManejadorDanioBase
{
    public Vida(UnidadBatalla unidad) : base(unidad) { }
    public override string Nombre => "Vida";

    protected override void Procesar(Danio danio)
    {
        danio.LlegoALaVida = danio.Cantidad;
        unidad.Vida = Math.Max(0, unidad.Vida - danio.Cantidad);
        danio.Registrar("[Vida] Recibe " + danio.Cantidad + ". Casco: " + unidad.Vida + "/" + unidad.VidaMax);
        danio.Cantidad = 0;
        if (!unidad.Viva) { danio.ObjetivoDerrotado = true; danio.Registrar("[Vida] ¡" + unidad.Nombre + " fue destruida!"); }
    }
}
