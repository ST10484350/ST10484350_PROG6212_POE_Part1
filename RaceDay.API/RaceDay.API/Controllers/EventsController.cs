using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BlobStorageService _blobStorageService;

        // injecting the db context and blob storage service for event and banner operations
        public EventsController(AppDbContext context, BlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        // checking session first and falling back to headers sent from the mvc app
        private int? GetCurrentUserId()
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId != null) return sessionUserId;

            if (Request.Headers.TryGetValue("X-User-Id", out var headerVal) && int.TryParse(headerVal, out var parsedId))
            {
                return parsedId;
            }

            return null;
        }

        // reading the logged-in user's role from session or mvc request headers
        private string? GetCurrentUserRole()
        {
            var sessionRole = HttpContext.Session.GetString("Role");
            if (!string.IsNullOrEmpty(sessionRole)) return sessionRole;

            if (Request.Headers.TryGetValue("X-User-Role", out var headerRole))
            {
                return headerRole.ToString();
            }

            return null;
        }

        // GET: api/Events
        [HttpGet]
        public IActionResult GetEvents()
        {
            // anyone logged in can view the races, so i'll just make sure they have an active session
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("You must be logged in to view events.");
            }

            var events = _context.Events.ToList();
            return Ok(events);
        }

        // GET: api/Events/5
        [HttpGet("{id}")]
        public IActionResult GetEventById(int id)
        {
            // checking that the user is logged in before returning full event details
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("You must be logged in to view event details.");
            }

            // finding the requested event by its id
            var raceEvent = _context.Events.FirstOrDefault(e => e.EventId == id);
            if (raceEvent == null)
            {
                return NotFound("Event not found.");
            }

            return Ok(raceEvent);
        }

        // POST: api/Events
        [HttpPost]
        public IActionResult CreateEvent([FromBody] EventCreateDto request)
        {
            // check the session to get the logged-in user's id and role
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            // security: only logged-in organisers can create new races
            if (userId == null || role != "Organiser")
            {
                return Unauthorized("Access Denied: Only logged-in Organisers can create events.");
            }

            // map the DTO to the actual Event model. securely mapping OrganiserId directly to the logged-in user from the session.
            var newEvent = new Event
            {
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                EventDate = request.EventDate,
                Location = request.Location,
                Distance = request.Distance,
                EventType = request.EventType,
                BannerImageUrl = request.BannerImageUrl,
                OrganiserId = userId.Value
            };

            _context.Events.Add(newEvent);
            _context.SaveChanges();

            return Ok(new { message = "Event created successfully.", eventId = newEvent.EventId });
        }

        // PUT: api/Events/5
        [HttpPut("{id}")]
        public IActionResult UpdateEvent(int id, [FromBody] EventCreateDto request)
        {
            // checking that only a logged-in organiser is editing the event
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            if (userId == null || role != "Organiser")
            {
                return Unauthorized("Access Denied: Only logged-in Organisers can edit events.");
            }

            // looking up the event and making sure it belongs to this organiser
            var existingEvent = _context.Events.FirstOrDefault(e => e.EventId == id);
            if (existingEvent == null)
            {
                return NotFound("Event not found.");
            }

            if (existingEvent.OrganiserId != userId.Value)
            {
                return StatusCode(403, "You can only modify or delete events that you created.");
            }

            // updating all editable event fields from the request
            existingEvent.Title = request.Title;
            existingEvent.Description = request.Description ?? string.Empty;
            existingEvent.EventDate = request.EventDate;
            existingEvent.Location = request.Location;
            existingEvent.Distance = request.Distance;
            existingEvent.EventType = request.EventType;

            if (!string.IsNullOrEmpty(request.BannerImageUrl))
            {
                existingEvent.BannerImageUrl = request.BannerImageUrl;
            }

            _context.SaveChanges();
            return Ok(new { message = "Event updated successfully.", eventId = existingEvent.EventId });
        }

        // DELETE: api/Events/5
        [HttpDelete("{id}")]
        public IActionResult DeleteEvent(int id)
        {
            // verifying that only a logged-in organiser can delete an event
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            if (userId == null || role != "Organiser")
            {
                return Unauthorized("Access Denied: Only logged-in Organisers can delete events.");
            }

            // finding the event and confirming ownership before removing it
            var existingEvent = _context.Events.FirstOrDefault(e => e.EventId == id);
            if (existingEvent == null)
            {
                return NotFound("Event not found.");
            }

            if (existingEvent.OrganiserId != userId.Value)
            {
                return StatusCode(403, "You can only modify or delete events that you created.");
            }

            // Enrolments are retained records and their CategoryId FK is intentionally
            // restrictive. Do not cascade-delete enrolments (or their payments/results)
            // when an organiser removes an event.
            var enrolmentCount = _context.Enrolments.Count(enrolment =>
                _context.Categories.Any(category =>
                    category.CategoryId == enrolment.CategoryId && category.EventId == id));

            if (enrolmentCount > 0)
            {
                return Conflict(new
                {
                    message = "This event cannot be deleted because participants are enrolled in one or more of its categories.",
                    enrolmentCount
                });
            }

            _context.Events.Remove(existingEvent);
            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException exception) when (IsCategoryEnrolmentDeleteConflict(exception))
            {
                // An enrolment could be added after the pre-check. Convert that
                // race into the same safe conflict response instead of a 500.
                return Conflict(new
                {
                    message = "This event cannot be deleted because participants are enrolled in one or more of its categories."
                });
            }

            return Ok(new { message = "Event deleted successfully." });
        }

        private static bool IsCategoryEnrolmentDeleteConflict(DbUpdateException exception)
        {
            for (var current = exception.InnerException; current != null; current = current.InnerException)
            {
                if (current is SqlException sqlException &&
                    sqlException.Number == 547 &&
                    sqlException.Message.Contains(
                        "FK_Enrolments_Categories_CategoryId",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // POST: api/Events/5/banner
        [HttpPost("{id}/banner")]
        public async Task<IActionResult> UploadEventBanner(int id, IFormFile file)
        {
            // verifying that only a logged-in organiser can upload an event banner
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            if (userId == null || role != "Organiser")
            {
                return Unauthorized("Access Denied: Only logged-in Organisers can upload event banners.");
            }

            // checking that the event exists and belongs to this organiser before uploading to azure
            var raceEvent = _context.Events.FirstOrDefault(e => e.EventId == id);
            if (raceEvent == null)
            {
                return NotFound("Event not found.");
            }

            if (raceEvent.OrganiserId != userId.Value)
            {
                return StatusCode(403, "You can only upload banners for events that you created.");
            }

            // validating that a real image file was attached
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please select an image file to upload.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("Only .jpg, .jpeg, .png, and .webp image files are allowed.");
            }

            // uploading the banner to azure blob storage and saving the returned url to sql server
            var imageUrl = await _blobStorageService.UploadFileAsync(file, "banners");
            raceEvent.BannerImageUrl = imageUrl;
            _context.SaveChanges();

            return Ok(new { message = "Banner uploaded successfully.", bannerImageUrl = imageUrl });
        }
    }

    // helper class so the frontend doesn't need to pass an OrganiserId (we get it securely from the session instead)
    public class EventCreateDto
    {
        public required string Title { get; set; }
        public string? Description { get; set; }
        public required DateTime EventDate { get; set; }
        public required string Location { get; set; }
        public required decimal Distance { get; set; }
        public required string EventType { get; set; }
        public string? BannerImageUrl { get; set; }
    }
}