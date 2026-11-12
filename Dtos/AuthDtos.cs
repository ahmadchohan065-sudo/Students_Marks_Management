using System.ComponentModel.DataAnnotations;

namespace MarksApi.Dtos;

public class LoginRequest
{
    [Required(ErrorMessage = "Username is required.")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    public string? Password { get; set; }
}

public record LoginResponse(string Token, DateTime ExpiresAtUtc, string Role);