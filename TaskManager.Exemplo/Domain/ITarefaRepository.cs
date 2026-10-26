namespace TaskManager.Exemplo.Domain;

// REPOSITORY PATTERN: este é o "contrato" de acesso a dados.
// A interface fica no Domain e a implementação fica na Infrastructure.
// Assim o Service depende só da interface e não sabe que existe EF Core ou SQL Server.
public interface ITarefaRepository
{
    Task<List<Tarefa>> ListarAsync();
    Task<Tarefa?> ObterPorIdAsync(int id);   // "?" = pode voltar null (tarefa não encontrada)
    Task AdicionarAsync(Tarefa tarefa);
    Task AtualizarAsync(Tarefa tarefa);
    Task RemoverAsync(Tarefa tarefa);
}
