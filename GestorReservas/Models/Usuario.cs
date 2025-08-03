using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GestorReservas.Models
{
    // Enumeración que define los niveles de acceso y permisos en el sistema
    // Los valores numéricos permiten comparaciones jerárquicas: Administrador > Coordinador > Profesor
    // Cada rol tiene permisos específicos para operaciones del sistema
    public enum RolUsuario
    {
        // Rol básico con permisos limitados
        // Puede crear reservas y ver sus propias reservas
        // No puede aprobar/rechazar reservas de otros usuarios
        Profesor = 1,

        // Rol intermedio con permisos de gestión departamental
        // Puede aprobar/rechazar reservas dentro de su área
        // Tiene acceso a reportes y estadísticas limitadas
        Coordinador = 2,

        // Rol con máximos permisos en el sistema
        // Puede gestionar usuarios, espacios y todas las reservas
        // Acceso completo a configuración y reportes del sistema
        Administrador = 3
    }

    // Entidad principal que representa un usuario del sistema
    // Contiene información personal, credenciales y relaciones con otras entidades
    // Implementa sistema de roles para control de acceso
    public class Usuario
    {
        #region Propiedades Básicas

        // Clave primaria única generada automáticamente por Entity Framework
        // Utilizada para relaciones con otras tablas (Reserva, Departamento)
        // Referenciada en tokens JWT para identificación del usuario
        [Key]
        public int Id { get; set; }

        // Nombre completo del usuario para identificación personal
        // Campo obligatorio con máximo 100 caracteres
        // Usado en interfaces de usuario y reportes
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        // Email único para login y comunicaciones del sistema
        // Campo obligatorio con validación de formato EmailAddress
        // Máximo 100 caracteres, normalizado a minúsculas en controladores
        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        // Contraseña hasheada con SHA256 para seguridad
        // Campo obligatorio con máximo 255 caracteres para el hash
        // NUNCA se almacena en texto plano por seguridad
        [Required]
        [StringLength(255)]
        public string Password { get; set; }

        // Rol del usuario que determina permisos y funcionalidades disponibles
        // Campo obligatorio basado en enum RolUsuario
        // Utilizado en toda la lógica de autorización del sistema
        [Required]
        public RolUsuario Rol { get; set; }

        #endregion

        #region Relación con Departamento - NUEVA FUNCIONALIDAD

        // ID del departamento al cual pertenece el usuario (opcional)
        // Foreign Key hacia tabla Departamentos
        // null significa que el usuario no tiene departamento asignado
        // Solo administradores pueden modificar esta asignación
        public int? DepartamentoId { get; set; }

        // Propiedad de navegación hacia el departamento del usuario
        // Configurada como virtual para Lazy Loading de Entity Framework
        // Permite acceder a información completa del departamento sin consultas adicionales
        // null si el usuario no pertenece a ningún departamento
        public virtual Departamento Departamento { get; set; }

        // Propiedad calculada que indica si el usuario es jefe de su departamento
        // Marcada con [NotMapped] para que no se cree columna en BD
        // Se calcula comparando el ID del usuario con el JefeId del departamento
        // Utilizada para determinar permisos adicionales y elementos de UI
        [NotMapped]
        public bool EsJefeDepartamento
        {
            get
            {
                // Solo puede ser jefe si tiene departamento asignado Y es el jefe designado
                return Departamento != null && Departamento.JefeId == Id;
            }
        }

        #endregion

        #region Relación con Reservas

        // Colección de todas las reservas realizadas por este usuario
        // Relación uno-a-muchos configurada como virtual para Lazy Loading
        // Permite consultar historial completo de reservas del usuario
        // HashSet garantiza unicidad y mejor rendimiento que List
        public virtual ICollection<Reserva> Reservas { get; set; }

        #endregion

        #region Constructor

        // Constructor por defecto que inicializa colecciones
        // Previene errores de referencia nula al agregar reservas
        // HashSet se usa por mejor rendimiento en operaciones de búsqueda y unicidad
        public Usuario()
        {
            Reservas = new HashSet<Reserva>();
        }

        #endregion

        #region Métodos Auxiliares Implícitos (Se pueden agregar)

        /*
        // Verifica si el usuario tiene permisos de administrador
        public bool EsAdministrador => Rol == RolUsuario.Administrador;

        // Verifica si el usuario tiene permisos de coordinador o superior
        public bool EsCoordinadorOMayor => Rol >= RolUsuario.Coordinador;

        // Obtiene el nombre del rol como string
        public string NombreRol => Rol.ToString();

        // Verifica si el usuario puede aprobar reservas
        public bool PuedeAprobarReservas => Rol >= RolUsuario.Coordinador;

        // Cuenta las reservas activas del usuario
        public int ReservasActivas => Reservas?.Count(r => r.Estado == EstadoReserva.Aprobada && r.Fecha >= DateTime.Today) ?? 0;

        // Representación string del usuario
        public override string ToString() => $"{Nombre} ({Email}) - {Rol}";
        */

        #endregion
    }
}