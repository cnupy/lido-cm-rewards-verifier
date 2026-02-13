# Copilot Instructions for Rewards Verifier

## Project Overview

A C# console application that aggregates stETH token transfers received by Ethereum addresses over a specified block range, using the Etherscan API. The app queries token transactions and computes both total amounts received and amounts divided by 3.5.

## Build, Test, and Lint Commands

### Building
```bash
dotnet build
```

### Running - With Dates (recommended)
```bash
dotnet run -- 0xfeef177E6168F9b7fd59e6C5b6c2d87FF398c6FD --startdate 2025-12-01 --enddate 2025-12-31
```

### Running - With Block Numbers
```bash
dotnet run -- 0xfeef177E6168F9b7fd59e6C5b6c2d87FF398c6FD --startblock 24327300 --endblock 24400000
```

Optionally override API key from command line:
```bash
dotnet run -- <address> --startdate <date> --enddate <date> --apikey YOUR_KEY
```

## Architecture

### Core Components

1. **Models/**: Data structures for API responses
   - `TokenTransfer`: Represents a single stETH transfer from Etherscan
   - `EtherscanResponse`: Wraps API response with status and transfers list
   - `RewardsSummary`: Aggregated rewards output (address, total, divided by 3.5)

2. **Services/**:
   - `EtherscanService`: Handles Etherscan API calls for token transfers, pagination, and block range filtering
   - `BlockNumberService`: Converts dates (yyyy-MM-dd) to Ethereum block numbers via Etherscan API
   - `RewardsAggregator`: Sums stETH received per address and calculates metrics

3. **Config/**:
   - `AppSettings`: Configuration model for API key and contract address

### Data Flow

1. Command-line parser validates addresses and either block range OR date range
2. If dates provided, BlockNumberService converts them to block numbers via Etherscan
3. Configuration loaded from `appsettings.json` (API key) + CLI overrides
4. For each address, fetch token transfers via EtherscanService
5. Filter by block range and "To" address (inbound transfers only)
6. Aggregate amounts per address with RewardsAggregator
7. Output sorted by total amount received

### Key Constants

- **stETH Contract**: `0xae7ab96520DE3A18E5e111B5EaAb095312D7fE84` (Ethereum mainnet)
- **API Base URL**: `https://api.etherscan.io/api` (for block lookups) and `https://api.etherscan.io/v2/api` (for transfers)
- **Token Decimals**: 18 (stETH uses 18 decimal places)
- **Division Factor**: 3.5 (configurable in RewardsAggregator)

## Key Conventions

1. **Configuration**: 
   - `appsettings.json` stores default API key
   - CLI `--apikey` flag overrides JSON config
   - Required: `EtherscanApiKey` must be present

2. **Address Validation**:
   - All addresses must start with `0x`
   - Stored and compared in lowercase
   - At least one address required

3. **Date/Block Range Input**:
   - **Two options (cannot mix)**:
     - Dates: `--startdate yyyy-MM-dd --enddate yyyy-MM-dd`
     - Blocks: `--startblock <number> --endblock <number>`
   - Dates must be valid and start ≤ end
   - BlockNumberService automatically converts dates to blocks
   - Block range filtering happens on Etherscan side

4. **Block Number Conversion**:
   - Uses Etherscan's `block/getblocknobytime` API action
   - Requests "before" closest block (at or before the timestamp)
   - Requires separate API call per date (2 calls total for date range)

5. **API Pagination**:
   - Etherscan returns max 100 results per request
   - EtherscanService handles pagination automatically
   - Continues fetching until fewer than 100 results returned

6. **Decimal Handling**:
   - Token amounts stored as strings from API
   - Parsed as decimal, divided by 10^18 for stETH
   - Results formatted to 8 decimal places

7. **Error Handling**:
   - Invalid config → exit code 1
   - Missing API key → error message and exit
   - Failed transfer fetch → warning, continue with other addresses
   - Failed date-to-block conversion → error message and exit

