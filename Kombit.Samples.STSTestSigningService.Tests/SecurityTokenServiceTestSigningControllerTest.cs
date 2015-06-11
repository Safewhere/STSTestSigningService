#region

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Kombit.Samples.Common;
using Xunit;

#endregion

namespace Kombit.Samples.STSTestSigningService.Tests
{
    /// <summary>
    ///     A collection of methods which will test again SecurityTokenServiceTestTestSigningController
    /// </summary>
    public class SecurityTokenServiceTestSigningControllerTest
    {
        /// <summary>
        ///     A test to make sure that it is able to post message to update with form encoded content
        /// </summary>
        [Fact]
        public void CanPostMessageToUpdateWithFormEncodedContent()
        {
            // Set up
            string certificateThumbprint = ConfigurationManager.AppSettings["SigningCertificateThumbprint"];
            var certificate = CertificateLoader.LoadCertificateFromMyStore(certificateThumbprint);

            // Start OWIN host 
            using (HostProvider.Start<Startup>(url: Constants.BaseAddress))
            {
                // Create HttpCient and make a request to api/values 
                HttpClient client = new HttpClient {BaseAddress = new Uri(Constants.BaseAddress)};
                var formUrlEncodedContent = new FormUrlEncodedContent(
                    new[]
                    {
                        new KeyValuePair<string, string>("", Constants.Based64SecurityTokenRaw)
                    }
                    );

                formUrlEncodedContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                // Exercise system
                var response = client.PostAsync(Constants.ApiUrl, formUrlEncodedContent).Result;

                Console.WriteLine(response);
                string result = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine(result);

                // Verify
                TokenHelper.AssertUpdatedToken(result, certificate, Constants.Based64SecurityTokenRaw);
            }
        }

        /// <summary>
        ///     A test to make sure that it can post message to update with string content
        /// </summary>
        [Fact]
        public void CanPostMessageToUpdateWithStringContent()
        {
            // Set up
            string certificateThumbprint = ConfigurationManager.AppSettings["SigningCertificateThumbprint"];
            var certificate = CertificateLoader.LoadCertificateFromMyStore(certificateThumbprint);

            // Start OWIN host 
            using (HostProvider.Start<Startup>(url: Constants.BaseAddress))
            {
                // Create HttpCient and make a request to api/values 
                HttpClient client = new HttpClient {BaseAddress = new Uri(Constants.BaseAddress)};
                HttpContent content = new StringContent("=" + HttpUtility.UrlEncode(Constants.Based64SecurityTokenRaw));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                // Exercise system
                var response = client.PostAsync(Constants.ApiUrl, content).Result;

                Console.WriteLine(response);
                string result = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine(result);

                // Verify
                TokenHelper.AssertUpdatedToken(result, certificate, Constants.Based64SecurityTokenRaw);
            }
        }
    }
}