using System.ComponentModel.DataAnnotations;

namespace MarksApi.Dtos;

public class StudentRequest
{
    [Required(ErrorMessage = "Registration number is required.")]
    [StringLength(20, ErrorMessage = "Registration number must be at most 20 characters.")]
    public string? RegistrationNo { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name must be at most 100 characters.")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
    [StringLength(150, ErrorMessage = "Email must be at most 150 characters.")]
    public string? Email { get; set; }
}

public record StudentResponse(int Id, string RegistrationNo, string FullName, string Email);