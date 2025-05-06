using Rcb.MessageReader.Service.Models;

namespace Rcb.MessageReader.Service.Repositories.Contracts;

public interface IChannelRepository
{
    Task<List<Channel>> GetChannelsAsync();
}