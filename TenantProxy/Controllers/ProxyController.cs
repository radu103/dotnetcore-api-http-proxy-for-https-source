using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TenantProxy.Services;

namespace TenantProxy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProxyController : ControllerBase
    {
        private readonly ILogger<ProxyController> _logger;
        private string _baseHref = string.Empty;

        public ProxyController(ILogger<ProxyController> logger)
        {
            _logger = logger;
            _baseHref = "https://hxehost:51027/analytics.xsodata";
        }

        [HttpGet]
        [Route("/{tenantId}")]
        public ActionResult GetData([FromRoute] string tenantId)
        {
            var odataService = new TenantXSODataProxyService(tenantId, _baseHref);
            var result = odataService.GetServiceDefinition();

            dynamic jsonObject = JsonConvert.DeserializeObject(result);
            var reducedJson = JsonConvert.SerializeObject(jsonObject.d.results);

            Response.ContentType = "application/json";
            return Content(reducedJson);
        }

        [HttpGet]
        [Route("/{tenantId}/{entitySet}")]
        public ActionResult GetData([FromRoute] string tenantId, [FromRoute] string entitySet)
        {
            var odataService = new TenantXSODataProxyService(tenantId, _baseHref, "tenant");
            var result = odataService.GetEntitySetData(entitySet, Request.QueryString.ToString());
            
            dynamic jsonObject = JsonConvert.DeserializeObject(result);
            var reducedJson = JsonConvert.SerializeObject(jsonObject.d.results);
            
            Response.ContentType = "application/json";
            return Content(reducedJson);
        }
    }
}
