namespace Kepler7.Core;

//Patrón State

// El comportamiento del gato (moverse, atacar, recibir daño) cambia
// según su estado actual, sin llenar el código de if.

// Interfaz común a todos los estados.
public interface IEstadoPersonaje
{
    string Nombre { get; }
    float Duracion { get; }            // segundos que dura (0 = no se termina solo)
    int DanioPorSegundo { get; }       // daño que causa con el tiempo (veneno)
    float Mover(float velocidadBase);  // devuelve la velocidad real
    bool Atacar(out float recarga);    // ¿puede disparar? y cada cuánto
    int RecibirDanio(int danioBase);   // devuelve el daño real que recibe
}

#region# "Estados"

// Estado Normal: el gato se comporta como siempre, sin modificaciones.
public sealed class Normal : IEstadoPersonaje
{
    public string Nombre => "Normal";
    public float Duracion => 0;
    public int DanioPorSegundo => 0;
    public float Mover(float velocidadBase) => velocidadBase;
    public bool Atacar(out float recarga) { recarga = .18f; return true; }
    public int RecibirDanio(int danioBase) => danioBase;
}

// Envenenado: pierde vida con el tiempo, se mueve y dispara más lento.
public sealed class Envenenado : IEstadoPersonaje
{
    public string Nombre => "Envenenado";
    public float Duracion => 5;
    public int DanioPorSegundo => 4;
    public float Mover(float velocidadBase) => velocidadBase * .8f;
    public bool Atacar(out float recarga) { recarga = .28f; return true; }
    public int RecibirDanio(int danioBase) => danioBase;
}

// Aturdido: no puede disparar y apenas se mueve.
public sealed class Aturdido : IEstadoPersonaje
{
    public string Nombre => "Aturdido";
    public float Duracion => 2;
    public int DanioPorSegundo => 0;
    public float Mover(float velocidadBase) => velocidadBase * .35f;
    public bool Atacar(out float recarga) { recarga = .18f; return false; }
    public int RecibirDanio(int danioBase) => danioBase;
}

// Berserk: más rápido y dispara a ráfagas, pero recibe más daño.
public sealed class Berserk : IEstadoPersonaje
{
    public string Nombre => "Berserk";
    public float Duracion => 6;
    public int DanioPorSegundo => 0;
    public float Mover(float velocidadBase) => velocidadBase * 1.3f;
    public bool Atacar(out float recarga) { recarga = .07f; return true; }
    public int RecibirDanio(int danioBase) => (int)(danioBase * 1.5f);
}

#endregion#

// Contexto: el gato. No decide nada por sí mismo; le delega todo a su estado actual.
public sealed class Personaje
{
    public IEstadoPersonaje Estado { get; private set; } = new Normal();
    public float TiempoRestante { get; private set; }
    private float venenoAcumulado;

    public void CambiarEstado(IEstadoPersonaje nuevo)
    {
        Estado = nuevo;
        TiempoRestante = nuevo.Duracion;
        venenoAcumulado = 0;
    }

    // Delegación. Acá es donde el personaje va a preguntar su estado.
    public float Mover(float velocidadBase) => Estado.Mover(velocidadBase);
    public bool Atacar(out float recarga) => Estado.Atacar(out recarga);
    public int RecibirDanio(int danioBase) => Estado.RecibirDanio(danioBase);

    // Se llama en cada cuadro del juego. Devuelve el daño por veneno de ese cuadro
    // y vuelve a Normal cuando se termina el tiempo del estado.
    public int Actualizar(float dt)
    {
        if (Estado.Duracion <= 0) return 0;
        venenoAcumulado += Estado.DanioPorSegundo * dt;
        int danio = (int)venenoAcumulado;
        venenoAcumulado -= danio;
        TiempoRestante -= dt;
        if (TiempoRestante <= 0) CambiarEstado(new Normal());
        return danio;
    }
}