using Microsoft.EntityFrameworkCore;
using TaskManager.Exemplo.Application.Services;
using TaskManager.Exemplo.Domain;
using TaskManager.Exemplo.Infrastructure.Data;
using TaskManager.Exemplo.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Registro dos serviços (injeção de dependência) ----------

builder.Services.AddControllers();

// EF Core + SQL Server, lendo a connection string "Default" do appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// "Quando alguém pedir a interface, entregue esta classe."
// Scoped = uma instância por requisição HTTP (mesmo ciclo de vida do DbContext).
builder.Services.AddScoped<ITarefaRepository, TarefaRepository>();
builder.Services.AddScoped<ITarefaService, TarefaService>();

// Tela do Swagger para testar a API pelo navegador
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---------- 2. Pipeline: por onde cada requisição passa ----------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Só para o exemplo rodar sem comando extra: aplica as migrations pendentes
    // ao subir a API, criando o banco TaskManagerDb_Exemplo se ele não existir.
    // No seu passo a passo você faz isso na mão com "dotnet ef database update".
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
