using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MarksApi.Data.Entities;

namespace MarksApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config)
    {
        if (await db.Users.AnyAsync()) return;   // already seeded, do nothing

        var now = DateTime.UtcNow;
        var s1 = new Student { RegistrationNo = "CS-2022-001", FullName = "Ali Raza", Email = "ali.raza@example.com", CreatedAtUtc = now };
        var s2 = new Student { RegistrationNo = "CS-2022-002", FullName = "Sara Khan", Email = "sara.khan@example.com", CreatedAtUtc = now };
        var c1 = new Course { Code = "CS401", Title = "Databases", CreditHours = 3, CreatedAtUtc = now };
        var c2 = new Course { Code = "MA201", Title = "Linear Algebra", CreditHours = 2, CreatedAtUtc = now };
        var c3 = new Course { Code = "EN101", Title = "English Composition", CreditHours = 4, CreatedAtUtc = now };

        db.Students.AddRange(s1, s2);
        db.Courses.AddRange(c1, c2, c3);
        db.Enrollments.AddRange(
            new Enrollment { Student = s1, Course = c1, Mark = 80, EnrolledAtUtc = now },
            new Enrollment { Student = s1, Course = c2, Mark = 60, EnrolledAtUtc = now },
            new Enrollment { Student = s1, Course = c3, Mark = null, EnrolledAtUtc = now },
            new Enrollment { Student = s2, Course = c1, Mark = 70, EnrolledAtUtc = now },
            new Enrollment { Student = s2, Course = c2, Mark = null, EnrolledAtUtc = now },
            new Enrollment { Student = s2, Course = c3, Mark = 45, EnrolledAtUtc = now });

        var hasher = new PasswordHasher<AppUser>();
        var teacher = new AppUser { Username = config["Seed:TeacherUsername"] ?? "teacher1", Role = "Teacher" };
        teacher.PasswordHash = hasher.HashPassword(teacher, config["Seed:TeacherPassword"] ?? "Teacher@12345");
        var u1 = new AppUser { Username = config["Seed:Student1Username"] ?? "student1", Role = "Student", Student = s1 };
        u1.PasswordHash = hasher.HashPassword(u1, config["Seed:Student1Password"] ?? "Student1@12345");
        var u2 = new AppUser { Username = config["Seed:Student2Username"] ?? "student2", Role = "Student", Student = s2 };
        u2.PasswordHash = hasher.HashPassword(u2, config["Seed:Student2Password"] ?? "Student2@12345");

        db.Users.AddRange(teacher, u1, u2);
        await db.SaveChangesAsync();
    }
}