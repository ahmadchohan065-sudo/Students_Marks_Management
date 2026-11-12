using Microsoft.EntityFrameworkCore;
using MarksApi.Common;
using MarksApi.Data;
using MarksApi.Data.Entities;
using MarksApi.Dtos;

namespace MarksApi.Services;

public interface IEnrollmentService
{
    Task<EnrollmentResponse> EnrollAsync(int courseId, EnrollRequest request, CancellationToken ct);
    Task<PagedResult<EnrollmentDetailResponse>> ListForCourseAsync(int courseId, PageQuery q, CancellationToken ct);
    Task<EnrollmentResponse> SetMarkAsync(int id, MarkRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

public class EnrollmentService : IEnrollmentService
{
    private readonly AppDbContext _db;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(AppDbContext db, ILogger<EnrollmentService> logger)
    {
        _db = db; _logger = logger;
    }

    public async Task<EnrollmentResponse> EnrollAsync(int courseId, EnrollRequest request, CancellationToken ct)
    {
        if (!await _db.Courses.AnyAsync(c => c.Id == courseId, ct))
            throw new NotFoundException("Course not found", $"No course with id {courseId}.");
        if (!await _db.Students.AnyAsync(s => s.Id == request.StudentId, ct))
            throw new NotFoundException("Student not found", $"No student with id {request.StudentId}.");

        var enrollment = new Enrollment
        {
            StudentId = request.StudentId,
            CourseId = courseId,
            EnrolledAtUtc = DateTime.UtcNow
        };
        _db.Enrollments.Add(enrollment);

        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            // the unique index on (student_id, course_id) caught the double enrollment
            throw new ConflictException("Already enrolled", "This student is already enrolled in this course.");
        }

        _logger.LogInformation("Student {StudentId} enrolled in course {CourseId} (enrollment {Id})",
            request.StudentId, courseId, enrollment.Id);
        return new EnrollmentResponse(enrollment.Id, enrollment.StudentId, enrollment.CourseId, enrollment.Mark);
    }

    public async Task<PagedResult<EnrollmentDetailResponse>> ListForCourseAsync(int courseId, PageQuery q, CancellationToken ct)
    {
        if (!await _db.Courses.AnyAsync(c => c.Id == courseId, ct))
            throw new NotFoundException("Course not found", $"No course with id {courseId}.");

        IQueryable<Enrollment> query = _db.Enrollments.AsNoTracking().Where(e => e.CourseId == courseId);

        if (q.SafeSearch is { } s)
        {
            var pattern = $"%{s}%";
            query = query.Where(e => EF.Functions.ILike(e.Student.FullName, pattern)
                                  || EF.Functions.ILike(e.Student.RegistrationNo, pattern));
        }

        query = q.Sort?.Trim() switch
        {
            "name" => query.OrderBy(e => e.Student.FullName),
            "-name" => query.OrderByDescending(e => e.Student.FullName),
            "mark" => query.OrderBy(e => e.Mark),
            "-mark" => query.OrderByDescending(e => e.Mark),
            _ => query.OrderBy(e => e.Id)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((q.SafePage - 1) * q.SafePageSize)
            .Take(q.SafePageSize)
            .Select(e => new EnrollmentDetailResponse(e.Id, e.StudentId, e.Student.RegistrationNo,
                e.Student.FullName, e.Mark, e.EnrolledAtUtc))
            .ToListAsync(ct);

        return new PagedResult<EnrollmentDetailResponse>(items, q.SafePage, q.SafePageSize, total);
    }

    public async Task<EnrollmentResponse> SetMarkAsync(int id, MarkRequest request, CancellationToken ct)
    {
        var enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException("Enrollment not found", $"No enrollment with id {id}.");

        enrollment.Mark = request.Mark;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Mark for enrollment {EnrollmentId} set to {Mark}", id, request.Mark);
        return new EnrollmentResponse(enrollment.Id, enrollment.StudentId, enrollment.CourseId, enrollment.Mark);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException("Enrollment not found", $"No enrollment with id {id}.");

        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Enrollment {EnrollmentId} deleted", id);
    }
}