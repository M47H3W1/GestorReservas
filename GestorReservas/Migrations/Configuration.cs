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
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data.
            context.Usuarios.AddOrUpdate(u => u.Email,
                new Usuario { Nombre = "Admin", Email = "admin@fiee.edu.ec", Password = "admin123", Rol = RolUsuario.Administrador },
                new Usuario { Nombre = "Profesor1", Email = "prof1@fiee.edu.ec", Password = "prof123", Rol = RolUsuario.Profesor },
                new Usuario { Nombre = "Coordinador", Email = "coord@fiee.edu.ec", Password = "coord123", Rol = RolUsuario.Coordinador }
            );

            // Espacios iniciales
            context.Espacios.AddOrUpdate(e => e.Nombre,
            // Aulas
            new Espacio
            {
                Nombre = "Aula 101",
                Tipo = TipoEspacio.Aula,
                Ubicacion = "E15/P1/A101"
            },
            new Espacio
            {
                Nombre = "Aula 102",
                Tipo = TipoEspacio.Aula,
                Ubicacion = "E15/P1/A102"
            },
            new Espacio
            {
                Nombre = "Aula 201",
                Tipo = TipoEspacio.Aula,
                Ubicacion = "E15/P2/A201"
            },
            new Espacio
            {
                Nombre = "Aula 301",
                Tipo = TipoEspacio.Aula,
                Ubicacion = "E16/P3/A301"
            },

            // Laboratorios
            new Espacio
            {
                Nombre = "Laboratorio de Redes",
                Tipo = TipoEspacio.Laboratorio,
                Ubicacion = "E17/P2/L001"
            },
            new Espacio
            {
                Nombre = "Laboratorio de Sistemas",
                Tipo = TipoEspacio.Laboratorio,
                Ubicacion = "E17/P2/L002"
            },
            new Espacio
            {
                Nombre = "Laboratorio de Hardware",
                Tipo = TipoEspacio.Laboratorio,
                Ubicacion = "E17/P3/L003"
            },
            new Espacio
            {
                Nombre = "Laboratorio de Programación",
                Tipo = TipoEspacio.Laboratorio,
                Ubicacion = "E17/P1/L004"
            },
            new Espacio
            {
                Nombre = "Laboratorio de Electrónica",
                Tipo = TipoEspacio.Laboratorio,
                Ubicacion = "E18/P2/L005"
            },

            // Auditorios
            new Espacio
            {
                Nombre = "Auditorio Principal",
                Tipo = TipoEspacio.Auditorio,
                Ubicacion = "E20/P1/AU001"
            },
            new Espacio
            {
                Nombre = "Auditorio de Conferencias",
                Tipo = TipoEspacio.Auditorio,
                Ubicacion = "E20/P2/AU002"
            },
            new Espacio
            {
                Nombre = "Sala de Eventos",                Tipo = TipoEspacio.Auditorio,
                Ubicacion = "E19/P1/SE001"
            }
        );
        }
    }
}
