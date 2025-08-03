namespace GestorReservas.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ReestructuracionDatos : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Reservas", "Horario", c => c.String(nullable: false, maxLength: 11));
            AlterColumn("dbo.Reservas", "Descripcion", c => c.String(maxLength: 500));
            AlterColumn("dbo.Espacios", "Nombre", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Espacios", "Ubicacion", c => c.String(nullable: false, maxLength: 200));
            AlterColumn("dbo.Espacios", "Descripcion", c => c.String(maxLength: 500));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Espacios", "Descripcion", c => c.String());
            AlterColumn("dbo.Espacios", "Ubicacion", c => c.String(nullable: false));
            AlterColumn("dbo.Espacios", "Nombre", c => c.String(nullable: false));
            AlterColumn("dbo.Reservas", "Descripcion", c => c.String());
            AlterColumn("dbo.Reservas", "Horario", c => c.String(nullable: false));
        }
    }
}
