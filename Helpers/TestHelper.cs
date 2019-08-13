using System;
using System.Diagnostics;
using System.Threading;

using StackExchange.Redis;
using RedisTester.Models;


namespace RedisTester.Helpers
{
    public enum TestDataType
    {
        RedisString,
        RedisList,
        RedisSet,
        RedisSortedSet,
        RedisHash
    }

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
                catch (RedisConnectionException e)
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
                catch (RedisConnectionException e)
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
                catch (RedisConnectionException e)
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
                catch (RedisConnectionException e)
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

            testResult.TestParams.RemoveTime = stopWatch.ElapsedMilliseconds;

            return testResult;
        }

        public static TestResults BasicListTest(ConnectionMultiplexer connectionMultiplexer, int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);
            string keyPrefix = "thread:" + Thread.CurrentThread.ManagedThreadId + ":key:";
            var SenitnelMonitoredDB = connectionMultiplexer.GetDatabase();
            Random rnd = new Random(DateTime.Now.Millisecond);

            // 2.Database write load
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var numberOfLists = testResult.TestLoadPerThread > 1000 ? 1000 : testResult.TestLoadPerThread;

            for (int i = 1; i <= testResult.TestLoadPerThread; i++)
            {
                try
                {
                    if (i % 2 == 0)
                    {
                        SenitnelMonitoredDB.ListLeftPush(keyPrefix + (i % numberOfLists).ToString(), rnd.Next(), When.Always, CommandFlags.DemandMaster);
                    }
                    else
                    {
                        SenitnelMonitoredDB.ListRightPush(keyPrefix + (i % numberOfLists).ToString(), rnd.Next(), When.Always, CommandFlags.DemandMaster);
                    }
                }
                catch (RedisConnectionException e)
                {
                    SenitnelMonitoredDB = SentinelConfigurationHelper.Reconnect().GetDatabase();
                }
            }

            stopWatch.Stop();

            testResult.TestDetails.Add(String.Format("Client {0} : Added lists to RDB {1}1 - {1}1000. Time: {2}ms.",
                                                Thread.CurrentThread.ManagedThreadId,
                                                keyPrefix,
                                                stopWatch.ElapsedMilliseconds));

            testResult.TestParams.WriteTime = stopWatch.ElapsedMilliseconds;

            // 3.Database read load
            stopWatch.Restart();

            for (int i = 1; i <= testResult.TestLoadPerThread; i++)
            {
                try
                {
                    if (i % 2 == 0)
                    {
                        var retreivedValue = SenitnelMonitoredDB.ListRange(keyPrefix + (i % numberOfLists).ToString());
                    }
                    else
                    {
                        long listLength = SenitnelMonitoredDB.ListLength(keyPrefix + (i % numberOfLists).ToString());

                        if (listLength > 0)
                        {
                            SenitnelMonitoredDB.ListGetByIndex(keyPrefix + (i % numberOfLists).ToString(), (int)Math.Truncate(listLength * 0.5));
                        }
                        
                    }
                    
                }
                catch (RedisConnectionException e)
                {
                    SenitnelMonitoredDB = SentinelConfigurationHelper.Reconnect().GetDatabase();
                }
            }

            stopWatch.Stop();

            testResult.TestDetails.Add(String.Format("Client {0} : Made {1} read requests. Time: {2}ms.",
                                                Thread.CurrentThread.ManagedThreadId,
                                                testResult.TestLoadPerThread,
                                                stopWatch.ElapsedMilliseconds));

            testResult.TestParams.ReadTime = stopWatch.ElapsedMilliseconds;

            // 4.Update exiting values

            stopWatch.Restart();

            for (int i = 1; i <= testResult.TestLoadPerThread; i++)
            {
                try
                {
                    long listLength = SenitnelMonitoredDB.ListLength(keyPrefix + (i % numberOfLists).ToString());

                    if (listLength == 0)
                    {
                        SenitnelMonitoredDB.ListLeftPush(keyPrefix + (i % numberOfLists).ToString(), rnd.Next());
                        continue;
                    }

                    if (i % 2 == 0)
                    {
                        SenitnelMonitoredDB.ListTrim(keyPrefix + (i % numberOfLists).ToString(), 0, (int) Math.Truncate(listLength * 0.5));
                    }
                    else
                    {
                        SenitnelMonitoredDB.ListSetByIndex(keyPrefix + (i % numberOfLists).ToString(), (int) Math.Truncate(listLength * 0.5), rnd.Next());
                    }
                }
                catch (RedisConnectionException e)
                {
                    SenitnelMonitoredDB = SentinelConfigurationHelper.Reconnect().GetDatabase();
                }
            }

            stopWatch.Stop();

            testResult.TestDetails.Add(String.Format("Client {0} : Updated keys to RDB {1}1 - {1}{2} using trim and set. Time: {3}ms.",
                                                Thread.CurrentThread.ManagedThreadId,
                                                keyPrefix,
                                                numberOfLists,
                                                stopWatch.ElapsedMilliseconds));

            testResult.TestParams.UpdateTime = stopWatch.ElapsedMilliseconds;

            // 4.Database delete load
            stopWatch.Restart();

            for (int i = 1; i <= numberOfLists; i++)
            {
                try
                {
                    SenitnelMonitoredDB.KeyDelete(keyPrefix + i.ToString(), CommandFlags.DemandMaster);
                }
                catch (RedisConnectionException e)
                {
                    SenitnelMonitoredDB = SentinelConfigurationHelper.Reconnect().GetDatabase();
                }
            }

            stopWatch.Stop();

            testResult.TestDetails.Add(String.Format("Client {0} : Removed keys from RDB {1}1 - {1}{2}. Time: {3}ms.",
                                                Thread.CurrentThread.ManagedThreadId,
                                                keyPrefix,
                                                numberOfLists,
                                                stopWatch.ElapsedMilliseconds));

            testResult.TestParams.RemoveTime = stopWatch.ElapsedMilliseconds;

            return testResult;
        }

        public static void ParralelClientWork(ConnectionMultiplexer clientConnectionMultiplexer, TestDataType redisDataType, TestResults testResults)
        {
            TestResults threadTestResult;
            switch (redisDataType)
            {
                case TestDataType.RedisString:
                    {
                        threadTestResult = BasicStringTest(clientConnectionMultiplexer, testResults.TestLoadPerThread);
                        break;
                    }
                case TestDataType.RedisList:
                    {
                        threadTestResult = BasicListTest(clientConnectionMultiplexer, testResults.TestLoadPerThread);
                        break;
                    }
                default:
                    {
                        throw new Exception("Unsupported data type");
                    }
            }

            lock (testResults)
            {
                testResults.TestParams.WriteTime += threadTestResult.TestParams.WriteTime;
                testResults.TestParams.ReadTime += threadTestResult.TestParams.ReadTime;
                testResults.TestParams.UpdateTime += threadTestResult.TestParams.UpdateTime;
                testResults.TestParams.RemoveTime += threadTestResult.TestParams.RemoveTime;

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
            results.TestParams.RemoveTime /= parallelClients;
        }
    }
}
