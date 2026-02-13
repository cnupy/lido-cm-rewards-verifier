namespace RewardsVerifier.Models;

public class EtherscanResponse
{
    public string? Status { get; set; }
    public string? Message { get; set; }
    public List<TokenTransfer>? Result { get; set; }
}
