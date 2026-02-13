using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using RewardsVerifier.Config;
using RewardsVerifier.Services;

Console.OutputEncoding = Encoding.UTF8;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddInMemoryCollection(GetCommandLineConfig(args))
    .Build();

var settings = new AppSettings();
config.GetSection("AppSettings").Bind(settings);

if (string.IsNullOrEmpty(settings.EtherscanApiKey))
{
    Console.WriteLine("Error: EtherscanApiKey not found in appsettings.json or command-line arguments");
    Environment.Exit(1);
}

var parser = new CommandLineParser();
try
{
    parser.Parse(args);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

var etherscanService = new EtherscanService(settings.EtherscanApiKey, settings.StEthContractAddress);
var aggregator = new RewardsAggregator();
var blockService = new BlockNumberService(settings.EtherscanApiKey);

long startBlock = parser.StartBlock;
long endBlock = parser.EndBlock;

// If no addresses provided via command line, use all from config
var addressesToQuery = parser.Addresses.Count > 0 ? parser.Addresses : settings.Operators.Select(o => o.Address!).ToList();

if (addressesToQuery.Count == 0)
{
    Console.WriteLine("Error: No addresses provided in command line and none configured in appsettings.json");
    Environment.Exit(1);
}

if (parser.StartDate.HasValue || parser.EndDate.HasValue)
{
    Console.WriteLine("Converting dates to block numbers...");
    
    if (!parser.StartDate.HasValue)
    {
        Console.WriteLine("Error: --startdate requires --enddate (or use --startblock/--endblock)");
        Environment.Exit(1);
    }
    if (!parser.EndDate.HasValue)
    {
        Console.WriteLine("Error: --enddate requires --startdate (or use --startblock/--endblock)");
        Environment.Exit(1);
    }

    try
    {
        startBlock = await blockService.GetBlockNumberForDateAsync(parser.StartDate.Value);
        endBlock = await blockService.GetBlockNumberForDateAsync(parser.EndDate.Value);
        Console.WriteLine($"[OK] Start date {parser.StartDate:yyyy-MM-dd} -> Block {startBlock}");
        Console.WriteLine($"[OK] End date {parser.EndDate:yyyy-MM-dd} -> Block {endBlock}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error converting dates to blocks: {ex.Message}");
        Environment.Exit(1);
    }
}

Console.WriteLine("\nFetching token transfers...");

var allTransfers = new List<RewardsVerifier.Models.TokenTransfer>();

foreach (var address in addressesToQuery)
{
    try
    {
        var transfers = await etherscanService.GetTokenTransfersAsync(
            address,
            startBlock,
            endBlock);

        allTransfers.AddRange(transfers);
        Console.WriteLine($"[OK] Fetched {transfers.Count} transfers for {address}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FAIL] Error fetching transfers for {address}: {ex.Message}");
    }
}

var results = aggregator.AggregateRewards(allTransfers, addressesToQuery, settings.Operators, senderFilter: settings.SenderFilterAddress);

Console.WriteLine("\n=== Rewards Summary ===\n");
Console.WriteLine($"{"ID",-5} {"Name",-35} {"stETH Received",-18} {"Rebate",-18} {"Type",-15}");
Console.WriteLine(new string('-', 91));

foreach (var result in results)
{
    var idStr = result.OperatorId > 0 ? result.OperatorId.ToString() : "";
    var rebateLabel = result.RebateType == "ExtraEffort" ? "ExtraEffort - 0.5%" : result.RebateType == "ClientTeam" ? "ClientTeam - 1%" : "";
    var relevantRebate = result.RebateType == "ExtraEffort" ? result.Rebate05Percent : result.RebateType == "ClientTeam" ? result.Rebate1Percent : 0m;
    var rebateStr = result.RebateType != null ? relevantRebate.ToString("F8") : "";
    
    Console.WriteLine($"{idStr,-5} {result.OperatorName,-35} {result.TotalStEthReceived,-18:F8} {rebateStr,-18} {rebateLabel,-15}");
}

Console.WriteLine();

static Dictionary<string, string?> GetCommandLineConfig(string[] args)
{
    var config = new Dictionary<string, string?>();
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--apikey" && i + 1 < args.Length)
        {
            config["AppSettings:EtherscanApiKey"] = args[i + 1];
            i++;
        }
    }
    return config;
}

class CommandLineParser
{
    public List<string> Addresses { get; private set; } = new();
    public long StartBlock { get; private set; }
    public long EndBlock { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }

    public void Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException(
                "Usage: dotnet run -- [<address1> <address2> ...] " +
                "(--startblock <block> --endblock <block>) OR (--startdate <yyyy-MM-dd> --enddate <yyyy-MM-dd>) " +
                "[--apikey <key>]\n" +
                "Note: If no addresses provided, all configured addresses will be used.");
        }

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--startblock" && i + 1 < args.Length)
            {
                if (!long.TryParse(args[i + 1], out var block))
                    throw new ArgumentException($"Invalid startblock: {args[i + 1]}");
                StartBlock = block;
                i++;
            }
            else if (args[i] == "--endblock" && i + 1 < args.Length)
            {
                if (!long.TryParse(args[i + 1], out var block))
                    throw new ArgumentException($"Invalid endblock: {args[i + 1]}");
                EndBlock = block;
                i++;
            }
            else if (args[i] == "--startdate" && i + 1 < args.Length)
            {
                if (!DateTime.TryParseExact(args[i + 1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    throw new ArgumentException($"Invalid startdate: {args[i + 1]}. Use format yyyy-MM-dd");
                StartDate = date;
                i++;
            }
            else if (args[i] == "--enddate" && i + 1 < args.Length)
            {
                if (!DateTime.TryParseExact(args[i + 1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    throw new ArgumentException($"Invalid enddate: {args[i + 1]}. Use format yyyy-MM-dd");
                EndDate = date;
                i++;
            }
            else if (args[i] == "--apikey" && i + 1 < args.Length)
            {
                i++; // Skip, handled by GetCommandLineConfig
            }
            else if (!args[i].StartsWith("--") && !args[i].StartsWith("-"))
            {
                if (!args[i].StartsWith("0x"))
                    throw new ArgumentException($"Invalid address: {args[i]}. Addresses must start with 0x");
                Addresses.Add(args[i]);
            }
        }

        bool hasBlockNumbers = StartBlock != 0 || EndBlock != 0;
        bool hasDates = StartDate.HasValue || EndDate.HasValue;

        if (hasBlockNumbers && hasDates)
            throw new ArgumentException("Cannot use both block numbers and dates. Use either (--startblock/--endblock) or (--startdate/--enddate)");

        if (!hasBlockNumbers && !hasDates)
            throw new ArgumentException("Must provide either block numbers (--startblock/--endblock) or dates (--startdate/--enddate)");

        if (hasBlockNumbers)
        {
            if (StartBlock == 0)
                throw new ArgumentException("--startblock is required when using block numbers");
            if (EndBlock == 0)
                throw new ArgumentException("--endblock is required when using block numbers");
            if (StartBlock > EndBlock)
                throw new ArgumentException("startblock must be less than or equal to endblock");
        }

        if (hasDates)
        {
            if (!StartDate.HasValue)
                throw new ArgumentException("--startdate is required when using dates");
            if (!EndDate.HasValue)
                throw new ArgumentException("--enddate is required when using dates");
            if (StartDate > EndDate)
                throw new ArgumentException("startdate must be before or equal to enddate");
        }
    }
}
