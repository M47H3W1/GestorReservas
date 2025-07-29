using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GestorReservas.Models
{
    public class ValidacionResult
    {
        public bool EsValido { get; set; }
        public string Mensaje { get; set; }

        public ValidacionResult(bool esValido, string mensaje)
        {
            EsValido = esValido;
            Mensaje = mensaje;
        }
    }

}