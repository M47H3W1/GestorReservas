using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GestorReservas.Models
{
    /// <summary>
    /// Enumeración que define los tipos de departamentos académicos
    /// Basado en la estructura organizacional específica de la institución
    /// Cada departamento tiene un ID específico que corresponde a su especialización
    /// </summary>
    public enum TipoDepartamento
    {
        /// <summary>
        /// Departamento de Automatización y Control Industrial
        /// Especializado en sistemas automatizados, robótica y control de procesos
        /// Código institucional: DACI
        /// </summary>
        DACI = 1,

        /// <summary>
        /// Departamento de Electrónica, Telecomunicaciones y Redes de Información
        /// Especializado en sistemas electrónicos, comunicaciones y redes
        /// Código institucional: DETRI
        /// </summary>
        DETRI = 2,

        /// <summary>
        /// Departamento de Energía Eléctrica
        /// Especializado en sistemas de potencia, energías renovables y distribución eléctrica
        /// Código institucional: DEE
        /// </summary>
        DEE = 3
    }

    /// <summary>
    /// Entidad que representa un departamento académico de la institución
    /// Organiza a los profesores por áreas de especialización
    /// Permite asignar jefes de departamento y gestionar jerarquías académicas
    /// NUEVA FUNCIONALIDAD: Gestión organizacional del sistema
    /// </summary>
    public class Departamento
    {
        #region Propiedades Básicas

        /// <summary>
        /// Identificador único del departamento
        /// Clave primaria generada automáticamente por Entity Framework
        /// Utilizada para relaciones con usuarios y consultas específicas
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre oficial completo del departamento
        /// Máximo 100 caracteres, campo obligatorio
        /// Ejemplo: "Departamento de Automatización y Control Industrial"
        /// Utilizado para identificación oficial y documentos
        /// </summary>
        [Required(ErrorMessage = "El nombre del departamento es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; }

        /// <summary>
        /// Código único abreviado del departamento
        /// Máximo 10 caracteres, campo obligatorio
        /// Valores esperados: "DACI", "DETRI", "DEE"
        /// Utilizado para referencias rápidas y sistemas internos
        /// </summary>
        [Required(ErrorMessage = "El código del departamento es obligatorio")]
        [StringLength(10, ErrorMessage = "El código no puede exceder 10 caracteres")]
        public string Codigo { get; set; }

        /// <summary>
        /// Tipo de departamento según el enum TipoDepartamento
        /// Campo obligatorio que clasifica el departamento
        /// Determina la especialización y área académica
        /// Utilizado para agrupaciones y reportes institucionales
        /// </summary>
        [Required(ErrorMessage = "El tipo de departamento es obligatorio")]
        public TipoDepartamento Tipo { get; set; }

        /// <summary>
        /// Descripción detallada del departamento (OPCIONAL)
        /// Máximo 500 caracteres
        /// Incluye misión, especialidades, recursos disponibles, etc.
        /// Información adicional para contexto académico
        /// </summary>
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string Descripcion { get; set; }

        #endregion

        #region Relaciones - Jerarquía Departamental

        /// <summary>
        /// ID del usuario que es jefe/coordinador del departamento (OPCIONAL)
        /// Foreign Key hacia la tabla Usuarios
        /// null = departamento sin jefe asignado
        /// Un usuario puede ser jefe de máximo un departamento
        /// </summary>
        public int? JefeId { get; set; }

        /// <summary>
        /// Propiedad de navegación hacia el usuario jefe del departamento
        /// Relación uno-a-uno opcional con Usuario
        /// Marcada como virtual para Lazy Loading de Entity Framework
        /// Permite acceder a información completa del jefe sin consultas adicionales
        /// </summary>
        public virtual Usuario Jefe { get; set; }

        #endregion

        #region Relaciones - Profesores del Departamento

        /// <summary>
        /// Colección de profesores que pertenecen a este departamento
        /// Relación uno-a-muchos con Usuario
        /// Marcada como virtual para Lazy Loading de Entity Framework
        /// Un departamento puede tener múltiples profesores
        /// Un profesor puede pertenecer a un solo departamento
        /// </summary>
        public virtual ICollection<Usuario> Profesores { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor por defecto del departamento
        /// Inicializa la colección de profesores como HashSet vacío
        /// Previene errores de referencia nula al agregar profesores
        /// HashSet garantiza unicidad y mejor rendimiento que List
        /// </summary>
        public Departamento()
        {
            // Inicializar colección para evitar NullReferenceException
            // HashSet evita duplicados y ofrece mejor rendimiento para búsquedas
            Profesores = new HashSet<Usuario>();
        }

        #endregion

        #region Métodos Auxiliares (Implícitos)

        /*
        // MÉTODOS QUE SE PODRÍAN AGREGAR PARA FUNCIONALIDAD ADICIONAL:

        /// <summary>
        /// Obtiene el número total de profesores en el departamento
        /// </summary>
        public int CantidadProfesores => Profesores?.Count ?? 0;

        /// <summary>
        /// Verifica si el departamento tiene un jefe asignado
        /// </summary>
        public bool TieneJefe => JefeId.HasValue;

        /// <summary>
        /// Obtiene una representación string del departamento
        /// </summary>
        public override string ToString() => $"{Codigo} - {Nombre}";
        */

        #endregion
    }
}