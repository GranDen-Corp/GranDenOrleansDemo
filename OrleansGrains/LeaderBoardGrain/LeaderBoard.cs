using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUlid;
using Orleans;
using RpcShared;

namespace LeaderBoardGrain
{
    public class LeaderBoard : Grain<LeaderBoardState>, ILeaderBoard
    {
        private readonly ILogger<LeaderBoard> _logger;

        public LeaderBoard(ILogger<LeaderBoard> logger)
        {
            _logger = logger;
        }

        public async Task InitLeaderBoard(Ulid boardId, DateTimeOffset startAt, DateTimeOffset endAt)
        {
            State.LeaderBoardId = boardId;
            State.StartTime = startAt;
            State.EndTime = endAt;
            State.LeaderBoardPlayerList = new LinkedList<RankingPlayerInfo>();
            await WriteStateAsync();
            _logger.LogInformation($"LeaderBoard {{{boardId}}} initialized");
        }

        public Task<long> GetPlayerRank(Guid playerId)
        {
            var count = 0L;
            for (var node = State.LeaderBoardPlayerList.First; node != null; node = node.Next, count++)
            {
                if (node.Value.PlayerId == playerId)
                {
                    return Task.FromResult(count);
                }
            }
            throw new Exception($"Player {playerId} is not join the Game");
        }

        public Task<List<RankingPlayerInfo>> TopRankings(int topCount)
        {
            var ret = new List<RankingPlayerInfo>();
            for (var node = State.LeaderBoardPlayerList.First; node != null && topCount > 0; topCount--)
            {
                ret.Add(node.Value);
            }

            return Task.FromResult(ret);
        }

        public Task<List<IPlayerDto>> GetAbove(Guid playerId, int aboveCount)
        {
            var ret = new List<IPlayerDto>();
            for (var node = State.LeaderBoardPlayerList.First; node != null; node = node.Next)
            {
                if (node.Value.PlayerId != playerId) { continue; }
                while (aboveCount > 0)
                {
                    node = node.Previous;
                    if (node == null)
                    {
                        break;
                    }
                    ret.Add(node.Value);
                    aboveCount--;
                }
                break;
            }

            return Task.FromResult(ret);
        }

        public Task<List<IPlayerDto>> GetBelow(Guid playerId, int aboveCount)
        {
            var ret = new List<IPlayerDto>();
            for (var node = State.LeaderBoardPlayerList.First; node != null; node = node.Next)
            {
                if (node.Value.PlayerId != playerId) { continue; }
                while (aboveCount > 0)
                {
                    node = node.Next;
                    if (node == null)
                    {
                        break;
                    }
                    ret.Add(node.Value);
                    aboveCount--;
                }
                break;
            }

            return Task.FromResult(ret);
        }

        public Task UpdatePlayerScore(Guid playerId, ulong score)
        {
            var rankList = State.LeaderBoardPlayerList;
            for (var node = rankList.First; node != null; node = node.Next)
            {
                if (node.Value.PlayerId == playerId)
                {
                    node.Value.Score = score;
                    break;
                }
            }
            State.LeaderBoardPlayerList = new LinkedList<RankingPlayerInfo>(rankList.OrderByDescending(i=>i.Score));
            return Task.CompletedTask;
        }
    }
}