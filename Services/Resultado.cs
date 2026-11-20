namespace SgdBaldan.Services;

/// <summary>Resultado de uma operação de negócio (sucesso ou mensagem de erro para o usuário).</summary>
public class Resultado
{
    public bool Sucesso { get; init; }
    /// <summary>Mensagem para o usuário. Várias mensagens são separadas por quebra de linha.</summary>
    public string? Erro { get; init; }

    public static Resultado Ok() => new() { Sucesso = true };
    public static Resultado Falha(string erro) => new() { Sucesso = false, Erro = erro };
}

public class Resultado<T> : Resultado
{
    public T? Valor { get; init; }

    public static Resultado<T> Ok(T valor) => new() { Sucesso = true, Valor = valor };
    public static new Resultado<T> Falha(string erro) => new() { Sucesso = false, Erro = erro };
}
