using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RedisTester.Helpers;
using RedisTester.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

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
        [Route("basic/{testLoad}")]
        public TestResults GetBasic(int testLoad)
        {
            TestResults testResult = new TestResults(testLoad);

            if (!SentinelHelper.IsConfigValid(sentinelConfiguration))
            {
                testResult.TestStatus = "Sentinel is not configured OK. Test failed.";
                return testResult;
            }

            // Database access
            var connection = ConfigurationHelper.GetSentinelRDBConnection(sentinelConfiguration);
            var SentinelRDB = connection.GetDatabase();

            //--- STRING TEST ---
            testResult.BasicStringTest(SentinelRDB);

            //--- STRING TEST ---
            testResult.BasicListTest(SentinelRDB);

            testResult.TestStatus = "Successfull test.";

            return testResult;
        }

        // GET api/sentinel/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/sentinel
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/sentinel/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/sentinel/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
