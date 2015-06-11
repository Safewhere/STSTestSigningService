#region

using System.Security.Cryptography.X509Certificates;

#endregion

namespace Kombit.Samples.STSTestSigningService.Code
{
    /// <summary>
    ///     An interface to specify all attributes, methods used in token signing service
    /// </summary>
    public interface ITokenSigningService
    {
        string UpdateToken(string encodedRstr, X509Certificate2 certificate);
    }
}