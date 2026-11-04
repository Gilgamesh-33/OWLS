using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OWLS.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, StringLength(100)]
        public string FullName { get; set; }

        [Display(Name = "ID Number (LRN / Employee No.)"), StringLength(30)]
        public string IdNumber { get; set; }
    }
}
