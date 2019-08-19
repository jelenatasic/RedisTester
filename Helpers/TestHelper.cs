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
        protected ConfigurationHelper configurationHelper { get; set; }

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
        public StringTestHelper(ConfigurationHelper configHelper)
        {
            this.configurationHelper = configHelper;
        }

        public override TestResults RunTest(int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);
            string keyPrefix = "thread:" + Thread.CurrentThread.ManagedThreadId + ":key:";
            var redisDB = this.configurationHelper.GetRDBConnection().GetDatabase();

            // 2.Database write load
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            for (int i = 1; i <= testResult.TestLoadPerThread; i++)
            {
                try
                {
                    redisDB.StringSet(keyPrefix + i.ToString(), i, null, When.Always, CommandFlags.DemandMaster);
                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
                    var retreivedValue = redisDB.StringGet(keyPrefix + i.ToString(), CommandFlags.DemandSlave);
                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
                        redisDB.StringIncrement(keyPrefix + i.ToString(), i % 10, CommandFlags.DemandMaster);
                    }
                    else
                    {
                        redisDB.StringDecrement(keyPrefix + i.ToString(), i % 10, CommandFlags.DemandMaster);
                    }

                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
                    redisDB.KeyDelete(keyPrefix + i.ToString(), CommandFlags.DemandMaster);
                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
        public ListTestHelper(ConfigurationHelper configHelper)
        {
            this.configurationHelper = configHelper;
        }

        public override TestResults RunTest(int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);
            string keyPrefix = "thread:" + Thread.CurrentThread.ManagedThreadId + ":key:";
            var redisDB = this.configurationHelper.GetRDBConnection().GetDatabase();
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
                        redisDB.ListLeftPush(keyPrefix + (i % numberOfLists).ToString(), rnd.Next(), When.Always, CommandFlags.DemandMaster);
                    }
                    else
                    {
                        redisDB.ListRightPush(keyPrefix + (i % numberOfLists).ToString(), rnd.Next(), When.Always, CommandFlags.DemandMaster);
                    }
                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
                        var retreivedValue = redisDB.ListRange(keyPrefix + (i % numberOfLists).ToString());
                    }
                    else
                    {
                        long listLength = redisDB.ListLength(keyPrefix + (i % numberOfLists).ToString());

                        if (listLength > 0)
                        {
                            redisDB.ListGetByIndex(keyPrefix + (i % numberOfLists).ToString(), (int)Math.Truncate(listLength * 0.5));
                        }

                    }

                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
                    long listLength = redisDB.ListLength(keyPrefix + (i % numberOfLists).ToString());

                    if (listLength == 0)
                    {
                        redisDB.ListLeftPush(keyPrefix + (i % numberOfLists).ToString(), rnd.Next());
                        continue;
                    }

                    if (i % 2 == 0)
                    {
                        redisDB.ListTrim(keyPrefix + (i % numberOfLists).ToString(), 0, (int)Math.Truncate(listLength * 0.5));
                    }
                    else
                    {
                        redisDB.ListSetByIndex(keyPrefix + (i % numberOfLists).ToString(), (int)Math.Truncate(listLength * 0.5), rnd.Next());
                    }
                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
                    redisDB.KeyDelete(keyPrefix + i.ToString(), CommandFlags.DemandMaster);
                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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

        public SetTestHelper(ConfigurationHelper configHelper, bool sorted)
        {
            this.configurationHelper = configHelper;
            this.SortedSet = sorted;
        }

    public override TestResults RunTest(int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);
            string keyPrefix = "thread:" + Thread.CurrentThread.ManagedThreadId + ":key:";
            var redisDB = this.configurationHelper.GetRDBConnection().GetDatabase();
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
                        redisDB.SortedSetAdd(keyPrefix + (i % numberOfSets).ToString(), rnd.Next(), rnd.Next());
                    }
                    else
                    {
                        redisDB.SetAdd(keyPrefix + (i % numberOfSets).ToString(), rnd.Next());
                    } 
                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
                        var retreivedValue = redisDB.SetMembers(keyPrefix + (i % numberOfSets).ToString());
                    }
                    else
                    {
                        long listLength = redisDB.SetLength(keyPrefix + (i % numberOfSets).ToString());

                        if (listLength > 0)
                        {
                            redisDB.SetRandomMember(keyPrefix + (i % numberOfSets).ToString());
                        }

                    }

                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
                    long listLength = redisDB.SetLength(keyPrefix + (i % numberOfSets).ToString());

                    if (listLength == 0)
                    {
                        if (SortedSet)
                        {
                            redisDB.SortedSetCombineAndStore(SetOperation.Union,
                                                        keyPrefix + (i % numberOfSets).ToString(),
                                                        keyPrefix + (i + 1 % numberOfSets).ToString(),
                                                        keyPrefix + (i + 2 % numberOfSets).ToString());
                        }
                        else
                        {
                            redisDB.SetCombineAndStore(SetOperation.Union,
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
                            redisDB.SortedSetRemove(keyPrefix + (i % numberOfSets).ToString(),
                                                        redisDB.SetRandomMember(keyPrefix + (i % numberOfSets).ToString()));
                        }
                        else
                        {
                            redisDB.SetRemove(keyPrefix + (i % numberOfSets).ToString(),
                                                        redisDB.SetRandomMember(keyPrefix + (i % numberOfSets).ToString()));
                        }
                    }
                    else
                    {
                        if (SortedSet)
                        {
                            redisDB.SortedSetPop(keyPrefix + (i % numberOfSets).ToString());
                        }
                        else
                        {
                            redisDB.SetPop(keyPrefix + (i % numberOfSets).ToString());
                        }
                    }
                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
                    redisDB.KeyDelete(keyPrefix + i.ToString(), CommandFlags.DemandMaster);
                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
        public HashTestHelper(ConfigurationHelper configHelper)
        {
            this.configurationHelper = configHelper;
        }

        public override TestResults RunTest(int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);
            string keyPrefix = "thread:" + Thread.CurrentThread.ManagedThreadId + ":key:";
            var redisDB = this.configurationHelper.GetRDBConnection().GetDatabase();
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

                    redisDB.HashSet(keyPrefix + i.ToString(), hashEntries);
                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
                        var retreivedValue = redisDB.HashGetAll(keyPrefix + i.ToString());
                    }
                    else
                    {
                        var hashKeys = redisDB.HashKeys(keyPrefix + i.ToString());

                        if (hashKeys.Length > 0)
                        {
                            var data = redisDB.HashGet(keyPrefix + i.ToString(), hashKeys[rnd.Next() % hashKeys.Length]);
                        }
                    }

                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
                    var hashKeys = redisDB.HashKeys(keyPrefix + i.ToString());

                    if (hashKeys.Length == 0)
                    {
                        redisDB.HashSet(keyPrefix + i.ToString(), new HashEntry[] 
                                                    { new HashEntry(rnd.Next(), rnd.Next()), new HashEntry(rnd.Next(), rnd.Next())});
                        continue;
                    }

                    if (i % 2 == 0)
                    {
                        redisDB.HashDelete(keyPrefix + i.ToString(), hashKeys[rnd.Next() % hashKeys.Length]);
                    }
                    else
                    {
                        redisDB.HashSet(keyPrefix + i.ToString(), new HashEntry[] { new HashEntry(hashKeys[rnd.Next() % hashKeys.Length], rnd.Next()) });
                    }
                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
                    redisDB.KeyDelete(keyPrefix + i.ToString(), CommandFlags.DemandMaster);
                }
                catch (RedisConnectionException e)
                {
                    redisDB = configurationHelper.Reconnect().GetDatabase();
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
        public static void SimulateMasterFail(ConfigurationHelper connection, int testload)
        {
            Random rnd = new Random(DateTime.Now.Millisecond);

            Thread.Sleep(rnd.Next() % (testload * 2) + 1);

            connection.SimulateMasterFail();
        }

        public static void AvrageOutResult(TestResults results, int parallelClients)
        {
            results.TestParams.WriteTime /= parallelClients;
            results.TestParams.ReadTime /= parallelClients;
            results.TestParams.UpdateTime /= parallelClients;
            results.TestParams.RemoveTime /= parallelClients;
        }

        public static ITest GetTestBasedOnDatatype(string datatype, ConfigurationHelper configHelper)
        {
            ITest testHelper;

            switch (datatype.ToLower())
            {
                case "string":
                    {
                        testHelper = new StringTestHelper(configHelper);
                        break;
                    }
                case "list":
                    {
                        testHelper = new ListTestHelper(configHelper);
                        break;
                    }
                case "set":
                    {
                        testHelper = new SetTestHelper(configHelper, false);
                        break;
                    }
                case "sortedset":
                    {
                        testHelper = new SetTestHelper(configHelper, true);
                        break;
                    }
                case "hash":
                    {
                        testHelper = new HashTestHelper(configHelper);
                        break;
                    }
                default:
                    {
                        testHelper = null;
                        break;
                    }
            }

            return testHelper;
        }
    }
}
