namespace GestorReservas.Models
{
    /// <summary>
    /// Clase que representa la información del usuario extraída del token JWT
    /// Utilizada para validación de autenticación y autorización en controladores
    /// Contiene los datos básicos necesarios para identificar al usuario autenticado
    /// REEMPLAZA el uso de 'dynamic' para proporcionar tipado fuerte y mejor rendimiento
    /// </summary>
    public class JwtUserInfo
    {
        /// <summary>
        /// Identificador único del usuario autenticado
        /// Extraído del claim "id" del token JWT
        /// Utilizado para consultas de base de datos y validaciones de autorización
        /// Permite identificar qué usuario está realizando la operación
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Email del usuario autenticado
        /// Extraído del claim "email" del token JWT
        /// Utilizado para logging, auditoría y comunicaciones
        /// Información adicional para identificación del usuario en logs
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Rol del usuario en el sistema
        /// Extraído del claim "role" del token JWT
        /// Valores posibles: "Profesor", "Coordinador", "Administrador"
        /// CRÍTICO para control de acceso y autorización de funcionalidades
        /// Determina qué operaciones puede realizar el usuario
        /// </summary>
        public string Role { get; set; }
    }
}