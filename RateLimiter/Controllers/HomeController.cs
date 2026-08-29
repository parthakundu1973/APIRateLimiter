using Microsoft.AspNetCore.Mvc;
using APIRateLimiter.Models;
using System.Diagnostics;

namespace APIRateLimiter.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

    }
}
