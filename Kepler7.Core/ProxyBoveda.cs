using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Kepler7.Core.IBoveda;

namespace Kepler7.Core
{
    public class ProxyBoveda: IBoveda
    {
        private readonly IBoveda bovedaReal;
        private readonly RolBoveda rolUsuario;
        private readonly RegistroIntentos registro;

        public ProxyBoveda(RolBoveda rol)
        {
            rolUsuario = rol;

            // Objeto real al que representa el Proxy.
            bovedaReal = new BovedaDatos();

            // Componente encargado de registrar los accesos denegados.
            registro = new RegistroIntentos();
        }

        public string LeerArchivo(string id)
        {
            if (!TienePermiso())
            {
                registro.Registrar(rolUsuario, "Lectura", id);

                return "ACCESO DENEGADO: tu rol no tiene permiso para leer documentos.";
            }

            return bovedaReal.LeerArchivo(id);
        }

        public string EscribirArchivo(string id, string contenido)
        {
            if (!TienePermiso())
            {
                registro.Registrar(rolUsuario, "Escritura", id);

                return "ACCESO DENEGADO: tu rol no tiene permiso para modificar documentos.";
            }

            return bovedaReal.EscribirArchivo(id, contenido);
        }

        private bool TienePermiso()
        {
            return rolUsuario == RolBoveda.Capitan ||
                   rolUsuario == RolBoveda.OficialSeguridad;
        }

        public string ConsultarIntentosDenegados()
        {
            return registro.ConsultarHistorial();
        }
    }
}
