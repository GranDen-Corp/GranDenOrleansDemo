using System;
using System.Collections.Generic;
using NUlid;
using RpcShared;

namespace PlayerGrain
{
    public class PlayerState : IPlayerDto
    {
        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; }

        public ulong Score { get; set; }

        public Ulid CurrentJoinedGame { get; set; }
    }
}