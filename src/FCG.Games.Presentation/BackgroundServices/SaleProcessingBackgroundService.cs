using FCG.Games.Presentation.Services;

namespace FCG.Games.Presentation.BackgroundServices
{
    /// <summary>
    /// Serviço em background para processar mensagens do Azure Service Bus.
    /// </summary>
    public class SaleProcessingBackgroundService : BackgroundService
    {
        private readonly ServiceBusService _serviceBusService;
        private readonly ILogger<SaleProcessingBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SaleProcessingBackgroundService(
            ServiceBusService serviceBusService,
            ILogger<SaleProcessingBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _serviceBusService = serviceBusService;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Iniciando serviço de processamento de vendas em background");

                // Iniciar o processamento de mensagens
                await _serviceBusService.StartProcessingAsync();
                
                _logger.LogInformation("Serviço de processamento de vendas iniciado com sucesso");

                // Manter o serviço rodando até ser cancelado
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Serviço de processamento de vendas foi cancelado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico no serviço de processamento de vendas");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Parando serviço de processamento de vendas");

            try
            {
                await _serviceBusService.StopProcessingAsync();
                await _serviceBusService.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao parar o serviço de processamento de vendas");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}