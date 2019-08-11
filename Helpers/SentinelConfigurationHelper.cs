using System;
using System.Linq;
using System.Text;

using StackExchange.Redis;

namespace RedisTester.Helpers
{
    public static class SentinelConfigurationHelper
    {
        private static object _lock = new object();

        private static ConnectionMultiplexer ConnectionMultiplexer { get; set; }

        private static SentinelConfiguration SentinelConfig { get; set; }

        private static string GetSentinelRDBConnectionString()
        {
            if (SentinelConfig == null)
            {
                return string.Empty;
            }

            StringBuilder connectionString = new StringBuilder();

            foreach (var address in SentinelConfig.RedisAddresses)
            {
                connectionString.Append(String.Format("{0}:{1},", address.IP, address.Port));
            }

            connectionString.Append(String.Format("password={0},", SentinelConfig.Password));
            connectionString.Append("allowAdmin = true,");
            connectionString.Append(String.Format("connectTimeout={0}", SentinelConfig.ConnectTimeout));

            return connectionString.ToString();
        }

        public static ConnectionMultiplexer GetSentinelRDBConnection(SentinelConfiguration sentinelConfig)
        {
            if (ConnectionMultiplexer != null)
            {
                return ConnectionMultiplexer;
            }
            else
            {
                lock (_lock)
                {
                    SentinelConfig = sentinelConfig;

                    ConnectionMultiplexer = ConnectionMultiplexer.Connect(GetSentinelRDBConnectionString());

                    return ConnectionMultiplexer;
                }
            }
        }

        /// <summary>
        /// Returns new connection multiplexer. Used for simulation of multiple clients.
        /// Every client gets separate connetion multiplexer.
        /// </summary>
        /// <param name="sentinelConfig">Data from sentinel configuration set in config file.</param>
        /// <returns>Returns new connection multiplexer.</returns>
        public static ConnectionMultiplexer GetSentinelRDBConnectionForClinet(SentinelConfiguration sentinelConfig = null)
        {
            if (sentinelConfig != null)
            {
                SentinelConfig = sentinelConfig;
            }

            var connectionMultiplexer = ConnectionMultiplexer.Connect(GetSentinelRDBConnectionString());

            return connectionMultiplexer;
        }

        public static ConnectionMultiplexer Reconnect(SentinelConfiguration sentinelConfig = null)
        {
            lock (_lock)
            {
                if (sentinelConfig != null)
                {
                    SentinelConfig = sentinelConfig;
                }

                ConnectionMultiplexer = ConnectionMultiplexer.Connect(GetSentinelRDBConnectionString());

                return ConnectionMultiplexer;
            }
        }

        /// <summary>
        /// Simulation of unavailable master.
        /// </summary>
        /// <param name="connection">Connection multiplexer for RDB.</param>
        /// <returns></returns>
        public static void SimulateMasterFail(ConnectionMultiplexer connection)
        {
            var endPoints = connection.GetEndPoints();

            foreach (var endPoint in endPoints)
            {
                var server = connection.GetServer(endPoint.ToString());
                
                if (server.IsConnected && !server.IsSlave)
                {
                    server.Shutdown();
                }
            }
        }

        /// <summary>
        /// Empties out RDB.
        /// </summary>
        /// <param name="connection">Connection multiplexer for RDB.</param>
        /// <returns></returns>
        public static void FlushDatabase(ConnectionMultiplexer connection)
        {
            var endPoints = connection.GetEndPoints();

            foreach (var endPoint in endPoints)
            {
                var server = connection.GetServer(endPoint.ToString());

                if (server.IsConnected && !server.IsSlave)
                {
                    server.FlushAllDatabases(CommandFlags.DemandMaster);
                }
            }
        }

        /// <summary>
        /// Checks the validity of sentinel configuration.
        /// </summary>
        /// <param name="sentinelConfig">Populated config object.</param>
        /// <returns></returns>
        public static bool IsSentinelConfigValid(SentinelConfiguration sentinelConfig)
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

            if (sentinelConfig.ParallelClientCount < 1)
            {
                return false;
            }

            if (sentinelConfig.ConnectTimeout < 1000)
            {
                return false;
            }

            return true;
        }
    }
}
