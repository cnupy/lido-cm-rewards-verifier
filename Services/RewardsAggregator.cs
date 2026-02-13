using RewardsVerifier.Config;
using RewardsVerifier.Models;

namespace RewardsVerifier.Services;

public class RewardsAggregator
{
    public List<RewardsSummary> AggregateRewards(List<TokenTransfer> transfers, List<string> queriedAddresses, List<OperatorInfo> operators, string? senderFilter = null)
    {
        var rewardsByAddress = new Dictionary<string, decimal>();
        var normalizedQueried = queriedAddresses.Select(a => a.ToLower()).ToHashSet();
        var normalizedSender = senderFilter?.ToLower();
        var operatorsByAddress = operators.ToDictionary(o => (o.Address ?? "").ToLower(), o => o);

        foreach (var transfer in transfers)
        {
            if (string.IsNullOrEmpty(transfer.To) || string.IsNullOrEmpty(transfer.Value))
                continue;

            var toAddress = transfer.To.ToLower();
            var fromAddress = transfer.From?.ToLower();
            
            // Only count transfers received by queried addresses
            if (!normalizedQueried.Contains(toAddress))
                continue;
            
            // If sender filter provided, only include transfers from that sender
            if (normalizedSender != null && fromAddress != normalizedSender)
                continue;

            var amount = decimal.Parse(transfer.Value) / (decimal)Math.Pow(10, transfer.TokenDecimal);

            if (!rewardsByAddress.ContainsKey(toAddress))
                rewardsByAddress[toAddress] = 0;

            rewardsByAddress[toAddress] += amount;
        }

        return rewardsByAddress
            .Select(x =>
            {
                var normalizedKey = queriedAddresses.First(a => a.ToLower() == x.Key);
                var operatorFound = operatorsByAddress.TryGetValue(normalizedKey.ToLower(), out var op);

                return new RewardsSummary
                {
                    Address = x.Key,
                    OperatorId = operatorFound ? op!.Id : 0,
                    OperatorName = operatorFound ? op!.Name : "Unknown",
                    TotalStEthReceived = x.Value,
                    RebateType = operatorFound ? op!.RebateType : null,
                    Rebate05Percent = x.Value / 7m,
                    Rebate1Percent = x.Value / 3.5m
                };
            })
            .OrderBy(r => r.OperatorId)
            .ToList();
    }
}
