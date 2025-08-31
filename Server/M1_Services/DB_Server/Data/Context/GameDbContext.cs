using Microsoft.EntityFrameworkCore;
using DbServer.Data.Entities;

namespace DbServer.Data.Context;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<GameRoom> GameRooms { get; set; }
    public DbSet<GameRoomPlayer> GameRoomPlayers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User 설정
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // UserSession 설정
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.HasIndex(e => e.Token);
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserSessions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // GameRoom 설정
        modelBuilder.Entity<GameRoom>(entity =>
        {
            entity.HasKey(e => e.RoomId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedBy);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Creator)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // GameRoomPlayer 설정
        modelBuilder.Entity<GameRoomPlayer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoomId, e.UserId }).IsUnique();
            entity.HasIndex(e => new { e.RoomId, e.PlayerSlot });
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Room)
                  .WithMany(r => r.Players)
                  .HasForeignKey(e => e.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // Seed data
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Development fallback - normally configured in Program.cs
            optionsBuilder.UseMySql(
                "Server=localhost;Database=GameDatabase;Uid=root;Pwd=password;",
                ServerVersion.AutoDetect("Server=localhost;Database=GameDatabase;Uid=root;Pwd=password;")
            );
        }
    }
}