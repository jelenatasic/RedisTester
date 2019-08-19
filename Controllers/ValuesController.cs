using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace RedisTester.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] 
            {
                "/api/sentinel/singleclient/{datatype}/{testLoad}",
                "/api/sentinel/singleclient/failover/{datatype}/{testLoad}",
                "/api/sentinel/multipleclients/{datatype}/{testLoad}",
                "/api/sentinel/multipleclients/failover/{datatype}/{testLoad}",
                "/api/sentinel/flush"
            };
        }
    }
}
