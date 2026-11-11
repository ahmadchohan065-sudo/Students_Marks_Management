namespace MarksApi.Data.Entities;

public class Student
{
    public int Id { get; set; }
    public string RegistrationNo { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }

    public List<Enrollment> Enrollments { get; set; } = new();
}