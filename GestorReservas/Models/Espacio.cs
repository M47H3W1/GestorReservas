using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GestorReservas.Models
{
    public enum TipoEspacio
    {
        Aula,
        Laboratorio,
        Auditorio
    }

    public class Espacio
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Nombre { get; set; }
        [Required]
        public TipoEspacio Tipo { get; set; }
        [Required]
        public string Ubicacion { get; set; }
        public int Capacidad { get; set; }
        public string Descripcion { get; set; }
        public bool Disponible { get; set; }

    }
}