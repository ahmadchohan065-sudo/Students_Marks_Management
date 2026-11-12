using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MarksApi.Common;
using MarksApi.Data;
using MarksApi.Dtos;

namespace MarksApi.Services;

public interface IReportService
{
    Task<CourseStatsResponse> CourseStatsAsync(int courseId, CancellationToken ct);
    Task<TranscriptResponse> TranscriptAsync(int studentId, CurrentUser user, CancellationToken ct);
}

public class ReportService : IReportService
{
    private readonly AppDbContext _db;
    private readonly int _passMark;

    public ReportService(AppDbContext db, IOptions<MarkingOptions> marking)
    {
        _db = db;
        _passMark = marking.Value.PassMark;   // from appsettings.json
    }

    public async Task<CourseStatsResponse> CourseStatsAsync(int courseId, CancellationToken ct)
    {
        var course = await _db.Courses.AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new { c.Id, c.Code })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Course not found", $"No course with id {courseId}.");

        var marks = await _db.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => e.Mark)
            .ToListAsync(ct);

        var graded = marks.Where(m => m.HasValue).Select(m => m!.Value).ToList();

        return new CourseStatsResponse(
            course.Id, course.Code,
            Enrolled: marks.Count,
            Graded: graded.Count,
            Average: graded.Count == 0 ? null : Math.Round(graded.Average(), 2),   // null, not 0
            Highest: graded.Count == 0 ? null : graded.Max(),
            Lowest: graded.Count == 0 ? null : graded.Min(),
            Passed: graded.Count(m => m >= _passMark));
    }

    public async Task<TranscriptResponse> TranscriptAsync(int studentId, CurrentUser user, CancellationToken ct)
    {
        if (!user.IsTeacher && user.StudentId != studentId)
            throw new ForbiddenException("Forbidden", "You can only view your own transcript.");

        var student = await _db.Students.AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new { s.Id, s.RegistrationNo, s.FullName })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Student not found", $"No student with id {studentId}.");

        var rows = await _db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .OrderBy(e => e.Course.Code)
            .Select(e => new { e.Course.Code, e.Course.CreditHours, e.Mark })
            .ToListAsync(ct);

        var courses = rows
            .Select(r => new TranscriptCourse(r.Code, r.CreditHours, r.Mark,
                r.Mark.HasValue ? r.Mark.Value >= _passMark : (bool?)null))
            .ToList();

        // Weighted average = sum(mark x credits) / sum(credits), graded courses only
        var graded = rows.Where(r => r.Mark.HasValue).ToList();
        double? weighted = null;
        if (graded.Count > 0)
        {
            var totalCredits = graded.Sum(r => r.CreditHours);
            var points = graded.Sum(r => (double)r.Mark!.Value * r.CreditHours);
            weighted = Math.Round(points / totalCredits, 2);
        }

        return new TranscriptResponse(student.Id, student.RegistrationNo, student.FullName, courses, weighted);
    }
}