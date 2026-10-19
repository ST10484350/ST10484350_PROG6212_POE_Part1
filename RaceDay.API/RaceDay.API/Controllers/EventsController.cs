using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EventsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Events
        [HttpGet]
        public IActionResult GetEvents()
        {
            // anyone logged in can view the races, so i'll just make sure they have an active session
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized("You must be logged in to view events.");
            }

            var events = _context.Events.ToList();
            return Ok(events);
        }

        // POST: api/Events
        [HttpPost]
        public IActionResult CreateEvent([FromBody] EventCreateDto request)
        {
            // check the session to get the logged-in user's id and role
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

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
                OrganiserId = userId.Value
            };

            _context.Events.Add(newEvent);
            _context.SaveChanges();

            return Ok(new { message = "Event created successfully.", eventId = newEvent.EventId });
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
    }
}