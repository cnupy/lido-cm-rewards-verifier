namespace RewardsVerifier.Models;

public class RewardsSummary
{
    public string? Address { get; set; }
    public int OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public decimal TotalStEthReceived { get; set; }
    public string? RebateType { get; set; }
    public decimal Rebate05Percent { get; set; }
    public decimal Rebate1Percent { get; set; }
}
