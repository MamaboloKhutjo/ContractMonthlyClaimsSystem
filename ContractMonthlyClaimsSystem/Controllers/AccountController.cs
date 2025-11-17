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
            HttpContext.Session.Clear();
            _userRepository.CurrentUser = null;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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


                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetString("UserRole", user.Role);

                return RedirectToAction("Dashboard", GetControllerName(user.Role));
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if user already exists
            bool userExists = await _userRepository.UserExistsAsync(model.Name, model.Surname);
            if (userExists)
            {
                ModelState.AddModelError("", "A user with this name already exists. Please use a different name or log in.");
                return View(model);
            }

            // Create new user
            var user = new User
            {
                Name = model.Name.Trim(),
                Surname = model.Surname.Trim(),
                Password = model.Password,
                Role = model.Role
            };

            await _userRepository.AddUserAsync(user);

            TempData["SuccessMessage"] = "Registration successful! Please log in with your credentials.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // Clear session
            HttpContext.Session.Clear();
            _userRepository.CurrentUser = null;

            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Helper method to get controller name based on role
        private string GetControllerName(string role)
        {
            return role.ToLower() switch
            {
                "lecturer" => "Lecturer",
                "manager" => "Manager",
                "program coordinator" => "Manager",
                _ => "Home"
            };
        }
    }
}
