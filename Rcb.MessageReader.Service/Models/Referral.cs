namespace Rcb.MessageReader.Service.Models;

public class Referral
{
    public string MobileNumber { get; set; }
    public int ReferralId { get; set; }
    public bool JoinedChannel { get; set; } = false;
    public string UserId { get; set; }
    public string ReferredUserId { get; set; }
    public DateTime InsertedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Username { get; set; }
    public string ReferredUsername { get; set; }
}