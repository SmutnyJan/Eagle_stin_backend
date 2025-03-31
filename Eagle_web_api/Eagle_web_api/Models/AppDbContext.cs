using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Eagle_web_api.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<FavoriteTicker> FavoriteTickers { get; set; } = null!;
        public DbSet<StockData> StockDatas { get; set; } = null!;
    }
}
