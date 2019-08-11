using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using RedisTester.Models;
using StackExchange.Redis;

namespace RedisTester.Helpers
{
    public static class TestHelper
    {
        public static TestResults BasicStringTest(ConnectionMultiplexer connectionMultiplexer, int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);
            string keyPrefix = "thread:" + Thread.CurrentThread.ManagedThreadId + ":key:";
            var SenitnelMonitoredDB = connectionMultiplexer.GetDatabase();

            // 2.Database write load
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            for (int i = 1; i <= testResult.TestLoadPerThread; i++)
            {
                try
                {
                    SenitnelMonitoredDB.StringSet(keyPrefix + i.ToString(), i, null, When.Always, CommandFlags.DemandMaster);
                }
                catch (Exception e)
                {
                    SenitnelMonitoredDB = SentinelConfigurationHelper.Reconnect().GetDatabase();
                }
            }

            stopWatch.Stop();

            testResult.TestDetails.Add(String.Format("Client {0} : Added keys to RDB {1}1 - {1}{2}. Time: {3}ms.",
                                                Thread.CurrentThread.ManagedThreadId,
                                                keyPrefix,
                                                testResult.TestLoadPerThread,
                                                stopWatch.ElapsedMilliseconds));

            testResult.TestParams.WriteTime = stopWatch.ElapsedMilliseconds;

            // 3.Database read load
            stopWatch.Restart();

            for (int i = 1; i <= testResult.TestLoadPerThread; i++)
            {
                try
                {
                    var retreivedValue = SenitnelMonitoredDB.StringGet(keyPrefix + i.ToString(), CommandFlags.DemandSlave);
                }
                catch (Exception e)
                {
                    SenitnelMonitoredDB = SentinelConfigurationHelper.Reconnect().GetDatabase();
                }
            }

            stopWatch.Stop();

            testResult.TestDetails.Add(String.Format("Client {0} : Read keys to RDB {1}1 - {1}{2}. Time: {3}ms.",
                                                Thread.CurrentThread.ManagedThreadId,
                                                keyPrefix,
                                                testResult.TestLoadPerThread,
                                                stopWatch.ElapsedMilliseconds));

            testResult.TestParams.ReadTime = stopWatch.ElapsedMilliseconds;

            // 4.Update exiting values
            Random rnd = new Random(DateTime.Now.Millisecond);
            stopWatch.Restart();

            for (int i = 1; i <= testResult.TestLoadPerThread; i++)
            {
                try
                {
                    if (rnd.Next() % 2 == 0)
                    {
                        SenitnelMonitoredDB.StringIncrement(keyPrefix + i.ToString(), i % 10, CommandFlags.DemandMaster);
                    }
                    else
                    {
                        SenitnelMonitoredDB.StringDecrement(keyPrefix + i.ToString(), i % 10, CommandFlags.DemandMaster);
                    }
                    
                }
                catch (Exception e)
                {
                    SenitnelMonitoredDB = SentinelConfigurationHelper.Reconnect().GetDatabase();
                }
            }

            stopWatch.Stop();

            testResult.TestDetails.Add(String.Format("Client {0} : Updated keys to RDB {1}1 - {1}{2}. Time: {3}ms.",
                                                Thread.CurrentThread.ManagedThreadId,
                                                keyPrefix,
                                                testResult.TestLoadPerThread,
                                                stopWatch.ElapsedMilliseconds));

            testResult.TestParams.UpdateTime = stopWatch.ElapsedMilliseconds;

            // 4.Database delete load
            stopWatch.Restart();

            for (int i = 1; i <= testResult.TestLoadPerThread; i++)
            {
                try
                {
                    SenitnelMonitoredDB.KeyDelete(keyPrefix + i.ToString(), CommandFlags.DemandMaster);
                }
                catch (Exception e)
                {
                    SenitnelMonitoredDB = SentinelConfigurationHelper.Reconnect().GetDatabase();
                }
            }

            stopWatch.Stop();

            testResult.TestDetails.Add(String.Format("Client {0} : Removed keys from RDB {1}1 - {1}{2}. Time: {3}ms.",
                                                Thread.CurrentThread.ManagedThreadId,
                                                keyPrefix,
                                                testResult.TestLoadPerThread,
                                                stopWatch.ElapsedMilliseconds));

            testResult.TestParams.CleanUpTime = stopWatch.ElapsedMilliseconds;

            return testResult;
        }

        public static void BasicListTest(this TestResults testResult, IDatabase SenitnelMonitoredDB)
        {
        }

        public static void ParralelClientWork(ConnectionMultiplexer clientConnectionMultiplexer, TestResults testResults)
        {
            var threadTestResult = BasicStringTest(clientConnectionMultiplexer, testResults.TestLoadPerThread);

            lock (testResults)
            {
                testResults.TestParams.WriteTime += threadTestResult.TestParams.WriteTime;
                testResults.TestParams.ReadTime += threadTestResult.TestParams.ReadTime;
                testResults.TestParams.UpdateTime += threadTestResult.TestParams.UpdateTime;
                testResults.TestParams.CleanUpTime += threadTestResult.TestParams.CleanUpTime;

                testResults.TestDetails.AddRange(threadTestResult.TestDetails);
            }
        }

        public static void SimulateMasterFail(ConnectionMultiplexer connection, int testload)
        {
            Random rnd = new Random(DateTime.Now.Millisecond);

            Thread.Sleep(rnd.Next() % (testload * 2) + 1);

            SentinelConfigurationHelper.SimulateMasterFail(connection);
        }

        public static void AvrageOutResult(TestResults results, int parallelClients)
        {
            results.TestParams.WriteTime /= parallelClients;
            results.TestParams.ReadTime /= parallelClients;
            results.TestParams.UpdateTime /= parallelClients;
            results.TestParams.CleanUpTime /= parallelClients;
        }
    }
}
