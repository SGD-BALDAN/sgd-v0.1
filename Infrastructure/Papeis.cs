namespace SgdBaldan.Infrastructure;

/// <summary>Nomes dos papéis (roles) usados em [Authorize(Roles = ...)] e nas views. Iguais aos valores de PerfilUsuario.</summary>
public static class Papeis
{
    public const string Programador = "Programador";
    public const string Gerente = "Gerente";
    public const string Projetista = "Projetista";
    public const string Operacional = "Operacional";
}
