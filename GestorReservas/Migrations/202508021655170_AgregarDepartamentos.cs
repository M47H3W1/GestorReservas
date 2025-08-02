namespace GestorReservas.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AgregarDepartamentos : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Departamentoes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 100),
                        Codigo = c.String(nullable: false, maxLength: 10),
                        Tipo = c.Int(nullable: false),
                        Descripcion = c.String(maxLength: 500),
                        JefeId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Usuarios", t => t.JefeId)
                .Index(t => t.JefeId);
            
            AddColumn("dbo.Usuarios", "DepartamentoId", c => c.Int());
            AlterColumn("dbo.Usuarios", "Nombre", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Usuarios", "Email", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Usuarios", "Password", c => c.String(nullable: false, maxLength: 255));
            CreateIndex("dbo.Usuarios", "DepartamentoId");
            AddForeignKey("dbo.Usuarios", "DepartamentoId", "dbo.Departamentoes", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Departamentoes", "JefeId", "dbo.Usuarios");
            DropForeignKey("dbo.Usuarios", "DepartamentoId", "dbo.Departamentoes");
            DropIndex("dbo.Usuarios", new[] { "DepartamentoId" });
            DropIndex("dbo.Departamentoes", new[] { "JefeId" });
            AlterColumn("dbo.Usuarios", "Password", c => c.String(nullable: false));
            AlterColumn("dbo.Usuarios", "Email", c => c.String(nullable: false));
            AlterColumn("dbo.Usuarios", "Nombre", c => c.String(nullable: false));
            DropColumn("dbo.Usuarios", "DepartamentoId");
            DropTable("dbo.Departamentoes");
        }
    }
}
