using System.Diagnostics;
using ContractMonthlyClaimsSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace ContractMonthlyClaimsSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }
    }
}
