namespace LexiCore.Application.Common.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    // using LexiCore.Domain.Entities; // añade los using que necesites

    // Define aquí SOLO lo que Application usa del DbContext:
    public interface IAppDbContext
    {
        // Ejemplos. Reemplaza por tus DbSet reales usados en AuthService/QrService:
        // DbSet<Usuario> Usuarios { get; }
        // DbSet<QrCode> QrCodes { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
