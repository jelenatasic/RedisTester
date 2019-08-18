using System;
using System.Diagnostics;
using System.Threading;

using StackExchange.Redis;
using RedisTester.Models;
using RedisTester.Interfaces;

namespace RedisTester.Helpers
{
    public abstract class BasicTestHelper : ITest
    {
        protected ConnectionMultiplexer connectionMultiplexer { get; set; }

        public void RunParallelTest(TestResults testResults)
        {
            var threadTestResult = RunTest(testResults.TestLoadPerThread);
            

            lock (testResults)
            {
                testResults.TestParams.WriteTime += threadTestResult.TestParams.WriteTime;
                testResults.TestParams.ReadTime += threadTestResult.TestParams.ReadTime;
                testResults.TestParams.UpdateTime += threadTestResult.TestParams.UpdateTime;
                testResults.TestParams.RemoveTime += threadTestResult.TestParams.RemoveTime;

                testResults.TestDetails.AddRange(threadTestResult.TestDetails);
            }
        }

        public abstract TestResults RunTest(int testLoad);
    }

    public class StringTestHelper : BasicTestHelper
    {
        public StringTestHelper(ConnectionMultiplexer connectionMultiplexer)
        {
            this.connectionMultiplexer = connectionMultiplexer;
        }

        public override TestResults RunTest(int testLoad)
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
    }

    public class ListTestHelper : BasicTestHelper
    {
        public ListTestHelper(ConnectionMultiplexer connectionMultiplexer)
        {
            this.connectionMultiplexer = connectionMultiplexer;
        }

        public override TestResults RunTest(int testLoad)
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
                        SenitnelMonitoredDB.ListTrim(keyPrefix + (i % numberOfLists).ToString(), 0, (int)Math.Truncate(listLength * 0.5));
                    }
                    else
                    {
                        SenitnelMonitoredDB.ListSetByIndex(keyPrefix + (i % numberOfLists).ToString(), (int)Math.Truncate(listLength * 0.5), rnd.Next());
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
    }

    public class SetTestHelper : BasicTestHelper
    {
        private bool SortedSet { get; set; }

        public SetTestHelper(ConnectionMultiplexer connectionMultiplexer, bool sorted)
        {
            this.connectionMultiplexer = connectionMultiplexer;
            this.SortedSet = sorted;
        }

        public override TestResults RunTest(int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);
            string keyPrefix = "thread:" + Thread.CurrentThread.ManagedThreadId + ":key:";
            var SenitnelMonitoredDB = connectionMultiplexer.GetDatabase();
            Random rnd = new Random(DateTime.Now.Millisecond);

            // 2.Database write load
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var numberOfSets = testResult.TestLoadPerThread > 1000 ? 1000 : testResult.TestLoadPerThread;

            for (int i = 1; i <= testResult.TestLoadPerThread; i++)
            {
                try
                {
                    if (SortedSet)
                    {
                        SenitnelMonitoredDB.SortedSetAdd(keyPrefix + (i % numberOfSets).ToString(), rnd.Next(), rnd.Next());
                    }
                    else
                    {
                        SenitnelMonitoredDB.SetAdd(keyPrefix + (i % numberOfSets).ToString(), rnd.Next());
                    } 
                }
                catch (RedisConnectionException e)
                {
                    SenitnelMonitoredDB = SentinelConfigurationHelper.Reconnect().GetDatabase();
                }
            }

            stopWatch.Stop();

