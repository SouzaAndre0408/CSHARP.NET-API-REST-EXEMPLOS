using Microsoft.EntityFrameworkCore;
using TaskManager.Exemplo.Domain;
using TaskManager.Exemplo.Infrastructure.Data;

namespace TaskManager.Exemplo.Infrastructure.Repositories;

// CAMADA INFRASTRUCTURE: a implementação real do acesso a dados.
// É o ÚNICO lugar que conhece o DbContext. Se um dia trocar EF Core por Dapper,
// só esta classe muda; Service e Controller continuam iguais.
public class TarefaRepository : ITarefaRepository
{
    private readonly AppDbContext _context;

    public TarefaRepository(AppDbContext context)
    {
        _context = context;
    }

    // SELECT * FROM Tarefas
    public async Task<List<Tarefa>> ListarAsync()
        => await _context.Tarefas.ToListAsync();

    // SELECT TOP 1 * FROM Tarefas WHERE Id = @id  → null se não achar
    // (Exercício de debug: troque FirstOrDefaultAsync por FirstAsync e veja o 500.)
    public async Task<Tarefa?> ObterPorIdAsync(int id)
        => await _context.Tarefas.FirstOrDefaultAsync(t => t.Id == id);

    // INSERT INTO Tarefas ...
    public async Task AdicionarAsync(Tarefa tarefa)
    {
        _context.Tarefas.Add(tarefa);
        await _context.SaveChangesAsync();   // só aqui o SQL é executado de fato
    }

    // UPDATE Tarefas SET ...
    // A tarefa veio do ObterPorIdAsync, então o EF já está "rastreando" ela
    // e sabe quais propriedades mudaram. Basta salvar.
    public async Task AtualizarAsync(Tarefa tarefa)
    {
        await _context.SaveChangesAsync();
    }

    // DELETE FROM Tarefas WHERE Id = ...
    public async Task RemoverAsync(Tarefa tarefa)
    {
        _context.Tarefas.Remove(tarefa);
        await _context.SaveChangesAsync();
    }
}
