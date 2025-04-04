using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Eagle_web_api.Models
{
    public class AppDbContext : DbContext
    {
        public const string API_KEY = "cvi0sn9r01qks9q7hi0gcvi0sn9r01qks9q7hi10";

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<FavoriteTicker> FavoriteTickers { get; set; } = null!;
        public DbSet<StockData> StockDatas { get; set; } = null!;


    }
}
