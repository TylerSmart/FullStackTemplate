using FullStackTemplate.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FullStackTemplate.Data;

public sealed class FullStackTemplateDbContext(DbContextOptions<FullStackTemplateDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FullStackTemplateDbContext).Assembly);
    }
}
