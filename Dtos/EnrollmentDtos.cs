using System.ComponentModel.DataAnnotations;

namespace MarksApi.Dtos;

public class EnrollRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid studentId is required.")]
    public int StudentId { get; set; }
}

public class MarkRequest
{
    [Required(ErrorMessage = "Mark is required.")]
    [Range(0, 100, ErrorMessage = "Mark must be a whole number from 0 to 100.")]
    public int? Mark { get; set; }
}

public record EnrollmentResponse(int Id, int StudentId, int CourseId, int? Mark);

public record EnrollmentDetailResponse(int Id, int StudentId, string RegistrationNo,
    string FullName, int? Mark, DateTime EnrolledAtUtc);