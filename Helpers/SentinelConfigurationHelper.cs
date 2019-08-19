using System;
using System.Linq;

using RedisTester.Models;


namespace RedisTester.Helpers
{
    public class SentinelConfigurationHelper : ConfigurationHelper
    {
        private new SentinelConfiguration rdbConfiguration { get; set; }

        public SentinelConfigurationHelper(SentinelConfiguration config)
        {
            this.rdbConfiguration = config;
        }

        /// <summary>
        /// Checks the validity of sentinel configuration.
        /// </summary>
        /// <param name="sentinelConfig">Populated config object.</param>
        /// <returns></returns>
        public override bool IsConfigValid()
        {
            if (rdbConfiguration == null ||
                rdbConfiguration.RedisAddresses == null || rdbConfiguration.RedisAddresses.Count() == 0 ||
                rdbConfiguration.SentinelAddresses == null || rdbConfiguration.SentinelAddresses.Count() == 0)
            {
                return false;
            }

            if (rdbConfiguration.RedisAddresses.Any(address => String.IsNullOrEmpty(address.IP) || address.Port <= 0))
            {
                return false;
            }

            if (rdbConfiguration.SentinelAddresses.Any(address => String.IsNullOrEmpty(address.IP) || address.Port <= 0))
            {
                return false;
            }

            if (String.IsNullOrEmpty(rdbConfiguration.Password))
            {
                return false;
            }

            if (rdbConfiguration.ParallelClientCount < 1)
            {
                return false;
            }

            if (rdbConfiguration.ConnectTimeout < 1000)
            {
                return false;
            }

            return true;
        }
    }
}
