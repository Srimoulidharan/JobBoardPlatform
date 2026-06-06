using Microsoft.AspNetCore.Mvc;

namespace JobBoardPlatform.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult Register()
        {
            return RedirectToAction("Register", "Account");
        }
    }
}
