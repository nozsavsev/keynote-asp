using Microsoft.EntityFrameworkCore;
using keynote_asp.Models.Keynote;
using keynote_asp.Models.User;

namespace keynote_asp.DbContexts
{
    public class KeynoteDbContext : DbContext
    {
        public KeynoteDbContext(DbContextOptions<KeynoteDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<DB_User> Users { get; set; }
        public DbSet<DB_Keynote> Keynotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<DB_User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Navigation(e => e.keynotes).AutoInclude();
                entity.Property(e => e.Id).ValueGeneratedNever();

            });


            modelBuilder.Entity<DB_Keynote>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(k => k.User)
                    .WithMany(u => u.keynotes)
                    .HasForeignKey(k => k.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
