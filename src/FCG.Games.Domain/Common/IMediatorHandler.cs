using FCG.Games.Domain.Core.Events;

namespace FCG.Games.Domain.Interfaces.Commons
{
    public interface IMediatorHandler
    {
        Task PublishEvent<T>(T pEvent) where T : Event;        
    }
}
