#region

using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Kombit.Samples.Common;
using Kombit.Samples.STSTestSigningService.Code;

#endregion

namespace Kombit.Samples.STSTestSigningService.Controllers
{
    /// <summary>
    ///     A controller to handle signing request
    /// </summary>
    public class SecurityTokenServiceTestSigningController : ApiController
    {
        private readonly ITokenSigningService tokenSigningService;

        /// <summary>
        ///     This is just a simple service. Use poor-man DI
        /// </summary>
        public SecurityTokenServiceTestSigningController()
            : this(new TokenSigningService())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the SecurityTokenServiceTestSigningController class with a TokenSigningService
        /// </summary>
        /// <param name="tokenSigningService"></param>
        public SecurityTokenServiceTestSigningController(ITokenSigningService tokenSigningService)
        {
            if (tokenSigningService == null)
                throw new ArgumentNullException("tokenSigningService");

            this.tokenSigningService = tokenSigningService;
        }

        /// <summary>
        ///     GET doesn't make sense because of message size will definitely exceed the limit
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<IHttpActionResult> Get()
        {
            var result = await Task.FromResult("Hello");
            return Ok(result);
        }

        /// <summary>
        ///     API to update the request security token response. Includes signature, NotBefore, NotOnOrAfter
        /// </summary>
        /// <param name="originRstr">the Rstr xml string in url-encoded base64 format</param>
        /// <returns>the update rstr xml string in url-encoded base64 format</returns>
        public HttpResponseMessage Post([FromBody] string originRstr)
        {
            try
            {
                if (string.IsNullOrEmpty(originRstr))
                {
                    Logging.Instance.Information("Returning code is {HttpStatusCode}", HttpStatusCode.BadRequest);
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "Origin request security token is null or empty");
                }
                Logging.Instance.Debug("Received message {originRstr} with thumbprint.", originRstr);

                string certificateThumbprint = ConfigurationManager.AppSettings["SigningCertificateThumbprint"];
                X509Certificate2 certificate = CertificateLoader.LoadCertificateFromMyStore(certificateThumbprint);
                var updatedMessage = tokenSigningService.UpdateToken(originRstr, certificate);

                Logging.Instance.Information("Returning code is {HttpStatusCode}", HttpStatusCode.OK);
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(updatedMessage, Encoding.UTF8, "text/plain")
                };
                return response;
            }
            catch (Exception ex)
            {
                Logging.Instance.Error(ex,
                    "An error has occurred while updating a token. Returning code is {HttpStatusCode}",
                    HttpStatusCode.InternalServerError);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message, ex);
            }
        }
    }
}