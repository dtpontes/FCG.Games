using FCG.Games.Domain.Core.Notifications;
using FCG.Games.Domain.Handlers;
using FCG.Games.Domain.Interfaces.Commons;
using FCG.Games.Domain.Repositories;
using FCG.Games.Infrastructure;
using FCG.Games.Infrastructure.Repositories;
using FCG.Games.Presentation.BackgroundServices;
using FCG.Games.Presentation.Services;
using FCG.Games.Service;
using FCG.Games.Service.Interfaces;
using GraphQL.AspNet.Configuration;
using MediatR;

namespace FCG.Games.Presentation.Configuration
{
    public static class ServicesConfiguration
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUserService, UserService>();            
            services.AddScoped<IGameService, GameService>();
            services.AddScoped<IStockService, StockService>();
            services.AddScoped<ISaleProcessingService, SaleProcessingService>();
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
            services.AddScoped<IMediatorHandler, MediatorHandler>();
            services.AddScoped<INotificationHandler<DomainNotification>, DomainNotificationHandler>();
            services.AddAutoMapper(typeof(FCG.Games.Service.AutoMapper.AutoMapperProfile).Assembly);
            services.AddGraphQL();
            return services;
        }

        public static IServiceCollection AddServiceBusServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configurar as configurações do Service Bus
            services.Configure<ServiceBusSettings>(configuration.GetSection("ServiceBus"));

            // Registrar o serviço do Service Bus como singleton
            services.AddSingleton<ServiceBusService>();

            // Registrar o background service
            services.AddHostedService<SaleProcessingBackgroundService>();

            return services;
        }
    }
}
