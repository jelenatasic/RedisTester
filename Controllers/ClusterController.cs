using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RedisTester.Helpers;
using RedisTester.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace RedisTester.Controllers
{
    [Route("api/cluster")]
    public class ClusterController : Controller
    {
        private ClusterConfiguration clusterConfiguration;
        private ConfiguratinHelper clusterConfigHelper;

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

            return new TestResults(2);
        }

    }
}
