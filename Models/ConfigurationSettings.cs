using System.Collections.Generic;

namespace RedisTester.Helpers
{
    public class SentinelConfiguration
    {
        public List<Address> RedisAddresses { get; set; }

        public List<Address> SentinelAddresses { get; set; }

        public string Password { get; set; }
    }

    public class Address
    {
        public string IP { get; set; }

        public int Port { get; set; }
    }
}
