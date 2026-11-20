using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Html;

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
        [Required(ErrorMessage = "School ID is required."), StringLength(30), Display(Name = "School ID")]
        public string IdNumber { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
        [Required, DataType(DataType.Password)] public string Password { get; set; }
        [DataType(DataType.Password), Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class SettingsViewModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string IdNumber { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsAdmin { get; set; }
        public int LoanDays { get; set; }
        public decimal PenaltyPerDay { get; set; }
        public int TotalTitles { get; set; }
        public int TotalBorrowers { get; set; }
        public int TotalTransactions { get; set; }
    }

    // Shared page heading used by every signed-in admin/borrower page.
    public class PageHeaderViewModel
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public Func<object, IHtmlContent> Actions { get; set; }
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

    // Admin borrower profile: grouped data so the details page can stay readable.
    public class BorrowerDetailsViewModel
    {
        public ApplicationUser User { get; set; }
        public bool IsAdmin { get; set; }
        public int PendingCount { get; set; }
        public int ActiveCount { get; set; }
        public int OverdueCount { get; set; }
        public int TotalRecords { get; set; }
        public List<BorrowTransaction> ActiveLoans { get; set; } = new();
        public List<BorrowTransaction> Attention { get; set; } = new();
        public List<BorrowTransaction> PastTransactions { get; set; } = new();
    }

    // Admin form for editing a user's account details.
    public class EditBorrowerViewModel
    {
        public string Id { get; set; } = "";
        public bool IsAdmin { get; set; }

        [Required(ErrorMessage = "Full name is required."), StringLength(100), Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "School ID is required."), StringLength(30), Display(Name = "School ID")]
        public string IdNumber { get; set; } = "";

        [Required(ErrorMessage = "Email is required."), EmailAddress, StringLength(256), Display(Name = "Email")]
        public string Email { get; set; } = "";
    }

    public class BorrowPageViewModel
    {
        public List<BorrowTransaction> PendingRequests { get; set; } = new();
        public List<BorrowTransaction> ActiveLoans { get; set; } = new();
        public List<BorrowTransaction> OverdueLoans { get; set; } = new();
        public List<BorrowTransaction> History { get; set; } = new();
    }
}