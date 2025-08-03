using GestorReservas.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls.WebParts;

namespace GestorReservas.DAL
{
    /// <summary>
    /// Contexto de Entity Framework para el sistema de gestión de reservas
    /// Hereda de DbContext para proporcionar acceso a la base de datos
    /// Implementa el patrón Repository para las entidades del sistema
    /// </summary>
    public class GestorReserva : DbContext
    {
        #region DbSets - Tablas de la Base de Datos

        /// <summary>
        /// DbSet para la entidad Usuario
        /// Representa la tabla de usuarios en la base de datos
        /// Permite operaciones CRUD sobre usuarios (profesores, coordinadores, administradores)
        /// </summary>
        public DbSet<Usuario> Usuarios { get; set; }

        /// <summary>
        /// DbSet para la entidad Espacio
        /// Representa la tabla de espacios/aulas en la base de datos
        /// Contiene información sobre aulas, laboratorios, auditorios, etc.
        /// </summary>
        public DbSet<Espacio> Espacios { get; set; }

        /// <summary>
        /// DbSet para la entidad Reserva
        /// Representa la tabla de reservas en la base de datos
        /// Gestiona las reservas de espacios realizadas por usuarios
        /// Incluye fechas, horarios, estados y relaciones con usuarios y espacios
        /// </summary>
        public DbSet<Reserva> Reservas { get; set; }

        /// <summary>
        /// DbSet para la entidad Departamento
        /// Representa la tabla de departamentos académicos
        /// Permite organizar usuarios por departamentos y asignar jefes
        /// AGREGADO: Nueva funcionalidad para gestión departamental
        /// </summary>
        public DbSet<Departamento> Departamentos { get; set; }

        #endregion

        #region Configuración del Modelo - Entity Framework Code First

        /// <summary>
        /// Configura las relaciones entre entidades usando Fluent API
        /// Se ejecuta cuando Entity Framework crea el modelo de datos
        /// Define Foreign Keys, relaciones y restricciones de cascada
        /// </summary>
        /// <param name="modelBuilder">Constructor del modelo EF</param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            #region Relación Usuario-Departamento (Uno a Muchos)

            // CONFIGURAR RELACIÓN: Un Departamento puede tener muchos Usuarios (Profesores)
            // Un Usuario puede pertenecer a un Departamento (opcional)
            modelBuilder.Entity<Usuario>()
                .HasOptional(u => u.Departamento)          // Usuario tiene departamento OPCIONAL
                .WithMany(d => d.Profesores)               // Departamento tiene MUCHOS profesores
                .HasForeignKey(u => u.DepartamentoId)      // FK: DepartamentoId en tabla Usuario
                .WillCascadeOnDelete(false);               // NO eliminar usuarios si se elimina departamento

            #endregion

            #region Relación Departamento-Jefe (Uno a Uno Opcional)

            // CONFIGURAR RELACIÓN: Un Departamento puede tener un Jefe (Usuario)
            // Un Usuario puede ser jefe de máximo un departamento
            modelBuilder.Entity<Departamento>()
                .HasOptional(d => d.Jefe)                  // Departamento tiene jefe OPCIONAL
                .WithMany()                                // Un usuario puede ser jefe de varios (aunque en lógica de negocio solo uno)
                .HasForeignKey(d => d.JefeId)              // FK: JefeId en tabla Departamento
                .WillCascadeOnDelete(false);               // NO eliminar departamento si se elimina el jefe

            #endregion

            #region Configuraciones Automáticas de EF

            // Llamar al método base para aplicar configuraciones por defecto
            // Esto permite que EF configure automáticamente otras relaciones
            // como Usuario-Reserva, Espacio-Reserva según las propiedades de navegación
            base.OnModelCreating(modelBuilder);

            #endregion
        }

        #endregion

        #region Constructor Implícito

        // CONSTRUCTOR POR DEFECTO:
        // Entity Framework usa el constructor por defecto
        // La cadena de conexión se toma de web.config
        // Busca una connection string con el mismo nombre que la clase (GestorReserva)
        // Si no encuentra, usa LocalDB por defecto

        #endregion

        #region Configuraciones Adicionales Implícitas

        // MIGRACIONES AUTOMÁTICAS:
        // Si Database.SetInitializer está configurado en Global.asax
        // EF puede crear/actualizar la base de datos automáticamente

        // LAZY LOADING:
        // Por defecto está habilitado, permite cargar entidades relacionadas bajo demanda
        // Ejemplo: al acceder a usuario.Departamento, EF hace query automáticamente

        // CHANGE TRACKING:
        // EF rastrea cambios en las entidades automáticamente
        // SaveChanges() detecta y persiste solo los cambios realizados

        #endregion

        #region Ejemplo de Uso del Contexto

        /*
        // EJEMPLO DE USO EN CONTROLADORES:
        
        using (var db = new GestorReserva())
        {
            // Consultar usuarios con departamentos (Eager Loading)
            var usuarios = db.Usuarios.Include(u => u.Departamento).ToList();
            
            // Crear nueva reserva
            var reserva = new Reserva
            {
                UsuarioId = 1,
                EspacioId = 1,
                Fecha = DateTime.Today,
                Horario = "08:00-10:00",
                Estado = EstadoReserva.Pendiente
            };
            
            db.Reservas.Add(reserva);
            db.SaveChanges();
        }
        */

        #endregion
    }
}