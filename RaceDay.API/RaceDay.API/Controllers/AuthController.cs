using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Auth/Register
        [HttpPost("Register")]
        public IActionResult Register([FromBody] UserRegisterDto request)
        {
            // check the database to make sure this email isn't already taken
            if (_context.Users.Any(u => u.Email == request.Email))
            {
                return BadRequest("A user with this email already exists.");
            }

            // enforce the role requirements from the brief (must be organiser or participant)
            if (request.Role != "Organiser" && request.Role != "Participant")
            {
                return BadRequest("Invalid role. Role must be either 'Organiser' or 'Participant'.");
            }

            // map the incoming data to the User model and securely hash the password using BCrypt
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Role = request.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { message = "User registered successfully." });
        }

        // POST: api/Auth/Login
        [HttpPost("Login")]
        public IActionResult Login([FromBody] UserLoginDto request)
        {
            // find the user by their email address
            var user = _context.Users.SingleOrDefault(u => u.Email == request.Email);

            // if the user doesn't exist or the password hash doesn't match, reject the login
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid email or password.");
            }

            // password is correct, so I'm setting up the server-side session here to track who is logged in and their role
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Role", user.Role);

            return Ok(new { message = "Login successful.", role = user.Role });
        }
    }

    // helper class to define the JSON structure we expect when someone registers
    public class UserRegisterDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Role { get; set; }
    }

    // helper class for the login payload so we only ask for email and password
    public class UserLoginDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}