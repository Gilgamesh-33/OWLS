using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OWLS.Models
{
    public static class LibrarySettings
    {
        public const int DefaultLoanDays = 7;
        public const decimal PenaltyPerDay = 5m; // PHP per overdue day
    }

    public class BorrowTransaction
    {
        public int Id { get; set; }

        public int BookId { get; set; }
        public Book Book { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [DataType(DataType.Date)]
        public DateTime BorrowDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Penalty { get; set; }

        [NotMapped]
        public string Status => ReturnDate != null ? "Returned"
            : DateTime.Today > DueDate.Date ? "Overdue" : "Borrowed";

        [NotMapped]
        public decimal CurrentPenalty => ReturnDate != null ? Penalty
            : DateTime.Today > DueDate.Date
                ? (DateTime.Today - DueDate.Date).Days * LibrarySettings.PenaltyPerDay : 0m;
    }
}
