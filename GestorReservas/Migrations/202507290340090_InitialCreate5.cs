﻿namespace GestorReservas.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate5 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Reservas", "Descripcion", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Reservas", "Descripcion");
        }
    }
}
