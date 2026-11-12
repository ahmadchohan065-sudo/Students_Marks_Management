namespace MarksApi.Dtos;

public record CourseStatsResponse(int CourseId, string CourseCode, int Enrolled, int Graded,
    double? Average, int? Highest, int? Lowest, int Passed);

public record TranscriptCourse(string Code, int CreditHours, int? Mark, bool? Passed);

public record TranscriptResponse(int StudentId, string RegistrationNo, string FullName,
    IReadOnlyList<TranscriptCourse> Courses, double? WeightedAverage);