            testResult.TestDetails.Add(String.Format("Client {0} : Added sets to RDB {1}1 - {1}1000. Time: {2}ms.",
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
                        var retreivedValue = SenitnelMonitoredDB.SetMembers(keyPrefix + (i % numberOfSets).ToString());
                    }
                    else
                    {
                        long listLength = SenitnelMonitoredDB.SetLength(keyPrefix + (i % numberOfSets).ToString());

                        if (listLength > 0)
                        {
                            SenitnelMonitoredDB.SetRandomMember(keyPrefix + (i % numberOfSets).ToString());
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
                    long listLength = SenitnelMonitoredDB.SetLength(keyPrefix + (i % numberOfSets).ToString());

                    if (listLength == 0)
                    {
                        if (SortedSet)
                        {
                            SenitnelMonitoredDB.SortedSetCombineAndStore(SetOperation.Union,
                                                        keyPrefix + (i % numberOfSets).ToString(),
                                                        keyPrefix + (i + 1 % numberOfSets).ToString(),
                                                        keyPrefix + (i + 2 % numberOfSets).ToString());
                        }
                        else
                        {
                            SenitnelMonitoredDB.SetCombineAndStore(SetOperation.Union,
                                                        keyPrefix + (i % numberOfSets).ToString(),
                                                        keyPrefix + (i + 1 % numberOfSets).ToString(), 
                                                        keyPrefix + (i + 2 % numberOfSets).ToString());

                        }

                        continue;
                    }
                    
                    if (i % 2 == 0)
                    {
                        if (SortedSet)
                        {
                            SenitnelMonitoredDB.SortedSetRemove(keyPrefix + (i % numberOfSets).ToString(),
                                                        SenitnelMonitoredDB.SetRandomMember(keyPrefix + (i % numberOfSets).ToString()));
                        }
                        else
                        {
                            SenitnelMonitoredDB.SetRemove(keyPrefix + (i % numberOfSets).ToString(),
                                                        SenitnelMonitoredDB.SetRandomMember(keyPrefix + (i % numberOfSets).ToString()));
                        }
                    }
                    else
                    {
                        if (SortedSet)
                        {
                            SenitnelMonitoredDB.SortedSetPop(keyPrefix + (i % numberOfSets).ToString());
                        }
                        else
                        {
                            SenitnelMonitoredDB.SetPop(keyPrefix + (i % numberOfSets).ToString());
                        }
                    }
                }
                catch (RedisConnectionException e)
                {
                    SenitnelMonitoredDB = SentinelConfigurationHelper.Reconnect().GetDatabase();
                }
            }

            stopWatch.Stop();

            testResult.TestDetails.Add(String.Format("Client {0} : Updated keys to RDB {1}1 - {1}{2} using set intersection and set element removal. Time: {3}ms.",
                                                Thread.CurrentThread.ManagedThreadId,
                                                keyPrefix,
                                                numberOfSets,
                                                stopWatch.ElapsedMilliseconds));

            testResult.TestParams.UpdateTime = stopWatch.ElapsedMilliseconds;

            // 4.Database delete load
            stopWatch.Restart();

            for (int i = 1; i <= numberOfSets; i++)
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
                                                numberOfSets,
                                                stopWatch.ElapsedMilliseconds));

            testResult.TestParams.RemoveTime = stopWatch.ElapsedMilliseconds;

            return testResult;
        }
    }

    public class HashTestHelper : BasicTestHelper
    {
        public HashTestHelper(ConnectionMultiplexer connectionMultiplexer)
        {
            this.connectionMultiplexer = connectionMultiplexer;
        }

        public override TestResults RunTest(int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);
            string keyPrefix = "thread:" + Thread.CurrentThread.ManagedThreadId + ":key:";
            var SenitnelMonitoredDB = connectionMultiplexer.GetDatabase();
            Random rnd = new Random(DateTime.Now.Millisecond);

            // 2.Database write load
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var hashEntriesCount = 5;

            for (int i = 1; i <= testResult.TestLoadPerThread; i++)
            {
                try
                {
                    HashEntry[] hashEntries = new HashEntry[hashEntriesCount];

                    for (int h= 1; h < hashEntriesCount; h++)
                    {
                        hashEntries[h] = new HashEntry("HE" + h.ToString(), rnd.Next());
                    }

                    SenitnelMonitoredDB.HashSet(keyPrefix + i.ToString(), hashEntries);
                }
                catch (RedisConnectionException e)
                {
                    SenitnelMonitoredDB = SentinelConfigurationHelper.Reconnect().GetDatabase();
                }
            }

            stopWatch.Stop();

            testResult.TestDetails.Add(String.Format("Client {0} : Added hashes to RDB {1}1 - {1}{2}. Time: {3}ms.",
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
                    if (i % 2 == 0)
                    {
                        var retreivedValue = SenitnelMonitoredDB.HashGetAll(keyPrefix + i.ToString());
                    }
                    else
                    {
                        var hashKeys = SenitnelMonitoredDB.HashKeys(keyPrefix + i.ToString());

                        if (hashKeys.Length > 0)
                        {
                            var data = SenitnelMonitoredDB.HashGet(keyPrefix + i.ToString(), hashKeys[rnd.Next() % hashKeys.Length]);
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
                    var hashKeys = SenitnelMonitoredDB.HashKeys(keyPrefix + i.ToString());

                    if (hashKeys.Length == 0)
                    {
                        SenitnelMonitoredDB.HashSet(keyPrefix + i.ToString(), new HashEntry[] 
                                                    { new HashEntry(rnd.Next(), rnd.Next()), new HashEntry(rnd.Next(), rnd.Next())});
                        continue;
                    }

                    if (i % 2 == 0)
                    {
                        SenitnelMonitoredDB.HashDelete(keyPrefix + i.ToString(), hashKeys[rnd.Next() % hashKeys.Length]);
                    }
                    else
                    {
                        SenitnelMonitoredDB.HashSet(keyPrefix + i.ToString(), new HashEntry[] { new HashEntry(hashKeys[rnd.Next() % hashKeys.Length], rnd.Next()) });
                    }
                }
                catch (RedisConnectionException e)
                {
                    SenitnelMonitoredDB = SentinelConfigurationHelper.Reconnect().GetDatabase();
                }
            }

            stopWatch.Stop();

            testResult.TestDetails.Add(String.Format("Client {0} : Updated keys to RDB {1}1 - {1}{2} using hash key delete and hash key set. Time: {3}ms.",
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
    }

    public static class TestHelper
    {
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
