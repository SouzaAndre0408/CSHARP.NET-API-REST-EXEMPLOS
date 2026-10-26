namespace TaskManager.Exemplo.Application.DTOs;

// DTO do PUT: na atualização o cliente pode mudar o título e marcar como concluída.
public class AtualizarTarefaDto
{
    public string Titulo { get; set; } = string.Empty;
    public bool Concluida { get; set; }
}
