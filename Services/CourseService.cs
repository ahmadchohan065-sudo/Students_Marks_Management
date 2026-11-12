using Microsoft.EntityFrameworkCore;
using MarksApi.Common;
using MarksApi.Data;
using MarksApi.Data.Entities;
using MarksApi.Dtos;

namespace MarksApi.Services;

public interface ICourseService
{
    Task<PagedResult<CourseResponse>> ListAsync(PageQuery q, CancellationToken ct);
    Task<CourseResponse> GetAsync(int id, CancellationToken ct);
    Task<CourseResponse> CreateAsync(CourseRequest r, CancellationToken ct);
    Task<CourseResponse> UpdateAsync(int id, CourseRequest r, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

public class CourseService : ICourseService
{
    private const string DuplicateTitle = "Duplicate course";
    private const string DuplicateDetail = "A course with this code already exists.";

    private readonly AppDbContext _db;
    private readonly ILogger<CourseService> _logger;

    public CourseService(AppDbContext db, ILogger<CourseService> logger)
    {
        _db = db; _logger = logger;
    }

    public async Task<PagedResult<CourseResponse>> ListAsync(PageQuery q, CancellationToken ct)
    {
        IQueryable<Course> query = _db.Courses.AsNoTracking();

        if (q.SafeSearch is { } s)
        {
            var pattern = $"%{s}%";
            query = query.Where(x => EF.Functions.ILike(x.Code, pattern) || EF.Functions.ILike(x.Title, pattern));
        }

        query = q.Sort?.Trim() switch
        {
            "code" => query.OrderBy(x => x.Code),
            "-code" => query.OrderByDescending(x => x.Code),
            "title" => query.OrderBy(x => x.Title),
            "-title" => query.OrderByDescending(x => x.Title),
            "credits" => query.OrderBy(x => x.CreditHours),
            "-credits" => query.OrderByDescending(x => x.CreditHours),
            _ => query.OrderBy(x => x.Id)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((q.SafePage - 1) * q.SafePageSize)
            .Take(q.SafePageSize)
            .Select(x => new CourseResponse(x.Id, x.Code, x.Title, x.CreditHours))
            .ToListAsync(ct);

        return new PagedResult<CourseResponse>(items, q.SafePage, q.SafePageSize, total);
    }

    public async Task<CourseResponse> GetAsync(int id, CancellationToken ct)
    {
        var course = await _db.Courses.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CourseResponse(x.Id, x.Code, x.Title, x.CreditHours))
            .FirstOrDefaultAsync(ct);
        return course ?? throw new NotFoundException("Course not found", $"No course with id {id}.");
    }

    public async Task<CourseResponse> CreateAsync(CourseRequest r, CancellationToken ct)
    {
        var course = new Course
        {
            Code = r.Code!.Trim().ToUpperInvariant(),
            Title = r.Title!.Trim(),
            CreditHours = r.CreditHours,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Courses.Add(course);

        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            throw new ConflictException(DuplicateTitle, DuplicateDetail);
        }

        _logger.LogInformation("Course {CourseId} created ({Code})", course.Id, course.Code);
        return new CourseResponse(course.Id, course.Code, course.Title, course.CreditHours);
    }

    public async Task<CourseResponse> UpdateAsync(int id, CourseRequest r, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Course not found", $"No course with id {id}.");

        course.Code = r.Code!.Trim().ToUpperInvariant();
        course.Title = r.Title!.Trim();
        course.CreditHours = r.CreditHours;

        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            throw new ConflictException(DuplicateTitle, DuplicateDetail);
        }

        _logger.LogInformation("Course {CourseId} updated", id);
        return new CourseResponse(course.Id, course.Code, course.Title, course.CreditHours);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Course not found", $"No course with id {id}.");

        _db.Courses.Remove(course);
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (DbErrors.IsForeignKeyViolation(ex))
        {
            throw new ConflictException("Course has enrollments", "This course still has enrollments and cannot be deleted.");
        }

        _logger.LogInformation("Course {CourseId} deleted", id);
    }
}