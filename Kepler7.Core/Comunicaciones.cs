using System.Security.Cryptography;
using System.Text;
namespace Kepler7.Core;

// Bridge: los tipos de mensaje y los mecanismos de cifrado varían por separado.
public interface ICifrado
{
    string Nombre { get; }
    string Cifrar(string texto);
    string Descifrar(string contenido);
}
public sealed class SinCifrado : ICifrado
{
    public string Nombre=>"Sin cifrado";
    public string Cifrar(string texto)=>texto;
    public string Descifrar(string contenido)=>contenido;
}
public sealed class CifradoAES : ICifrado
{
    private readonly byte[] clave=RandomNumberGenerator.GetBytes(32);
    public string Nombre=>"AES";
    public string Cifrar(string texto)
    {
        using var aes=Aes.Create();aes.Key=clave;aes.GenerateIV();
        byte[] bytes=Encoding.UTF8.GetBytes(texto);
        byte[] result=aes.CreateEncryptor().TransformFinalBlock(bytes,0,bytes.Length);
        return Convert.ToBase64String(aes.IV.Concat(result).ToArray());
    }
    public string Descifrar(string contenido)
    {
        byte[] bytes=Convert.FromBase64String(contenido);
        if(bytes.Length<32)throw new ArgumentException("Paquete AES incompleto.");
        using var aes=Aes.Create();aes.Key=clave;aes.IV=bytes[..16];
        return Encoding.UTF8.GetString(aes.CreateDecryptor().TransformFinalBlock(bytes,16,bytes.Length-16));
    }
}
// Simulación de ficción: XOR con clave efímera local. No es criptografía cuántica real.
public sealed class CifradoCuantico : ICifrado
{
    private readonly Dictionary<string,byte[]> claves=new();
    public string Nombre=>"Cuántico (simulado)";
    public string Cifrar(string texto)
    {
        byte[] data=Encoding.UTF8.GetBytes(texto),key=RandomNumberGenerator.GetBytes(data.Length);
        string id=Guid.NewGuid().ToString("N");claves[id]=key;
        for(int i=0;i<data.Length;i++)data[i]^=key[i];
        return "QSIM:"+id+":"+Convert.ToBase64String(data);
    }
    public string Descifrar(string contenido)
    {
        var parts=contenido.Split(':');
        if(parts.Length!=3 || parts[0]!="QSIM" || !claves.TryGetValue(parts[1],out var key))throw new ArgumentException("Canal cuántico desconocido.");
        byte[] data=Convert.FromBase64String(parts[2]);
        if(data.Length!=key.Length)throw new ArgumentException("Paquete incompleto.");
        for(int i=0;i<data.Length;i++)data[i]^=key[i];
        return Encoding.UTF8.GetString(data);
    }
}
public abstract class Mensaje
{
    public ICifrado Cifrado { get; set; }
    public abstract string Tipo { get; }
    protected Mensaje(ICifrado cifrado)=>Cifrado=cifrado;
    public Transmision Enviar(string texto)
    {
        if(string.IsNullOrWhiteSpace(texto) || texto.Length>160)throw new ArgumentException("Escribí entre 1 y 160 caracteres.");
        string original="["+Tipo+"] "+texto.Trim();
        string resultado=Cifrado.Cifrar(original);
        return new Transmision { Tipo=Tipo,Cifrado=Cifrado.Nombre,Original=original,Resultado=resultado,Recibido=Cifrado.Descifrar(resultado) };
    }
}
public sealed class MensajeTexto : Mensaje { public MensajeTexto(ICifrado cifrado):base(cifrado){} public override string Tipo=>"Texto"; }
public sealed class MensajeAlerta : Mensaje { public MensajeAlerta(ICifrado cifrado):base(cifrado){} public override string Tipo=>"Alerta"; }
public sealed class MensajeEmergencia : Mensaje { public MensajeEmergencia(ICifrado cifrado):base(cifrado){} public override string Tipo=>"Emergencia"; }
public sealed class Transmision
{
    public string MisionId {get;set;}="";
    public string RespuestaTierra {get;set;}="";
    public string Tipo {get;set;}="";
    public string Cifrado {get;set;}="";
    public string Original {get;set;}="";
    public string Resultado {get;set;}="";
    public string Recibido {get;set;}="";
}
public sealed class EnlaceComunicaciones
{
    private readonly float[] objetivos=new float[3],ajustes=new float[3];
    public IReadOnlyList<float> Objetivos=>Array.AsReadOnly(objetivos);
    public IReadOnlyList<float> Ajustes=>Array.AsReadOnly(ajustes);
    public float Estabilidad {get;private set;}
    public bool Alineado=>Enumerable.Range(0,3).All(i=>Math.Abs(objetivos[i]-ajustes[i])<=5);
    public bool Listo=>Estabilidad>=1.2f;
    public EnlaceComunicaciones(Random? random=null)
    {
        random??=new Random();for(int i=0;i<3;i++){objetivos[i]=random.Next(25,76);ajustes[i]=i%2==0?0:100;}
    }
    public void Ajustar(int canal,float valor)
    {
        if(canal<0 || canal>=3 || !float.IsFinite(valor))return;
        ajustes[canal]=Math.Clamp(valor,0,100);if(!Alineado)Estabilidad=0;
    }
    public void Avanzar(float dt){if(!float.IsFinite(dt) || dt<0)return;Estabilidad=Alineado?Math.Min(1.2f,Estabilidad+dt):0;}
    public Transmision Transmitir(Mensaje mensaje,string texto)
    {
        if(!Listo)throw new InvalidOperationException("Alineá las tres antenas y estabilizá la señal.");
        return mensaje.Enviar(texto);
    }
}
