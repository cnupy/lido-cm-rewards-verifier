namespace RewardsVerifier.Models;

public class TokenTransfer
{
    public string? Hash { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Value { get; set; }
    public string? BlockNumber { get; set; }
    public string? TimeStamp { get; set; }
    public string? TokenName { get; set; }
    public string? TokenSymbol { get; set; }
    public int TokenDecimal { get; set; }
}
