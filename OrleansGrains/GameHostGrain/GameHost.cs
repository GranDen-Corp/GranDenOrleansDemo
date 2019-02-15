using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUlid;
using Orleans;
using RpcShared;

namespace GameHostGrain
{
    public class GameHost : Grain<GameHostState>, IGameHost
    {
        private readonly ILogger<GameHost> _logger;

        public GameHost(ILogger<GameHost> logger)
        {
            _logger = logger;
        }

        public async Task<Ulid> OpenLeaderBoard(DateTimeOffset startAt, TimeSpan openDuration)
        {
            var boardId = Ulid.NewUlid(startAt);
            var boardGrain = GrainFactory.GetGrain<ILeaderBoard>(boardId.ToGuid());
            await boardGrain.InitLeaderBoard(boardId, startAt, startAt + openDuration);
            State.CurrentLeaderBoard = boardId;
            await WriteStateAsync();
            _logger.LogInformation($"LeaderBoard {boardId} created");
            return boardId;
        }
    }
}