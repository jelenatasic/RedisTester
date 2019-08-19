using System.Collections.Generic;

namespace RedisTester.Models
{
    public class SentinelConfiguration : RedisDBConfiguration
    {
        public List<Address> SentinelAddresses { get; set; }
    }

    public class ClusterConfiguration : RedisDBConfiguration
    {

    }

    public class RedisDBConfiguration
    {
        public List<Address> RedisAddresses { get; set; }

        public string Password { get; set; }

        public int ParallelClientCount { get; set; }

        public int ConnectTimeout { get; set; }
    }

    public class Address
    {
        public string IP { get; set; }

        public int Port { get; set; }
    }

}
