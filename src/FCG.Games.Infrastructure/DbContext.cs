using FCG.Games.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FCG.Games.Infrastructure
{
    public class AppDbContext :  IdentityDbContext
    {    
        public DbSet<Game> Games { get; set; } = null!;

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
            
        }


    }
}
