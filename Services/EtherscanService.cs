using RestSharp;
using System.Globalization;
using RewardsVerifier.Models;

namespace RewardsVerifier.Services;

public class EtherscanService
{
    private readonly string _apiKey;
    private readonly string _stEthContractAddress;
    private const string BaseUrl = "https://api.etherscan.io/v2/api";

    public EtherscanService(string apiKey, string stEthContractAddress)
    {
        _apiKey = apiKey;
        _stEthContractAddress = stEthContractAddress;
    }

    public async Task<List<TokenTransfer>> GetTokenTransfersAsync(
        string address, 
        long startBlock, 
        long endBlock)
    {
        var allTransfers = new List<TokenTransfer>();
        int page = 1;
        const int pageSize = 100;

        while (true)
        {
            var options = new RestClientOptions(BuildUrl(address, startBlock, endBlock, page, pageSize));
            var client = new RestClient(options);
            var request = new RestRequest("");
            
            var response = await client.GetAsync<EtherscanResponse>(request);

            if (response?.Result == null || response.Status != "1")
            {
                if (response?.Message?.Contains("No transactions found") == true)
                    break;
                throw new Exception($"Error fetching from Etherscan: {response?.Message}");
            }

            allTransfers.AddRange(response.Result);

            if (response.Result.Count < pageSize)
                break;

            page++;
        }

        return allTransfers;
    }

    private string BuildUrl(string address, long startBlock, long endBlock, int page, int pageSize)
    {
        return $"{BaseUrl}?" +
               $"apikey={_apiKey}&" +
               $"chainid=1&" +
               $"module=account&" +
               $"action=tokentx&" +
               $"contractaddress={_stEthContractAddress}&" +
               $"address={address}&" +
               $"startblock={startBlock}&" +
               $"endblock={endBlock}&" +
               $"page={page}&" +
               $"offset={pageSize}&" +
               $"sort=asc";
    }
}
