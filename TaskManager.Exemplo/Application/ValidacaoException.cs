namespace TaskManager.Exemplo.Application;

// Exceção própria para erros de regra de negócio.
// O Service lança, e a Controller transforma em 400 Bad Request.
public class ValidacaoException : Exception
{
    public ValidacaoException(string mensagem) : base(mensagem) { }
}
