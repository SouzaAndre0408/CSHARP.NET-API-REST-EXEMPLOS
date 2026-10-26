namespace TaskManager.Exemplo.Application.DTOs;

// DTO (Data Transfer Object): o formato do JSON que o cliente envia.
// Não recebemos a entidade Tarefa direto para o cliente não conseguir
// mandar campos que ele não deveria controlar, como Id e CriadaEm.
public class CriarTarefaDto
{
    public string Titulo { get; set; } = string.Empty;
}
