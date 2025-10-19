using Microsoft.EntityFrameworkCore;
using BadeHava.Models;

namespace BadeHava.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Events> Events { get; set; }
    public DbSet<Friendships> Friendships { get; set; }

    public DbSet<UserGroupChat> UserGroupChat { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserGroupChat>()
            .HasKey(gc => new { gc.GroupChatId, gc.UserId });

        modelBuilder.Entity<UserGroupChat>()
            .HasIndex(gc => gc.GroupChatId);
    }
}