using Microsoft.AspNetCore.Mvc;
using TaskManager.Exemplo.Application;
using TaskManager.Exemplo.Application.DTOs;
using TaskManager.Exemplo.Application.Services;
using TaskManager.Exemplo.Domain;

namespace TaskManager.Exemplo.Controllers;

// CAMADA API: a porta de entrada. A Controller só cuida de HTTP:
// recebe a requisição, chama o Service e escolhe o status code da resposta.
// Ela NÃO tem regra de negócio e NÃO acessa o banco.
[ApiController]
[Route("api/[controller]")]   // [controller] = nome da classe sem "Controller" → /api/tarefas
public class TarefasController : ControllerBase
{
    private readonly ITarefaService _service;

    public TarefasController(ITarefaService service)
    {
        _service = service;
    }

    // GET /api/tarefas → 200
    [HttpGet]
    public async Task<ActionResult<List<Tarefa>>> Listar()
    {
        var tarefas = await _service.ListarAsync();
        return Ok(tarefas);
    }

    // GET /api/tarefas/5 → 200 ou 404
    [HttpGet("{id}")]
    public async Task<ActionResult<Tarefa>> ObterPorId(int id)
    {
        var tarefa = await _service.ObterPorIdAsync(id);
        return tarefa is null ? NotFound() : Ok(tarefa);
    }

    // POST /api/tarefas → 201 ou 400
    [HttpPost]
    public async Task<ActionResult<Tarefa>> Criar(CriarTarefaDto dto)
    {
        try
        {
            var tarefa = await _service.CriarAsync(dto);
            // 201 Created + header "Location" apontando para GET /api/tarefas/{id}
            return CreatedAtAction(nameof(ObterPorId), new { id = tarefa.Id }, tarefa);
        }
        catch (ValidacaoException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // PUT /api/tarefas/5 → 204, 404 ou 400
    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(int id, AtualizarTarefaDto dto)
    {
        try
        {
            var atualizou = await _service.AtualizarAsync(id, dto);
            return atualizou ? NoContent() : NotFound();
        }
        catch (ValidacaoException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // DELETE /api/tarefas/5 → 204 ou 404
    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(int id)
    {
        var removeu = await _service.RemoverAsync(id);
        return removeu ? NoContent() : NotFound();
    }
}
