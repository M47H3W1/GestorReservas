using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GestorReservas.Models
{
    public enum TipoDepartamento
    {
        DACI = 1,  // Departamento de Automatización y Control Industrial
        DETRI = 2, // Departamento de Electrónica, Telecomunicaciones y Redes de Información
        DEE = 3    // Departamento de Energía Eléctrica
    }

    public class Departamento
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(10)]
        public string Codigo { get; set; } // DACI, DETRI, DEE

        [Required]
        public TipoDepartamento Tipo { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }

        // Jefe del departamento (Coordinador)
        public int? JefeId { get; set; }
        public virtual Usuario Jefe { get; set; }

        // Profesores del departamento
        public virtual ICollection<Usuario> Profesores { get; set; }

        public Departamento()
        {
            Profesores = new HashSet<Usuario>();
        }
    }
}