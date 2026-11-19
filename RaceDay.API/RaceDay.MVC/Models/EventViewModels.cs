using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    // mapping event details returned from the raceday api
    public class EventViewModel
    {
        public int EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public double Distance { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public int OrganiserId { get; set; }
    }

    // holding the form fields and validation rules for creating or editing an event
    public class EventFormViewModel
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Event title is required.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event date is required.")]
        [Display(Name = "Event Date & Time")]
        public DateTime EventDate { get; set; } = DateTime.Now.AddDays(7);

        [Required(ErrorMessage = "Location is required.")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Distance is required.")]
        [Range(0.1, 1000, ErrorMessage = "Distance must be greater than 0.")]
        [Display(Name = "Distance (km)")]
        public double Distance { get; set; }

        [Required(ErrorMessage = "Please select an event type.")]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = "run";

        [Display(Name = "Event Banner Image")]
        public IFormFile? BannerImage { get; set; }

        public string? ExistingBannerUrl { get; set; }
    }
}