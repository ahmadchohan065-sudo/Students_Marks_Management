using System.ComponentModel.DataAnnotations;

namespace MarksApi.Dtos;

public class CourseRequest
{
    [Required(ErrorMessage = "Course code is required.")]
    [StringLength(10, ErrorMessage = "Course code must be at most 10 characters.")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(150, ErrorMessage = "Title must be at most 150 characters.")]
    public string? Title { get; set; }

    [Range(1, 4, ErrorMessage = "Credit hours must be between 1 and 4.")]
    public int CreditHours { get; set; }
}

public record CourseResponse(int Id, string Code, string Title, int CreditHours);