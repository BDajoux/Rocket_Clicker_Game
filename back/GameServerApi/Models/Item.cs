using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerApi.Models
{
    public class Item
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Name { get; set; }
        public int Price { get; set; }
        public int MaxQuantity { get; set; }
        public int ClickValue { get; set; }


    }

    //public class ItemContext : DbContext
    //{
    //    public ItemContext(DbContextOptions<ItemContext> options) : base(options)
    //    {

    //    }

    //    public DbSet<Item> Items { get; set; } = null!;

    //    protected override void OnConfiguring(DbContextOptionsBuilder options)
    //    {
    //        // Connexion a la base sqlite
    //        options.UseSqlite("Data Source=Items.db");
    //    }



    //}
}
