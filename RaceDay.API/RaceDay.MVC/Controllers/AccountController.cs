using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;

namespace RaceDay.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // injecting the httpclient factory to communicate with the raceday api
        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // displaying the login page
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // sending login credentials to the api and saving the returned user details in session
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var json = JsonSerializer.Serialize(new { model.Email, model.Password });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/Auth/Login", content);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // reading the api response and storing user info in mvc session state
            var responseBody = await response.Content.ReadAsStringAsync();
            var loginData = JsonSerializer.Deserialize<LoginApiResponse>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (loginData != null)
            {
                HttpContext.Session.SetInt32("UserId", loginData.UserId);
                HttpContext.Session.SetString("Role", loginData.Role);
                HttpContext.Session.SetString("FirstName", loginData.FirstName);
                HttpContext.Session.SetString("LastName", loginData.LastName);
                HttpContext.Session.SetString("Email", loginData.Email);

                if (!string.IsNullOrEmpty(loginData.ProfilePictureUrl))
                {
                    HttpContext.Session.SetString("ProfilePictureUrl", loginData.ProfilePictureUrl);
                }

                // saving the api's session cookie so subsequent api requests stay authenticated
                if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
                {
                    var cookieHeader = string.Join("; ", setCookies.Select(c => c.Split(';')[0]));
                    HttpContext.Session.SetString("ApiSessionCookie", cookieHeader);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        // displaying the registration page
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // sending new user details to the api to create an organiser or participant account
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var json = JsonSerializer.Serialize(new
            {
                model.FirstName,
                model.LastName,
                model.Email,
                model.Password,
                model.Role
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/Auth/Register", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(errorMessage) ? "Registration failed." : errorMessage.Trim('"'));
                return View(model);
            }

            // passing a success banner to the login page after registering
            TempData["SuccessMessage"] = "Account created successfully! Please log in.";
            return RedirectToAction("Login");
        }

        // clearing the mvc session and logging the user out
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var client = _httpClientFactory.CreateClient("RaceDayApi");
            await client.PostAsync("api/Auth/Logout", null);

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}