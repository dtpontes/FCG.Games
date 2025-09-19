using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Repositories;
using FCG.Games.Infrastructure.Repositories;

namespace FCG.Games.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;        
        private IRepository<Game>? _games;
        // Add other repositories as needed

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }        

        public IRepository<Game> Games => _games ??= new Repository<Game>(_context);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
