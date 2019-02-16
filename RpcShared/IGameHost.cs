using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUlid;
using Orleans;

namespace RpcShared
{
    public interface IGameHost : IGrainWithIntegerKey
    {
        ValueTask<Ulid> OpenLeaderBoard(DateTimeOffset startAt, TimeSpan openDuration);

        Task<Ulid?> GetCurrentLeaderBoard();
    }
}
