using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class ResultEntryViewModel
    {
        public int EnrolmentId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? ResultId { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }

        [Required]
        [RegularExpression(@"^([0-9]+):([0-5][0-9]):([0-5][0-9])$", ErrorMessage = "Use hh:mm:ss, for example 01:24:37.")]
        public string FinishTimeInput { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Position must be at least 1.")]
        public int PositionInput { get; set; }
    }

    public class ParticipantResultViewModel
    {
        public int EnrolmentId { get; set; }
        public int CategoryId { get; set; }
        public DateTime EnrolmentDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventLocation { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public int? ResultId { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
    }
}
