using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EnrolmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Enrolments/my-enrolments
        [HttpGet("my-enrolments")]
        public IActionResult GetMyEnrolments()
        {
            // checking the session
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized("You must be logged in to view your enrolments.");
            }

            // fetching enrolments with category and event details projected cleanly for the mvc client
            var enrolments = _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c!.Event)
                .Where(e => e.ParticipantId == userId)
                .OrderByDescending(e => e.EnrolmentDate)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.CategoryId,
                    e.EnrolmentDate,
                    CategoryName = e.Category != null ? e.Category.CategoryName : string.Empty,
                    DistanceKm = e.Category != null ? e.Category.DistanceKm : 0,
                    EntryFee = e.Category != null ? e.Category.EntryFee : 0,
                    EventId = e.Category != null ? e.Category.EventId : 0,
                    EventTitle = e.Category != null && e.Category.Event != null ? e.Category.Event.Title : string.Empty,
                    EventDate = e.Category != null && e.Category.Event != null ? e.Category.Event.EventDate : DateTime.MinValue,
                    EventLocation = e.Category != null && e.Category.Event != null ? e.Category.Event.Location : string.Empty,
                    BannerImageUrl = e.Category != null && e.Category.Event != null ? e.Category.Event.BannerImageUrl : null
                })
                .ToList();

            return Ok(enrolments);
        }

        // POST: api/Enrolments
        [HttpPost]
        public IActionResult CreateEnrolment([FromBody] EnrolmentCreateDto request)
        {
            // verify session and participant role
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null || role != "Participant")
            {
                return Unauthorized("Access Denied: Only logged-in Participants can enrol in races.");
            }

            // verifying that the selected category exists
            var categoryExists = _context.Categories.Any(c => c.CategoryId == request.CategoryId);
            if (!categoryExists)
            {
                return NotFound("Selected race category was not found.");
            }

            // stopping them from enrolling in the exact same category twice
            var alreadyEnrolled = _context.Enrolments.Any(e => e.ParticipantId == userId && e.CategoryId == request.CategoryId);
            if (alreadyEnrolled)
            {
                return BadRequest("You are already enrolled in this race category.");
            }

            // create the enrolment using the session ID and the requested Category ID
            var newEnrolment = new Enrolment
            {
                ParticipantId = userId.Value,
                CategoryId = request.CategoryId
            };

            _context.Enrolments.Add(newEnrolment);
            _context.SaveChanges();

            return Ok(new { message = "Successfully enrolled!", enrolmentId = newEnrolment.EnrolmentId });
        }

        // DELETE: api/Enrolments/5
        [HttpDelete("{id}")]
        public IActionResult CancelEnrolment(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null || role != "Participant")
            {
                return Unauthorized("Access Denied: Only logged-in Participants can cancel enrolments.");
            }

            var enrolment = _context.Enrolments.FirstOrDefault(e => e.EnrolmentId == id);
            if (enrolment == null)
            {
                return NotFound("Enrolment not found.");
            }

            if (enrolment.ParticipantId != userId.Value)
            {
                return StatusCode(403, "You can only cancel your own enrolments.");
            }

            _context.Enrolments.Remove(enrolment);
            _context.SaveChanges();

            return Ok(new { message = "Enrolment cancelled successfully." });
        }
    }

    // helper class so the frontend only needs to send the CategoryId
    public class EnrolmentCreateDto
    {
        public required int CategoryId { get; set; }
    }
}