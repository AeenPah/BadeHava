using Microsoft.EntityFrameworkCore;
using BadeHava.Models;

namespace BadeHava.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
}