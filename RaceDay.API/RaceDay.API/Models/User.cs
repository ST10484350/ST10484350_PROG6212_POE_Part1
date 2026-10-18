using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    // i decided to use a single User table for organisers and participants 
    // to keep the database normalized. the 'Role' column will tell what they can access.
    public class User
    {
        [Key]
        public int UserId { get; set; } // primary key, EF Core will handle the auto-incrementing for me

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty; // this will act as the username during the login process

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // storing the BCrypt hash here. making sure its not plain text passwords

        [Required]
        public string Role { get; set; } = string.Empty; // must be "organiser" or "participant" for session management
    }
}