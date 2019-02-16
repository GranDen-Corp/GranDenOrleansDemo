using System;
using System.Threading;
using System.Threading.Tasks;
using DemoWebPlayerClient.TypedOptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUlid;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using RpcShared;
using Serilog;

namespace DemoWebPlayerClient.Hubs
{
    public class PlayerClientHub : Hub
    {
        private readonly ClusterInfoOption _clusterInfo;
        private readonly OrleansProviderOption _providerInfo;
        private readonly ILogger<PlayerClientHub> _logger;

        private IClusterClient _orleansClient;

        public PlayerClientHub(
            IOptionsMonitor<ClusterInfoOption> clusterInfoOptions,
            IOptionsMonitor<OrleansProviderOption> providerOptions,
            ILogger<PlayerClientHub> logger)
        {
            _clusterInfo = clusterInfoOptions.CurrentValue;
            _providerInfo = providerOptions.CurrentValue;
            _logger = logger;
        }

        ~PlayerClientHub()
        {
            if (_orleansClient != null)
            {
                _orleansClient.Close().Wait();
                _orleansClient.Dispose();
            }
        }

        public async Task JoinGame(string playerName)
        {
            try
            {
                if (_orleansClient == null)
                {
                    _orleansClient = await StartClientWithRetries();
                }

                var gameHostGrain = _orleansClient.GetGrain<IGameHost>(0);
                var playerAggregateGrain = _orleansClient.GetGrain<IPlayerAggregate>(0);
                var playerData = await playerAggregateGrain.CreateOrGetPlayer(playerName);
                var playerGrain = _orleansClient.GetGrain<IPlayer>(playerData.PlayerId);

                var leaderBoardId = await gameHostGrain.GetCurrentLeaderBoard();
                if (!leaderBoardId.HasValue)
                {
                    throw new Exception("Game not start yet");
                }

                await playerGrain.JoinGame(leaderBoardId.Value);

                await Clients.Caller.SendAsync("DoGameStarted", leaderBoardId.Value);

            }
            catch (Exception ex)
            {
                _logger.Error(400, "Runtime error", ex);
                throw;
            }
        }

        public async Task AddScore(string playerName, int score)
        {
            try
            {
                if (_orleansClient == null)
                {
                    throw new Exception("Not join Game yet");
                }

                var playerAggregateGrain = _orleansClient.GetGrain<IPlayerAggregate>(0);
                var playerData = await playerAggregateGrain.CreateOrGetPlayer(playerName);
                var playerGrain = _orleansClient.GetGrain<IPlayer>(playerData.PlayerId);

                await playerGrain.AddScore(score);
            }
            catch (Exception ex)
            {
                _logger.Error(400, "Runtime error", ex);
                throw;
            }
        }


        private async Task<IClusterClient> StartClientWithRetries()
        {
            var attempt = 0;
            const int initializeAttemptsBeforeFailing = 5;
            const double retryWaitSeconds = 4.0;

            var clientBuilder = new ClientBuilder();

            if (_providerInfo.DefaultProvider == "MongoDB")
            {
                clientBuilder.UseMongoDBClustering(options =>
                    {
                        var mongoSetting = _providerInfo.MongoDB.Cluster;
                        options.ConnectionString = mongoSetting.DbConn;
                        options.DatabaseName = mongoSetting.DbName;

                        // see:https://github.com/OrleansContrib/Orleans.Providers.MongoDB/issues/54
                        options.CollectionPrefix = mongoSetting.CollectionPrefix;
                    })
                    .Configure<ClientMessagingOptions>(options =>
                    {
                        options.ResponseTimeout = TimeSpan.FromSeconds(20);
                        options.ResponseTimeoutWithDebugger = TimeSpan.FromMinutes(60);
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "HelloWorldApp";
                    });
            }

            clientBuilder.ConfigureApplicationParts(manager =>
                {
                    manager.AddApplicationPart(typeof(IPlayerAggregate).Assembly).WithReferences();
                    manager.AddApplicationPart(typeof(IPlayer).Assembly).WithReferences();
                    manager.AddApplicationPart(typeof(IGameHost).Assembly).WithReferences();
                    manager.AddApplicationPart(typeof(ILeaderBoard).Assembly).WithReferences();
                })
                .ConfigureLogging(builder =>
                {
                    builder.AddSerilog(dispose: true);
                });
            var client = clientBuilder.Build();

            await client.Connect(RetryFilter);
            _logger.LogInformation("Player Orleans Client successfully connect to silo host");

            return client;

            #region Orleans Connect Retry Filter
            async Task<bool> RetryFilter(Exception exception)
            {
                if (exception.GetType() != typeof(SiloUnavailableException))
                {
                    _logger.LogError($"Cluster client failed to connect to cluster with unexpected error.  Exception: {exception}");
                    return false;
                }

                attempt++;

                _logger.LogInformation($"Cluster client attempt {attempt} of {initializeAttemptsBeforeFailing} failed to connect to cluster.  Exception: {exception}");

                if (attempt > initializeAttemptsBeforeFailing)
                {
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(retryWaitSeconds));
                return true;
            }
            #endregion
        }

    }
}