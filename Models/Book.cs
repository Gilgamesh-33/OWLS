using System.ComponentModel.DataAnnotations;

namespace OWLS.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string ISBN { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        [Required, StringLength(120)]
        public string Author { get; set; }

        [Required, StringLength(60)]
        public string Category { get; set; }

        [Display(Name = "Year Published"), Range(1000, 2100)]
        public int YearPublished { get; set; }

        [Display(Name = "Total Copies"), Range(1, 1000)]
        public int TotalCopies { get; set; }

        [Display(Name = "Available")]
        public int AvailableCopies { get; set; }

        public List<BorrowTransaction> Transactions { get; set; }

        [Display(Name = "Cover Image"), StringLength(200)]
        public string CoverImagePath { get; set; }
    }
}
