using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GameHostGrain;
using LeaderBoardGrain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using PlayerGrain;
using Serilog;
using SiloBackend.TypedOptions;

namespace SiloBackend.HostedServices
{
    public class OrleansSiloHostedService : IHostedService
    {
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly ILogger<OrleansSiloHostedService> _logger;
        private ISiloHost _siloHost;
        private IOptions<SiloConfigOption> _siloOptions;
        private IOptions<OrleansProviderOption> _providerOptions;
        private IOptions<OrleansDashboardOption> _dashboardOptions;

        public OrleansSiloHostedService(IApplicationLifetime applicationLifetime,
            IOptions<SiloConfigOption> siloOptions,
            IOptions<OrleansProviderOption> providerOptions,
            IOptions<OrleansDashboardOption> dashboardOptions,
            ILogger<OrleansSiloHostedService> logger)
        {
            _applicationLifetime = applicationLifetime;
            _siloOptions = siloOptions;
            _providerOptions = providerOptions;
            _dashboardOptions = dashboardOptions;

            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Register .NET Generic Host life time");
            _applicationLifetime.ApplicationStarted.Register(OnApplicationStartedAsync);
            _applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //do nothing
            return Task.CompletedTask;
        }

        private async void OnApplicationStartedAsync()
        {
            _logger.LogInformation("initialize Orleans silo host...");

            _siloHost = CreateSiloHost();
            try
            {
                await _siloHost.StartAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Start up Silo Host failed");
                throw;
            }
        }

        private async void OnApplicationStopping()
        {
            _logger.LogInformation("stopping Orleans silo host...");

            try
            {
                await _siloHost.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when shutdown Silo Host");
                throw;
            }
        }

        private ISiloHost CreateSiloHost()
        {
            var builder = new SiloHostBuilder();

            if (_dashboardOptions.Value.Enable)
            {
                builder.UseDashboard(options =>
                {
                    options.Port = _dashboardOptions.Value.Port;
                });
            }

            builder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = _siloOptions.Value.ClusterId;
                options.ServiceId = _siloOptions.Value.ServiceId;
            });

            if (string.IsNullOrEmpty(_siloOptions.Value.AdvertisedIp) || "*".Equals(_siloOptions.Value.AdvertisedIp))
            {
                builder.ConfigureEndpoints(_siloOptions.Value.SiloPort, _siloOptions.Value.GatewayPort,
                    listenOnAnyHostAddress: _siloOptions.Value.ListenOnAnyHostAddress);
            }
            else
            {
                var ip = IPAddress.Parse(_siloOptions.Value.AdvertisedIp);

                builder.ConfigureEndpoints(ip, _siloOptions.Value.SiloPort, _siloOptions.Value.GatewayPort,
                    listenOnAnyHostAddress: _siloOptions.Value.ListenOnAnyHostAddress);
            }

            if (_providerOptions.Value.DefaultProvider == "MongoDB")
            {
                var mongoDbOption = _providerOptions.Value.MongoDB;
                builder.UseMongoDBClustering(options =>
                {
                    var clusterOption = mongoDbOption.Cluster;

                    options.ConnectionString = clusterOption.DbConn;
                    options.DatabaseName = clusterOption.DbName;

                    // see:https://github.com/OrleansContrib/Orleans.Providers.MongoDB/issues/54
                    options.CollectionPrefix = clusterOption.CollectionPrefix;
                })
                .UseMongoDBReminders(options =>
                {
                    var reminderOption = mongoDbOption.Reminder;

                    options.ConnectionString = reminderOption.DbConn;
                    options.DatabaseName = reminderOption.DbName;

                    if (!string.IsNullOrEmpty(reminderOption.CollectionPrefix))
                    {
                        options.CollectionPrefix = reminderOption.CollectionPrefix;
                    }
                })
                .AddMongoDBGrainStorageAsDefault(options =>
                {
                    var storageOption = mongoDbOption.Storage;

                    options.ConnectionString = storageOption.DbConn;
                    options.DatabaseName = storageOption.DbName;

                    if (!string.IsNullOrEmpty(storageOption.CollectionPrefix))
                    {
                        options.CollectionPrefix = storageOption.CollectionPrefix;
                    }
                });
            }

            builder.ConfigureServices(services =>
                {
                    services
                        .AddLogging(loggingBuilder => loggingBuilder.AddSerilog())
                        .AddTransient<GameHost>()
                        .AddTransient<LeaderBoard>()
                        .AddTransient<Player>();
                })
                .ConfigureApplicationParts(parts => { parts.AddFromApplicationBaseDirectory().WithReferences(); })
                .ConfigureLogging(logging => { logging.AddSerilog(dispose: true); });

            return builder.Build();
        }
    }
}