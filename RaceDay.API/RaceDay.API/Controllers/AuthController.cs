using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BlobStorageService _blobStorageService;

        // injecting the db context and blob storage service for auth and profile picture uploads
        public AuthController(AppDbContext context, BlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
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

            // returning user details so the mvc app can store user id, name, and role in session
            return Ok(new
            {
                message = "Login successful.",
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                role = user.Role,
                profilePictureUrl = user.ProfilePictureUrl
            });
        }

        // POST: api/Auth/Logout
        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            // clearing the active server session when the user logs out
            HttpContext.Session.Clear();
            return Ok(new { message = "Logged out successfully." });
        }

        // GET: api/Auth/Profile/5
        [HttpGet("Profile/{id}")]
        public IActionResult GetProfile(int id)
        {
            // fetching the user's profile info without exposing their password hash
            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(new
            {
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                role = user.Role,
                profilePictureUrl = user.ProfilePictureUrl
            });
        }

        // PUT: api/Auth/Profile/5
        [HttpPut("Profile/{id}")]
        public IActionResult UpdateProfile(int id, [FromBody] UserProfileUpdateDto request)
        {
            // finding the user record to update their personal details
            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // checking that the new email isn't already used by a different account
            if (_context.Users.Any(u => u.Email == request.Email && u.UserId != id))
            {
                return BadRequest("That email address is already in use by another account.");
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;

            _context.SaveChanges();
            return Ok(new { message = "Profile updated successfully." });
        }

        // POST: api/Auth/Profile/5/picture
        [HttpPost("Profile/{id}/picture")]
        public async Task<IActionResult> UploadProfilePicture(int id, IFormFile file)
        {
            // looking up the user before uploading their profile photo to azure
            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // validating that a valid image file was provided
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

            // uploading the profile picture to azure blob storage and saving the url in the database
            var imageUrl = await _blobStorageService.UploadFileAsync(file, "profiles");
            user.ProfilePictureUrl = imageUrl;
            _context.SaveChanges();

            return Ok(new { message = "Profile picture uploaded successfully.", profilePictureUrl = imageUrl });
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

    // helper class for updating participant profile information
    public class UserProfileUpdateDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
    }
}