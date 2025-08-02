using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;



namespace GestorReservas.Models
{
    public enum RolUsuario
    {
        Profesor = 1,
        Coordinador = 2,
        Administrador = 3
    }

    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string Password { get; set; }

        [Required]
        public RolUsuario Rol { get; set; }

        // NUEVO: Relación con departamento
        public int? DepartamentoId { get; set; }
        public virtual Departamento Departamento { get; set; }

        // NUEVO: Propiedad para saber si es jefe de departamento
        [NotMapped]
        public bool EsJefeDepartamento
        {
            get
            {
                return Departamento != null && Departamento.JefeId == Id;
            }
        }

        public virtual ICollection<Reserva> Reservas { get; set; }

        public Usuario()
        {
            Reservas = new HashSet<Reserva>();
        }
    }
}