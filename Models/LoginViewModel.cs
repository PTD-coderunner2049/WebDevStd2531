using System.ComponentModel.DataAnnotations;

namespace WebDevStd2531.Models
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public required string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        // Optional: for handling redirects after successful login
        public string? ReturnUrl { get; set; }

        // Optional: for handling external logins (if you uncomment that part of the view)
        // public IList<AuthenticationScheme>? ExternalLogins { get; set; } 
    }
}