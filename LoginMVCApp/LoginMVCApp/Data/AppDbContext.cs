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
        public DbSet<Robots> Robots { get; set; }
        public DbSet<Transactions> Transactions { get; set; }
        public DbSet<Ng_Categories> Ng_Categories { get; set; }
        public DbSet<Qr_Counter> Qr_Counter { get; set; }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<Robots>()
        //        .HasOne(r => r.Line)
        //        .WithMany(l => l.Robots)
        //        .HasForeignKey(r => r.LineId)
        //        .OnDelete(DeleteBehavior.Restrict);
        //}
    }
}
