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
                "SENTIEL TESTS:",
                "/api/sentinel/singleclient/{datatype}/{testLoad}",
                "/api/sentinel/singleclient/failover/{datatype}/{testLoad}",
                "/api/sentinel/multipleclients/{datatype}/{testLoad}",
                "/api/sentinel/multipleclients/failover/{datatype}/{testLoad}",
                "/api/sentinel/flush",
                "CLUSTER TESTS:",
                "/api/cluster/singleclient/{datatype}/{testLoad}",
                "/api/cluster/singleclient/failover/{datatype}/{testLoad}",
                "/api/cluster/multipleclients/{datatype}/{testLoad}",
                "/api/cluster/multipleclients/failover/{datatype}/{testLoad}",
                "/api/cluster/flush"
            };
        }
    }
}
