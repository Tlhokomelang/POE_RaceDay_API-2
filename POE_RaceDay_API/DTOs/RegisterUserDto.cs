using System.ComponentModel.DataAnnotations;

namespace POE_RaceDay_API.DTOs
{
    public class RegisterUserDto
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(72, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            "^(Participant|Organiser)$",
            ErrorMessage = "Role must be Participant or Organiser.")]
        public string Role { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }
    }
}