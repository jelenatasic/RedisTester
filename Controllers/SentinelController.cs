using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using RedisTester.Helpers;
using RedisTester.Models;

namespace RedisTester.Controllers
{
    [Route("api/sentinel")]
    public class SentinelController : Controller
    {
        private SentinelConfiguration sentinelConfiguration;

        public SentinelController(IOptions<SentinelConfiguration> config)
        {
            sentinelConfiguration = config.Value;
        }

        [HttpGet]
        [Route("basic/stringtest/{testLoad}")]
        public TestResults BasicStringTest(int testLoad)
        {
            if (!SentinelConfigurationHelper.IsSentinelConfigValid(sentinelConfiguration))
            {
                TestResults tr = new TestResults("Sentinel is not configured OK. Test failed.");
                return tr;
            }

            // Database access
            var connection = SentinelConfigurationHelper.GetSentinelRDBConnection(sentinelConfiguration);

            // Test
            var testResult  = TestHelper.BasicStringTest(connection, testLoad);

            testResult.TestStatus = "Successfull test. Test performed by one client.";

            return testResult;
        }

        [HttpGet]
        [Route("basic/stringtest/failover/{testLoad}")]
        public TestResults BasicStringTestFailover(int testLoad)
        {
            if (!SentinelConfigurationHelper.IsSentinelConfigValid(sentinelConfiguration))
            {
                TestResults tr = new TestResults("Sentinel is not configured OK. Test failed.");
                return tr;
            }

            // Database access
            var connection = SentinelConfigurationHelper.GetSentinelRDBConnection(sentinelConfiguration);

            // Start thread that will shutdown master at some point
            var masterFailThread = new Thread(() => TestHelper.SimulateMasterFail(connection, testLoad));
            masterFailThread.Start();

            // Test
            var testResult = TestHelper.BasicStringTest(connection, testLoad);

            testResult.TestStatus = "Successfull test. Test performed by one client.";

            return testResult;
        }

        [HttpGet]
        [Route("parallel/stringtest/{testLoad}")]
        public TestResults GetParallelTest(int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);

            if (!SentinelConfigurationHelper.IsSentinelConfigValid(sentinelConfiguration))
            {
                testResult.TestStatus = "Sentinel is not configured OK. Test failed.";
                return testResult;
            }

            Thread[] clientThreads = new Thread[sentinelConfiguration.ParallelClientCount];

            for (int i = 0; i < sentinelConfiguration.ParallelClientCount; i++)
            {
                var clientConnectionMultiplexer = SentinelConfigurationHelper.GetSentinelRDBConnectionForClinet(sentinelConfiguration);

                clientThreads[i] = new Thread(() => TestHelper.ParralelClientWork(clientConnectionMultiplexer, testResult));

                clientThreads[i].Start();
            }

            for (int i = 0; i < sentinelConfiguration.ParallelClientCount; i++)
            {
                clientThreads[i].Join();
            }

            TestHelper.AvrageOutResult(testResult, sentinelConfiguration.ParallelClientCount);

            testResult.TestStatus = string.Format("Parallel test successfull. Parallel client count: {0}. Total test load: {1}. Results are avraged out per client.",
                                                    sentinelConfiguration.ParallelClientCount,
                                                    sentinelConfiguration.ParallelClientCount * testLoad);

            return testResult;
        }

        [HttpGet]
        [Route("parallel/stringtest/failover/{testLoad}")]
        public TestResults GetParallelTestFailover(int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);

            if (!SentinelConfigurationHelper.IsSentinelConfigValid(sentinelConfiguration))
            {
                testResult.TestStatus = "Sentinel is not configured OK. Test failed.";
                return testResult;
            }

            Thread[] clientThreads = new Thread[sentinelConfiguration.ParallelClientCount];

            var connection = SentinelConfigurationHelper.GetSentinelRDBConnection(sentinelConfiguration);

            // Start thread that will shutdown master at some point
            var masterFailThread = new Thread(() => TestHelper.SimulateMasterFail(connection, testLoad));
            masterFailThread.Start();


            for (int i = 0; i < sentinelConfiguration.ParallelClientCount; i++)
            {
                var clientConnectionMultiplexer = SentinelConfigurationHelper.GetSentinelRDBConnectionForClinet(sentinelConfiguration);

                clientThreads[i] = new Thread(() => TestHelper.ParralelClientWork(clientConnectionMultiplexer, testResult));

                clientThreads[i].Start();
            }

            for (int i = 0; i < sentinelConfiguration.ParallelClientCount; i++)
            {
                clientThreads[i].Join();
            }

            TestHelper.AvrageOutResult(testResult, sentinelConfiguration.ParallelClientCount);

            testResult.TestStatus = string.Format("Parallel test successfull. Parallel client count: {0}. Total test load: {1}. Results are avraged out per client.",
                                                    sentinelConfiguration.ParallelClientCount,
                                                    sentinelConfiguration.ParallelClientCount * testLoad);

            return testResult;
        }


        [HttpGet]
        [Route("flush")]
        public string Flush()
        {
            var connection = SentinelConfigurationHelper.GetSentinelRDBConnection(sentinelConfiguration);

            SentinelConfigurationHelper.FlushDatabase(connection);

            return "Database flush performed.";
        }
    }
}
