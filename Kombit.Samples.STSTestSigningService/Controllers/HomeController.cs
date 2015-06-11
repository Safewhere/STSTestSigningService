#region

using System.Web.Mvc;

#endregion

namespace Kombit.Samples.STSTestSigningService.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }
    }
}