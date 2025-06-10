using Microsoft.EntityFrameworkCore;
using prj1.Models;

namespace prj1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<LegalProfile> LegalProfiles { get; set; }
        public DbSet<LegalProfileFile> LegalProfileFiles { get; set; }
        public DbSet<LegalProfilePermission> LegalProfilePermissions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<DocumentTemplate> DocumentTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasMany(u => u.LegalProfiles)
                .WithOne(l => l.User)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Permissions)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.AuditLogs)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LegalProfile>()
                .HasMany(l => l.Files)
                .WithOne(f => f.LegalProfile)
                .HasForeignKey(f => f.LegalProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LegalProfile>()
                .HasMany(l => l.Permissions)
                .WithOne(p => p.LegalProfile)
                .HasForeignKey(p => p.LegalProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LegalProfile>()
                .HasMany(l => l.AuditLogs)
                .WithOne(a => a.LegalProfile)
                .HasForeignKey(a => a.LegalProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 