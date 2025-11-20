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
            Console.WriteLine($"Login attempt for: {model.Username}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state invalid");
                return View(model);
            }

            var user = await _userRepository.GetUserByCredentialsAsync(model.Username, model.Password);
            Console.WriteLine($"User found: {user != null}");

            if (user != null)
            {
                Console.WriteLine($"User role: {user.Role}");

                // Store user in session
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetString("UserRole", user.Role);

                TempData["SuccessMessage"] = $"Welcome back, {user.Name}!";

                // Redirect based on role
                if (user.Role.ToLower() == "lecturer")
                {
                    Console.WriteLine("Redirecting to Lecturer Dashboard");
                    return RedirectToAction("Dashboard", "Lecturer");
                }
                else if (user.Role.ToLower() == "manager" || user.Role.ToLower() == "program coordinator")
                {
                    Console.WriteLine("Redirecting to Manager Dashboard");
                    return RedirectToAction("Dashboard", "Manager");
                }
                else if (user.Role.ToLower() == "hr")
                {
                    Console.WriteLine("Redirecting to HR Dashboard");
                    return RedirectToAction("Dashboard", "HR");
                }
                else
                {
                    Console.WriteLine($"Unknown role: {user.Role}, redirecting to Home");
                    return RedirectToAction("Index", "Home");
                }
            }

            Console.WriteLine("Invalid credentials");
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
                "hr" => "HR",
                _ => "Home"
            };
        }
    }
}
