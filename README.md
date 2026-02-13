# Rewards Verifier

A C# console application that aggregates stETH token transfers for Ethereum addresses, retrieving and analyzing reward distributions from the Lido protocol.

## Features

- Query stETH token transfers via Etherscan API
- Aggregate received amounts by address
- Calculate metrics (total received and divided by 3.5)
- Support for multiple addresses in a single run
- Block range filtering for precise time windows
- Configurable API key management

## Prerequisites

- .NET 10.0 or later
- Etherscan API key (free tier available at https://etherscan.io/apis)

## Setup

1. **Configure API Key**

   Edit `appsettings.json` and add your Etherscan API key:

   ```json
   {
     "AppSettings": {
       "EtherscanApiKey": "YOUR_API_KEY_HERE",
       "StEthContractAddress": "0xae7ab96520DE3A18E5e111B5EaAb095312D7fE84"
     }
   }
   ```

2. **Build the project**

   ```bash
   dotnet build
   ```

## Usage

### Using Block Numbers (original method)

```bash
dotnet run -- 0xfeef177E6168F9b7fd59e6C5b6c2d87FF398c6FD --startblock 24327300 --endblock 24400000
```

### Using Dates (new - automatically converts to blocks)

```bash
dotnet run -- 0xfeef177E6168F9b7fd59e6C5b6c2d87FF398c6FD --startdate 2025-12-01 --enddate 2025-12-31
```

### Multiple Addresses

```bash
dotnet run -- 0xaddress1 0xaddress2 0xaddress3 --startdate 2025-12-01 --enddate 2025-12-31
```

### Override API Key (optional)

```bash
dotnet run -- 0xaddress --startdate 2025-12-01 --enddate 2025-12-31 --apikey YOUR_API_KEY
```

## Output

```
Converting dates to block numbers...
✓ Start date 2025-12-01 → Block 24327300
✓ End date 2025-12-31 → Block 24400000

Fetching token transfers...
✓ Fetched 156 transfers for 0xfeef177E6168F9b7fd59e6C5b6c2d87FF398c6FD

=== Rewards Summary ===

Address                                   stETH Received       ÷ 3.5
---------------------------------------------------------------------------
0xfeef177e6168f9b7fd59e6c5b6c2d87ff398c6fd  2500.12345678     714.32098770
```

## Project Structure

```
rewards-verifier/
├── Models/                    # Data structures
│   ├── TokenTransfer.cs       # Etherscan transfer response
│   ├── EtherscanResponse.cs   # API response wrapper
│   └── RewardsSummary.cs      # Aggregated results
├── Services/                  # Business logic
│   ├── EtherscanService.cs    # API client with pagination
│   └── RewardsAggregator.cs   # Aggregation logic
├── Config/                    # Configuration models
│   └── AppSettings.cs         # App configuration
├── Program.cs                 # Entry point
└── appsettings.json          # Configuration file
```

## API Details

- **Data Source**: Etherscan API (https://etherscan.io/apis)
- **Endpoint**: `/v2/api?module=account&action=tokentx`
- **Token**: stETH (0xae7ab96520DE3A18E5e111B5EaAb095312D7fE84)
- **Chain**: Ethereum Mainnet (chainid=1)
- **Pagination**: 100 results per request (automatic)

## Notes

- **Date vs Block Input**: Use dates (yyyy-MM-dd) for human-friendly queries, block numbers for exact range control
- Date conversion: Etherscan API provides "closest before" block for each date (may be slightly before the requested date)
- Block ranges are Ethereum block numbers, not timestamps
- Only inbound transfers (To = queried address) are counted
- All addresses are normalized to lowercase in output
- Amounts are in stETH units (18 decimal places)
- Results sorted by total amount received (descending)
