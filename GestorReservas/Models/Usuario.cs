﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;



namespace GestorReservas.Models
{
    public enum RolUsuario
    {
        Profesor,
        Administrador,
        Coordinador
    }

    public class Usuario
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Nombre { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public RolUsuario Rol { get; set; }
    }
}