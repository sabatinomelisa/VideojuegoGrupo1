using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepler7.Core
{
    public class BovedaDatos: IBoveda
    {
        private readonly string carpeta;

        public BovedaDatos()
        {
            carpeta = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "Kepler7",
                "Boveda"
            );

            // Si la carpeta no existe, la crea.
            Directory.CreateDirectory(carpeta);

            // Crea un documento inicial para poder probar la lectura.
            string rutaInicial = ObtenerRuta("informe-mision");

            if (!File.Exists(rutaInicial))
            {
                File.WriteAllText(
                    rutaInicial,
                    "INFORME DE MISIÓN\nEstación Kepler-7.\nEstado: operativo."
                );
            }
        }

        public string LeerArchivo(string id)
        {
            string ruta = ObtenerRuta(id);

            if (!File.Exists(ruta))
            {
                return "El documento solicitado no existe.";
            }

            return File.ReadAllText(ruta);
        }

        public string EscribirArchivo(string id, string contenido)
        {
            string ruta = ObtenerRuta(id);

            File.WriteAllText(ruta, contenido);

            return "Documento guardado correctamente.";
        }

        private string ObtenerRuta(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("El identificador no puede estar vacío.");
            }

            // Evita que el nombre permita acceder a archivos fuera de la bóveda.
            if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                id.Contains("/") || id.Contains("\\") ||
                id == "." || id == "..")
            {
                throw new ArgumentException("El identificador del documento no es válido.");
            }

            return Path.Combine(carpeta, id + ".txt");
        }
    }
}
