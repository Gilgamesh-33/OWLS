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

    public class ReportViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int TotalBorrowed { get; set; }
        public int TotalReturned { get; set; }
        public int ActiveLoans { get; set; }
        public int OverdueCount { get; set; }
        public decimal TotalPenalties { get; set; }
        public List<TopBook> TopBooks { get; set; } = new();
        public List<BorrowTransaction> OverdueLog { get; set; } = new();
    }
}
