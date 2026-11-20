using SgdBaldan.Models.Enums;

namespace SgdBaldan.Infrastructure;

public static class RotulosPerfil
{
    public static string Rotulo(this PerfilUsuario perfil) => perfil switch
    {
        PerfilUsuario.Programador => "Programador",
        PerfilUsuario.Gerente => "Gerente (somente consulta)",
        PerfilUsuario.Projetista => "Projetista",
        PerfilUsuario.Operacional => "Operacional",
        _ => perfil.ToString()
    };

    public static string Rotulo(this StatusUsuario status) => status switch
    {
        StatusUsuario.Pendente => "Pendente",
        StatusUsuario.Ativo => "Ativo",
        StatusUsuario.Recusado => "Recusado",
        StatusUsuario.Inativo => "Inativo",
        _ => status.ToString()
    };
}
