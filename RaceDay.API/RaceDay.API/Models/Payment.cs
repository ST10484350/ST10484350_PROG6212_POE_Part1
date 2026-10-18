using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    // tracking the entry fee payments for each enrolment
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        // linking the payment directly to the specific enrolment record
        [ForeignKey("Enrolment")]
        public int EnrolmentId { get; set; }
        public Enrolment? Enrolment { get; set; }

        [Required]
        public decimal AmountPaid { get; set; }

        // setting a default status. in practice this must connect to a payment gateway
        public string PaymentStatus { get; set; } = "Successful";

        public DateTime PaymentDate { get; set; } = DateTime.Now;
    }
}