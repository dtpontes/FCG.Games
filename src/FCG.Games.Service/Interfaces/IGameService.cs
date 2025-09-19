using FCG.Games.Domain.Entities;
using FCG.Games.Service.DTO.Request;
using FCG.Games.Service.DTO.Response;

namespace FCG.Games.Service.Interfaces
{
    public interface IGameService
    {
        Task<GameResponseDto?> CreateGameAsync(GameRequestDto gameRequestDto);

        Task<GameResponseDto?> GetGameByIdAsync(long id);

        Task<IEnumerable<GameResponseDto>> GetAllGamesAsync();

        Task<bool?> UpdateGameAsync(long id, GameRequestDto gameRequestDto);

        Task<bool> DeleteGameAsync(long id);



    }
}
