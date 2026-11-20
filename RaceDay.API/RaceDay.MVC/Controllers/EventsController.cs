using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;

namespace RaceDay.MVC.Controllers
{
    public class EventsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // injecting the httpclient factory to communicate with the raceday api
        public EventsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // checking whether the current session belongs to a logged-in user
        private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId") != null;

        // checking whether the current session belongs to an organiser
        private bool IsOrganiser() => HttpContext.Session.GetString("Role") == "Organiser";

        // fetching all events from the api and applying optional search or type filters
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? eventType)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var response = await client.GetAsync("api/Events");

            var events = new List<EventViewModel>();
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                events = JsonSerializer.Deserialize<List<EventViewModel>>(json, _jsonOptions) ?? new();
            }

            // filtering events by title or location when a search term is provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                events = events.Where(e =>
                    e.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    e.Location.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // filtering events by category when an event type is selected
            if (!string.IsNullOrWhiteSpace(eventType))
            {
                events = events.Where(e =>
                    e.EventType.Equals(eventType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.Search = search;
            ViewBag.EventType = eventType;
            return View(events);
        }

        // fetching a single event by id along with its race categories and participant enrolment status
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var response = await client.GetAsync($"api/Events/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Index));
            }

            var json = await response.Content.ReadAsStringAsync();
            var ev = JsonSerializer.Deserialize<EventViewModel>(json, _jsonOptions);

            // loading the race categories linked to this event
            var categories = new List<CategoryViewModel>();
            var catResponse = await client.GetAsync($"api/Categories/event/{id}");
            if (catResponse.IsSuccessStatusCode)
            {
                var catJson = await catResponse.Content.ReadAsStringAsync();
                categories = JsonSerializer.Deserialize<List<CategoryViewModel>>(catJson, _jsonOptions) ?? new();
            }
            ViewBag.Categories = categories;

            // checking which categories the participant is already enrolled in so the ui can reflect it
            var enrolledCategoryIds = new HashSet<int>();
            if (HttpContext.Session.GetString("Role") == "Participant")
            {
                var enrolResponse = await client.GetAsync("api/Enrolments/my-enrolments");
                if (enrolResponse.IsSuccessStatusCode)
                {
                    var enrolJson = await enrolResponse.Content.ReadAsStringAsync();
                    var enrolments = JsonSerializer.Deserialize<List<EnrolmentViewModel>>(enrolJson, _jsonOptions) ?? new();
                    enrolledCategoryIds = enrolments.Select(e => e.CategoryId).ToHashSet();
                }
            }
            ViewBag.EnrolledCategoryIds = enrolledCategoryIds;

            return View(ev);
        }

        // adding a new race category to an event for organisers
        [HttpPost]
        public async Task<IActionResult> AddCategory(CategoryViewModel model)
        {
            if (!IsOrganiser())
            {
                TempData["ErrorMessage"] = "Only organisers can add race categories.";
                return RedirectToAction(nameof(Details), new { id = model.EventId });
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please enter a valid category name, distance, and entry fee.";
                return RedirectToAction(nameof(Details), new { id = model.EventId });
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var payload = JsonSerializer.Serialize(new
            {
                model.EventId,
                model.CategoryName,
                model.DistanceKm,
                model.EntryFee
            });

            var response = await client.PostAsync(
                "api/Categories",
                new StringContent(payload, Encoding.UTF8, "application/json")
            );

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = $"Category '{model.CategoryName}' added successfully!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(error)
                    ? "Could not add category."
                    : error.Trim('"');
            }

            return RedirectToAction(nameof(Details), new { id = model.EventId });
        }

        // displaying the create event form for organisers
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsOrganiser())
            {
                TempData["ErrorMessage"] = "Only organisers can create events.";
                return RedirectToAction(nameof(Index));
            }

            return View(new EventFormViewModel());
        }

        // submitting a new event to the api and uploading its optional banner image to azure
        [HttpPost]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            if (!IsOrganiser())
            {
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var payload = JsonSerializer.Serialize(new
            {
                model.Title,
                model.Description,
                model.EventDate,
                model.Location,
                model.Distance,
                model.EventType
            });

            var response = await client.PostAsync(
                "api/Events",
                new StringContent(payload, Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(error) ? "Could not create event." : error.Trim('"'));
                return View(model);
            }

            // reading the created event id so we can attach the banner image if one was chosen
            var responseBody = await response.Content.ReadAsStringAsync();
            var createdEvent = JsonSerializer.Deserialize<EventViewModel>(responseBody, _jsonOptions);

            if (createdEvent != null && model.BannerImage != null && model.BannerImage.Length > 0)
            {
                await UploadBannerAsync(client, createdEvent.EventId, model.BannerImage);
            }

            TempData["SuccessMessage"] = "Event created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // loading an existing event into the edit form for organisers
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsOrganiser())
            {
                TempData["ErrorMessage"] = "Only organisers can edit events.";
                return RedirectToAction(nameof(Index));
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var response = await client.GetAsync($"api/Events/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Index));
            }

            var json = await response.Content.ReadAsStringAsync();
            var ev = JsonSerializer.Deserialize<EventViewModel>(json, _jsonOptions);

            if (ev == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var formModel = new EventFormViewModel
            {
                EventId = ev.EventId,
                Title = ev.Title,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location,
                Distance = ev.Distance,
                EventType = ev.EventType,
                ExistingBannerUrl = ev.BannerImageUrl
            };

            return View(formModel);
        }

        // updating an existing event via the api and replacing its banner image if a new file is uploaded
        [HttpPost]
        public async Task<IActionResult> Edit(int id, EventFormViewModel model)
        {
            if (!IsOrganiser())
            {
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var payload = JsonSerializer.Serialize(new
            {
                model.Title,
                model.Description,
                model.EventDate,
                model.Location,
                model.Distance,
                model.EventType
            });

            var response = await client.PutAsync(
                $"api/Events/{id}",
                new StringContent(payload, Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(error) ? "Could not update event." : error.Trim('"'));
                return View(model);
            }

            if (model.BannerImage != null && model.BannerImage.Length > 0)
            {
                await UploadBannerAsync(client, id, model.BannerImage);
            }

            TempData["SuccessMessage"] = "Event updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // deleting an event through the api for organisers
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsOrganiser())
            {
                TempData["ErrorMessage"] = "Only organisers can delete events.";
                return RedirectToAction(nameof(Index));
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var response = await client.DeleteAsync($"api/Events/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Event deleted successfully.";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(error)
                    ? $"Could not delete event (HTTP {(int)response.StatusCode})."
                    : $"Could not delete event: {error.Trim('"')}";
            }

            return RedirectToAction(nameof(Index));
        }

        // sending a multipart banner image upload request to the api's azure blob endpoint
        private static async Task UploadBannerAsync(HttpClient client, int eventId, IFormFile bannerImage)
        {
            using var formData = new MultipartFormDataContent();
            using var stream = bannerImage.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(bannerImage.ContentType);

            formData.Add(fileContent, "file", bannerImage.FileName);
            await client.PostAsync($"api/Events/{eventId}/banner", formData);
        }
    }
}