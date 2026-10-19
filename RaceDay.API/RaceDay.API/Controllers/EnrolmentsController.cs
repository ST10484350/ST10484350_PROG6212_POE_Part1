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

            // fetch only the enrolments from the logged-in user
            var enrolments = _context.Enrolments
                .Include(e => e.Category) // get the category details too
                .Where(e => e.ParticipantId == userId)
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
                // EnrolmentDate is now set to DateTime.Now by default
            };

            _context.Enrolments.Add(newEnrolment);
            _context.SaveChanges();

            return Ok(new { message = "Successfully enrolled!", enrolmentId = newEnrolment.EnrolmentId });
        }
    }

    // helper class so the frontend only needs to send the CategoryId
    public class EnrolmentCreateDto
    {
        public required int CategoryId { get; set; }
    }
}