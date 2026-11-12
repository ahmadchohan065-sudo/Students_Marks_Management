using Microsoft.EntityFrameworkCore;
using MarksApi.Common;
using MarksApi.Data;
using MarksApi.Data.Entities;
using MarksApi.Dtos;

namespace MarksApi.Services;

public interface IStudentService
{
    Task<PagedResult<StudentResponse>> ListAsync(PageQuery q, CancellationToken ct);
    Task<StudentResponse> GetAsync(int id, CurrentUser user, CancellationToken ct);
    Task<StudentResponse> CreateAsync(StudentRequest r, CancellationToken ct);
    Task<StudentResponse> UpdateAsync(int id, StudentRequest r, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

public class StudentService : IStudentService
{
    private const string DuplicateTitle = "Duplicate student";
    private const string DuplicateDetail = "A student with this registration number or email already exists.";

    private readonly AppDbContext _db;
    private readonly ILogger<StudentService> _logger;

    public StudentService(AppDbContext db, ILogger<StudentService> logger)
    {
        _db = db; _logger = logger;
    }

    public async Task<PagedResult<StudentResponse>> ListAsync(PageQuery q, CancellationToken ct)
    {
        IQueryable<Student> query = _db.Students.AsNoTracking();

        if (q.SafeSearch is { } s)
        {
            var pattern = $"%{s}%";
            query = query.Where(x => EF.Functions.ILike(x.FullName, pattern)
                                  || EF.Functions.ILike(x.RegistrationNo, pattern)
                                  || EF.Functions.ILike(x.Email, pattern));
        }

        // Only these sort values are accepted; anything else falls back to id.
        query = q.Sort?.Trim() switch
        {
            "name" => query.OrderBy(x => x.FullName),
            "-name" => query.OrderByDescending(x => x.FullName),
            "regNo" => query.OrderBy(x => x.RegistrationNo),
            "-regNo" => query.OrderByDescending(x => x.RegistrationNo),
            "-id" => query.OrderByDescending(x => x.Id),
            _ => query.OrderBy(x => x.Id)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((q.SafePage - 1) * q.SafePageSize)
            .Take(q.SafePageSize)
            .Select(x => new StudentResponse(x.Id, x.RegistrationNo, x.FullName, x.Email))
            .ToListAsync(ct);

        return new PagedResult<StudentResponse>(items, q.SafePage, q.SafePageSize, total);
    }

    public async Task<StudentResponse> GetAsync(int id, CurrentUser user, CancellationToken ct)
    {
        // A student may only read his/her own record. Checked BEFORE the lookup so we don't reveal which ids exist.
        if (!user.IsTeacher && user.StudentId != id)
            throw new ForbiddenException("Forbidden", "You can only view your own student record.");

        var student = await _db.Students.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new StudentResponse(x.Id, x.RegistrationNo, x.FullName, x.Email))
            .FirstOrDefaultAsync(ct);

        return student ?? throw new NotFoundException("Student not found", $"No student with id {id}.");
    }

    public async Task<StudentResponse> CreateAsync(StudentRequest r, CancellationToken ct)
    {
        var student = new Student
        {
            RegistrationNo = r.RegistrationNo!.Trim(),
            FullName = r.FullName!.Trim(),
            Email = r.Email!.Trim().ToLowerInvariant(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Students.Add(student);

        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            throw new ConflictException(DuplicateTitle, DuplicateDetail);
        }

        _logger.LogInformation("Student {StudentId} created ({RegNo})", student.Id, student.RegistrationNo);
        return new StudentResponse(student.Id, student.RegistrationNo, student.FullName, student.Email);
    }

    public async Task<StudentResponse> UpdateAsync(int id, StudentRequest r, CancellationToken ct)
    {
        var student = await _db.Students.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Student not found", $"No student with id {id}.");

        student.RegistrationNo = r.RegistrationNo!.Trim();
        student.FullName = r.FullName!.Trim();
        student.Email = r.Email!.Trim().ToLowerInvariant();

        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            throw new ConflictException(DuplicateTitle, DuplicateDetail);
        }

        _logger.LogInformation("Student {StudentId} updated", id);
        return new StudentResponse(student.Id, student.RegistrationNo, student.FullName, student.Email);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var student = await _db.Students.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Student not found", $"No student with id {id}.");

        _db.Students.Remove(student);
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (DbErrors.IsForeignKeyViolation(ex))
        {
            throw new ConflictException("Student has enrollments",
                "This student still has enrollments (or a user account) and cannot be deleted.");
        }

        _logger.LogInformation("Student {StudentId} deleted", id);
    }
}