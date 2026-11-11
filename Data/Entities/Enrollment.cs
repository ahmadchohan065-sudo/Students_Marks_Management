namespace MarksApi.Data.Entities;

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public int? Mark { get; set; }          // null = not graded yet
    public DateTime EnrolledAtUtc { get; set; }

    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}