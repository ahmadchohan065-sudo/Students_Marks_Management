namespace MarksApi.Data.Entities;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";   // "Teacher" or "Student"
    public int? StudentId { get; set; }

    public Student? Student { get; set; }
}