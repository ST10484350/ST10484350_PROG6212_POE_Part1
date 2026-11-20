using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ResultsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("organiser-enrolments")]
        public IActionResult GetOrganiserEnrolments()
        {
            var organiserId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (organiserId == null || role != "Organiser")
            {
                return Unauthorized("Only logged-in organisers can manage results.");
            }

            var enrolments = _context.Enrolments
                .AsNoTracking()
                .Include(e => e.Participant)
                .Include(e => e.Category)
                    .ThenInclude(c => c!.Event)
                .Include(e => e.Result)
                .Where(e => e.Category != null && e.Category.Event != null && e.Category.Event.OrganiserId == organiserId.Value)
                .OrderBy(e => e.Category!.Event!.EventDate)
                .ThenBy(e => e.Participant!.LastName)
                .Select(e => new
                {
                    e.EnrolmentId,
                    ParticipantName = e.Participant!.FirstName + " " + e.Participant.LastName,
                    ParticipantEmail = e.Participant.Email,
                    EventTitle = e.Category!.Event!.Title,
                    EventDate = e.Category.Event.EventDate,
                    CategoryName = e.Category.CategoryName,
                    ResultId = e.Result != null ? e.Result.ResultId : (int?)null,
                    FinishTime = e.Result != null ? e.Result.FinishTime : (TimeSpan?)null,
                    Position = e.Result != null ? e.Result.Position : (int?)null
                })
                .ToList();

            return Ok(enrolments);
        }

        [HttpGet("my-upcoming")]
        public IActionResult GetMyUpcomingResults()
        {
            var participantId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (participantId == null || role != "Participant")
            {
                return Unauthorized("Only logged-in participants can view their results.");
            }

            var now = DateTime.Now;
            var enrolments = _context.Enrolments
                .AsNoTracking()
                .Include(e => e.Category)
                    .ThenInclude(c => c!.Event)
                .Include(e => e.Result)
                .Where(e => e.ParticipantId == participantId.Value && e.Category != null && e.Category.Event != null &&
                    (e.Category.Event.EventDate >= now || e.Result != null))
                .OrderBy(e => e.Category!.Event!.EventDate)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.CategoryId,
                    e.EnrolmentDate,
                    CategoryName = e.Category!.CategoryName,
                    DistanceKm = e.Category.DistanceKm,
                    EntryFee = e.Category.EntryFee,
                    EventId = e.Category.EventId,
                    EventTitle = e.Category.Event!.Title,
                    EventDate = e.Category.Event.EventDate,
                    EventLocation = e.Category.Event.Location,
                    BannerImageUrl = e.Category.Event.BannerImageUrl,
                    ResultId = e.Result != null ? e.Result.ResultId : (int?)null,
                    FinishTime = e.Result != null ? e.Result.FinishTime : (TimeSpan?)null,
                    Position = e.Result != null ? e.Result.Position : (int?)null
                })
                .ToList();

            return Ok(enrolments);
        }

        [HttpPost]
        public IActionResult SaveResult([FromBody] SaveResultDto request)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (organiserId == null || role != "Organiser")
            {
                return Unauthorized("Only logged-in organisers can record results.");
            }

            if (request.FinishTime <= TimeSpan.Zero || request.Position <= 0)
            {
                return BadRequest("Finish time and position must be positive values.");
            }

            var enrolment = _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c!.Event)
                .Include(e => e.Result)
                .FirstOrDefault(e => e.EnrolmentId == request.EnrolmentId);

            if (enrolment == null)
            {
                return NotFound("The enrolment was not found.");
            }

            if (enrolment.Category?.Event?.OrganiserId != organiserId.Value)
            {
                return Forbid();
            }

            if (enrolment.Result == null)
            {
                enrolment.Result = new Result
                {
                    EnrolmentId = enrolment.EnrolmentId,
                    FinishTime = request.FinishTime,
                    Position = request.Position
                };
                _context.Results.Add(enrolment.Result);
            }
            else
            {
                enrolment.Result.FinishTime = request.FinishTime;
                enrolment.Result.Position = request.Position;
            }

            _context.SaveChanges();
            return Ok(new { message = "Result saved successfully.", resultId = enrolment.Result.ResultId });
        }
    }

    public class SaveResultDto
    {
        public int EnrolmentId { get; set; }
        public TimeSpan FinishTime { get; set; }
        public int Position { get; set; }
    }
}
