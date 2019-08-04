using System;
using System.Linq;

namespace RedisTester.Helpers
{
    public static class SentinelHelper
    {
        public static bool IsConfigValid(SentinelConfiguration sentinelConfig)
        {
            if (sentinelConfig == null ||
                sentinelConfig.RedisAddresses == null || sentinelConfig.RedisAddresses.Count() == 0 ||
                sentinelConfig.SentinelAddresses == null || sentinelConfig.SentinelAddresses.Count() == 0)
            {
                return false;
            }
            
            if (sentinelConfig.RedisAddresses.Any(address => String.IsNullOrEmpty(address.IP) || address.Port <= 0))
            {
                return false;
            }

            if (sentinelConfig.SentinelAddresses.Any(address => String.IsNullOrEmpty(address.IP) || address.Port <= 0))
            {
                return false;
            }

            if (String.IsNullOrEmpty(sentinelConfig.Password))
            {
                return false;
            }

            return true;
        }
    }
}
