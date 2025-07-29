namespace GestorReservas.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate3 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Espacios", "Capacidad", c => c.Int(nullable: false));
            AddColumn("dbo.Espacios", "Descripcion", c => c.String());
            AddColumn("dbo.Espacios", "Disponible", c => c.Boolean(nullable: false));
            AlterColumn("dbo.Espacios", "Ubicacion", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Espacios", "Ubicacion", c => c.String());
            DropColumn("dbo.Espacios", "Disponible");
            DropColumn("dbo.Espacios", "Descripcion");
            DropColumn("dbo.Espacios", "Capacidad");
        }
    }
}
