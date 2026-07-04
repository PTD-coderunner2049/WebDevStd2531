using Microsoft.AspNetCore.Identity;

namespace UserService.Models;

public class AppUser : IdentityUser
{
    public string? FullName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
}
