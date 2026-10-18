using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    // this is the bridge table that connects a participant to a specific category
    public class Enrolment
    {
        [Key]
        public int EnrolmentId { get; set; }

        // linking back to the user (who must have the participant role)
        [ForeignKey("Participant")]
        public int ParticipantId { get; set; }
        public User? Participant { get; set; }

        // linking to the specific race category they chose (eg 10km)
        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // automatically capture when they entered the race
        public DateTime EnrolmentDate { get; set; } = DateTime.Now;
    }
}