using System;
using System.Diagnostics;
using System.Linq;

using RedisTester.Models;
using StackExchange.Redis;

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

        public static void BasicStringTest(this TestResults testResult, IDatabase SenitnelMonitoredDB)
        {
            // 2.Database write load
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            for (int i = 1; i <= testResult.TestLoad; i++)
            {
                SenitnelMonitoredDB.StringSet("key_" + i.ToString(), i);
            }

            stopWatch.Stop();

            testResult.StringTest.WriteTime = stopWatch.ElapsedMilliseconds;

            // 3.Database read load
            stopWatch.Restart();

            for (int i = 1; i <= testResult.TestLoad; i++)
            {
                var retreivedValue = SenitnelMonitoredDB.StringGet("key_" + i.ToString());

                if (retreivedValue.IsNullOrEmpty)
                {
                    testResult.StringTest.LostWriteCount++;
                }
            }

            stopWatch.Stop();

            testResult.StringTest.ReadTime = stopWatch.ElapsedMilliseconds;

            // 4.Update exiting values
            stopWatch.Restart();

            for (int i = 1; i <= testResult.TestLoad; i++)
            {
                SenitnelMonitoredDB.StringSet("key_" + i.ToString(), i * 10);
            }

            stopWatch.Stop();

            testResult.StringTest.UpdateTime = stopWatch.ElapsedMilliseconds;

            // 4.Database delete load
            stopWatch.Restart();

            for (int i = 1; i <= testResult.TestLoad; i++)
            {
                SenitnelMonitoredDB.KeyDelete("key_" + i.ToString());
            }

            stopWatch.Stop();

            testResult.StringTest.CleanUpTime = stopWatch.ElapsedMilliseconds;
        }

        public static void BasicListTest(this TestResults testResult, IDatabase SenitnelMonitoredDB)
        {
            // 2.Database write load
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            for (int i = 1; i <= testResult.TestLoad; i++)
            {
                SenitnelMonitoredDB.ListLeftPush("redis_list", i);
            }

            stopWatch.Stop();

            testResult.ListTest.WriteTime = stopWatch.ElapsedMilliseconds;

            // 3.Database read load
            stopWatch.Restart();

            for (int i = 1; i <= testResult.TestLoad; i++)
            {
                var retreivedValue = SenitnelMonitoredDB.ListLeftPop("redis_list");

                if (retreivedValue.IsNullOrEmpty)
                {
                    testResult.ListTest.LostWriteCount++;
                }
            }

            stopWatch.Stop();

            testResult.ListTest.ReadTime = stopWatch.ElapsedMilliseconds;
        }
    }
}
