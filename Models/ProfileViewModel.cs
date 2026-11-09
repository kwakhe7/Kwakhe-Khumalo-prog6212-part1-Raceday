using System.ComponentModel.DataAnnotations;

namespace RACEDAY_P3.Models;

public class ProfileViewModel
{
    // The two fields the user can change
    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(100, ErrorMessage = "Your name must be 100 characters or fewer.")]
    [Display(Name = "Full name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(150)]
    public string Email { get; set; } = "";

    // Read-only details shown on the page (filled from the API, never taken from the form)
    public int UserId { get; set; }
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool HasProfilePicture { get; set; }
}