using RestSharp;

namespace RewardsVerifier.Services;

public class BlockNumberService
{
    private readonly string _apiKey;
    private const string BaseUrl = "https://api.etherscan.io/v2/api";

    public BlockNumberService(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<long> GetBlockNumberForDateAsync(DateTime date)
    {
        long unixTimestamp = ((DateTimeOffset)date).ToUnixTimeSeconds();
        
        var options = new RestClientOptions(
            $"{BaseUrl}?apikey={_apiKey}&chainid=1&module=block&action=getblocknobytime&timestamp={unixTimestamp}&closest=before");
        
        var client = new RestClient(options);
        var request = new RestRequest("");
        
        var response = await client.GetAsync<BlockNumberResponse>(request);

        if (response?.Result == null)
            throw new Exception($"Failed to fetch block number for date {date:yyyy-MM-dd}: {response?.Message}");

        if (!long.TryParse(response.Result, out long blockNumber))
            throw new Exception($"Invalid block number response: {response.Result}");

        return blockNumber;
    }

    private class BlockNumberResponse
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? Result { get; set; }
    }
}
