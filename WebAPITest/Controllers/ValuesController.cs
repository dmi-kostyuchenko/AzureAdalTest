using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;

namespace WebAPITest.Controllers
{
    [Authorize]
    public class ValuesController : ApiController
    {
        private static ConcurrentBag<String> _valuesStorage = new ConcurrentBag<String>();

        // GET api/values
        [HttpGet]
        public IEnumerable<String> Get()
        {
            ValidatePermissions();

            return _valuesStorage.ToArray();
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]String value)
        {
            ValidatePermissions();

            _valuesStorage.Add(value);
        }

        private void ValidatePermissions()
        {
            //
            // The Scope claim tells you what permissions the client application has in the service.
            // In this case we look for a scope value of user_impersonation, or full access to the service as the user.
            //
            var scopeClaim = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/scope");
            if (scopeClaim != null)
            {
                if (scopeClaim.Value != "user_impersonation")
                {
                    throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "The Scope claim does not contain 'user_impersonation' or scope claim not found" });
                }
            }
        }
    }
}
