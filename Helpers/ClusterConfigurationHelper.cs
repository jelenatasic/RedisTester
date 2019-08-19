using System;
using System.Linq;

namespace RedisTester.Helpers
{
    public class ClusterConfigurationHelper : ConfigurationHelper
    {

        public ClusterConfigurationHelper(Models.ClusterConfiguration config)
        {
            rdbConfiguration = config;
        }

        /// <summary>
        /// Checks the validity of cluster configuration.
        /// </summary>
        /// <param name="clusterConfig">Populated config object.</param>
        /// <returns></returns>
        public override bool IsConfigValid()
        {
            if (rdbConfiguration == null ||
                rdbConfiguration.RedisAddresses == null || rdbConfiguration.RedisAddresses.Count() == 0)
            {
                return false;
            }

            if (rdbConfiguration.RedisAddresses.Any(address => String.IsNullOrEmpty(address.IP) || address.Port <= 0))
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
