using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using RpcShared;

namespace PlayerGrain
{
    public class PlayerAggregate : Grain<PlayerAggregateState> , IPlayerAggregate
    {
        public async Task<IPlayerDto> CreateOrGetPlayer(string playerName)
        {
            var player = State.Players.FirstOrDefault(p => p.PlayerName == playerName);
            if (player != null)
            {
                return player;
            }
            
            var playerGrain = GrainFactory.GetGrain<IPlayer>(Guid.NewGuid());
            var ret = await playerGrain.CreatePlayer(playerName);
            State.Players.Add(ret);
            await WriteStateAsync();
            return ret;
        }
    }

    public class PlayerAggregateState
    {
        public List<IPlayerDto> Players => new List<IPlayerDto>();
    }
}