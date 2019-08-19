using System;
using System.Text;
using RedisTester.Models;
using StackExchange.Redis;

namespace RedisTester.Helpers
{
    public abstract class ConfigurationHelper
    {
        private object _lock = new object();

        private ConnectionMultiplexer ConnectionMultiplexer { get; set; }

        protected RedisDBConfiguration rdbConfiguration { get; set; }

        private string GetRDBConnectionString()
        {
            if (rdbConfiguration == null)
            {
                return string.Empty;
            }

            StringBuilder connectionString = new StringBuilder();

            foreach (var address in rdbConfiguration.RedisAddresses)
            {
                connectionString.Append(String.Format("{0}:{1},", address.IP, address.Port));
            }

            connectionString.Append(String.Format("password={0},", rdbConfiguration.Password));
            connectionString.Append("allowAdmin = true,");
            connectionString.Append(String.Format("connectTimeout={0}", rdbConfiguration.ConnectTimeout));

            return connectionString.ToString();
        }


        /// <summary>
        /// Fetches connection multiplexer for given configuration.
        /// </summary>
        /// <returns>Returns connection multiplexer that is used for communication with Redis DB.</returns>
        public ConnectionMultiplexer GetRDBConnection()
        {
            if (ConnectionMultiplexer != null)
            {
                return ConnectionMultiplexer;
            }
            else
            {
                lock (_lock)
                {
                    ConnectionMultiplexer = ConnectionMultiplexer.Connect(GetRDBConnectionString());

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
        public ConnectionMultiplexer GetRDBConnectionForClinet(RedisDBConfiguration config = null)
        {
            if (config != null)
            {
                rdbConfiguration = config;
            }

            var connectionMultiplexer = ConnectionMultiplexer.Connect(GetRDBConnectionString());

            return connectionMultiplexer;
        }

        public ConnectionMultiplexer Reconnect(RedisDBConfiguration config = null)
        {
            lock (_lock)
            {
                if (config != null)
                {
                    rdbConfiguration = config;
                }

                ConnectionMultiplexer = ConnectionMultiplexer.Connect(GetRDBConnectionString());

                return ConnectionMultiplexer;
            }
        }

        /// <summary>
        /// Empties out RDB.
        /// </summary>
        /// <param name="connection">Connection multiplexer for RDB.</param>
        /// <returns></returns>
        public bool FlushDatabase()
        {
            if (ConnectionMultiplexer == null)
            {
                return false;
            }

            var endPoints = ConnectionMultiplexer.GetEndPoints();

            foreach (var endPoint in endPoints)
            {
                var server = ConnectionMultiplexer.GetServer(endPoint.ToString());

                if (server.IsConnected && !server.IsSlave)
                {
                    server.FlushAllDatabases(CommandFlags.DemandMaster);
                }
            }

            return true;
        }

        /// <summary>
        /// Simulation of unavailable master.
        /// </summary>
        /// <param name="connection">Connection multiplexer for RDB.</param>
        /// <returns></returns>
        public void SimulateMasterFail()
        {
            if(ConnectionMultiplexer == null)
            {
                return;
            }

            var endPoints = ConnectionMultiplexer.GetEndPoints();

            foreach (var endPoint in endPoints)
            {
                var server = ConnectionMultiplexer.GetServer(endPoint.ToString());

                if (server.IsConnected && !server.IsSlave)
                {
                    server.Shutdown();
                }
            }
        }

        public abstract bool IsConfigValid();
    }
}
