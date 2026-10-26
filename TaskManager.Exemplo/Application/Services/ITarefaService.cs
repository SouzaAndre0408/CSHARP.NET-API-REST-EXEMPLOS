using TaskManager.Exemplo.Application.DTOs;
using TaskManager.Exemplo.Domain;

namespace TaskManager.Exemplo.Application.Services;

// Contrato do Service. A Controller depende desta interface, não da classe concreta.
public interface ITarefaService
{
    Task<List<Tarefa>> ListarAsync();
    Task<Tarefa?> ObterPorIdAsync(int id);
    Task<Tarefa> CriarAsync(CriarTarefaDto dto);
    Task<bool> AtualizarAsync(int id, AtualizarTarefaDto dto);   // false = tarefa não encontrada
    Task<bool> RemoverAsync(int id);                             // false = tarefa não encontrada
}
