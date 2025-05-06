namespace Rcb.MessageReader.Service.Models;

public class Leaderboard
{
    public int LeaderboardId { get; set; }
    public string UserId { get; set; }
    public long ReferralCount { get; set; } = 0;
}