namespace TaskManager.Exemplo.Domain;

// CAMADA DOMAIN: o "coração" da aplicação.
// A entidade representa o dado do negócio. O EF Core usa esta classe
// para criar a tabela "Tarefas" (cada propriedade vira uma coluna).
public class Tarefa
{
    public int Id { get; set; }                        // chave primária (o EF reconhece pelo nome "Id")
    public string Titulo { get; set; } = string.Empty;
    public bool Concluida { get; set; }
    public DateTime CriadaEm { get; set; }
}
