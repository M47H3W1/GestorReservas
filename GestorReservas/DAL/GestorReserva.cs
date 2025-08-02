using GestorReservas.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls.WebParts;

namespace GestorReservas.DAL
{
    public class GestorReserva : DbContext
    {
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Espacio> Espacios { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<Departamento> Departamentos { get; set; } // NUEVO

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configurar relación Usuario-Departamento
            modelBuilder.Entity<Usuario>()
                .HasOptional(u => u.Departamento)
                .WithMany(d => d.Profesores)
                .HasForeignKey(u => u.DepartamentoId)
                .WillCascadeOnDelete(false);

            // Configurar relación Departamento-Jefe
            modelBuilder.Entity<Departamento>()
                .HasOptional(d => d.Jefe)
                .WithMany()
                .HasForeignKey(d => d.JefeId)
                .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}