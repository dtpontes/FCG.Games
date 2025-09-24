using FCG.Games.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FCG.Games.Infrastructure
{
    public class AppDbContext :  IdentityDbContext
    {    
        public DbSet<Game> Games { get; set; } = null!;
        public DbSet<Stock> Stocks { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure the Game entity
            builder.Entity<Game>(entity =>
            {
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100); 

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(255); 

                entity.Property(e => e.DateRelease)
                    .IsRequired();

                entity.Property(e => e.DateUpdate)
                    .IsRequired();
            });

            // Configure the Stock entity
            builder.Entity<Stock>(entity =>
            {
                entity.Property(e => e.GameId)
                    .IsRequired();

                entity.Property(e => e.Quantity)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.UpdatedAt)
                    .IsRequired();

                // Configure relationship between Stock and Game
                entity.HasOne(s => s.Game)
                    .WithMany()
                    .HasForeignKey(s => s.GameId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Create unique index on GameId to ensure one stock record per game
                entity.HasIndex(s => s.GameId)
                    .IsUnique();
            });
            
        }


    }
}
