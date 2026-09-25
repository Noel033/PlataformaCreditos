using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<SolicitudCredito> SolicitudesCredito { get; set; } = null!;
    public DbSet<Notificacion> Notificaciones { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Constraints
        builder.Entity<Cliente>(entity =>
        {
            entity.ToTable(t => t.HasCheckConstraint("CK_Cliente_IngresosMensuales", "\"IngresosMensuales\" > 0"));
        });

        builder.Entity<SolicitudCredito>(entity =>
        {
            entity.ToTable(t => t.HasCheckConstraint("CK_SolicitudCredito_MontoSolicitado", "\"MontoSolicitado\" > 0"));
        });

        // Seed Data
        var roleId = "1";
        builder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = roleId, Name = "Analista", NormalizedName = "ANALISTA", ConcurrencyStamp = "STATIC_ROLE_STAMP" }
        );

        var analistaUser = new IdentityUser
        {
            Id = "1",
            UserName = "analista@banco.com",
            NormalizedUserName = "ANALISTA@BANCO.COM",
            Email = "analista@banco.com",
            NormalizedEmail = "ANALISTA@BANCO.COM",
            EmailConfirmed = true,
            PasswordHash = "AQAAAAIAAYagAAAAEAabc1234567890", // Hash estático simulado
            SecurityStamp = "STATIC_SECURITY_STAMP_1",
            ConcurrencyStamp = "STATIC_CONCURRENCY_STAMP_1"
        };
        
        var cliente1User = new IdentityUser 
        { 
            Id = "2", 
            UserName = "cliente1@test.com", 
            NormalizedUserName = "CLIENTE1@TEST.COM", 
            Email = "cliente1@test.com", 
            NormalizedEmail = "CLIENTE1@TEST.COM", 
            EmailConfirmed = true,
            PasswordHash = "AQAAAAIAAYagAAAAEBabc1234567890",
            SecurityStamp = "STATIC_SECURITY_STAMP_2",
            ConcurrencyStamp = "STATIC_CONCURRENCY_STAMP_2"
        };

        var cliente2User = new IdentityUser 
        { 
            Id = "3", 
            UserName = "cliente2@test.com", 
            NormalizedUserName = "CLIENTE2@TEST.COM", 
            Email = "cliente2@test.com", 
            NormalizedEmail = "CLIENTE2@TEST.COM", 
            EmailConfirmed = true,
            PasswordHash = "AQAAAAIAAYagAAAAECabc1234567890",
            SecurityStamp = "STATIC_SECURITY_STAMP_3",
            ConcurrencyStamp = "STATIC_CONCURRENCY_STAMP_3"
        };

        builder.Entity<IdentityUser>().HasData(analistaUser, cliente1User, cliente2User);

        builder.Entity<IdentityUserRole<string>>().HasData(
            new IdentityUserRole<string> { RoleId = roleId, UserId = analistaUser.Id }
        );

        builder.Entity<Cliente>().HasData(
            new Cliente { Id = 1, UsuarioId = cliente1User.Id, IngresosMensuales = 5000m, Activo = true },
            new Cliente { Id = 2, UsuarioId = cliente2User.Id, IngresosMensuales = 8000m, Activo = true }
        );

        builder.Entity<SolicitudCredito>().HasData(
            new SolicitudCredito { Id = 1, ClienteId = 1, MontoSolicitado = 1000m, FechaSolicitud = new DateTime(2023, 10, 1), Estado = EstadoSolicitud.Pendiente },
            new SolicitudCredito { Id = 2, ClienteId = 2, MontoSolicitado = 2500m, FechaSolicitud = new DateTime(2023, 10, 2), Estado = EstadoSolicitud.Aprobado }
        );
    }
}
