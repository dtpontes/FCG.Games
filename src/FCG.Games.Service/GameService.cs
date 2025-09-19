using AutoMapper;
using FCG.Games.Domain.Core.Notifications;
using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces.Commons;
using FCG.Games.Domain.Repositories;
using FCG.Games.Service.DTO.Request;
using FCG.Games.Service.DTO.Response;
using FCG.Games.Service.Interfaces;
using MediatR;

namespace FCG.Games.Service
{
    /// <summary>
    /// Serviço responsável pelas operações de clientes, incluindo cadastro e associação de usuário.
    /// </summary>
    public class GameService : BaseService, IGameService
    {
        private readonly IUnitOfWork _unitOfWork;        
        private readonly IMapper _mapper;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="ClientService"/>.
        /// </summary>
        /// <param name="unitOfWork">Gerenciador de transações e repositórios.</param>
        /// <param name="notifications">Handler de notificações de domínio.</param>
        /// <param name="mediator">Handler de eventos do domínio.</param>        
        /// <param name="mapper">Instância do AutoMapper.</param>
        public GameService(IUnitOfWork unitOfWork,
                            INotificationHandler<DomainNotification> notifications,
                            IMediatorHandler mediator,                            
                            IMapper mapper): base(notifications, mediator) 
        {
            _unitOfWork = unitOfWork;            
            _mapper = mapper;   
        }

        public async Task<GameResponseDto?> CreateGameAsync(GameRequestDto gameRequestDto)
        {
            if (!IsValidTransaction(gameRequestDto))
            {
                return null;
            }

            var game = _mapper.Map<Game>(gameRequestDto);
            await _unitOfWork.Games.AddAsync(game);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<GameResponseDto>(game);
        }

        public async Task<GameResponseDto?> GetGameByIdAsync(long id)
        {
            var game = await _unitOfWork.Games.GetByIdAsync(id);

            return game != null ? _mapper.Map<GameResponseDto>(game) : null;
        }

        public async Task<IEnumerable<GameResponseDto>> GetAllGamesAsync()
        {
            var games = await _unitOfWork.Games.GetAllAsync();

            return _mapper.Map<IEnumerable<GameResponseDto>>(games);
        }

        public async Task<bool?> UpdateGameAsync(long id, GameRequestDto gameRequestDto)
        {
            if (!IsValidTransaction(gameRequestDto))
            {
                return null;
            }

            var game = await _unitOfWork.Games.GetByIdAsync(id);
            if (game == null)
            {
                NotifyError("NotFound", "Game not found.");
                return false;
            }

            _mapper.Map(gameRequestDto, game);
            game.DateUpdate = DateTime.Now;

            _unitOfWork.Games.Update(game);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteGameAsync(long id)
        {
            var game = await _unitOfWork.Games.GetByIdAsync(id);

            if (game == null)
            {
                NotifyError("NotFound", "Game not found.");
                return false;
            }

            _unitOfWork.Games.Delete(game);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

    }
}
