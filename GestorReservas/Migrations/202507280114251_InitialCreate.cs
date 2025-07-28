namespace GestorReservas.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Espacios",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false),
                        Tipo = c.Int(nullable: false),
                        Ubicacion = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Reservas",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UsuarioId = c.Int(nullable: false),
                        EspacioId = c.Int(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Horario = c.String(nullable: false),
                        Estado = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Espacios", t => t.EspacioId, cascadeDelete: true)
                .ForeignKey("dbo.Usuarios", t => t.UsuarioId, cascadeDelete: true)
                .Index(t => t.UsuarioId)
                .Index(t => t.EspacioId);
            
            CreateTable(
                "dbo.Usuarios",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false),
                        Email = c.String(nullable: false),
                        Password = c.String(nullable: false),
                        Rol = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Reservas", "UsuarioId", "dbo.Usuarios");
            DropForeignKey("dbo.Reservas", "EspacioId", "dbo.Espacios");
            DropIndex("dbo.Reservas", new[] { "EspacioId" });
            DropIndex("dbo.Reservas", new[] { "UsuarioId" });
            DropTable("dbo.Usuarios");
            DropTable("dbo.Reservas");
            DropTable("dbo.Espacios");
        }
    }
}
