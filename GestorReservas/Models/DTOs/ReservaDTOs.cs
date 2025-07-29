using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GestorReservas.Controllers;

namespace GestorReservas.Models.DTOs
{
    public class ReservaCreateDto
    {
        public int UsuarioId { get; set; }
        public int EspacioId { get; set; }
        public DateTime Fecha { get; set; }
        public string Horario { get; set; }
        public string Descripcion { get; set; }
    }

    public class ReservaUpdateDto
    {
        public DateTime? Fecha { get; set; }
        public string Horario { get; set; }
        public string Descripcion { get; set; }
    }

    public class ReservaResponseDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public UsuarioResponseDto Usuario { get; set; }
        public int EspacioId { get; set; }
        public EspacioResponseDto Espacio { get; set; }
        public DateTime Fecha { get; set; }
        public string Horario { get; set; }
        public string Estado { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}