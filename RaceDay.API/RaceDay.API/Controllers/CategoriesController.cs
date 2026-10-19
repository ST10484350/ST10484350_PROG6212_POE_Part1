using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories/event/{eventId}
        // get all categories for a specific event so participants know what they can enrol in
        [HttpGet("event/{eventId}")]
        public IActionResult GetCategoriesForEvent(int eventId)
        {
            // check if they are logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return Unauthorized("You must be logged in to view categories.");
            }

            var categories = _context.Categories
                .Where(c => c.EventId == eventId)
                .ToList();

            return Ok(categories);
        }

        // POST: api/Categories
        [HttpPost]
        public IActionResult CreateCategory([FromBody] CategoryCreateDto request)
        {
            // verify they are an logged in and have the organiser role
            var role = HttpContext.Session.GetString("Role");
            if (role != "Organiser")
            {
                return Unauthorized("Access Denied: Only Organisers can create race categories.");
            }

            // map the dto to the model
            var newCategory = new Category
            {
                EventId = request.EventId,
                CategoryName = request.CategoryName,
                DistanceKm = request.DistanceKm,
                EntryFee = request.EntryFee
            };

            _context.Categories.Add(newCategory);
            _context.SaveChanges();

            return Ok(new { message = "Category created successfully.", categoryId = newCategory.CategoryId });
        }
    }

    // helper class to shape the incoming json data for creating a category
    public class CategoryCreateDto
    {
        public required int EventId { get; set; }
        public required string CategoryName { get; set; }
        public required decimal DistanceKm { get; set; }
        public required decimal EntryFee { get; set; }
    }
}