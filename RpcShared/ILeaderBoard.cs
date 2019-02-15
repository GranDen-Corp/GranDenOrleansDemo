using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUlid;
using Orleans;

namespace RpcShared
{
    public interface ILeaderBoard : IGrainWithGuidKey
    {
        Task InitLeaderBoard(Ulid boardId, DateTimeOffset startAt, DateTimeOffset endAt);

        #region Report Player Score

        Task<long> GetPlayerRank(Guid playerId);

        Task<List<RankingPlayerInfo>> TopRankings(int topCount);

        Task<List<IPlayerDto>> GetAbove(Guid playerId, int aboveCount);

        Task<List<IPlayerDto>> GetBelow(Guid playerId, int aboveCount);

        #endregion

        #region Register Player Data

        Task UpdatePlayerScore(IPlayerDto playerDto, ulong score);

        #endregion
    }
}
