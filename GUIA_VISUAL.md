# Guia visual — entendendo o projeto

Leia na ordem. Cada seção tem um desenho e um exemplo com o código real deste projeto.

1. [O que é o projeto](#1-o-que-é-o-projeto)
2. [Clean Architecture: quem faz o quê](#2-clean-architecture-quem-faz-o-quê)
3. [O caminho de uma requisição](#3-o-caminho-de-uma-requisição-post)
4. [Design patterns e SOLID](#4-design-patterns-e-solid)
5. [Como testar a API](#5-como-testar-a-api)
6. [Como ver o banco](#6-como-ver-o-banco)
7. [Debug passo a passo](#7-debug-passo-a-passo)

---

## 1. O que é o projeto

> **Task Manager API** é uma **API REST** simples de **gerenciamento de tarefas**: criar, listar, buscar, atualizar e remover (CRUD).
>
> Feita em **C# com ASP.NET Core (.NET 10)**, organizada em **Clean Architecture** (Controllers, Application, Domain e Infrastructure), usando **Repository Pattern** e **injeção de dependência**. O acesso a dados é com **Entity Framework Core** e o banco é **SQL Server** (LocalDB).

| Peça | O que é | Neste projeto |
|---|---|---|
| Linguagem | C# | — |
| Framework | ASP.NET Core Web API | .NET 10, com Controllers |
| Arquitetura | Clean Architecture | 4 pastas: `Controllers`, `Application`, `Domain`, `Infrastructure` |
| Design patterns | Repository, Injeção de Dependência, DTO | `ITarefaRepository`, `AddScoped`, `CriarTarefaDto` |
| ORM | Entity Framework Core | traduz C# em SQL |
| Banco | SQL Server LocalDB | banco `TaskManagerDb_Exemplo` |
| Documentação/teste | Swagger | `http://localhost:5192/swagger` |

Os 5 endpoints:

```text
GET    /api/tarefas        → lista todas               → 200
GET    /api/tarefas/1      → busca uma                 → 200 ou 404
POST   /api/tarefas        → cria                      → 201 ou 400
PUT    /api/tarefas/1      → atualiza                  → 204, 400 ou 404
DELETE /api/tarefas/1      → remove                    → 204 ou 404
```

**Versão para falar na entrevista (20 segundos):**

> "Fiz uma API REST de tarefas em .NET, com Clean Architecture. A requisição entra pela controller, a regra de negócio fica no service da camada Application, e o acesso a dados fica isolado num repository na Infrastructure, usando Entity Framework com SQL Server."

---

## 2. Clean Architecture: quem faz o quê

### Analogia: um restaurante

```text
  Cliente faz o pedido
         │
         ▼
┌─────────────────┐
│     GARÇOM      │  Controllers     Recebe o pedido e entrega o prato.
│                 │                  Não cozinha.
└────────┬────────┘
         ▼
┌─────────────────┐
│      CHEF       │  Application     Decide como fazer e confere as regras:
│                 │                  "sem ingrediente vencido".
└────────┬────────┘
         ▼
┌─────────────────┐
│   ESTOQUISTA    │  Infrastructure  Busca e guarda os ingredientes na
│                 │                  despensa (o banco). Só ele mexe lá.
└─────────────────┘

┌─────────────────┐
│ RECEITA/PRATO   │  Domain          O que é o produto: "uma tarefa tem
│                 │                  título, se está concluída, data".
└─────────────────┘                  Todos usam, ele não depende de ninguém.
```

### Cada camada em uma linha

| Camada | Responsável por | Arquivos aqui | NÃO pode |
|---|---|---|---|
| **Controllers** (API) | Receber HTTP e devolver status code | `TarefasController.cs` | ter regra de negócio ou acessar o banco |
| **Application** | Regras de negócio e validações | `TarefaService.cs`, DTOs, `ValidacaoException.cs` | saber de HTTP ou de SQL |
| **Domain** | Entidades e contratos (interfaces) | `Tarefa.cs`, `ITarefaRepository.cs` | depender de qualquer outra camada |
| **Infrastructure** | Falar com o mundo externo (banco, APIs, arquivos) | `AppDbContext.cs`, `TarefaRepository.cs` | ter regra de negócio |

### Quem conhece quem

As setas apontam para **dentro**. O Domain fica no centro e não conhece ninguém:

```text
 Controllers ───► Application ───► Domain ◄─── Infrastructure
                                     ▲
                       a interface   │   a implementação
                       ITarefaRepository está aqui...    ...TarefaRepository está lá
```

Por isso a interface do repository fica no **Domain** e a implementação na **Infrastructure**. O Service usa a interface e nunca sabe que existe SQL Server. O `Program.cs` é quem liga tudo.

### "Onde eu coloco isso?"

| Demanda | Camada |
|---|---|
| "Não pode cadastrar tarefa com título repetido" | **Application** (regra no `TarefaService`) |
| "Toda tarefa agora tem Prioridade" | **Domain** (propriedade na `Tarefa`) + migration |
| "Criar endpoint para listar só as concluídas" | **Controllers** (endpoint) + **Application** + **Infrastructure** (consulta) |
| "A consulta está lenta" | **Infrastructure** (`TarefaRepository`) |
| "Mudar o formato do JSON que o cliente envia" | **Application** (DTO) |

---

## 3. O caminho de uma requisição (POST)

O cliente envia:

```http
POST /api/tarefas
{ "titulo": "Estudar EF Core" }
```

O que acontece, camada por camada:

```text
┌─ 1. CONTROLLER ── Controllers/TarefasController.cs → Criar(dto) ─────────────┐
│  O ASP.NET transforma o JSON em CriarTarefaDto { Titulo = "Estudar EF Core" } │
│  e chama o service:   var tarefa = await _service.CriarAsync(dto);           │
└──────────────────────────────────────┬───────────────────────────────────────┘
                                       ▼
┌─ 2. SERVICE ───── Application/Services/TarefaService.cs → CriarAsync(dto) ────┐
│  ValidarTitulo("Estudar EF Core")  → ok, tem 3+ caracteres                    │
│  (se fosse "", lançaria ValidacaoException e a controller devolveria 400)    │
│  Monta a entidade: new Tarefa { Titulo = "...", CriadaEm = agora }  Id = 0   │
│  Chama:   await _repository.AdicionarAsync(tarefa);                          │
└──────────────────────────────────────┬───────────────────────────────────────┘
                                       ▼
┌─ 3. REPOSITORY ── Infrastructure/Repositories/TarefaRepository.cs ───────────┐
│  _context.Tarefas.Add(tarefa);      → só marca "isso vai ser inserido"       │
│  await _context.SaveChangesAsync(); → AGORA o EF executa o SQL               │
└──────────────────────────────────────┬───────────────────────────────────────┘
                                       ▼
┌─ 4. BANCO ─────── SQL Server, banco TaskManagerDb_Exemplo ────────────────────┐
│  INSERT INTO [Tarefas] ([Concluida], [CriadaEm], [Titulo])                   │
│  OUTPUT INSERTED.[Id]  VALUES (@p0, @p1, @p2);                               │
│  O banco gera o Id = 1 e o EF coloca na entidade:  tarefa.Id = 1             │
└──────────────────────────────────────┬───────────────────────────────────────┘
                                       ▼
            volta: Repository → Service (return tarefa) → Controller
                                       ▼
┌─ 5. RESPOSTA ─────────────────────────────────────────────────────────────────┐
│  return CreatedAtAction(...)  →  201 Created                                 │
│  { "id": 1, "titulo": "Estudar EF Core", "concluida": false, "criadaEm": ... }│
└──────────────────────────────────────────────────────────────────────────────┘
```

**Para gravar:** *"Entra pela controller, valida no service, grava pelo repository, volta com o status code."*

Os outros caminhos, resumidos:

```text
POST com título ""   → Controller → Service: ValidarTitulo lança ValidacaoException
                       → Controller: catch → 400 Bad Request
                         (não chega no repository, nem no banco)

GET /api/tarefas/999 → Controller → Service → Repository: FirstOrDefaultAsync → null
                       → Service devolve null → Controller: NotFound() → 404
```

### Quem devolve a resposta? Quase sempre, a controller

A resposta sai **da API para o cliente** (Swagger, front-end, Postman).

**No caminho normal é sempre a controller.** O service e o repository só devolvem dados; quem transforma isso em HTTP é a controller:

```text
Repository  →  devolve Tarefa ou null           (não sabe o que é HTTP)
Service     →  devolve Tarefa, null, true/false (não sabe o que é HTTP)
Controller  →  transforma em HTTP:
               tarefa is null ? NotFound() : Ok(tarefa)
                                  │              │
                                 404            200
        ↓
ASP.NET     →  converte o objeto em JSON e envia ao cliente
```

**Há três casos em que a resposta sai sem passar pela controller:**

| Situação | Quem responde | Status |
|---|---|---|
| A URL não existe (ex.: `/api/xyz`) | O roteamento do ASP.NET: nenhuma controller é encontrada | 404 |
| O JSON está quebrado (ex.: `{ "titulo": `) | O `[ApiController]`, antes de entrar no método | 400 automático |
| Uma exceção que ninguém tratou (o bug do `FirstAsync`) | O middleware de erro do ASP.NET | 500 |

O terceiro caso é o do [Roteiro 3](#roteiro-3-achar-um-bug-simulando-um-chamado). A exceção "fura" a controller, porque não tem `catch` para ela.

**Para a entrevista:** *"A controller é a porta de entrada e de saída: recebe a requisição e decide o status code. Service e repository não sabem nada de HTTP."*

---

## 4. Design patterns e SOLID

### Os que estão neste projeto

| Pattern | O que é, em uma frase | Onde está |
|---|---|---|
| **Repository** | Abstração do acesso a dados: o service usa uma interface e não o banco direto | `ITarefaRepository` + `TarefaRepository` |
| **Injeção de Dependência** | A classe recebe o que precisa pelo construtor em vez de criar com `new` | Construtores + `AddScoped` no `Program.cs` |
| **DTO** | Objeto só para transportar dados de entrada/saída, separado da entidade | `CriarTarefaDto`, `AtualizarTarefaDto` |

Repository em desenho:

```text
SEM repository                          COM repository
──────────────                          ──────────────
Service ──► DbContext ──► Banco          Service ──► ITarefaRepository (interface)
                                                         ▲
(service preso ao EF e ao SQL)                           │ implementa
                                                    TarefaRepository ──► DbContext ──► Banco

                                         (trocar EF por Dapper = mudar só o TarefaRepository)
```

### Injeção de Dependência em detalhe

**A ideia em uma frase: a classe não cria o que precisa. Ela recebe pronto.**

**Sem injeção de dependência:**

```csharp
public class TarefaService
{
    private readonly TarefaRepository _repository;

    public TarefaService()
    {
        // o service CRIA o repository sozinho
        var context = new AppDbContext(/* e a connection string? */);
        _repository = new TarefaRepository(context);
    }
}
```

Problemas:
- **O service fica preso** ao `TarefaRepository` concreto. Para trocar a implementação, tem que mexer no service.
- **O service precisa saber montar** o `AppDbContext` e a connection string, o que não é responsabilidade dele.
- **Não dá para testar** com um repository falso (mock).

**Com injeção de dependência (como está no projeto):**

```csharp
public class TarefaService
{
    private readonly ITarefaRepository _repository;

    // "Eu preciso de um ITarefaRepository. Alguém me entregue um."
    public TarefaService(ITarefaRepository repository)
    {
        _repository = repository;
    }
}
```

O service só **declara** o que precisa, no construtor, e pede uma **interface**. Não sabe qual classe vai chegar nem como ela foi criada.

**Quem entrega? O "container" do ASP.NET.** No `Program.cs` você monta um **cardápio**: "quando pedirem X, entregue Y".

```csharp
builder.Services.AddDbContext<AppDbContext>(...);                       // pediram AppDbContext → crio com a connection string
builder.Services.AddScoped<ITarefaRepository, TarefaRepository>();      // pediram ITarefaRepository → entrego TarefaRepository
builder.Services.AddScoped<ITarefaService, TarefaService>();            // pediram ITarefaService → entrego TarefaService
```

**O que acontece a cada requisição:** o ASP.NET monta a corrente sozinho, de trás para frente.

```text
"Preciso criar a TarefasController"
   └─ o construtor dela pede ITarefaService
        └─ cardápio: ITarefaService → TarefaService. Vou criar
             └─ o construtor dele pede ITarefaRepository
                  └─ cardápio: ITarefaRepository → TarefaRepository. Vou criar
                       └─ o construtor dele pede AppDbContext
                            └─ cardápio: AppDbContext → crio com a connection string ✔

Resultado (você nunca escreveu nenhum "new"):
   TarefasController( TarefaService( TarefaRepository( AppDbContext ) ) )
```

**Os 3 tempos de vida:**

| Registro | Tempo de vida | Exemplo de uso |
|---|---|---|
| `AddTransient` | Uma nova **toda vez** que alguém pede | Classes leves, sem estado |
| `AddScoped` | **Uma por requisição** HTTP, compartilhada nela | Service, repository, `DbContext` ← **este projeto** |
| `AddSingleton` | **Uma só** para a aplicação inteira, enquanto ela estiver rodando | Cache, configurações |

O `DbContext` é scoped (o `AddDbContext` já registra assim). Por isso o service e o repository também são scoped: tudo dentro de uma requisição usa o mesmo `DbContext`.

**Por que é bom: trocar uma linha troca o comportamento.**

```csharp
// hoje
builder.Services.AddScoped<ITarefaRepository, TarefaRepository>();         // EF Core + SQL Server

// amanhã, se migrar para Dapper:
builder.Services.AddScoped<ITarefaRepository, TarefaRepositoryDapper>();   // só muda ESTA linha
```

O service e a controller não mudam. No teste unitário é a mesma ideia: você entrega um repository falso (mock), sem precisar de banco.

**Se esquecer de registrar**, a API dá este erro na primeira requisição:

```text
Unable to resolve service for type 'ITarefaService'
while attempting to activate 'TarefasController'.
```

Tradução: *"a controller pediu um `ITarefaService` e ele não está no cardápio"*. A solução é registrar no `Program.cs`. Esse erro é muito comum no dia a dia.

**Para a entrevista:** *"Injeção de dependência é a classe receber suas dependências pelo construtor em vez de criá-las com `new`. Eu registro no `Program.cs` qual implementação atende cada interface, e o container do ASP.NET monta tudo a cada requisição. Isso deixa o código desacoplado e fácil de testar com mock."*

### SOLID (são princípios, não patterns)

| Letra | Princípio | Em uma frase | Exemplo aqui |
|---|---|---|---|
| **S** | Single Responsibility | Cada classe tem um motivo para mudar | Service só regra, repository só banco |
| **O** | Open/Closed | Aberto para estender, fechado para modificar | Nova implementação da interface sem mexer no service |
| **L** | Liskov Substitution | Qualquer implementação pode substituir outra | Qualquer `ITarefaRepository` funciona no service |
| **I** | Interface Segregation | Interfaces pequenas e específicas | `ITarefaRepository` só tem métodos de tarefa |
| **D** | Dependency Inversion | Depender de abstração, não de implementação | Service recebe `ITarefaRepository`, não `TarefaRepository` |

Se perguntarem, **S** e **D** bastam. São os que você consegue mostrar no projeto.

### Outros patterns comuns em .NET (só para conhecer o nome)

| Pattern | Em uma frase | Onde aparece no dia a dia |
|---|---|---|
| **Unit of Work** | Agrupa várias operações para salvar tudo de uma vez | O `DbContext` já é um: o `SaveChanges` salva tudo junto |
| **Singleton** | Uma única instância para a aplicação inteira | `AddSingleton` no `Program.cs` |
| **Factory** | Uma classe responsável por criar objetos | `IHttpClientFactory` |
| **Builder** | Monta um objeto passo a passo | `WebApplication.CreateBuilder(args)` |
| **Strategy** | Troca o algoritmo por meio de uma interface | Calcular frete diferente por transportadora |
| **Adapter** | Adapta uma interface externa para a sua | Integração com API de terceiros (TOTVS, por exemplo) |
| **Decorator** | Adiciona comportamento "envolvendo" uma classe | Cache em volta de um repository |
| **Observer** | Avisa vários interessados quando algo acontece | Eventos, mensageria (RabbitMQ) |
| **Mediator** | Um intermediário entrega o pedido para quem trata | Biblioteca MediatR, comum junto com CQRS |
| **CQRS** | Separa leitura (Query) de escrita (Command) | Projetos maiores com MediatR |
| **Options** | Configurações do `appsettings.json` em classes tipadas | `IOptions<MinhasConfiguracoes>` |

---

## 5. Como testar a API

O **Swagger basta para estudar e para a entrevista**. Nas empresas, o mais comum é Postman. Pergunte ao time qual usam.

| Ferramenta | Onde roda | Vantagem | Quando usar |
|---|---|---|---|
| **Swagger** | Navegador, já vem na API | Zero instalação, mostra todos os endpoints | Agora, para estudar |
| **Arquivo `.http`** | Visual Studio / VS Code | As requisições ficam salvas no projeto | Já tem `TaskManager.Exemplo.http` pronto |
| **Thunder Client** | Extensão do VS Code | **Já está instalada no seu VS Code** | Se preferir testar pelo VS Code |
| **Postman** | Programa separado | O mais usado no mercado | Quando o time usar |
| **Insomnia** | Programa separado | Parecido com o Postman, mais leve | Alternativa ao Postman |
| **Bruno** | Programa separado | Coleções salvas em arquivos, fáceis de pôr no Git | Alternativa open source |

Todos fazem a mesma coisa: montar a requisição (verbo + URL + JSON) e mostrar a resposta (status + JSON). Aprendeu um, sabe todos.

### Testando no Swagger

```text
1. F5 no Visual Studio → abre http://localhost:5192/swagger
2. Clique em  POST /api/tarefas
3. Clique em  [Try it out]
4. No corpo, escreva:  { "titulo": "Estudar EF Core" }
5. Clique em  [Execute]
6. Role até "Responses": Code 201 e o JSON com o id
```

---

## 6. Como ver o banco

### Como saber qual banco a API está usando

Siga o fio, do `appsettings.json` até a tabela:

```text
appsettings.json
  "Default": "Server=(localdb)\\MSSQLLocalDB;Database=TaskManagerDb_Exemplo;Trusted_Connection=True;TrustServerCertificate=True"
        │
        ▼
Program.cs
  options.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
        │                                                        └── lê a "Default" acima
        ▼
AppDbContext.cs
  DbSet<Tarefa> Tarefas   ──►  tabela  dbo.Tarefas
```

A connection string, pedaço por pedaço:

| Pedaço | Significa |
|---|---|
| `Server=(localdb)\\MSSQLLocalDB` | **Qual servidor**: o SQL Server LocalDB da sua máquina (no JSON a `\` é escrita como `\\`) |
| `Database=TaskManagerDb_Exemplo` | **Qual banco** dentro do servidor |
| `Trusted_Connection=True` | Entra com o seu usuário do Windows, sem senha |
| `TrustServerCertificate=True` | Aceita o certificado local, senão dá erro de SSL |

**Quer mudar de banco? Muda só esta linha.** Em projeto real existe uma connection string por ambiente (dev, homologação, produção).

### Quando o banco e a tabela são criados

```text
Tarefa.cs (classe C#)
     │   dotnet ef migrations add Inicial      ← gera o "roteiro" em C#
     ▼
Migrations/..._Inicial.cs                      ← abra e veja o CreateTable("Tarefas")
     │   dotnet ef database update             ← no SEU projeto, na mão
     │   ou Database.Migrate() no Program.cs   ← neste exemplo, automático no F5
     ▼
SQL Server: CREATE DATABASE + CREATE TABLE Tarefas + tabela __EFMigrationsHistory
```

A tabela `__EFMigrationsHistory` é onde o EF anota quais migrations já rodaram, para não rodar de novo.

### Ver na prática, de 3 jeitos

**Jeito 1: o console da API (o mais rápido).** O EF escreve no console cada SQL que executa. Faça um POST no Swagger e olhe a janela do console:

```text
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (2ms) [Parameters=[...], CommandType='Text', CommandTimeout='30']
      INSERT INTO [Tarefas] ([Concluida], [CriadaEm], [Titulo])
      OUTPUT INSERTED.[Id]
      VALUES (@p0, @p1, @p2);
```

Na primeira execução aparecem também o `CREATE DATABASE [TaskManagerDb_Exemplo]` e o `CREATE TABLE`.

**Jeito 2: SSMS (você já tem o SSMS 22).**

```text
1. Abra o SQL Server Management Studio
2. Na tela de conexão:
     Server name:     (localdb)\MSSQLLocalDB        ← aqui com UMA barra
     Authentication:  Windows Authentication
     ☑ Trust server certificate
   → Connect
3. No Object Explorer (lado esquerdo):
     (localdb)\MSSQLLocalDB
       └─ Databases
            └─ TaskManagerDb_Exemplo
                 └─ Tables
                      ├─ dbo.__EFMigrationsHistory
                      └─ dbo.Tarefas   ← botão direito → "Select Top 1000 Rows"
```

Ou abra *New Query* e rode:

```sql
USE TaskManagerDb_Exemplo;
SELECT * FROM Tarefas;
SELECT * FROM __EFMigrationsHistory;
```

**Jeito 3: dentro do próprio Visual Studio, sem abrir outro programa.**

```text
Menu Exibir → Pesquisador de Objetos do SQL Server
(em inglês: View → SQL Server Object Explorer)
  SQL Server → (localdb)\MSSQLLocalDB → Bancos de Dados → TaskManagerDb_Exemplo → Tabelas
  → botão direito em dbo.Tarefas → Exibir Dados
```

### Exercício: ver a API alterando o banco

```text
1. F5 na API
2. SSMS:     SELECT * FROM Tarefas                  → vazio
3. Swagger:  POST { "titulo": "Estudar EF Core" }   → 201
4. Console:  apareceu o INSERT INTO [Tarefas]
5. SSMS:     SELECT * FROM Tarefas                  → 1 linha, Id = 1
6. Swagger:  PUT /api/tarefas/1 { "titulo": "Estudar LINQ", "concluida": true } → 204
7. SSMS:     SELECT de novo                         → Titulo e Concluida mudaram
8. Swagger:  DELETE /api/tarefas/1                  → 204
9. SSMS:     SELECT de novo                         → vazio
```

---

## 7. Debug passo a passo

### Atalhos do Visual Studio

| Tecla | Nome | O que faz |
|---|---|---|
| **F9** | Breakpoint | Liga/desliga uma bolinha vermelha na linha; a execução para ali |
| **F5** | Continue | Roda a API, ou continua até o próximo breakpoint |
| **F10** | Step Over | Executa a linha e vai para a próxima **sem entrar** no método |
| **F11** | Step Into | **Entra** no método chamado naquela linha |
| **Shift + F11** | Step Out | Termina o método atual e volta para quem chamou |
| **Shift + F5** | Stop | Para o debug |

### Janelas para olhar (Menu Depurar → Janelas)

| Janela (pt-BR / inglês) | Mostra |
|---|---|
| **Locais / Locals** | Todas as variáveis do método atual e seus valores |
| **Inspeção / Watch** | Variáveis que você escolhe acompanhar (ex.: `tarefa.Id`) |
| **Pilha de Chamadas / Call Stack** | O caminho até aqui: quem chamou quem |

Dica: passe o mouse em cima de qualquer variável para ver o valor.

### Roteiro 1: seguir um POST até o banco

**Preparação:** coloque **um breakpoint só**, com **F9**, em `Controllers/TarefasController.cs`, no método `Criar`, nesta linha:

```csharp
var tarefa = await _service.CriarAsync(dto);   // ← F9 aqui
```

**Execute:** F5 → no Swagger, `POST /api/tarefas` com `{ "titulo": "Estudar EF Core" }` → Execute. O Visual Studio vem para a frente, parado na linha.

| # | Você está em | Faça | Observe |
|---|---|---|---|
| 1 | `TarefasController.Criar` | Passe o mouse em `dto` | `Titulo = "Estudar EF Core"`: o JSON virou objeto C# |
| 2 | mesma linha | **F11** | Entrou em `TarefaService.CriarAsync` |
| 3 | `ValidarTitulo(dto.Titulo)` | **F11** | Entrou na validação. Veja o `titulo` em **Locais** |
| 4 | dentro de `ValidarTitulo` | **Shift + F11** | Passou na validação e voltou para o `CriarAsync` |
| 5 | `var tarefa = new Tarefa {...}` | **F10** | Em **Locais**, `tarefa.Id = 0`: ainda não foi para o banco |
| 6 | `await _repository.AdicionarAsync(tarefa)` | **F11** | Entrou em `TarefaRepository.AdicionarAsync` |
| 7 | `_context.Tarefas.Add(tarefa)` | **F10** | Ainda `Id = 0`. O `Add` só marca, não grava |
| 8 | `await _context.SaveChangesAsync()` | **F10** | **Olhe o console: apareceu o `INSERT`**. E agora `tarefa.Id = 1` |
| 9 | qualquer linha | Abra a **Pilha de Chamadas** | `TarefaRepository` ← `TarefaService` ← `TarefasController`: o caminho inteiro |
| 10 | — | **Shift + F11** duas vezes | Voltou para o service e depois para a controller, com `tarefa` preenchida |
| 11 | — | **F5** | Continua. No Swagger aparece o **201** |

Esse roteiro é o desenho da [seção 3](#3-o-caminho-de-uma-requisição-post) acontecendo de verdade.

### Roteiro 2: o caminho do erro de validação (400)

```text
1. Mantenha o breakpoint na controller e ponha outro (F9) na linha do catch:
      return BadRequest(ex.Message);
2. Swagger: POST { "titulo": "" }
3. F11 → entra no CriarAsync → F11 → entra no ValidarTitulo
4. F10 → o "if" é verdadeiro → a próxima linha é o  throw new ValidacaoException(...)
5. F5 → a exceção "sobe" e para no breakpoint do catch, na controller
6. Passe o mouse em  ex.Message  → "Título obrigatório..."
7. F5 → Swagger mostra 400
   Repare: o repository NUNCA foi chamado. Não houve SQL no console.
```

### Roteiro 3: achar um bug (simulando um chamado)

> **Chamado:** "Buscar uma tarefa que não existe retorna 500. Deveria retornar 404."

```text
1. Crie o bug: em TarefaRepository.ObterPorIdAsync, troque
      FirstOrDefaultAsync   por   FirstAsync
2. Shift + F5 para parar, e F5 para rodar de novo
3. Swagger: GET /api/tarefas/999 → 500
4. Olhe o console: a exceção e a stack trace
      InvalidOperationException: Sequence contains no elements
      at ... TarefaRepository.ObterPorIdAsync ...   ← o arquivo e a linha do problema
5. F9 na linha do  FirstAsync , repita o GET e F10:
      o Visual Studio normalmente para mostrando a mesma exceção ali
6. Entenda: FirstAsync   → exige que exista; se não existir, EXCEÇÃO (500)
            FirstOrDefaultAsync → se não existir, devolve null (a controller vira 404)
7. Desfaça a troca, rode de novo, GET 999 → 404 ✔
```

**Para gravar:** *reproduzir → ler o log → breakpoint → achar a linha → entender → corrigir → testar de novo.* Esse é o fluxo de sustentação.
