using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        // creating the 1-to-many relationship: every category belongs to one specific event
        [ForeignKey("Event")]
        public int EventId { get; set; }
        public Event? Event { get; set; }

        [Required]
        public string CategoryName { get; set; } = string.Empty; // for eg "senior 10km" or "under 20 5km"

        [Required]
        public decimal DistanceKm { get; set; }

        [Required]
        public decimal EntryFee { get; set; }
    }
}