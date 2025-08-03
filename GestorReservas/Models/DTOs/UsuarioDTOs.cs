using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using GestorReservas.DAL;
using GestorReservas.Models;
using GestorReservas.Models.DTOs;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace GestorReservas.Controllers
{
    /// <summary>
    /// DTO para registro de nuevos usuarios
    /// Contiene todos los campos necesarios para crear un usuario
    /// Utilizado en el endpoint POST del UsuarioController
    /// </summary>
    public class UsuarioRegistroDto
    {
        /// <summary>
        /// Nombre completo del usuario
        /// Campo obligatorio para identificación personal
        /// Ejemplo: "Juan Carlos Pérez", "María González"
        /// </summary>
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; }

        /// <summary>
        /// Email único del usuario para login y comunicaciones
        /// Debe tener formato válido y ser único en el sistema
        /// Se normaliza a minúsculas en el controller
        /// </summary>
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(150, ErrorMessage = "El email no puede exceder 150 caracteres")]
        public string Email { get; set; }

        /// <summary>
        /// Contraseña en texto plano para el nuevo usuario
        /// Será hasheada con SHA256 antes de almacenar en BD
        /// Debe cumplir requisitos mínimos de seguridad
        /// </summary>
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [StringLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
        public string Password { get; set; }

        /// <summary>
        /// Rol del usuario en el sistema según enum RolUsuario
        /// Valores posibles: Profesor, Coordinador, Administrador
        /// Determina permisos y funcionalidades disponibles
        /// </summary>
        [Required(ErrorMessage = "El rol es obligatorio")]
        public RolUsuario Rol { get; set; }

        /// <summary>
        /// ID del departamento al cual pertenece el usuario (OPCIONAL)
        /// Solo aplicable para profesores y coordinadores
        /// Null para administradores o usuarios sin departamento asignado
        /// AGREGADO: Nueva funcionalidad para gestión departamental
        /// </summary>
        public int? DepartamentoId { get; set; }
    }

    /// <summary>
    /// DTO para actualización de usuarios existentes
    /// Solo incluye campos modificables después de la creación
    /// Utilizado en el endpoint PUT del UsuarioController
    /// </summary>
    public class UsuarioActualizarDto
    {
        /// <summary>
        /// Nuevo nombre del usuario (OPCIONAL)
        /// Solo se actualiza si se proporciona un valor
        /// Mantiene valor actual si es null o vacío
        /// </summary>
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; }

        /// <summary>
        /// Nuevo email del usuario (OPCIONAL)
        /// Se valida unicidad y formato antes de actualizar
        /// Solo se actualiza si se proporciona un valor válido
        /// </summary>
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(150, ErrorMessage = "El email no puede exceder 150 caracteres")]
        public string Email { get; set; }

        /// <summary>
        /// Nueva contraseña para el usuario (OPCIONAL)
        /// Será hasheada con SHA256 antes de almacenar
        /// Solo se actualiza si se proporciona un valor
        /// </summary>
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [StringLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
        public string NuevoPassword { get; set; }

        /// <summary>
        /// Nuevo rol para el usuario (OPCIONAL)
        /// SOLO ADMINISTRADORES pueden cambiar roles de otros usuarios
        /// Se valida autorización en el controller antes de aplicar
        /// </summary>
        public RolUsuario? Rol { get; set; }

        /// <summary>
        /// Nuevo departamento para el usuario (OPCIONAL)
        /// SOLO ADMINISTRADORES pueden cambiar departamentos
        /// Valor 0 = remover departamento, null = mantener actual
        /// AGREGADO: Nueva funcionalidad para gestión departamental
        /// </summary>
        public int? DepartamentoId { get; set; }
    }

    /// <summary>
    /// DTO de respuesta completa para usuarios
    /// Incluye todos los datos del usuario más información relacionada
    /// Utilizado en respuestas GET, POST y PUT del UsuarioController
    /// </summary>
    public class UsuarioResponseDto
    {
        /// <summary>
        /// Identificador único del usuario
        /// Generado automáticamente por Entity Framework
        /// Utilizado para operaciones UPDATE, DELETE y consultas específicas
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nombre completo del usuario
        /// Información personal para identificación y visualización
        /// Tal como se almacena en la base de datos
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Email único del usuario
        /// Utilizado para login y comunicaciones del sistema
        /// Normalizado a minúsculas para consistencia
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Rol del usuario convertido a string
        /// Conversión del enum RolUsuario para facilitar consumo por clientes
        /// Valores: "Profesor", "Coordinador", "Administrador"
        /// </summary>
        public string Rol { get; set; }

        /// <summary>
        /// Fecha y hora de creación del usuario
        /// Timestamp automático generado al crear el registro
        /// Útil para auditoría y ordenamiento cronológico
        /// </summary>
        public DateTime FechaCreacion { get; set; }

        /// <summary>
        /// ID del departamento al cual pertenece el usuario
        /// Foreign Key hacia la tabla Departamentos
        /// Puede ser null si el usuario no tiene departamento asignado
        /// AGREGADO: Información del departamento para gestión organizacional
        /// </summary>
        public int? DepartamentoId { get; set; }

        /// <summary>
        /// Información completa del departamento del usuario
        /// Objeto anidado con datos básicos del departamento
        /// Null si el usuario no pertenece a ningún departamento
        /// Evita consultas adicionales del frontend para mostrar datos
        /// AGREGADO: Nueva funcionalidad para gestión departamental
        /// </summary>
        public DepartamentoBasicoDto Departamento { get; set; }

        /// <summary>
        /// Indica si el usuario es jefe de su departamento
        /// Calculado comparando Usuario.Id con Departamento.JefeId
        /// Útil para determinar permisos adicionales y UI específica
        /// AGREGADO: Funcionalidad de jerarquía departamental
        /// </summary>
        public bool EsJefeDepartamento { get; set; }
    }

    /// <summary>
    /// DTO para información básica de departamentos
    /// Utilizado como objeto anidado en respuestas de usuarios
    /// Evita circular references y mantiene respuestas ligeras
    /// NUEVA CLASE: Para información básica del departamento
    /// </summary>
    public class DepartamentoBasicoDto
    {
        /// <summary>
        /// Identificador único del departamento
        /// Referencia para operaciones adicionales si es necesario
        /// Corresponde al ID en la tabla Departamentos
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nombre oficial del departamento
        /// Ejemplo: "Ingeniería de Sistemas", "Matemáticas", "Ciencias Naturales"
        /// Información principal para identificación del departamento
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Código único del departamento
        /// Abreviación oficial del departamento
        /// Ejemplo: "INGSIST", "MAT", "CIENAT"
        /// Útil para referencias rápidas y visualización compacta
        /// </summary>
        public string Codigo { get; set; }

        /// <summary>
        /// Tipo de departamento convertido a string
        /// Conversión del enum TipoDepartamento para facilitar consumo
        /// Valores posibles: "Academico", "Administrativo", "Investigacion"
        /// Proporciona contexto sobre la naturaleza del departamento
        /// </summary>
        public string Tipo { get; set; }
    }

    /// <summary>
    /// DTO para búsqueda y filtrado de usuarios
    /// Permite consultas avanzadas con múltiples criterios
    /// Utilizado en endpoints de búsqueda y reportes administrativos
    /// </summary>
    public class UsuarioFiltroDto
    {
        /// <summary>
        /// Búsqueda por texto en nombre o email (OPCIONAL)
        /// Búsqueda parcial case-insensitive
        /// Ejemplo: "juan" encuentra usuarios con "Juan" en nombre o email
        /// </summary>
        public string BuscarTexto { get; set; }

        /// <summary>
        /// Filtrar por rol específico (OPCIONAL)
        /// Si se especifica, solo devuelve usuarios del rol indicado
        /// Útil para listar solo profesores, solo coordinadores, etc.
        /// </summary>
        public RolUsuario? Rol { get; set; }

        /// <summary>
        /// Filtrar por departamento específico (OPCIONAL)
        /// Si se especifica, solo devuelve usuarios del departamento indicado
        /// Útil para gestión departamental y reportes organizacionales
        /// </summary>
        public int? DepartamentoId { get; set; }

        /// <summary>
        /// Filtrar solo jefes de departamento (OPCIONAL)
        /// true = Solo usuarios que son jefes de departamento
        /// false = Solo usuarios que NO son jefes
        /// null = Todos los usuarios independiente de su estatus
        /// </summary>
        public bool? SoloJefes { get; set; }

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

