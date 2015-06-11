#region

using System;
using Microsoft.Owin.Hosting;

#endregion

namespace Kombit.Samples.STSTestSigningService.Tests
{
    /// <summary>
    /// </summary>
    public static class HostProvider
    {
        /// <summary>
        ///     Specify which environment will start the service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <returns></returns>
        public static IDisposable Start<T>(string url)
        {
#if DEBUG
            return WebApp.Start<Startup>(url);
#endif
#if !DEBUG
            return new EmptyWebApp();
#endif
        }

        private class EmptyWebApp : IDisposable
        {
            public void Dispose()
            {
                // no-op
            }
        }
    }
}