using GameServerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Data
{
    public class BDDContext : DbContext
    {
        public BDDContext(DbContextOptions<BDDContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Progression> Progressions { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<InventoryEntry> Inventories { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                // Connexion a la base sqlite
                options.UseSqlite("Data Source=BDDContext.db");
            }
        }





    }
}
