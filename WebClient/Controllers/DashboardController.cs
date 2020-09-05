using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebClient.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("lvl") == "Sales" || HttpContext.Session.GetString("lvl") == "Admin");
            {
                return View();
            }
            return RedirectToAction("Login", "Account");
        }
    }
}