using System.Threading.Tasks;
using Orleans;

namespace RpcShared
{
    public interface IPlayerAggregate : IGrainWithIntegerKey
    {
        Task<IPlayerDto> CreateOrGetPlayer(string playerName);
    }
}
