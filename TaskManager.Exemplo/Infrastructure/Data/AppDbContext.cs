using Microsoft.EntityFrameworkCore;
using TaskManager.Exemplo.Domain;

namespace TaskManager.Exemplo.Infrastructure.Data;

// O DbContext é a "ponte" entre o C# e o banco de dados.
// Cada DbSet vira uma tabela. Quando você faz _context.Tarefas.Where(...),
// o EF Core traduz isso para SELECT ... FROM Tarefas WHERE ...
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
}
