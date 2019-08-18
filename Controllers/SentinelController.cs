using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using RedisTester.Helpers;
using RedisTester.Interfaces;
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
        [Route("singleclient/{datatype}/{testLoad}")]
        public TestResults SingleClientTest(string datatype, int testLoad)
        {
            if (!SentinelConfigurationHelper.IsSentinelConfigValid(sentinelConfiguration))
            {
                TestResults tr = new TestResults("Sentinel is not configured OK. Test failed.");
                return tr;
            }

            // Database access
            var connection = SentinelConfigurationHelper.GetSentinelRDBConnection(sentinelConfiguration);

            // Test
            ITest testHelper;

            switch (datatype.ToLower())
            {
                case "string":
                    {
                        testHelper = new StringTestHelper(connection);
                        break;
                    }
                case "list":
                    {
                        testHelper = new ListTestHelper(connection);
                        break;
                    }
                case "set":
                    {
                        testHelper = new SetTestHelper(connection, false);
                        break;
                    }
                case "sortedset":
                    {
                        testHelper = new SetTestHelper(connection, true);
                        break;
                    }
                default:
                    {
                        TestResults tr = new TestResults("Unknown redis data type.");
                        return tr;
                    }
            }

            var testResult  = testHelper.RunTest(testLoad);

            testResult.TestStatus = "Successfull test. Test performed by one client.";

            return testResult;
        }

        [HttpGet]
        [Route("singleclient/failover/{datatype}/{testLoad}")]
        public TestResults SingleClientFailoverTest(string datatype, int testLoad)
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
            ITest testHelper;

            switch (datatype.ToLower())
            {
                case "string":
                    {
                        testHelper = new StringTestHelper(connection);
                        break;
                    }
                case "list":
                    {
                        testHelper = new ListTestHelper(connection);
                        break;
                    }
                case "set":
                    {
                        testHelper = new SetTestHelper(connection, false);
                        break;
                    }
                case "sortedset":
                    {
                        testHelper = new SetTestHelper(connection, true);
                        break;
                    }
                default:
                    {
                        TestResults tr = new TestResults("Unknown redis data type.");
                        return tr;
                    }
            }

            var testResult = testHelper.RunTest(testLoad);

            testResult.TestStatus = "Successfull test. Test performed by one client.";

            return testResult;
        }

        [HttpGet]
        [Route("multipleclients/{datatype}/{testLoad}")]
        public TestResults MultipleClientsTest(string datatype, int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);

            if (!SentinelConfigurationHelper.IsSentinelConfigValid(sentinelConfiguration))
            {
                testResult.TestStatus = "Sentinel is not configured OK. Test failed.";
                return testResult;
            }

            Thread[] clientThreads = new Thread[sentinelConfiguration.ParallelClientCount];
            BasicTestHelper[] testHelpers = new BasicTestHelper[sentinelConfiguration.ParallelClientCount];

            for (int i = 0; i < sentinelConfiguration.ParallelClientCount; i++)
            {
                var clientConnectionMultiplexer = SentinelConfigurationHelper.GetSentinelRDBConnectionForClinet(sentinelConfiguration);

                switch (datatype.ToLower())
                {
                    case "string":
                        {
                            testHelpers[i] = new StringTestHelper(clientConnectionMultiplexer);
                            break;
                        }
                    case "list":
                        {
                            testHelpers[i] = new ListTestHelper(clientConnectionMultiplexer);
                            break;
                        }
                    case "set":
                        {
                            testHelpers[i] = new SetTestHelper(clientConnectionMultiplexer, false);
                            break;
                        }
                    case "sortedset":
                        {
                            testHelpers[i] = new SetTestHelper(clientConnectionMultiplexer, true);
                            break;
                        }
                    default:
                        {
                            TestResults tr = new TestResults("Unknown redis data type.");
                            return tr;
                        }
                }

                var test = testHelpers[i];

                clientThreads[i] = new Thread(() => test.RunParallelTest(testResult));

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
        [Route("multipleclients/failover/{datatype}/{testLoad}")]
        public TestResults GetParallelTestFailover(string datatype, int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);

            if (!SentinelConfigurationHelper.IsSentinelConfigValid(sentinelConfiguration))
            {
                testResult.TestStatus = "Sentinel is not configured OK. Test failed.";
                return testResult;
            }

            Thread[] clientThreads = new Thread[sentinelConfiguration.ParallelClientCount];
            BasicTestHelper[] testHelpers = new BasicTestHelper[sentinelConfiguration.ParallelClientCount];

            var connection = SentinelConfigurationHelper.GetSentinelRDBConnection(sentinelConfiguration);

            // Start thread that will shutdown master at some point
            var masterFailThread = new Thread(() => TestHelper.SimulateMasterFail(connection, testLoad));
            masterFailThread.Start();

            for (int i = 0; i < sentinelConfiguration.ParallelClientCount; i++)
            {
                var clientConnectionMultiplexer = SentinelConfigurationHelper.GetSentinelRDBConnectionForClinet(sentinelConfiguration);

                switch (datatype.ToLower())
                {
                    case "string":
                        {
                            testHelpers[i] = new StringTestHelper(clientConnectionMultiplexer);
                            break;
                        }
                    case "list":
                        {
                            testHelpers[i] = new ListTestHelper(clientConnectionMultiplexer);
                            break;
                        }
                    case "set":
                        {
                            testHelpers[i] = new SetTestHelper(clientConnectionMultiplexer, false);
                            break;
                        }
                    case "sortedset":
                        {
                            testHelpers[i] = new SetTestHelper(clientConnectionMultiplexer, true);
                            break;
                        }
                    default:
                        {
                            TestResults tr = new TestResults("Unknown redis data type.");
                            return tr;
                        }
                }

                var test = testHelpers[i];

                clientThreads[i] = new Thread(() => test.RunParallelTest(testResult));

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
