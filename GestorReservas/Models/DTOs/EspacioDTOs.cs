using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GestorReservas.Models.DTOs
{
    /// <summary>
    /// DTO para crear y actualizar espacios
    /// Contiene todos los campos modificables de un espacio
    /// Utilizado en operaciones POST y PUT del EspacioController
    /// </summary>
    public class EspacioDto
    {
        /// <summary>
        /// Nombre descriptivo del espacio
        /// Ejemplo: "Aula 101", "Laboratorio de Química", "Auditorio Principal"
        /// Campo obligatorio para identificación del espacio
        /// </summary>
        [Required(ErrorMessage = "El nombre del espacio es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; }

        /// <summary>
        /// Tipo de espacio según el enum TipoEspacio
        /// Valores posibles: Aula, Laboratorio, Auditorio, SalaReuniones, etc.
        /// Determina las características y uso del espacio
        /// </summary>
        [Required(ErrorMessage = "El tipo de espacio es obligatorio")]
        public TipoEspacio Tipo { get; set; }

        /// <summary>
        /// Capacidad máxima de personas en el espacio
        /// Debe ser un número positivo mayor a 0
        /// Utilizado para validar reservas según número de asistentes
        /// </summary>
        [Required(ErrorMessage = "La capacidad es obligatoria")]
        [Range(1, 1000, ErrorMessage = "La capacidad debe estar entre 1 y 1000 personas")]
        public int Capacidad { get; set; }

        /// <summary>
        /// Ubicación física del espacio
        /// Ejemplo: "Edificio A - Piso 2", "Campus Norte - Bloque C"
        /// Ayuda a los usuarios a encontrar el espacio reservado
        /// </summary>
        [Required(ErrorMessage = "La ubicación es obligatoria")]
        [StringLength(200, ErrorMessage = "La ubicación no puede exceder 200 caracteres")]
        public string Ubicacion { get; set; }

        /// <summary>
        /// Descripción detallada del espacio (OPCIONAL)
        /// Incluye equipamiento, características especiales, restricciones
        /// Ejemplo: "Proyector, aire acondicionado, 30 computadoras"
        /// </summary>
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string Descripcion { get; set; }

        /// <summary>
        /// Estado de disponibilidad del espacio (OPCIONAL en creación)
        /// true = Disponible para reservas
        /// false = No disponible (mantenimiento, fuera de servicio)
        /// null = Se establece como true por defecto en el controller
        /// </summary>
        public bool? Disponible { get; set; }
    }

    /// <summary>
    /// DTO específico para consultas de disponibilidad
    /// Utilizado en endpoints que solo necesitan saber si un espacio está libre
    /// Respuesta simplificada para consultas rápidas
    /// </summary>
    public class DisponibilidadDto
    {
        /// <summary>
        /// Indica si el espacio está disponible para reserva
        /// true = Libre para la fecha/horario consultado
        /// false = Ocupado o no disponible
        /// Calculado en base a reservas existentes y estado del espacio
        /// </summary>
        public bool Disponible { get; set; }

        /// <summary>
        /// Mensaje descriptivo del estado (OPCIONAL)
        /// Ejemplo: "Disponible", "Ocupado de 10:00-12:00", "Fuera de servicio"
        /// Proporciona contexto adicional sobre la disponibilidad
        /// </summary>
        public string Mensaje { get; set; }

        /// <summary>
        /// Lista de horarios ocupados para la fecha consultada (OPCIONAL)
        /// Permite mostrar franjas horarias específicas no disponibles
        /// Útil para sugerir horarios alternativos al usuario
        /// </summary>
        public List<string> HorariosOcupados { get; set; }
    }

    /// <summary>
    /// DTO de respuesta completa para espacios
    /// Incluye todos los datos del espacio incluyendo campos calculados
    /// Utilizado en respuestas GET, POST y PUT del EspacioController
    /// </summary>
    public class EspacioResponseDto
    {
        /// <summary>
        /// Identificador único del espacio
        /// Generado automáticamente por Entity Framework
        /// Utilizado para operaciones UPDATE, DELETE y consultas específicas
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nombre del espacio tal como se almacena en BD
        /// Campo no modificable en la respuesta
        /// Permite identificación rápida del espacio
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Tipo de espacio convertido a string
        /// Conversión del enum TipoEspacio para facilitar consumo por clientes
        /// Ejemplo: "Aula", "Laboratorio", "Auditorio"
        /// </summary>
        public string Tipo { get; set; }

        /// <summary>
        /// Capacidad máxima del espacio
        /// Número entero que indica cuántas personas pueden usar el espacio
        /// Utilizado por el frontend para validaciones
        /// </summary>
        public int Capacidad { get; set; }

        /// <summary>
        /// Ubicación física completa del espacio
        /// Información tal como se almacena en la base de datos
        /// Ayuda a usuarios a localizar el espacio reservado
        /// </summary>
        public string Ubicacion { get; set; }

        /// <summary>
        /// Descripción detallada del espacio
        /// Puede incluir equipamiento, características especiales
        /// Null si no se proporcionó descripción al crear el espacio
        /// </summary>
        public string Descripcion { get; set; }

        /// <summary>
        /// Estado actual de disponibilidad
        /// true = Activo y disponible para reservas
        /// false = Deshabilitado (mantenimiento, reparación, etc.)
        /// Los espacios no disponibles no aparecen en búsquedas de reserva
        /// </summary>
        public bool Disponible { get; set; }

        /// <summary>
        /// Fecha y hora de creación del espacio
        /// Timestamp automático generado al crear el registro
        /// Útil para auditoría y ordenamiento cronológico
        /// </summary>
        public DateTime FechaCreacion { get; set; }

        /// <summary>
        /// Información adicional sobre reservas activas (OPCIONAL)
        /// Puede incluir conteo de reservas pendientes/aprobadas
        /// Calculado dinámicamente según necesidades del endpoint
        /// </summary>
        public int? ReservasActivas { get; set; }

        /// <summary>
        /// Última fecha de reserva del espacio (OPCIONAL)
        /// Útil para análisis de uso y gestión de espacios
        /// Calculado mediante consulta a tabla Reservas
        /// </summary>
        public DateTime? UltimaReserva { get; set; }
    }

    /// <summary>
    /// DTO para consultas de espacios con filtros
    /// Utilizado en búsquedas avanzadas con múltiples criterios
    /// Permite filtrar por tipo, capacidad, disponibilidad, etc.
    /// </summary>
    public class EspacioFiltroDto
    {
        /// <summary>
        /// Filtro por tipo de espacio (OPCIONAL)
        /// Si se especifica, solo devuelve espacios del tipo indicado
        /// Útil para buscar solo aulas, solo laboratorios, etc.
        /// </summary>
        public TipoEspacio? Tipo { get; set; }

        /// <summary>
        /// Capacidad mínima requerida (OPCIONAL)
        /// Filtra espacios que puedan acomodar al menos esta cantidad de personas
        /// Usado para encontrar espacios adecuados según número de asistentes
        /// </summary>
        public int? CapacidadMinima { get; set; }

        /// <summary>
        /// Filtro por disponibilidad (OPCIONAL)
        /// true = Solo espacios disponibles
        /// false = Solo espacios no disponibles
        /// null = Todos los espacios independiente de su estado
        /// </summary>
        public bool? SoloDisponibles { get; set; }

        /// <summary>
        /// Búsqueda por texto en nombre o ubicación (OPCIONAL)
        /// Búsqueda parcial case-insensitive
        /// Ejemplo: "aula" encuentra "Aula 101", "Aula Magna", etc.
        /// </summary>
        public string BuscarTexto { get; set; }
    }
}