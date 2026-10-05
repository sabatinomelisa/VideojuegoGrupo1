namespace Kepler7.Core;
public sealed class Emergencia
{
    public const float LimiteSegundos=120;
    public float Restante { get; private set; }=LimiteSegundos;
    public bool Resuelta { get; private set; }
    public bool Critica => !Resuelta && Restante>0 && Restante<15;
    public bool Agotada => !Resuelta && Restante<=0;
    public void Avanzar(float segundos,bool pausada=false)
    {if(!Resuelta && !pausada && float.IsFinite(segundos))Restante=Math.Max(0,Restante-Math.Max(0,segundos));}
    public void Resolver(){if(!Agotada)Resuelta=true;}
    public void Reiniciar(){Restante=LimiteSegundos;Resuelta=false;}
}

public sealed record MedallaRescate(float TiempoSegundos,string Nivel)
{
    public static MedallaRescate Obtener(float restante)
    {
        if(!float.IsFinite(restante) || restante<=0 || restante>Emergencia.LimiteSegundos)throw new ArgumentOutOfRangeException(nameof(restante));
        float transcurrido=Emergencia.LimiteSegundos-restante;
        return new(transcurrido,transcurrido<=60?"Oro":transcurrido<=90?"Plata":"Bronce");
    }
}
