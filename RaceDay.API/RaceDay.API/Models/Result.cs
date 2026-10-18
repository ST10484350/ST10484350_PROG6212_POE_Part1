using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    // organisers will use this to log the final finish times and positions for the participants
    public class Result
    {
        [Key]
        public int ResultId { get; set; }

        // tied directly to the enrolment so its known exactly who ran in which category
        [ForeignKey("Enrolment")]
        public int EnrolmentId { get; set; }
        public Enrolment? Enrolment { get; set; }

        // TimeSpan is perfect for capturing hours:minutes:seconds
        public TimeSpan FinishTime { get; set; }

        public int Position { get; set; }
    }
}