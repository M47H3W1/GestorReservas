using GestorReservas.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GestorReservas.Models.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Password { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Password { get; set; }
    }

    public class CambiarPasswordDto 
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria")]
        public string PasswordActual { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres")]
        public string PasswordNuevo { get; set; }

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria")]
        [Compare("PasswordNuevo", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarPassword { get; set; }
    }

    public class LoginResponse
    {
        public string Message { get; set; }
        public UsuarioResponseDto Usuario { get; set; }
        public string Token { get; set; }
        public int ExpiresIn { get; set; }
    }
}