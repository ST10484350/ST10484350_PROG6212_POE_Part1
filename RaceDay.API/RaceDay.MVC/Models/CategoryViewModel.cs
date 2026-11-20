using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [Range(0.1, 500, ErrorMessage = "Distance must be greater than 0.")]
        [Display(Name = "Distance (km)")]
        public decimal DistanceKm { get; set; }

        [Required]
        [Range(0, 100000, ErrorMessage = "Entry fee cannot be negative.")]
        [Display(Name = "Entry Fee (R)")]
        public decimal EntryFee { get; set; }
    }
}