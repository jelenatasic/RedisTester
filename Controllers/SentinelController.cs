using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RedisTester.Helpers;

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

        // GET: api/sentinel
        [HttpGet]
        public IEnumerable<string> Get()
        {
            if (!SentinelHelper.IsConfigValid(sentinelConfiguration))
            {
                return new string[] {
                    "Sentinel is not configured OK."
                };
            }

            var connection = ConfigurationHelper.GetSentinelRDBConnection(sentinelConfiguration);

            var SRDB = connection.GetDatabase();

            return new string[] {
                "Success."
            };
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
