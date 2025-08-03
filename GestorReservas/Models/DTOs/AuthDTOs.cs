using GestorReservas.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GestorReservas.Models.DTOs
{
    /// <summary>
    /// DTO para solicitudes de login (versión alternativa)
    /// Contiene las credenciales del usuario para autenticación
    /// Incluye validaciones de formato y campos obligatorios
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// Email del usuario para autenticación
        /// Debe tener formato de email válido y es obligatorio
        /// </summary>
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; }

        /// <summary>
        /// Contraseña del usuario en texto plano
        /// Será hasheada antes de comparar con la base de datos
        /// Campo obligatorio para la autenticación
        /// </summary>
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Password { get; set; }
    }

    /// <summary>
    /// DTO principal para solicitudes de login
    /// Estructura idéntica a LoginDto (considera eliminar duplicación)
    /// Utilizado por el AuthController en el endpoint de login
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Email del usuario que intenta iniciar sesión
        /// Validado con DataAnnotations para formato correcto
        /// Se normaliza a minúsculas en el controller
        /// </summary>
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; }

        /// <summary>
        /// Contraseña proporcionada por el usuario
        /// Se compara con el hash SHA256 almacenado en BD
        /// No se almacena en texto plano por seguridad
        /// </summary>
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Password { get; set; }
    }

    /// <summary>
    /// DTO para cambio de contraseña de usuarios autenticados
    /// Requiere contraseña actual para verificación de identidad
    /// Incluye confirmación para prevenir errores de tipeo
    /// </summary>
    public class CambiarPasswordDto
    {
        /// <summary>
        /// Contraseña actual del usuario
        /// Se verifica contra el hash almacenado antes de permitir el cambio
        /// Medida de seguridad para confirmar identidad
        /// </summary>
        [Required(ErrorMessage = "La contraseña actual es obligatoria")]
        public string PasswordActual { get; set; }

        /// <summary>
        /// Nueva contraseña que el usuario desea establecer
        /// Debe cumplir requisitos mínimos de seguridad (6+ caracteres)
        /// Será hasheada con SHA256 antes de almacenar
        /// </summary>
        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres")]
        public string PasswordNuevo { get; set; }

        /// <summary>
        /// Confirmación de la nueva contraseña
        /// Debe coincidir exactamente con PasswordNuevo
        /// Previene errores de escritura al cambiar contraseña
        /// </summary>
        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria")]
        [Compare("PasswordNuevo", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarPassword { get; set; }
    }

    /// <summary>
    /// DTO de respuesta para login exitoso
    /// Contiene toda la información que el cliente necesita después del login
    /// Incluye token JWT y datos del usuario autenticado
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// Mensaje descriptivo del resultado del login
        /// Generalmente "Login exitoso" o mensaje de error
        /// Útil para mostrar feedback al usuario
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Información completa del usuario autenticado
        /// Incluye datos personales, rol y departamento
        /// Utiliza UsuarioResponseDto para estructura consistente
        /// </summary>
        public UsuarioResponseDto Usuario { get; set; }

        /// <summary>
        /// Token JWT generado para el usuario
        /// Contiene claims con ID, email y rol del usuario
        /// Debe incluirse en header Authorization de requests posteriores
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Tiempo de expiración del token en segundos
        /// Permite al cliente calcular cuando renovar el token
        /// Configurado desde AppConfig.JwtExpirationDays
        /// </summary>
        public int ExpiresIn { get; set; }
    }
}