using Microsoft.EntityFrameworkCore;
using MarksApi.Data.Entities;

namespace MarksApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Student>(e =>
        {
            e.ToTable("students");
            e.Property(x => x.RegistrationNo).HasMaxLength(20).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.RegistrationNo).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Course>(e =>
        {
            e.ToTable("courses", t => t.HasCheckConstraint("ck_courses_credit_hours", "credit_hours BETWEEN 1 AND 4"));
            e.Property(x => x.Code).HasMaxLength(10).IsRequired();
            e.Property(x => x.Title).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<Enrollment>(e =>
        {
            e.ToTable("enrollments", t => t.HasCheckConstraint("ck_enrollments_mark", "mark IS NULL OR (mark BETWEEN 0 AND 100)"));
            e.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
            e.HasOne(x => x.Student).WithMany(s => s.Enrollments)
                .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Course).WithMany(c => c.Enrollments)
                .HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<AppUser>(e =>
        {
            e.ToTable("users");
            e.Property(x => x.Username).HasMaxLength(50).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        });
    }
}