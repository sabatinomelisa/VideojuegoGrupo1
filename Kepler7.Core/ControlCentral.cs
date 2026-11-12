
using System;
using System.Collections.Generic;

namespace Kepler7.Core
{
    // Interfaz común para los subsistemas de la estación
    public interface ISubsistemaEstacion
    {
        string Activar();
        string Desactivar();
    }

    // Subsistema de energía
    public class Energia : ISubsistemaEstacion
    {
        public string Activar()
        {
            return "Sistema de energía activado.";
        }

        public string Desactivar()
        {
            return "Sistema de energía desactivado.";
        }
    }

    // Subsistema de soporte vital
    public class SoporteVital : ISubsistemaEstacion
    {
        public string Activar()
        {
            return "Sistema de soporte vital activado.";
        }

        public string Desactivar()
        {
            return "Sistema de soporte vital desactivado.";
        }
    }

    // Subsistema de navegación
    public class Navegacion : ISubsistemaEstacion
    {
        public string Activar()
        {
            return "Sistema de navegación activado.";
        }

        public string Desactivar()
        {
            return "Sistema de navegación desactivado.";
        }
    }

    // Subsistema de comunicaciones de la estación
    public class ComunicacionesEstacion : ISubsistemaEstacion
    {
        public string Activar()
        {
            return "Sistema de comunicaciones activado.";
        }

        public string Desactivar()
        {
            return "Sistema de comunicaciones desactivado.";
        }
    }

    // Subsistema de seguridad
    public class SeguridadEstacion : ISubsistemaEstacion
    {
        public string Activar()
        {
            return "Sistema de seguridad activado.";
        }

        public string Desactivar()
        {
            return "Sistema de seguridad desactivado.";
        }
    }

    // Subsistema de defensa
    public class DefensaEstacion : ISubsistemaEstacion
    {
        public string Activar()
        {
            return "Sistema de defensa activado.";
        }

        public string Desactivar()
        {
            return "Sistema de defensa desactivado.";
        }
    }

    // Fachada: coordina todos los subsistemas
    public class ControlCentral
    {
        private readonly Energia energia = new Energia();
        private readonly SoporteVital soporteVital = new SoporteVital();
        private readonly Navegacion navegacion = new Navegacion();
        private readonly ComunicacionesEstacion comunicaciones =
            new ComunicacionesEstacion();
        private readonly SeguridadEstacion seguridad =
            new SeguridadEstacion();
        private readonly DefensaEstacion defensa = new DefensaEstacion();

        public List<string> IniciarTurno()
        {
            List<string> resultado = new List<string>();

            resultado.Add(energia.Activar());
            resultado.Add(soporteVital.Activar());
            resultado.Add(navegacion.Activar());
            resultado.Add(comunicaciones.Activar());
            resultado.Add(seguridad.Activar());
            resultado.Add(defensa.Activar());

            return resultado;
        }

        public List<string> EntrarEnEmergencia()
        {
            List<string> resultado = new List<string>();

            resultado.Add("¡ALERTA! La estación entra en emergencia.");
            resultado.Add(seguridad.Activar());
            resultado.Add(defensa.Activar());
            resultado.Add(soporteVital.Activar());
            resultado.Add(comunicaciones.Activar());

            return resultado;
        }

        public List<string> ApagarEstacion()
        {
            List<string> resultado = new List<string>();

            resultado.Add(defensa.Desactivar());
            resultado.Add(comunicaciones.Desactivar());
            resultado.Add(navegacion.Desactivar());
            resultado.Add(seguridad.Desactivar());
            resultado.Add(soporteVital.Desactivar());
            resultado.Add(energia.Desactivar());

            resultado.Add("La estación fue apagada.");

            return resultado;
        }
    }
}
