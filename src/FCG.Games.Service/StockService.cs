using AutoMapper;
using FCG.Games.Domain.Core.Notifications;
using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces.Commons;
using FCG.Games.Domain.Repositories;
using FCG.Games.Service.DTO.Request;
using FCG.Games.Service.DTO.Response;
using FCG.Games.Service.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FCG.Games.Service
{
    /// <summary>
    /// Serviço responsável pelas operações de estoque de jogos.
    /// </summary>
    public class StockService : BaseService, IStockService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="StockService"/>.
        /// </summary>
        /// <param name="unitOfWork">Gerenciador de transações e repositórios.</param>
        /// <param name="notifications">Handler de notificações de domínio.</param>
        /// <param name="mediator">Handler de eventos do domínio.</param>
        /// <param name="mapper">Instância do AutoMapper.</param>
        public StockService(IUnitOfWork unitOfWork,
                           INotificationHandler<DomainNotification> notifications,
                           IMediatorHandler mediator,
                           IMapper mapper) : base(notifications, mediator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<StockResponseDto?> AddStockAsync(AddStockRequestDto addStockRequestDto)
        {
            if (!IsValidTransaction(addStockRequestDto))
            {
                return null;
            }

            // Verificar se o jogo existe
            var game = await _unitOfWork.Games.GetByIdAsync(addStockRequestDto.GameId);
            if (game == null)
            {
                NotifyError("GameNotFound", "Jogo não encontrado.");
                return null;
            }

            // Verificar se a quantidade é positiva
            if (addStockRequestDto.Quantity <= 0)
            {
                NotifyError("InvalidQuantity", "A quantidade deve ser maior que zero.");
                return null;
            }

            // Buscar estoque existente ou criar novo
            var existingStock = await _unitOfWork.Stocks
                .GetAllAsync()
                .ContinueWith(task => task.Result.FirstOrDefault(s => s.GameId == addStockRequestDto.GameId));

            if (existingStock != null)
            {
                // Atualizar estoque existente
                existingStock.Quantity += addStockRequestDto.Quantity;
                existingStock.UpdatedAt = DateTime.UtcNow;
                
                await _unitOfWork.SaveChangesAsync();

                // Incluir dados do jogo na resposta
                var stockWithGame = await _unitOfWork.Stocks
                    .GetAllAsync()
                    .ContinueWith(task => task.Result
                        .Where(s => s.Id == existingStock.Id)
                        .Select(s => new StockResponseDto
                        {
                            Id = s.Id,
                            GameId = s.GameId,
                            GameName = s.Game.Name,
                            Quantity = s.Quantity,
                            CreatedAt = s.CreatedAt,
                            UpdatedAt = s.UpdatedAt
                        })
                        .FirstOrDefault());

                return stockWithGame;
            }
            else
            {
                // Criar novo registro de estoque
                var newStock = new Stock
                {
                    GameId = addStockRequestDto.GameId,
                    Quantity = addStockRequestDto.Quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Stocks.AddAsync(newStock);
                await _unitOfWork.SaveChangesAsync();

                return new StockResponseDto
                {
                    Id = newStock.Id,
                    GameId = newStock.GameId,
                    GameName = game.Name,
                    Quantity = newStock.Quantity,
                    CreatedAt = newStock.CreatedAt,
                    UpdatedAt = newStock.UpdatedAt
                };
            }
        }

        public async Task<StockResponseDto?> SubStockAsync(SubStockRequestDto subStockRequestDto)
        {
            if (!IsValidTransaction(subStockRequestDto))
            {
                return null;
            }

            // Verificar se o jogo existe
            var game = await _unitOfWork.Games.GetByIdAsync(subStockRequestDto.GameId);
            if (game == null)
            {
                NotifyError("GameNotFound", "Jogo não encontrado.");
                return null;
            }

            // Verificar se a quantidade é positiva
            if (subStockRequestDto.Quantity <= 0)
            {
                NotifyError("InvalidQuantity", "A quantidade deve ser maior que zero.");
                return null;
            }

            // Buscar estoque existente
            var existingStock = await _unitOfWork.Stocks
                .GetAllAsync()
                .ContinueWith(task => task.Result.FirstOrDefault(s => s.GameId == subStockRequestDto.GameId));

            if (existingStock == null)
            {
                NotifyError("StockNotFound", "Não existe registro de estoque para este jogo.");
                return null;
            }

            // Verificar se há estoque suficiente
            if (existingStock.Quantity < subStockRequestDto.Quantity)
            {
                NotifyError("InsufficientStock", $"Estoque insuficiente. Disponível: {existingStock.Quantity}, Solicitado: {subStockRequestDto.Quantity}.");
                return null;
            }

            // Atualizar estoque
            existingStock.Quantity -= subStockRequestDto.Quantity;
            existingStock.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.SaveChangesAsync();

            return new StockResponseDto
            {
                Id = existingStock.Id,
                GameId = existingStock.GameId,
                GameName = game.Name,
                Quantity = existingStock.Quantity,
                CreatedAt = existingStock.CreatedAt,
                UpdatedAt = existingStock.UpdatedAt
            };
        }

        public async Task<StockResponseDto?> GetStockByGameIdAsync(long gameId)
        {
            // Verificar se o jogo existe
            var game = await _unitOfWork.Games.GetByIdAsync(gameId);
            if (game == null)
            {
                NotifyError("GameNotFound", "Jogo não encontrado.");
                return null;
            }

            var stock = await _unitOfWork.Stocks
                .GetAllAsync()
                .ContinueWith(task => task.Result.FirstOrDefault(s => s.GameId == gameId));

            if (stock == null)
            {
                // Retornar estoque zerado se não existir registro
                return new StockResponseDto
                {
                    Id = 0,
                    GameId = gameId,
                    GameName = game.Name,
                    Quantity = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            return new StockResponseDto
            {
                Id = stock.Id,
                GameId = stock.GameId,
                GameName = game.Name,
                Quantity = stock.Quantity,
                CreatedAt = stock.CreatedAt,
                UpdatedAt = stock.UpdatedAt
            };
        }

        public async Task<IEnumerable<StockResponseDto>> GetAllStocksAsync()
        {
            var stocks = await _unitOfWork.Stocks.GetAllAsync();
            var games = await _unitOfWork.Games.GetAllAsync();

            var stocksWithGames = stocks.Select(stock =>
            {
                var game = games.FirstOrDefault(g => g.Id == stock.GameId);
                return new StockResponseDto
                {
                    Id = stock.Id,
                    GameId = stock.GameId,
                    GameName = game?.Name ?? "Jogo não encontrado",
                    Quantity = stock.Quantity,
                    CreatedAt = stock.CreatedAt,
                    UpdatedAt = stock.UpdatedAt
                };
            });

            return stocksWithGames;
        }
    }
}