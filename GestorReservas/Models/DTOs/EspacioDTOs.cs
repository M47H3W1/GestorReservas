using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GestorReservas.Models.DTOs
{
    public class EspacioDto
    {
        public string Nombre { get; set; }
        public TipoEspacio Tipo { get; set; }
        public int Capacidad { get; set; }
        public string Ubicacion { get; set; }
        public string Descripcion { get; set; }
        public bool? Disponible { get; set; }
    }

    public class DisponibilidadDto
    {
        public bool Disponible { get; set; }
    }

    public class EspacioResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public int Capacidad { get; set; }
        public string Ubicacion { get; set; }
        public string Descripcion { get; set; }
        public bool Disponible { get; set; }
        public System.DateTime FechaCreacion { get; set; }
    }
}