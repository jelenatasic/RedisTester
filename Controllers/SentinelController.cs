using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace RedisTester.Controllers
{
    [Route("api/sentinel")]
    public class SentinelController : Controller
    {
        // GET: api/sentinel
        [HttpGet]
        public IEnumerable<string> Get()
        {
            //ConfigurationOptions option = new ConfigurationOptions
            //{
            //    AbortOnConnectFail = false,
            //    EndPoints = { "localhost" }
            //};

            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");

            IDatabase db = redis.GetDatabase();
            db.StringSet("test", "fuck the police");

            return new string[] { "value1", "value2" };
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
