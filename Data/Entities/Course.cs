namespace MarksApi.Data.Entities;

public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public int CreditHours { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<Enrollment> Enrollments { get; set; } = new();
}