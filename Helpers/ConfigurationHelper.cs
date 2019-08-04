using StackExchange.Redis;
using System;
using System.Text;

namespace RedisTester.Helpers
{
    public static class ConfigurationHelper
    {
        private static object _lock = new object();

        private static ConnectionMultiplexer connectionMultiplexer;

        public static ConnectionMultiplexer GetSentinelRDBConnection(SentinelConfiguration sentinelConfig)
        {
            if (connectionMultiplexer != null)
            {
                return connectionMultiplexer;
            }
            else
            {
                lock (_lock)
                {
                    StringBuilder connectionString = new StringBuilder();

                    foreach (var address in sentinelConfig.RedisAddresses)
                    {
                        connectionString.Append(String.Format("{0}:{1},", address.IP, address.Port));
                    }

                    connectionString.Append(String.Format("password={0}", sentinelConfig.Password));

                    connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString.ToString());

                    return connectionMultiplexer;
                }
            }
        }
    }
}
