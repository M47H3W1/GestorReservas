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
            // ==================== DEPARTAMENTOS ====================

            // Crear departamentos iniciales si no existen
            var departamentos = new[]
            {
                new Departamento
                {
                    Id = 1,
                    Nombre = "Departamento de Automatización y Control Industrial",
                    Codigo = "DACI",
                    Tipo = TipoDepartamento.DACI,
                    Descripcion = "Departamento especializado en automatización y control industrial, sistemas embebidos y robótica"
                },
                new Departamento
                {
                    Id = 2,
                    Nombre = "Departamento de Electrónica, Telecomunicaciones y Redes de Información",
                    Codigo = "DETRI",
                    Tipo = TipoDepartamento.DETRI,
                    Descripcion = "Departamento especializado en electrónica, telecomunicaciones, redes de información y comunicaciones"
                },
                new Departamento
                {
                    Id = 3,
                    Nombre = "Departamento de Energía Eléctrica",
                    Codigo = "DEE",
                    Tipo = TipoDepartamento.DEE,
                    Descripcion = "Departamento especializado en energía eléctrica, sistemas de potencia y energías renovables"
                }
            };

            // Agregar departamentos si no existen
            foreach (var departamento in departamentos)
            {
                if (!context.Departamentos.Any(d => d.Codigo == departamento.Codigo))
                {
                    context.Departamentos.AddOrUpdate(d => d.Codigo, departamento);
                }
            }

            // Guardar cambios de departamentos primero
            context.SaveChanges();

            // ==================== USUARIOS ====================

            // Crear usuarios iniciales si no existen (incluyendo asignación de departamentos)
            var usuarios = new[]
            {
                new Usuario
                {
                    Id = 1,
                    Nombre = "Administrador Sistema",
                    Email = "admin@fiee.edu",
                    Password = HashPassword("admin123"),
                    Rol = RolUsuario.Administrador,
                    DepartamentoId = null // Administrador no pertenece a departamento específico
                },
                new Usuario
                {
                    Id = 2,
                    Nombre = "Dr. Roberto Mendoza",
                    Email = "roberto.mendoza@fiee.edu",
                    Password = HashPassword("coord123"),
                    Rol = RolUsuario.Coordinador,
                    DepartamentoId = 1 // DACI - será jefe de este departamento
                },
                new Usuario
                {
                    Id = 3,
                    Nombre = "Dra. Ana Vásquez",
                    Email = "ana.vasquez@fiee.edu",
                    Password = HashPassword("coord123"),
                    Rol = RolUsuario.Coordinador,
                    DepartamentoId = 2 // DETRI - será jefa de este departamento
                },
                new Usuario
                {
                    Id = 4,
                    Nombre = "Dr. Carlos Salinas",
                    Email = "carlos.salinas@fiee.edu",
                    Password = HashPassword("coord123"),
                    Rol = RolUsuario.Coordinador,
                    DepartamentoId = 3 // DEE - será jefe de este departamento
                },
                new Usuario
                {
                    Id = 5,
                    Nombre = "Dr. Miguel Avilez",
                    Email = "miguel.avilez@fiee.edu",
                    Password = HashPassword("profesor123"),
                    Rol = RolUsuario.Profesor,
                    DepartamentoId = 1 // DACI
                },
                new Usuario
                {
                    Id = 6,
                    Nombre = "Dr. Mathew Gutiérrez",
                    Email = "mathew.gutierrez@fiee.edu",
                    Password = HashPassword("profesor123"),
                    Rol = RolUsuario.Profesor,
                    DepartamentoId = 1 // DACI
                },
                new Usuario
                {
                    Id = 7,
                    Nombre = "Prof. Laura Martínez",
                    Email = "laura.martinez@fiee.edu",
                    Password = HashPassword("profesor123"),
                    Rol = RolUsuario.Profesor,
                    DepartamentoId = 2 // DETRI
                },
                new Usuario
                {
                    Id = 8,
                    Nombre = "Dr. José Rodríguez",
                    Email = "jose.rodriguez@fiee.edu",
                    Password = HashPassword("profesor123"),
                    Rol = RolUsuario.Profesor,
                    DepartamentoId = 2 // DETRI
                },
                new Usuario
                {
                    Id = 9,
                    Nombre = "Prof. Elena Torres",
                    Email = "elena.torres@fiee.edu",
                    Password = HashPassword("profesor123"),
                    Rol = RolUsuario.Profesor,
                    DepartamentoId = 3 // DEE
                },
                new Usuario
                {
                    Id = 10,
                    Nombre = "Dr. Francisco López",
                    Email = "francisco.lopez@fiee.edu",
                    Password = HashPassword("profesor123"),
                    Rol = RolUsuario.Profesor,
                    DepartamentoId = 3 // DEE
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

            // Guardar cambios de usuarios
            context.SaveChanges();

            // ==================== ASIGNAR JEFES DE DEPARTAMENTO ====================

            // Asignar jefes a los departamentos
            var daciDept = context.Departamentos.FirstOrDefault(d => d.Codigo == "DACI");
            var detriDept = context.Departamentos.FirstOrDefault(d => d.Codigo == "DETRI");
            var deeDept = context.Departamentos.FirstOrDefault(d => d.Codigo == "DEE");

            if (daciDept != null && daciDept.JefeId == null)
            {
                var jefeDACI = context.Usuarios.FirstOrDefault(u => u.Email == "roberto.mendoza@fiee.edu");
                if (jefeDACI != null)
                {
                    daciDept.JefeId = jefeDACI.Id;
                }
            }

            if (detriDept != null && detriDept.JefeId == null)
            {
                var jefeDETRI = context.Usuarios.FirstOrDefault(u => u.Email == "ana.vasquez@fiee.edu");
                if (jefeDETRI != null)
                {
                    detriDept.JefeId = jefeDETRI.Id;
                }
            }

            if (deeDept != null && deeDept.JefeId == null)
            {
                var jefeDEE = context.Usuarios.FirstOrDefault(u => u.Email == "carlos.salinas@fiee.edu");
                if (jefeDEE != null)
                {
                    deeDept.JefeId = jefeDEE.Id;
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
                    Nombre = "Laboratorio de Automatización",
                    Tipo = TipoEspacio.Laboratorio,
                    Ubicacion = "Edificio B - Piso 2",
                    Capacidad = 25,
                    Descripcion = "Laboratorio con PLCs, sensores y actuadores para prácticas de automatización",
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
                    Nombre = "Laboratorio de Telecomunicaciones",
                    Tipo = TipoEspacio.Laboratorio,
                    Ubicacion = "Edificio C - Piso 1",
                    Capacidad = 20,
                    Descripcion = "Laboratorio equipado con analizadores de espectro y generadores de señal",
                    Disponible = true
                },
                new Espacio
                {
                    Id = 5,
                    Nombre = "Laboratorio de Energía Eléctrica",
                    Tipo = TipoEspacio.Laboratorio,
                    Ubicacion = "Edificio D - Piso 1",
                    Capacidad = 15,
                    Descripcion = "Laboratorio con equipos de medición de potencia y sistemas trifásicos",
                    Disponible = true
                },
                new Espacio
                {
                    Id = 6,
                    Nombre = "Sala de Conferencias DACI",
                    Tipo = TipoEspacio.Auditorio,
                    Ubicacion = "Edificio B - Piso 3",
                    Capacidad = 50,
                    Descripcion = "Sala para conferencias del departamento DACI con sistema de videoconferencia",
                    Disponible = true
                },
                new Espacio
                {
                    Id = 7,
                    Nombre = "Aula 205",
                    Tipo = TipoEspacio.Aula,
                    Ubicacion = "Edificio A - Piso 2",
                    Capacidad = 35,
                    Descripcion = "Aula con pizarra inteligente y conectividad WiFi",
                    Disponible = true
                },
                new Espacio
                {
                    Id = 8,
                    Nombre = "Laboratorio de Redes",
                    Tipo = TipoEspacio.Laboratorio,
                    Ubicacion = "Edificio C - Piso 2",
                    Capacidad = 30,
                    Descripcion = "Laboratorio con switches, routers y equipos de networking",
                    Disponible = true
                },
                new Espacio
                {
                    Id = 9,
                    Nombre = "Aula 301 - En Mantenimiento",
                    Tipo = TipoEspacio.Aula,
                    Ubicacion = "Edificio A - Piso 3",
                    Capacidad = 40,
                    Descripcion = "Aula amplia con iluminación natural - Temporalmente fuera de servicio",
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

            // Crear algunas reservas de ejemplo con los nuevos usuarios
            var reservasEjemplo = new[]
            {
                new Reserva
                {
                    Id = 1,
                    UsuarioId = 5, // Dr. Miguel Avilez (DACI)
                    EspacioId = 2, // Laboratorio de Automatización
                    Fecha = DateTime.Today.AddDays(1), // Mañana
                    Horario = "08:00-10:00",
                    Descripcion = "Práctica de Control de Procesos",
                    Estado = EstadoReserva.Aprobada
                },
                new Reserva
                {
                    Id = 2,
                    UsuarioId = 7, // Prof. Laura Martínez (DETRI)
                    EspacioId = 4, // Laboratorio de Telecomunicaciones
                    Fecha = DateTime.Today.AddDays(2), // Pasado mañana
                    Horario = "14:00-16:00",
                    Descripcion = "Análisis de Señales y Comunicaciones",
                    Estado = EstadoReserva.Pendiente
                },
                new Reserva
                {
                    Id = 3,
                    UsuarioId = 9, // Prof. Elena Torres (DEE)
                    EspacioId = 5, // Laboratorio de Energía Eléctrica
                    Fecha = DateTime.Today.AddDays(3),
                    Horario = "10:00-12:00",
                    Descripcion = "Mediciones en Sistemas de Potencia",
                    Estado = EstadoReserva.Aprobada
                },
                new Reserva
                {
                    Id = 4,
                    UsuarioId = 6, // Dr. Mathew Gutiérrez (DACI)
                    EspacioId = 1, // Aula 101
                    Fecha = DateTime.Today.AddDays(4),
                    Horario = "16:00-18:00",
                    Descripcion = "Clase teórica de Sistemas de Control",
                    Estado = EstadoReserva.Pendiente
                },
                new Reserva
                {
                    Id = 5,
                    UsuarioId = 8, // Dr. José Rodríguez (DETRI)
                    EspacioId = 8, // Laboratorio de Redes
                    Fecha = DateTime.Today.AddDays(5),
                    Horario = "09:00-11:00",
                    Descripcion = "Configuración de Redes Cisco",
                    Estado = EstadoReserva.Aprobada
                },
                new Reserva
                {
                    Id = 6,
                    UsuarioId = 10, // Dr. Francisco López (DEE)
                    EspacioId = 3, // Auditorio Principal
                    Fecha = DateTime.Today.AddDays(7),
                    Horario = "15:00-17:00",
                    Descripcion = "Conferencia sobre Energías Renovables",
                    Estado = EstadoReserva.Pendiente
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
