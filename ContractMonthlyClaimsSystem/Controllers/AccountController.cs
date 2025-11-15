using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ContractMonthlyClaimsSystem.Models;
using ContractMonthlyClaimsSystem.Services;
using System.Threading.Tasks;

namespace ContractMonthlyClaimsSystem.Controllers
{
    public class AccountController : Controller
    {

        private readonly IUserRepository _userRepository;

            public AccountController(IUserRepository userRepository)
            {
                _userRepository = userRepository;
            }

            [HttpGet]
            public IActionResult Login()
            {
                return View();
            }

            [HttpPost]
            public async Task<IActionResult> Login(LoginViewModel model)
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = await _userRepository.GetUserByCredentialsAsync(model.Username, model.Password);
                if (user != null)
                {
                    _userRepository.CurrentUser = user;

                    // Store user in session
                    HttpContext.Session.SetString("UserId", user.Id.ToString());
                    HttpContext.Session.SetString("UserName", user.FullName);
                    HttpContext.Session.SetString("UserRole", user.Role);

                    // Redirect based on role
                    return RedirectToAction("Dashboard", user.Role.ToLower().Replace(" ", ""));
                }

                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            [HttpGet]
            public IActionResult Register()
            {
                return View();
            }

            [HttpPost]
            public async Task<IActionResult> Register(RegisterViewModel model)
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = new User
                {
                    Name = model.Name,
                    Surname = model.Surname,
                    Password = model.Password,
                    Role = model.Role
                };

                await _userRepository.AddUserAsync(user);

                TempData["SuccessMessage"] = "Registration successful! Please log in.";
                return RedirectToAction("Login");
            }

            [HttpPost]
            public IActionResult Logout()
            {
                HttpContext.Session.Clear();
                _userRepository.CurrentUser = null;
                return RedirectToAction("Index", "Home");
            }
        }
}
