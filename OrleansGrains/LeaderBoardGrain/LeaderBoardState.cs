using System;
using System.Collections.Generic;
using NUlid;
using RpcShared;

namespace LeaderBoardGrain
{
    public class LeaderBoardState
    {
        public Ulid LeaderBoardId { get; set; }

        public LinkedList<RankingPlayerInfo> LeaderBoardPlayerList { get; set; }

        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
    }
}