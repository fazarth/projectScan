using LoginMVCApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginMVCApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Users> Users { get; set; }
        public DbSet<Inventories> Inventories { get; set; }
        public DbSet<Lines> Lines { get; set; } 
    }
}
