using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RedisTester.Helpers;
using RedisTester.Interfaces;
using RedisTester.Models;
using System.Threading;

namespace RedisTester.Controllers
{
    [Route("api/cluster")]
    public class ClusterController : Controller
    {
        private ClusterConfiguration clusterConfiguration;
        private ConfigurationHelper clusterConfigHelper;

        public ClusterController(IOptions<ClusterConfiguration> config)
        {
            clusterConfiguration = config.Value;

            clusterConfigHelper = new ClusterConfigurationHelper(clusterConfiguration);
        }

        [HttpGet]
        [Route("singleclient/{datatype}/{testLoad}")]
        public TestResults SingleClientTest(string datatype, int testLoad)
        {
            if (!clusterConfigHelper.IsConfigValid())
            {
                TestResults tr = new TestResults("Cluster is not configured OK. Test failed.");
                return tr;
            }

            TestResults testResult;

            // Test
            ITest testHelper = TestHelper.GetTestBasedOnDatatype(datatype, clusterConfigHelper);

            if (testHelper == null)
            {
                testResult = new TestResults("Unknown redis data type.");
            }
            else
            {
                testResult = testHelper.RunTest(testLoad);

                testResult.TestStatus = "Successfull test. Test performed by one client.";
            }

            return testResult;
        }

        [HttpGet]
        [Route("singleclient/failover/{datatype}/{testLoad}")]
        public TestResults SingleClientFailoverTest(string datatype, int testLoad)
        {
            if (!clusterConfigHelper.IsConfigValid())
            {
                TestResults tr = new TestResults("Sentinel is not configured OK. Test failed.");
                return tr;
            }

            // Start thread that will shutdown master at some point
            var masterFailThread = new Thread(() => TestHelper.SimulateMasterFail(clusterConfigHelper, testLoad));
            masterFailThread.Start();

            TestResults testResult;

            // Test
            ITest testHelper = TestHelper.GetTestBasedOnDatatype(datatype, clusterConfigHelper);

            if (testHelper == null)
            {
                testResult = new TestResults("Unknown redis data type.");
            }
            else
            {
                testResult = testHelper.RunTest(testLoad);

                testResult.TestStatus = "Successfull test. Test performed by one client.";
            }

            return testResult;
        }

        [HttpGet]
        [Route("multipleclients/{datatype}/{testLoad}")]
        public TestResults MultipleClientsTest(string datatype, int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);

            if (!clusterConfigHelper.IsConfigValid())
            {
                testResult.TestStatus = "Sentinel is not configured OK. Test failed.";
                return testResult;
            }

            Thread[] clientThreads = new Thread[clusterConfiguration.ParallelClientCount];
            ITest[] testHelpers = new BasicTestHelper[clusterConfiguration.ParallelClientCount];

            for (int i = 0; i < clusterConfiguration.ParallelClientCount; i++)
            {
                var clientConfigurationHelper = new ClusterConfigurationHelper(clusterConfiguration);

                testHelpers[i] = TestHelper.GetTestBasedOnDatatype(datatype, clientConfigurationHelper);

                if (testHelpers[i] == null)
                {
                    testResult = new TestResults("Unknown redis data type.");
                }
                else
                {
                    var test = testHelpers[i];

                    clientThreads[i] = new Thread(() => test.RunParallelTest(testResult));

                    clientThreads[i].Start();

                    testResult.TestStatus = "Successfull test. Test performed by one client.";
                }
            }

            for (int i = 0; i < clusterConfiguration.ParallelClientCount; i++)
            {
                clientThreads[i].Join();
            }

            TestHelper.AvrageOutResult(testResult, clusterConfiguration.ParallelClientCount);

            testResult.TestStatus = string.Format("Parallel test successfull. Parallel client count: {0}. Total test load: {1}. Results are avraged out per client.",
                                                    clusterConfiguration.ParallelClientCount,
                                                    clusterConfiguration.ParallelClientCount * testLoad);


            return testResult;
        }

        [HttpGet]
        [Route("multipleclients/failover/{datatype}/{testLoad}")]
        public TestResults GetParallelTestFailover(string datatype, int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);

            if (!clusterConfigHelper.IsConfigValid())
            {
                testResult.TestStatus = "Sentinel is not configured OK. Test failed.";
                return testResult;
            }

            Thread[] clientThreads = new Thread[clusterConfiguration.ParallelClientCount];
            ITest[] testHelpers = new BasicTestHelper[clusterConfiguration.ParallelClientCount];

            var connection = clusterConfigHelper.GetRDBConnection();

            // Start thread that will shutdown master at some point
            var masterFailThread = new Thread(() => TestHelper.SimulateMasterFail(clusterConfigHelper, testLoad));
            masterFailThread.Start();

            for (int i = 0; i < clusterConfiguration.ParallelClientCount; i++)
            {
                var clientConfigurationHelper = new ClusterConfigurationHelper(clusterConfiguration);

                testHelpers[i] = TestHelper.GetTestBasedOnDatatype(datatype, clientConfigurationHelper);

                if (testHelpers[i] == null)
                {
                    testResult = new TestResults("Unknown redis data type.");
                }
                else
                {
                    var test = testHelpers[i];

                    clientThreads[i] = new Thread(() => test.RunParallelTest(testResult));

                    clientThreads[i].Start();

                    testResult.TestStatus = "Successfull test. Test performed by one client.";
                }
            }

            for (int i = 0; i < clusterConfiguration.ParallelClientCount; i++)
            {
                clientThreads[i].Join();
            }

            TestHelper.AvrageOutResult(testResult, clusterConfiguration.ParallelClientCount);

            testResult.TestStatus = string.Format("Parallel test successfull. Parallel client count: {0}. Total test load: {1}. Results are avraged out per client.",
                                                    clusterConfiguration.ParallelClientCount,
                                                    clusterConfiguration.ParallelClientCount * testLoad);

            return testResult;
        }

        [HttpGet]
        [Route("flush")]
        public string Flush()
        {
            if (clusterConfigHelper.IsConfigValid())
            {
                if (clusterConfigHelper.FlushDatabase())
                {
                    return "Sentinel DB flushed.";
                }
            }

            return "Ups";
        }

    }
}
