using Microsoft.AspNetCore.Identity;

namespace PlataformaCreditos.Models;

public class Cliente
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public IdentityUser? Usuario { get; set; }
    public decimal IngresosMensuales { get; set; }
    public bool Activo { get; set; }
}
