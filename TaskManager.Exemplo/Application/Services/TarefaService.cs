using TaskManager.Exemplo.Application.DTOs;
using TaskManager.Exemplo.Domain;

namespace TaskManager.Exemplo.Application.Services;

// CAMADA APPLICATION: aqui ficam as REGRAS DE NEGÓCIO e as VALIDAÇÕES.
// O Service não sabe nada de HTTP (isso é da Controller)
// nem de banco de dados (isso é do Repository).
public class TarefaService : ITarefaService
{
    private readonly ITarefaRepository _repository;

    // Injeção de dependência: o repository chega pronto pelo construtor.
    // Quem cria a instância é o ASP.NET, conforme registrado no Program.cs.
    public TarefaService(ITarefaRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Tarefa>> ListarAsync() => _repository.ListarAsync();

    public Task<Tarefa?> ObterPorIdAsync(int id) => _repository.ObterPorIdAsync(id);

    public async Task<Tarefa> CriarAsync(CriarTarefaDto dto)
    {
        ValidarTitulo(dto.Titulo);

        var tarefa = new Tarefa
        {
            Titulo = dto.Titulo.Trim(),
            Concluida = false,
            CriadaEm = DateTime.Now
        };

        await _repository.AdicionarAsync(tarefa);
        return tarefa;   // depois do SaveChanges o EF já preencheu o Id
    }

    public async Task<bool> AtualizarAsync(int id, AtualizarTarefaDto dto)
    {
        ValidarTitulo(dto.Titulo);

        var tarefa = await _repository.ObterPorIdAsync(id);
        if (tarefa is null)
            return false;

        tarefa.Titulo = dto.Titulo.Trim();
        tarefa.Concluida = dto.Concluida;

        await _repository.AtualizarAsync(tarefa);
        return true;
    }

    public async Task<bool> RemoverAsync(int id)
    {
        var tarefa = await _repository.ObterPorIdAsync(id);
        if (tarefa is null)
            return false;

        await _repository.RemoverAsync(tarefa);
        return true;
    }

    // Regra de negócio em um lugar só, usada no POST e no PUT.
    private static void ValidarTitulo(string titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo) || titulo.Trim().Length < 3)
            throw new ValidacaoException("Título obrigatório, com no mínimo 3 caracteres.");
    }
}
