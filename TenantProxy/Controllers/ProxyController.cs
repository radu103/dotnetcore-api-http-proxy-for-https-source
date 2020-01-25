using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TenantProxy.Services;

namespace TenantProxy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProxyController : ControllerBase
    {
        private readonly ILogger<ProxyController> _logger;
        private IMemoryCache _memoryCache;

        private string _baseHref = string.Empty;

        public ProxyController(IMemoryCache memoryCache, ILogger<ProxyController> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _baseHref = "https://hxehost:51027/analytics.xsodata";
        }

        [HttpGet]
        [Route("/{tenantId}")]
        public ActionResult GetServiceDefinition([FromRoute] string tenantId)
        {
            var odataService = new TenantXSODataProxyService(_memoryCache, tenantId, "tenant", _baseHref);

            var result = odataService.GetServiceDefinition();

            Response.ContentType = "application/json";

            return Content(result);
        }

        [HttpGet]
        [Route("/{tenantId}/{entitySet}")]
        public ActionResult GetServiceData([FromRoute] string tenantId, [FromRoute] string entitySet)
        {
            var newBaseHref = string.Format("{0}://{1}", Request.Scheme.ToString(), Request.Host.ToUriComponent());
            var odataService = new TenantXSODataProxyService(_memoryCache, tenantId, "tenant", _baseHref, newBaseHref);

            var result = odataService.GetEntitySetData(entitySet, Request.QueryString.ToString());
            
            Response.ContentType = "application/json";

            return Content(result);
        }
    }
}
