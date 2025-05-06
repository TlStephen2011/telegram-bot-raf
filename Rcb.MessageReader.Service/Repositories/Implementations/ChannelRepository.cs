using Dapper;
using Microsoft.Data.SqlClient;
using Rcb.MessageReader.Service.Models;
using Rcb.MessageReader.Service.Repositories.Contracts;

namespace Rcb.MessageReader.Service.Repositories.Implementations;

public class ChannelRepository(IConfiguration configuration) : IChannelRepository
{
    public async Task<List<Channel>> GetChannelsAsync()
    {
        await using var conn = new SqlConnection(configuration.GetConnectionString("TelegramBotConnection"));

        var query = @"SELECT * FROM Channels WHERE Active = 1 ORDER BY InsertedAt DESC;";

        var result = await conn.QueryAsync<Channel>(query);
        
        return result.ToList();
    }
}