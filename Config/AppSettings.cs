namespace RewardsVerifier.Config;

public class AppSettings
{
    public string? EtherscanApiKey { get; set; }
    public string StEthContractAddress { get; set; } = "0xae7ab96520DE3A18E5e111B5EaAb095312D7fE84";
    public string? SenderFilterAddress { get; set; } = "0x55032650b14df07b85bF18A3a3eC8E0Af2e028d5";
    public List<OperatorInfo> Operators { get; set; } = new();
}
