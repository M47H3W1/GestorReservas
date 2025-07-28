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
                new Espacio { Nombre = "Aula 101", Tipo = TipoEspacio.Aula },
                new Espacio { Nombre = "Laboratorio 1", Tipo = TipoEspacio.Laboratorio },
                new Espacio { Nombre = "Auditorio Principal", Tipo = TipoEspacio.Auditorio }
            );
        }
    }
}
