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
            _baseHref = "https://hxehost:51096/analytics.xsodata";
        }

        [HttpGet]
        [Route("/favicon.ico")]
        [ResponseCache(Duration=int.MaxValue)]
        public ActionResult GetFavicon([FromRoute] string tenantId)
        {
            return NotFound();
        }

        [HttpGet]
        [Route("/{tenantId}")]
        [ResponseCache(NoStore=true)]
        public ActionResult GetServiceDefinition([FromRoute] string tenantId)
        {
            var odataService = new TenantXSODataProxyService(_memoryCache, tenantId, "tenant", _baseHref);

            var result = odataService.GetServiceDefinition();

            Response.ContentType = "application/json";

            return Content(result);            
        }

        [HttpGet]
        [Route("/{tenantId}/{entitySet}")]
        [ResponseCache(NoStore = true)]
        public ActionResult GetServiceData([FromRoute] string tenantId, [FromRoute] string entitySet)
        {
            var newBaseHref = string.Format("{0}://{1}/{2}", Request.Scheme.ToString(), Request.Host.ToUriComponent(), tenantId);
            
            var odataService = new TenantXSODataProxyService(_memoryCache, tenantId, "tenant", _baseHref, newBaseHref);

            var result = odataService.GetEntitySetData(entitySet, Request.QueryString.ToString());
            
            Response.ContentType = "application/json";

            return Content(result);
        }
    }
}
