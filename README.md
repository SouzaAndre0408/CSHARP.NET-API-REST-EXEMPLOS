# Exemplo pronto — Task Manager API

Esta é a versão **pronta** da API do roteiro (`../TODO_TaskManager_API_DotNet_1_Dia.md`). Serve para você rodar, ver funcionando e entender. Depois, feche e crie a sua do zero no passo a passo.

Usa um banco próprio, o `TaskManagerDb_Exemplo`, então não interfere no projeto que você vai criar.

> 🏢 **O código real da Smart:** a pasta [smart-unidade-medida/](smart-unidade-medida/README.md) tem o cadastro de Unidade de Medida do SGO, front e back, para ler e comparar com este exemplo.

> 👀 **Comece pelo [GUIA_VISUAL.md](GUIA_VISUAL.md):** o que é o projeto, o que faz cada camada, o caminho de uma requisição desenhado, os design patterns, como testar, como ver o banco e o debug passo a passo.

## Como rodar

**No Visual Studio** (recomendado):

1. Abra `TaskManager.Exemplo.slnx`
2. Aperte **F5**
3. O navegador abre no Swagger (`http://localhost:5192/swagger`)

**Pelo terminal:**

```bash
dotnet run --project TaskManager.Exemplo
```

Depois abra `http://localhost:5192/swagger`.

Na primeira execução a API cria o banco sozinha (veja o final do `Program.cs`). Não precisa rodar `dotnet ef database update`.

## Como testar

- **Swagger:** clique num endpoint → *Try it out* → *Execute*
- **Arquivo `.http`:** abra `TaskManager.Exemplo.http` no Visual Studio e clique em *Send request* acima de cada requisição. Já tem todos os casos: sucesso, 400 e 404.
- **Banco:** no SSMS, conecte em `(localdb)\MSSQLLocalDB` e rode `SELECT * FROM TaskManagerDb_Exemplo.dbo.Tarefas`

## Ordem de leitura do código

Siga o caminho de uma requisição, de fora para dentro:

| # | Arquivo | Camada | O que observar |
|---|---|---|---|
| 1 | `Controllers/TarefasController.cs` | API | Só HTTP: recebe, chama o service e escolhe o status code |
| 2 | `Application/DTOs/` | Application | O formato do JSON que chega |
| 3 | `Application/Services/TarefaService.cs` | Application | Regras de negócio e validação (`ValidarTitulo`) |
| 4 | `Domain/ITarefaRepository.cs` | Domain | O contrato que o service usa |
| 5 | `Infrastructure/Repositories/TarefaRepository.cs` | Infrastructure | O único lugar que fala com o banco |
| 6 | `Infrastructure/Data/AppDbContext.cs` | Infrastructure | A ponte C# ↔ SQL Server |
| 7 | `Domain/Tarefa.cs` | Domain | A entidade que vira tabela |
| 8 | `Program.cs` | — | Onde tudo é ligado (injeção de dependência) |
| 9 | `Migrations/` | — | O `CREATE TABLE` que o EF gerou a partir da entidade |

## Exercícios com este projeto

1. **Siga um POST com o debugger.** Breakpoint (**F9**) no `Criar` da controller, rode com **F5**, faça um POST pelo Swagger e vá com **F11** entrando em cada camada até o `SaveChangesAsync`.
2. **Provoque o bug do 500.** No `TarefaRepository.ObterPorIdAsync`, troque `FirstOrDefaultAsync` por `FirstAsync`. Chame `GET /api/tarefas/999` e veja o 500. Ache a linha pela **Call Stack** e desfaça.
3. **Explique em voz alta** o caminho do POST, como faria na entrevista: controller → service (validação) → repository → banco → volta com 201.

Quando conseguir fazer os três, crie o seu projeto pelo passo a passo **sem olhar este**. Use este só quando travar.
