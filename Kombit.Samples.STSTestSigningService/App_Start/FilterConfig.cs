#region

using System.Web.Mvc;

#endregion

namespace Kombit.Samples.STSTestSigningService
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}