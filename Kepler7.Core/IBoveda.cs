using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepler7.Core
{
    public interface IBoveda
    {
        // Interfaz que define las operaciones disponibles
        // para acceder a los documentos de la estación.
        string LeerArchivo(string id);

        string EscribirArchivo(string id, string contenido);
        // Define los roles que pueden utilizar la bóveda.
        public enum RolBoveda
        {
            Tripulante,
            Capitan,
            OficialSeguridad
        }

    }
}
