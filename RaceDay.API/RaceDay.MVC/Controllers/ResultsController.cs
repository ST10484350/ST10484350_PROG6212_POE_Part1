using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;

namespace RaceDay.MVC.Controllers
{
    public class ResultsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ResultsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private bool IsOrganiser() => HttpContext.Session.GetString("Role") == "Organiser";

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsOrganiser())
            {
                TempData["ErrorMessage"] = "Only organisers can manage race results.";
                return RedirectToAction("Index", "Home");
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var response = await client.GetAsync("api/Results/organiser-enrolments");
            var results = new List<ResultEntryViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                results = JsonSerializer.Deserialize<List<ResultEntryViewModel>>(json, JsonOptions) ?? new();
            }
            else
            {
                TempData["ErrorMessage"] = "Could not load your event participants.";
            }

            return View(results);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(ResultEntryViewModel model)
        {
            if (!IsOrganiser())
            {
                return RedirectToAction("Index", "Home");
            }

            if (!TryParseFinishTime(model.FinishTimeInput, out var finishTime))
            {
                ModelState.AddModelError(nameof(model.FinishTimeInput), "Use a valid time in hh:mm:ss format.");
            }

            if (!ModelState.IsValid)
            {
                var client = _httpClientFactory.CreateClient("RaceDayApi");
                var response = await client.GetAsync("api/Results/organiser-enrolments");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var results = JsonSerializer.Deserialize<List<ResultEntryViewModel>>(json, JsonOptions) ?? new();
                    var selected = results.FirstOrDefault(r => r.EnrolmentId == model.EnrolmentId);
                    if (selected != null)
                    {
                        selected.FinishTimeInput = model.FinishTimeInput;
                        selected.PositionInput = model.PositionInput;
                    }
                    return View("Index", results);
                }

                return RedirectToAction(nameof(Index));
            }

            var apiClient = _httpClientFactory.CreateClient("RaceDayApi");
            var payload = JsonSerializer.Serialize(new
            {
                model.EnrolmentId,
                FinishTime = finishTime,
                Position = model.PositionInput
            });

            var apiResponse = await apiClient.PostAsync(
                "api/Results",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            if (apiResponse.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Race result saved successfully.";
            }
            else
            {
                var error = await apiResponse.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(error)
                    ? "Could not save the race result."
                    : error.Trim('"');
            }

            return RedirectToAction(nameof(Index));
        }

        private static bool TryParseFinishTime(string? value, out TimeSpan finishTime)
        {
            finishTime = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var parts = value.Split(':');
            if (parts.Length != 3 ||
                !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours) ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
                !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
                hours < 0 || minutes is < 0 or > 59 || seconds is < 0 or > 59)
            {
                return false;
            }

            finishTime = new TimeSpan(hours, minutes, seconds);
            return finishTime > TimeSpan.Zero;
        }
    }
}
