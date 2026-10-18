using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        // i made this optional in case the organiser doesn't have a full description yet
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public decimal Distance { get; set; }

        [Required]
        public string EventType { get; set; } = string.Empty; // keeping this restricted to "run", "walk", or "cycle" based on my Part 1 spec

        // linking this event to the specific Organiser who created it
        [ForeignKey("Organiser")]
        public int OrganiserId { get; set; }
        public User? Organiser { get; set; }
    }
}