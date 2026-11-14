using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Kepler7.Core.IBoveda;

namespace Kepler7.Core
{
    public class RegistroIntentos
    {
        private readonly string rutaArchivo;

        public RegistroIntentos()
        {
            string carpeta = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "Kepler7"
            );

            Directory.CreateDirectory(carpeta);

            rutaArchivo = Path.Combine(
                carpeta,
                "intentos-denegados.json"
            );
        }

        public void Registrar(RolBoveda rol, string operacion, string documento)
        {
            var intento = new
            {
                Fecha = DateTime.Now,
                Usuario = rol.ToString(),
                Operacion = operacion,
                Documento = documento,
                Resultado = "Acceso denegado"
            };

            string json = JsonSerializer.Serialize(intento);

            File.AppendAllText(
                rutaArchivo,
                json + Environment.NewLine
            );
        }

        public string ConsultarHistorial()
        {
            if (!File.Exists(rutaArchivo))
            {
                return "Todavía no hay intentos registrados.";
            }

            return File.ReadAllText(rutaArchivo);
        }
    }
}
