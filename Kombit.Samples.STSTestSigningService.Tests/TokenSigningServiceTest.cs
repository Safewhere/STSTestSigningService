#region

using System.Configuration;
using Kombit.Samples.Common;
using Kombit.Samples.STSTestSigningService.Code;
using Xunit;

#endregion

namespace Kombit.Samples.STSTestSigningService.Tests
{
    /// <summary>
    ///     A collection of method which will test against Token signing service
    /// </summary>
    public class TokenSigningServiceTest
    {
        /// <summary>
        ///     A test is to make sure that token signing service can update a token with Sha1 signing algorithm
        /// </summary>
        [Fact]
        public void TokenSigningServiceCanUpdateToken()
        {
            // Set up
            string certificateThumbprint = ConfigurationManager.AppSettings["SigningCertificateThumbprint"];
            var certificate = CertificateLoader.LoadCertificateFromMyStore(certificateThumbprint);

            var tss = new TokenSigningService();

            // Exercise system
            string updatedToken = tss.UpdateToken(Constants.Based64SecurityTokenRaw, certificate);

            // Verify
            TokenHelper.AssertUpdatedToken(updatedToken, certificate, Constants.Based64SecurityTokenRaw);
        }

        /// <summary>
        ///     A test is to make sure that token signing service can update a token with Sha256 signing algorithm
        /// </summary>
        [Fact]
        public void TokenSigningServiceCanUpdateTokenSha256()
        {
            // Set up
            string certificateThumbprint = ConfigurationManager.AppSettings["SigningCertificateThumbprint"];
            var certificate = CertificateLoader.LoadCertificateFromMyStore(certificateThumbprint);

            var tss = new TokenSigningService();

            // Exercise system
            string updatedToken = tss.UpdateToken(Constants.SecurityTokenRawSha256, certificate);

            // Verify
            TokenHelper.AssertUpdatedToken(updatedToken, certificate, Constants.SecurityTokenRaw);
        }
    }
}