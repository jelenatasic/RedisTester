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
        private ConfiguratinHelper sentinelConfigHelper;

        public SentinelController(IOptions<SentinelConfiguration> config)
        {
            sentinelConfiguration = config.Value;

            sentinelConfigHelper = new SentinelConfigurationHelper(sentinelConfiguration);
        }

        [HttpGet]
        [Route("singleclient/{datatype}/{testLoad}")]
        public TestResults SingleClientTest(string datatype, int testLoad)
        {
            if (!sentinelConfigHelper.IsConfigValid())
            {
                TestResults tr = new TestResults("Sentinel is not configured OK. Test failed.");
                return tr;
            }

            // Test
            ITest testHelper;

            switch (datatype.ToLower())
            {
                case "string":
                    {
                        testHelper = new StringTestHelper(sentinelConfigHelper);
                        break;
                    }
                case "list":
                    {
                        testHelper = new ListTestHelper(sentinelConfigHelper);
                        break;
                    }
                case "set":
                    {
                        testHelper = new SetTestHelper(sentinelConfigHelper, false);
                        break;
                    }
                case "sortedset":
                    {
                        testHelper = new SetTestHelper(sentinelConfigHelper, true);
                        break;
                    }
                case "hash":
                    {
                        testHelper = new HashTestHelper(sentinelConfigHelper);
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
            if (!sentinelConfigHelper.IsConfigValid())
            {
                TestResults tr = new TestResults("Sentinel is not configured OK. Test failed.");
                return tr;
            }

            // Start thread that will shutdown master at some point
            var masterFailThread = new Thread(() => TestHelper.SimulateMasterFail(sentinelConfigHelper, testLoad));
            masterFailThread.Start();

            // Test
            ITest testHelper;

            switch (datatype.ToLower())
            {
                case "string":
                    {
                        testHelper = new StringTestHelper(sentinelConfigHelper);
                        break;
                    }
                case "list":
                    {
                        testHelper = new ListTestHelper(sentinelConfigHelper);
                        break;
                    }
                case "set":
                    {
                        testHelper = new SetTestHelper(sentinelConfigHelper, false);
                        break;
                    }
                case "sortedset":
                    {
                        testHelper = new SetTestHelper(sentinelConfigHelper, true);
                        break;
                    }
                case "hash":
                    {
                        testHelper = new HashTestHelper(sentinelConfigHelper);
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

            if (!sentinelConfigHelper.IsConfigValid())
            {
                testResult.TestStatus = "Sentinel is not configured OK. Test failed.";
                return testResult;
            }

            Thread[] clientThreads = new Thread[sentinelConfiguration.ParallelClientCount];
            ITest[] testHelpers = new BasicTestHelper[sentinelConfiguration.ParallelClientCount];

            for (int i = 0; i < sentinelConfiguration.ParallelClientCount; i++)
            {
                var clientConfigurationHelper = new SentinelConfigurationHelper(sentinelConfiguration);

                switch (datatype.ToLower())
                {
                    case "string":
                        {
                            testHelpers[i] = new StringTestHelper(clientConfigurationHelper);
                            break;
                        }
                    case "list":
                        {
                            testHelpers[i] = new ListTestHelper(clientConfigurationHelper);
                            break;
                        }
                    case "set":
                        {
                            testHelpers[i] = new SetTestHelper(clientConfigurationHelper, false);
                            break;
                        }
                    case "sortedset":
                        {
                            testHelpers[i] = new SetTestHelper(clientConfigurationHelper, true);
                            break;
                        }
                    case "hash":
                        {
                            testHelpers[i] = new HashTestHelper(clientConfigurationHelper);
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

            if (!sentinelConfigHelper.IsConfigValid())
            {
                testResult.TestStatus = "Sentinel is not configured OK. Test failed.";
                return testResult;
            }

            Thread[] clientThreads = new Thread[sentinelConfiguration.ParallelClientCount];
            ITest[] testHelpers = new BasicTestHelper[sentinelConfiguration.ParallelClientCount];

            var connection = sentinelConfigHelper.GetRDBConnection();

            // Start thread that will shutdown master at some point
            var masterFailThread = new Thread(() => TestHelper.SimulateMasterFail(sentinelConfigHelper, testLoad));
            masterFailThread.Start();

            for (int i = 0; i < sentinelConfiguration.ParallelClientCount; i++)
            {
                var clientConfigurationHelper = new SentinelConfigurationHelper(sentinelConfiguration);

                switch (datatype.ToLower())
                {
                    case "string":
                        {
                            testHelpers[i] = new StringTestHelper(clientConfigurationHelper);
                            break;
                        }
                    case "list":
                        {
                            testHelpers[i] = new ListTestHelper(clientConfigurationHelper);
                            break;
                        }
                    case "set":
                        {
                            testHelpers[i] = new SetTestHelper(clientConfigurationHelper, false);
                            break;
                        }
                    case "sortedset":
                        {
                            testHelpers[i] = new SetTestHelper(clientConfigurationHelper, true);
                            break;
                        }
                    case "hash":
                        {
                            testHelpers[i] = new HashTestHelper(clientConfigurationHelper);
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
            if (sentinelConfigHelper.IsConfigValid())
            {
                if (sentinelConfigHelper.FlushDatabase())
                {
                    return "Sentinel DB flushed.";
                }     
            }

            return "Ups";
        }
    }
}
