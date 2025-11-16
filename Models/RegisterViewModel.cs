using System.ComponentModel.DataAnnotations;

namespace WebDevStd2531.Models
{
    public class RegisterViewModel
    {
        // === Identity Fields ===
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public required string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public required string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public required string ConfirmPassword { get; set; }

        // === AppUser Additional Fields ===
        [Required(ErrorMessage = "UserName is required.")]
        [Display(Name = "UserName")]
        [StringLength(100)]
        public required string UserName { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Address")]
        [StringLength(200)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Gender selection is required.")]
        [Display(Name = "Gender")]
        public required string Gender { get; set; }
    }
}