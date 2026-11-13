using System.ComponentModel.DataAnnotations;

namespace OWLS.Models
{
    public class LoginViewModel
    {
        [Required, EmailAddress] public string Email { get; set; }
        [Required, DataType(DataType.Password)] public string Password { get; set; }
        [Display(Name = "Remember me")] public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required, Display(Name = "Full Name")] public string FullName { get; set; }
        [Display(Name = "ID Number (LRN / Employee No.)")] public string IdNumber { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
        [Required, DataType(DataType.Password)] public string Password { get; set; }
        [DataType(DataType.Password), Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class TopBook { public string Title { get; set; } public int Count { get; set; } }
    public class ChartPoint { public string Label { get; set; } = ""; public int Value { get; set; } }
    public class CategoryPoint { public string Category { get; set; } = ""; public int TotalCopies { get; set; } public int AvailableCopies { get; set; } public int BorrowedCopies { get; set; } }

    public class ReportViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string Range { get; set; } = "custom";
        public int TotalBorrowed { get; set; }
        public int TotalReturned { get; set; }
        public int ActiveLoans { get; set; }
        public int OverdueCount { get; set; }
        public int ApprovedCount { get; set; }
        public int ReturnedAllTime { get; set; }
        public decimal TotalPenalties { get; set; }
        public int PendingRequests { get; set; }
        public int DeclinedRequests { get; set; }
        public int TotalTitles { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int BorrowedCopiesNow { get; set; }
        public List<TopBook> TopBooks { get; set; } = new();
        public List<BorrowTransaction> OverdueLog { get; set; } = new();
        public List<ChartPoint> BorrowTrend { get; set; } = new();
        public List<ChartPoint> MonthlyReturns { get; set; } = new();
        public List<ChartPoint> StatusBreakdown { get; set; } = new();
        public List<CategoryPoint> InventoryByCategory { get; set; } = new();
        public List<ChartPoint> AvailabilityBreakdown { get; set; } = new();
    }

    public class BorrowPageViewModel
    {
        public List<Book> Books { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public List<int> MyPendingBookIds { get; set; } = new();
        public List<int> MyBorrowedBookIds { get; set; } = new();
        public List<BorrowTransaction> PendingRequests { get; set; } = new();
        public List<BorrowTransaction> ActiveLoans { get; set; } = new();
        public List<BorrowTransaction> OverdueLoans { get; set; } = new();
        public List<BorrowTransaction> History { get; set; } = new();
        public string Search { get; set; }
        public string Category { get; set; }
        public string Sort { get; set; }
        public bool AvailableOnly { get; set; }
    }
}