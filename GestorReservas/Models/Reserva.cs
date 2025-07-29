using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GestorReservas.Models
{
    public enum EstadoReserva
    {
        Pendiente,
        Aprobada,
        Rechazada
    }

    public class Reserva
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int UsuarioId { get; set; }
        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; }
        [Required]
        public int EspacioId { get; set; }
        [ForeignKey("EspacioId")]
        public Espacio Espacio { get; set; }
        [Required]
        public DateTime Fecha { get; set; }
        [Required]
        public string Horario { get; set; }
        [Required]
        public EstadoReserva Estado { get; set; }

        public string Descripcion { get; set; }
    }
}