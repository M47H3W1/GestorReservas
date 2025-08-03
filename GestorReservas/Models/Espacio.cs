using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GestorReservas.Models
{
    /// <summary>
    /// Enumeración que define los tipos de espacios disponibles para reserva
    /// Clasifica los espacios según su propósito y características principales
    /// Utilizado para filtrar búsquedas y aplicar reglas específicas por tipo
    /// </summary>
    public enum TipoEspacio
    {
        /// <summary>
        /// Aula tradicional para clases magistrales
        /// Equipamiento básico: pizarra, proyector, pupitres
        /// Capacidad típica: 20-50 estudiantes
        /// Uso común: clases teóricas, conferencias
        /// </summary>
        Aula,

        /// <summary>
        /// Laboratorio para prácticas y experimentos
        /// Equipamiento especializado según área (computadores, instrumentos)
        /// Capacidad típica: 15-30 estudiantes
        /// Uso común: prácticas de programación, electrónica, automatización
        /// </summary>
        Laboratorio,

        /// <summary>
        /// Auditorio para eventos masivos
        /// Equipamiento avanzado: sistema de sonido, iluminación, escenario
        /// Capacidad típica: 100+ asistentes
        /// Uso común: conferencias, graduaciones, eventos institucionales
        /// </summary>
        Auditorio
    }

    /// <summary>
    /// Entidad que representa un espacio físico disponible para reserva
    /// Incluye aulas, laboratorios, auditorios y otros espacios académicos
    /// Contiene información básica para identificación y gestión de reservas
    /// </summary>
    public class Espacio
    {
        #region Propiedades Básicas

        /// <summary>
        /// Identificador único del espacio
        /// Clave primaria generada automáticamente por Entity Framework
        /// Utilizada para relaciones con reservas y consultas específicas
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre descriptivo del espacio
        /// Campo obligatorio para identificación
        /// Ejemplos: "Aula 101", "Laboratorio de Electrónica", "Auditorio Principal"
        /// Debe ser único y descriptivo para fácil localización
        /// </summary>
        [Required(ErrorMessage = "El nombre del espacio es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; }

        /// <summary>
        /// Tipo de espacio según el enum TipoEspacio
        /// Campo obligatorio que determina la categoría del espacio
        /// Utilizado para filtros, búsquedas y aplicación de reglas específicas
        /// Valores posibles: Aula, Laboratorio, Auditorio
        /// </summary>
        [Required(ErrorMessage = "El tipo de espacio es obligatorio")]
        public TipoEspacio Tipo { get; set; }

        /// <summary>
        /// Ubicación física del espacio dentro de las instalaciones
        /// Campo obligatorio para que los usuarios puedan encontrar el espacio
        /// Ejemplos: "Edificio A - Piso 2", "Bloque Norte - Sala 205"
        /// Información crítica para la logística de reservas
        /// </summary>
        [Required(ErrorMessage = "La ubicación es obligatoria")]
        [StringLength(200, ErrorMessage = "La ubicación no puede exceder 200 caracteres")]
        public string Ubicacion { get; set; }

        /// <summary>
        /// Capacidad máxima de personas que puede acomodar el espacio
        /// Utilizado para validar reservas según número de asistentes esperados
        /// Debe ser un número positivo mayor a 0
        /// Información importante para planificación de eventos
        /// </summary>
        [Required(ErrorMessage = "La capacidad es obligatoria")]
        [Range(1, 1000, ErrorMessage = "La capacidad debe estar entre 1 y 1000 personas")]
        public int Capacidad { get; set; }

        /// <summary>
        /// Descripción detallada del espacio y su equipamiento (OPCIONAL)
        /// Incluye características especiales, equipos disponibles, restricciones
        /// Ejemplos: "Proyector 4K, aire acondicionado, 30 computadoras Dell"
        /// Ayuda a los usuarios a elegir el espacio más adecuado
        /// </summary>
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string Descripcion { get; set; }

        /// <summary>
        /// Estado de disponibilidad del espacio para reservas
        /// true = Disponible para nuevas reservas
        /// false = No disponible (mantenimiento, fuera de servicio, etc.)
        /// Los espacios no disponibles no aparecen en búsquedas de reserva
        /// </summary>
        public bool Disponible { get; set; } = true; // Por defecto disponible

        #endregion

        #region Propiedades de Auditoría (Implícitas - Se pueden agregar)

        /*
        /// <summary>
        /// Fecha y hora de creación del espacio
        /// Timestamp automático para auditoría
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha y hora de última modificación
        /// Actualizado automáticamente en cada cambio
        /// </summary>
        public DateTime? FechaModificacion { get; set; }
        */

        #endregion

        #region Relaciones con Otras Entidades (Implícitas)

        /*
        /// <summary>
        /// Colección de reservas asociadas a este espacio
        /// Relación uno-a-muchos con Reserva
        /// Permite consultar historial de uso del espacio
        /// </summary>
        public virtual ICollection<Reserva> Reservas { get; set; }
        */

        #endregion

        #region Constructor (Implícito)

        /*
        /// <summary>
        /// Constructor por defecto del espacio
        /// Inicializa valores por defecto y colecciones
        /// </summary>
        public Espacio()
        {
            Disponible = true;
            FechaCreacion = DateTime.Now;
            Reservas = new HashSet<Reserva>();
        }
        */

        #endregion

        #region Métodos Auxiliares (Implícitos - Se pueden agregar)

        /*
        /// <summary>
        /// Verifica si el espacio está disponible para una fecha específica
        /// </summary>
        /// <param name="fecha">Fecha a consultar</param>
        /// <returns>True si está disponible</returns>
        public bool EstaDisponibleEn(DateTime fecha)
        {
            return Disponible; // Lógica más compleja considerando reservas
        }

        /// <summary>
        /// Obtiene una representación string del espacio
        /// </summary>
        /// <returns>String con nombre y ubicación</returns>
        public override string ToString()
        {
            return $"{Nombre} ({Ubicacion})";
        }

        /// <summary>
        /// Calcula el porcentaje de ocupación del espacio
        /// </summary>
        /// <returns>Porcentaje de uso del espacio</returns>
        public double CalcularPorcentajeOcupacion()
        {
            // Lógica para calcular ocupación basada en reservas
            return 0.0;
        }
        */

        #endregion
    }
}