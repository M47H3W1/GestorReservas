using System;
using System.Data.Entity.Migrations;
using System.Linq;
using GestorReservas.Models;
using GestorReservas.DAL;
using System.Security.Cryptography;
using System.Text;

namespace GestorReservas.Migrations
{
    using GestorReservas.Models;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<GestorReservas.DAL.GestorReserva>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(GestorReservas.DAL.GestorReserva context)
        {
            // ==================== USUARIOS ====================

            // Crear usuarios iniciales si no existen
            var usuarios = new[]
            {
                new Usuario
                {
                    Id = 1,
                    Nombre = "Administrador Sistema",
                    Email = "admin@fiee.edu",
                    Password = HashPassword("admin123"),
                    Rol = RolUsuario.Administrador
                },
                new Usuario
                {
                    Id = 2,
                    Nombre = "Coordinador Académico",
                    Email = "coordinador@fiee.edu",
                    Password = HashPassword("coord123"),
                    Rol = RolUsuario.Coordinador
                },
                new Usuario
                {
                    Id = 3,
                    Nombre = "Dr. Miguel Avilez",
                    Email = "miguel.avilez@fiee.edu",
                    Password = HashPassword("profesor123"),
                    Rol = RolUsuario.Profesor
                },
                new Usuario
                {
                    Id = 4,
                    Nombre = "Dr. Mathew Gutiérrez",
                    Email = "mathew.gutierrez@fiee.edu",
                    Password = HashPassword("profesor123"),
                    Rol = RolUsuario.Profesor
                },
                new Usuario
                {
                    Id = 5,
                    Nombre = "Prof. Carlos López",
                    Email = "carlos.lopez@fiee.edu",
                    Password = HashPassword("profesor123"),
                    Rol = RolUsuario.Profesor
                }
            };

            // Agregar usuarios si no existen
            foreach (var usuario in usuarios)
            {
                if (!context.Usuarios.Any(u => u.Email == usuario.Email))
                {
                    context.Usuarios.AddOrUpdate(u => u.Email, usuario);
                }
            }

            // ==================== ESPACIOS ====================

            // Crear espacios iniciales con todas las propiedades
            var espacios = new[]
            {
                new Espacio
                {
                    Id = 1,
                    Nombre = "Aula 101",
                    Tipo = TipoEspacio.Aula,
                    Ubicacion = "Edificio A - Piso 1",
                    Capacidad = 30,
                    Descripcion = "Aula equipada con proyector y sistema de audio",
                    Disponible = true
                },
                new Espacio
                {
                    Id = 2,
                    Nombre = "Laboratorio de Informática",
                    Tipo = TipoEspacio.Laboratorio,
                    Ubicacion = "Edificio B - Piso 2",
                    Capacidad = 25,
                    Descripcion = "Laboratorio con 25 computadoras y software especializado",
                    Disponible = true
                },
                new Espacio
                {
                    Id = 3,
                    Nombre = "Auditorio Principal",
                    Tipo = TipoEspacio.Auditorio,
                    Ubicacion = "Edificio Central - Planta Baja",
                    Capacidad = 150,
                    Descripcion = "Auditorio principal con sistema de sonido profesional y proyección",
                    Disponible = true
                },
                new Espacio
                {
                    Id = 4,
                    Nombre = "Aula 205",
                    Tipo = TipoEspacio.Aula,
                    Ubicacion = "Edificio A - Piso 2",
                    Capacidad = 35,
                    Descripcion = "Aula con pizarra inteligente y conectividad WiFi",
                    Disponible = true
                },
                new Espacio
                {
                    Id = 5,
                    Nombre = "Laboratorio de Química",
                    Tipo = TipoEspacio.Laboratorio,
                    Ubicacion = "Edificio C - Piso 1",
                    Capacidad = 20,
                    Descripcion = "Laboratorio equipado con campanas extractoras y material de seguridad",
                    Disponible = true
                },
                new Espacio
                {
                    Id = 6,
                    Nombre = "Sala de Conferencias",
                    Tipo = TipoEspacio.Auditorio,
                    Ubicacion = "Edificio D - Piso 3",
                    Capacidad = 50,
                    Descripcion = "Sala para conferencias con sistema de videoconferencia",
                    Disponible = true
                },
                new Espacio
                {
                    Id = 7,
                    Nombre = "Aula 301",
                    Tipo = TipoEspacio.Aula,
                    Ubicacion = "Edificio A - Piso 3",
                    Capacidad = 40,
                    Descripcion = "Aula amplia con iluminación natural",
                    Disponible = false
                    
                }
            };

            // Agregar espacios si no existen
            foreach (var espacio in espacios)
            {
                if (!context.Espacios.Any(e => e.Nombre == espacio.Nombre))
                {
                    context.Espacios.AddOrUpdate(e => e.Nombre, espacio);
                }
            }

            // ==================== RESERVAS DE EJEMPLO ====================

            // Crear algunas reservas de ejemplo
            var reservasEjemplo = new[]
            {
                new Reserva
                {
                    Id = 1,
                    UsuarioId = 3, // Dr. Juan Pérez
                    EspacioId = 1, // Aula 101
                    Fecha = DateTime.Today.AddDays(1), // Mañana
                    Horario = "08:00-10:00",
                    Descripcion = "Clase de Matemáticas Discretas",
                    Estado = EstadoReserva.Aprobada
                },
                new Reserva
                {
                    Id = 2,
                    UsuarioId = 4, // Dra. María García
                    EspacioId = 2, // Laboratorio de Informática
                    Fecha = DateTime.Today.AddDays(2), // Pasado mañana
                    Horario = "14:00-16:00",
                    Descripcion = "Práctica de Programación",
                    Estado = EstadoReserva.Pendiente
                },
                new Reserva
                {
                    Id = 3,
                    UsuarioId = 5, // Prof. Carlos López
                    EspacioId = 3, // Auditorio Principal
                    Fecha = DateTime.Today.AddDays(3),
                    Horario = "10:00-12:00",
                    Descripcion = "Conferencia de Investigación",
                    Estado = EstadoReserva.Aprobada
                }
            };

            // Agregar reservas si no existen
            foreach (var reserva in reservasEjemplo)
            {
                if (!context.Reservas.Any(r => r.Id == reserva.Id))
                {
                    context.Reservas.AddOrUpdate(r => r.Id, reserva);
                }
            }

            // Guardar cambios
            context.SaveChanges();
        }

        // Método para hashear contraseñas (igual que en los controladores)
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
