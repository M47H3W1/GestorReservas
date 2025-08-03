using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GestorReservas.Controllers;
using System.ComponentModel.DataAnnotations;

namespace GestorReservas.Models.DTOs
{
    /// <summary>
    /// DTO para crear nuevas reservas
    /// Contiene todos los campos necesarios para crear una reserva
    /// Utilizado en el endpoint POST del ReservaController
    /// </summary>
    public class ReservaCreateDto
    {
        /// <summary>
        /// ID del usuario que realiza la reserva
        /// Debe corresponder a un usuario existente en la base de datos
        /// Se valida contra la tabla Usuarios en el controller
        /// </summary>
        [Required(ErrorMessage = "El ID del usuario es obligatorio")]
        public int UsuarioId { get; set; }

        /// <summary>
        /// ID del espacio que se desea reservar
        /// Debe corresponder a un espacio disponible en la base de datos
        /// Se valida existencia y disponibilidad en el controller
        /// </summary>
        [Required(ErrorMessage = "El ID del espacio es obligatorio")]
        public int EspacioId { get; set; }

        /// <summary>
        /// Fecha para la cual se solicita la reserva
        /// Debe ser una fecha futura (validado en controller)
        /// Formato: yyyy-MM-dd desde el frontend
        /// </summary>
        [Required(ErrorMessage = "La fecha es obligatoria")]
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Horario de la reserva en formato "HH:mm-HH:mm"
        /// Ejemplo: "08:00-10:00", "14:30-16:00"
        /// Validado para formato correcto y solapamientos en controller
        /// </summary>
        [Required(ErrorMessage = "El horario es obligatorio")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]-([0-1]?[0-9]|2[0-3]):[0-5][0-9]$",
            ErrorMessage = "El formato del horario debe ser HH:mm-HH:mm")]
        public string Horario { get; set; }

        /// <summary>
        /// Descripción opcional de la reserva
        /// Propósito de la reserva, notas especiales, etc.
        /// Ejemplo: "Clase de Matemáticas Avanzadas", "Reunión de departamento"
        /// </summary>
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string Descripcion { get; set; }
    }

    /// <summary>
    /// DTO para actualizar reservas existentes
    /// Solo incluye campos modificables después de la creación
    /// Utilizado en el endpoint PUT del ReservaController
    /// </summary>
    public class ReservaUpdateDto
    {
        /// <summary>
        /// Nueva fecha para la reserva (OPCIONAL)
        /// Solo se actualiza si se proporciona un valor
        /// Debe ser fecha futura y validada contra conflictos
        /// </summary>
        public DateTime? Fecha { get; set; }

        /// <summary>
        /// Nuevo horario para la reserva (OPCIONAL)
        /// Validado para formato correcto y solapamientos
        /// Solo se actualiza si se proporciona un valor
        /// </summary>
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]-([0-1]?[0-9]|2[0-3]):[0-5][0-9]$",
            ErrorMessage = "El formato del horario debe ser HH:mm-HH:mm")]
        public string Horario { get; set; }

        /// <summary>
        /// Nueva descripción para la reserva (OPCIONAL)
        /// Permite modificar el propósito o agregar notas
        /// null = mantiene descripción actual, string vacío = elimina descripción
        /// </summary>
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string Descripcion { get; set; }
    }

    /// <summary>
    /// DTO de respuesta completa para reservas
    /// Incluye todos los datos de la reserva más información relacionada
    /// Utilizado en respuestas GET, POST y PUT del ReservaController
    /// </summary>
    public class ReservaResponseDto
    {
        /// <summary>
        /// Identificador único de la reserva
        /// Generado automáticamente por Entity Framework
        /// Utilizado para operaciones UPDATE, DELETE y consultas específicas
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID del usuario que realizó la reserva
        /// Foreign Key hacia la tabla Usuarios
        /// Mantiene referencia para consultas y validaciones
        /// </summary>
        public int UsuarioId { get; set; }

        /// <summary>
        /// Información completa del usuario que reservó
        /// Objeto anidado con datos del usuario (nombre, email, rol, departamento)
        /// Evita consultas adicionales del frontend para mostrar datos
        /// </summary>
        public UsuarioResponseDto Usuario { get; set; }

        /// <summary>
        /// ID del espacio reservado
        /// Foreign Key hacia la tabla Espacios
        /// Mantiene referencia para consultas y validaciones
        /// </summary>
        public int EspacioId { get; set; }

        /// <summary>
        /// Información completa del espacio reservado
        /// Objeto anidado con datos del espacio (nombre, tipo, capacidad, ubicación)
        /// Proporciona contexto completo sin consultas adicionales
        /// </summary>
        public EspacioResponseDto Espacio { get; set; }

        /// <summary>
        /// Fecha de la reserva
        /// Formato DateTime para fácil manipulación en frontend
        /// Representa el día para el cual está programada la reserva
        /// </summary>
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Horario de la reserva en formato "HH:mm-HH:mm"
        /// Rango de tiempo específico durante el día
        /// Utilizado para validar conflictos y mostrar información
        /// </summary>
        public string Horario { get; set; }

        /// <summary>
        /// Estado actual de la reserva convertido a string
        /// Valores posibles: "Pendiente", "Aprobada", "Rechazada", "Cancelada"
        /// Conversión del enum EstadoReserva para facilitar consumo
        /// </summary>
        public string Estado { get; set; }

        /// <summary>
        /// Descripción de la reserva
        /// Propósito, notas especiales o información adicional
        /// Puede ser null si no se proporcionó descripción
        /// </summary>
        public string Descripcion { get; set; }

        /// <summary>
        /// Fecha y hora de creación de la reserva
        /// Timestamp automático generado al crear el registro
        /// Útil para auditoría y ordenamiento cronológico
        /// </summary>
        public DateTime FechaCreacion { get; set; }
    }

    /// <summary>
    /// DTO para cambio de estado de reservas
    /// Utilizado por coordinadores y administradores
    /// Permite aprobar, rechazar o cancelar reservas
    /// </summary>
    public class CambiarEstadoReservaDto
    {
        /// <summary>
        /// Nuevo estado para la reserva
        /// Valores válidos según enum EstadoReserva
        /// Solo ciertos roles pueden realizar ciertos cambios
        /// </summary>
        [Required(ErrorMessage = "El nuevo estado es obligatorio")]
        public EstadoReserva NuevoEstado { get; set; }

        /// <summary>
        /// Motivo del cambio de estado (OPCIONAL)
        /// Útil para rechazos o cancelaciones
        /// Ejemplo: "Conflicto de horarios", "Mantenimiento del espacio"
        /// </summary>
        [StringLength(300, ErrorMessage = "El motivo no puede exceder 300 caracteres")]
        public string Motivo { get; set; }
    }

    /// <summary>
    /// DTO para consultas de reservas con filtros
    /// Permite búsquedas avanzadas con múltiples criterios
    /// Utilizado en endpoints de búsqueda y reportes
    /// </summary>
    public class ReservaFiltroDto
    {
        /// <summary>
        /// Filtrar por ID de usuario específico (OPCIONAL)
        /// Útil para ver reservas de un usuario particular
        /// Si es null, incluye reservas de todos los usuarios
        /// </summary>
        public int? UsuarioId { get; set; }

        /// <summary>
        /// Filtrar por ID de espacio específico (OPCIONAL)
        /// Útil para ver ocupación de un espacio particular
        /// Si es null, incluye reservas de todos los espacios
        /// </summary>
        public int? EspacioId { get; set; }

        /// <summary>
        /// Fecha de inicio del rango de búsqueda (OPCIONAL)
        /// Solo incluye reservas desde esta fecha en adelante
        /// Si es null, no aplica filtro de fecha inicial
        /// </summary>
        public DateTime? FechaDesde { get; set; }

        /// <summary>
        /// Fecha de fin del rango de búsqueda (OPCIONAL)
        /// Solo incluye reservas hasta esta fecha
        /// Si es null, no aplica filtro de fecha final
        /// </summary>
        public DateTime? FechaHasta { get; set; }

        /// <summary>
        /// Filtrar por estado específico (OPCIONAL)
        /// Permite ver solo reservas pendientes, aprobadas, etc.
        /// Si es null, incluye reservas de todos los estados
        /// </summary>
        public EstadoReserva? Estado { get; set; }

        /// <summary>
        /// Número de página para paginación (OPCIONAL)
        /// Por defecto página 1 si no se especifica
        /// Utilizado junto con TamañoPagina para limitar resultados
        /// </summary>
        public int? Pagina { get; set; } = 1;

        /// <summary>
        /// Cantidad de registros por página (OPCIONAL)
        /// Por defecto 20 registros si no se especifica
        /// Máximo recomendado: 100 para evitar sobrecarga
        /// </summary>
        public int? TamañoPagina { get; set; } = 20;
    }
}