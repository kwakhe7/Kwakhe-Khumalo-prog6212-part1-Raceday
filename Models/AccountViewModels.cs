using System.ComponentModel.DataAnnotations;

namespace RACEDAY_P3.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(100, ErrorMessage = "Your name must be 100 characters or fewer.")]
    [Display(Name = "Full name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(150)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Please choose a role.")]
    [Display(Name = "I am joining as")]
    public string Role { get; set; } = "";
}