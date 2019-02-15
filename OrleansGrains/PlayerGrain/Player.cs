using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUlid;
using Orleans;
using RpcShared;

namespace PlayerGrain
{
    public class Player : Grain<PlayerState>, IPlayer
    {
        private readonly ILogger<Player> _logger;
        private ILeaderBoard _leaderBoardGrain;

        public Player(ILogger<Player> logger)
        {
            _logger = logger;
        }

        public async Task<IPlayerDto> CreatePlayer(string playerName)
        {
            State.PlayerId = Guid.NewGuid();
            State.PlayerName = playerName;
            State.Score = 0L;
            await WriteStateAsync();
            _logger.LogInformation($"PlayerId {{{State.PlayerId}:{State.PlayerName}}} created");
            return State;
        }

        public async Task<bool> JoinGame(Ulid leaderBoardId)
        {
            _leaderBoardGrain = GrainFactory.GetGrain<ILeaderBoard>(leaderBoardId.ToGuid());
            State.CurrentJoinedGame = leaderBoardId;
            await WriteStateAsync();
            return true;
        }

        public async Task AddScore(int amount)
        {
            if (_leaderBoardGrain == null)
            {
                throw new Exception("Not join any game(s) yet.");
            }

            var currentScore = State.Score;
            if (amount >= 0)
            {
                currentScore += (uint)amount;
            }
            else
            {
                currentScore -= (uint)Math.Abs(amount);
            }
            State.Score = currentScore;

            await _leaderBoardGrain.UpdatePlayerScore(State.PlayerId, currentScore);
            await WriteStateAsync();
        }

        public Task<ulong> CurrentScore()
        {
            return Task.FromResult(State.Score);
        }

        public async Task<long> GetCurrentRank()
        {
            if (_leaderBoardGrain == null)
            {
                throw new Exception("Not join any game(s) yet.");
            }

            return await _leaderBoardGrain.GetPlayerRank(State.PlayerId);
        }

        public async Task<List<IPlayerDto>> GetAboveMe3Players()
        {
            if (_leaderBoardGrain == null)
            {
                throw new Exception("Not join any game(s) yet.");
            }

            return await _leaderBoardGrain.GetAbove(State.PlayerId, 3);
        }

        public async Task<List<IPlayerDto>> GetBelowMe3Players()
        {
            if (_leaderBoardGrain == null)
            {
                throw new Exception("Not join any game(s) yet.");
            }

            return await _leaderBoardGrain.GetBelow(State.PlayerId, 3);
        }
    }
}