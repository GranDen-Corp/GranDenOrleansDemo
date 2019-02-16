using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUlid;
using Orleans;

namespace RpcShared
{
    public interface IPlayer : IGrainWithGuidKey
    {
        Task<IPlayerDto> CreatePlayer(string playerName);

        Task JoinGame(Ulid leaderBoardId);

        Task AddScore(int amount);
        ValueTask<ulong> CurrentScore();

        ValueTask<long> GetCurrentRank();

        Task<List<IPlayerDto>> GetAboveMe3Players();

        Task<List<IPlayerDto>> GetBelowMe3Players();
    }
}
