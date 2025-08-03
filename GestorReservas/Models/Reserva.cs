using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GestorReservas.Models
{
    /// <summary>
    /// Enumeración que define los posibles estados de una reserva
    /// Representa el flujo de trabajo (workflow) del sistema de aprobación
    /// Utilizado para controlar el ciclo de vida de las reservas
    /// </summary>
    public enum EstadoReserva
    {
        /// <summary>
        /// Estado inicial de toda reserva recién creada
        /// Indica que la reserva está esperando revisión por parte de coordinadores
        /// Permite al usuario ver que su solicitud fue recibida
        /// Transiciones posibles: → Aprobada, → Rechazada
        /// </summary>
        Pendiente,

        /// <summary>
        /// Estado cuando la reserva ha sido aprobada por un coordinador
        /// Indica que el espacio está confirmado para el usuario
        /// La reserva se considera activa y válida
        /// Transiciones posibles: → Rechazada (cancelación posterior)
        /// </summary>
        Aprobada,

        /// <summary>
        /// Estado cuando la reserva ha sido rechazada por un coordinador
        /// Indica que la solicitud no fue aprobada por algún motivo
        /// Estado final - no hay más transiciones posibles
        /// Motivos comunes: conflicto de horarios, espacio no disponible
        /// </summary>
        Rechazada
    }

    /// <summary>
    /// Entidad principal que representa una reserva de espacio
    /// Contiene toda la información necesaria para gestionar reservas
    /// Incluye relaciones con Usuario y Espacio para contexto completo
    /// NÚCLEO del sistema de gestión de reservas
    /// </summary>
    public class Reserva
    {
        #region Propiedades Básicas

        /// <summary>
        /// Identificador único de la reserva
        /// Clave primaria generada automáticamente por Entity Framework
        /// Utilizada para operaciones CRUD y referencias entre tablas
        /// </summary>
        [Key]
        public int Id { get; set; }

        #endregion

        #region Relación con Usuario

        /// <summary>
        /// ID del usuario que realizó la reserva
        /// Foreign Key obligatoria hacia la tabla Usuarios
        /// Establece la relación de propiedad de la reserva
        /// Utilizada para validaciones de autorización
        /// </summary>
        [Required(ErrorMessage = "El ID del usuario es obligatorio")]
        public int UsuarioId { get; set; }

        /// <summary>
        /// Propiedad de navegación hacia el usuario que hizo la reserva
        /// Configurada con ForeignKey para establecer la relación explícitamente
        /// Permite acceder a información completa del usuario sin consultas adicionales
        /// Utilizada en respuestas para mostrar datos del solicitante
        /// </summary>
        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }

        #endregion

        #region Relación con Espacio

        /// <summary>
        /// ID del espacio que se está reservando
        /// Foreign Key obligatoria hacia la tabla Espacios
        /// Establece qué recurso físico está siendo solicitado
        /// Utilizada para validaciones de disponibilidad
        /// </summary>
        [Required(ErrorMessage = "El ID del espacio es obligatorio")]
        public int EspacioId { get; set; }

        /// <summary>
        /// Propiedad de navegación hacia el espacio reservado
        /// Configurada con ForeignKey para establecer la relación explícitamente
        /// Permite acceder a información completa del espacio sin consultas adicionales
        /// Utilizada en respuestas para mostrar detalles del recurso reservado
        /// </summary>
        [ForeignKey("EspacioId")]
        public virtual Espacio Espacio { get; set; }

        #endregion

        #region Información Temporal

        /// <summary>
        /// Fecha para la cual se solicita la reserva
        /// Campo obligatorio que especifica el día de uso del espacio
        /// Debe ser una fecha futura (validado en controller)
        /// Utilizada para consultas de disponibilidad y conflictos
        /// </summary>
        [Required(ErrorMessage = "La fecha de la reserva es obligatoria")]
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Horario específico dentro del día reservado
        /// Campo obligatorio en formato "HH:mm-HH:mm"
        /// Ejemplos: "08:00-10:00", "14:30-16:00"
        /// Utilizado para detectar solapamientos entre reservas
        /// </summary>
        [Required(ErrorMessage = "El horario es obligatorio")]
        [StringLength(11, ErrorMessage = "El formato de horario debe ser HH:mm-HH:mm")]
        public string Horario { get; set; }

        #endregion

        #region Estado y Gestión

        /// <summary>
        /// Estado actual de la reserva según el enum EstadoReserva
        /// Campo obligatorio que indica el estado en el workflow
        /// Valores: Pendiente (inicial), Aprobada, Rechazada
        /// Utilizado para filtros, validaciones y lógica de negocio
        /// </summary>
        [Required(ErrorMessage = "El estado de la reserva es obligatorio")]
        public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente; // Estado inicial por defecto

        #endregion

        #region Información Adicional

        /// <summary>
        /// Descripción opcional de la reserva
        /// Propósito del uso, notas especiales, requerimientos adicionales
        /// Ejemplos: "Clase de Programación Avanzada", "Reunión de departamento"
        /// Ayuda a coordinadores a entender el contexto de la solicitud
        /// </summary>
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string Descripcion { get; set; }

        #endregion

        #region Propiedades de Auditoría (Implícitas - Se pueden agregar)

        /*
        /// <summary>
        /// Fecha y hora de creación de la reserva
        /// Timestamp automático para auditoría y ordenamiento
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha y hora de última modificación
        /// Actualizada automáticamente en cada cambio de estado
        /// </summary>
        public DateTime? FechaModificacion { get; set; }

        /// <summary>
        /// ID del usuario que aprobó/rechazó la reserva
        /// Útil para trazabilidad de decisiones administrativas
        /// </summary>
        public int? AprobadoPorId { get; set; }

        /// <summary>
        /// Motivo de rechazo en caso de reserva rechazada
        /// Información para el usuario sobre por qué no fue aprobada
        /// </summary>
        public string MotivoRechazo { get; set; }
        */

        #endregion

        #region Constructor (Implícito)

        /*
        /// <summary>
        /// Constructor por defecto de la reserva
        /// Establece valores iniciales apropiados
        /// </summary>
        public Reserva()
        {
            Estado = EstadoReserva.Pendiente;
            FechaCreacion = DateTime.Now;
        }
        */

        #endregion

        #region Métodos Auxiliares (Implícitos - Se pueden agregar)

        /*
        /// <summary>
        /// Verifica si la reserva está activa (aprobada y en fecha futura)
        /// </summary>
        /// <returns>True si la reserva está activa</returns>
        public bool EstaActiva()
        {
            return Estado == EstadoReserva.Aprobada && Fecha >= DateTime.Today;
        }

        /// <summary>
        /// Obtiene las horas de inicio y fin del horario
        /// </summary>
        /// <returns>Tupla con hora de inicio y fin</returns>
        public (TimeSpan inicio, TimeSpan fin) ObtenerHorarios()
        {
            var partes = Horario.Split('-');
            return (TimeSpan.Parse(partes[0]), TimeSpan.Parse(partes[1]));
        }

        /// <summary>
        /// Verifica si esta reserva se solapa con otra en la misma fecha
        /// </summary>
        /// <param name="otraReserva">Reserva a comparar</param>
        /// <returns>True si hay solapamiento</returns>
        public bool SeSolapaCon(Reserva otraReserva)
        {
            if (Fecha.Date != otraReserva.Fecha.Date || EspacioId != otraReserva.EspacioId)
                return false;

            var (inicio1, fin1) = ObtenerHorarios();
            var (inicio2, fin2) = otraReserva.ObtenerHorarios();

            return !(fin1 <= inicio2 || inicio1 >= fin2);
        }

        /// <summary>
        /// Representación string de la reserva
        /// </summary>
        /// <returns>String descriptivo de la reserva</returns>
        public override string ToString()
        {
            return $"Reserva {Id}: {Espacio?.Nombre} - {Fecha:dd/MM/yyyy} {Horario} ({Estado})";
        }
        */

        #endregion
    }
}