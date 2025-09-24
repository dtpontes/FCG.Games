using FCG.Games.Domain.Entities;

namespace FCG.Games.Domain.Repositories
{
    public interface IUnitOfWork : IDisposable
    {       
        IRepository<Game> Games { get; }
        IRepository<Stock> Stocks { get; }        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
