using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;

namespace RaceDay.MVC.Controllers
{
    public class EnrolmentsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // injecting the httpclient factory to communicate with the raceday api
        public EnrolmentsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // checking whether the current session belongs to a logged-in user
        private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId") != null;

        // checking whether the current session belongs to a participant
        private bool IsParticipant() => HttpContext.Session.GetString("Role") == "Participant";

        // fetching all enrolments for the currently logged-in participant
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!IsParticipant())
            {
                TempData["ErrorMessage"] = "Only participants have a My Enrolments list.";
                return RedirectToAction("Index", "Events");
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var response = await client.GetAsync("api/Enrolments/my-enrolments");

            var enrolments = new List<EnrolmentViewModel>();
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                enrolments = JsonSerializer.Deserialize<List<EnrolmentViewModel>>(json, _jsonOptions) ?? new();
            }
            else
            {
                TempData["ErrorMessage"] = "Could not load your enrolments.";
            }

            return View(enrolments);
        }

        // enrolling the logged-in participant into a specific race category
        [HttpPost]
        public async Task<IActionResult> Enrol(int categoryId, int eventId)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!IsParticipant())
            {
                TempData["ErrorMessage"] = "Only participants can enrol in races.";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var payload = JsonSerializer.Serialize(new { CategoryId = categoryId });

            var response = await client.PostAsync(
                "api/Enrolments",
                new StringContent(payload, Encoding.UTF8, "application/json")
            );

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Successfully enrolled in race category!";
                return RedirectToAction(nameof(Index));
            }

            var error = await response.Content.ReadAsStringAsync();
            TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(error)
                ? "Could not complete enrolment."
                : error.Trim('"');

            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        // cancelling an existing enrolment for the logged-in participant
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!IsParticipant())
            {
                TempData["ErrorMessage"] = "Only participants can cancel enrolments.";
                return RedirectToAction("Index", "Events");
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var response = await client.DeleteAsync($"api/Enrolments/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Enrolment cancelled successfully.";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(error)
                    ? "Could not cancel enrolment."
                    : error.Trim('"');
            }

            return RedirectToAction(nameof(Index));
        }
    }
}