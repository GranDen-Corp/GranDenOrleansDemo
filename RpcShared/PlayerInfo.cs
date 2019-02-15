using System;

namespace RpcShared
{
    public class RankingPlayerInfo : IPlayerDto
    {
        public Guid PlayerId { get; set; }

        public string PlayerName { get; set; }

        public ulong Score { get; set; }
    }

    public interface IPlayerDto
    {
        Guid PlayerId { get; set; }

        string PlayerName { get; set; }
    }
}
