using AttendanceRegister.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Data;

// IdentityDbContext<ApplicationUser> instead of the plain IdentityDbContext so Identity's
// tables carry our extra FullName / StudentNumber columns.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Lecture> Lectures => Set<Lecture>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AttendanceRecord>()
            .HasOne(a => a.Lecture)
            .WithMany(l => l.AttendanceRecords)
            .HasForeignKey(a => a.LectureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AttendanceRecord>()
            .HasOne(a => a.Student)
            .WithMany(u => u.AttendanceRecords)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // A student can only have one attendance row per lecture - prevents duplicate
        // check-ins and keeps "edit" (not "insert another row") the correct operation.
        builder.Entity<AttendanceRecord>()
            .HasIndex(a => new { a.LectureId, a.StudentId })
            .IsUnique();

        builder.Entity<Lecture>()
            .HasOne(l => l.CreatedByLecturer)
            .WithMany()
            .HasForeignKey(l => l.CreatedByLecturerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Lecture>()
            .HasIndex(l => l.CheckInCode);

        // Two accounts could otherwise both claim the same Student No - since uploads and
        // history lookups both key off StudentNumber/StudentId, a duplicate silently splits
        // one student's data across two accounts. NULL StudentNumber (lecturers) is exempt.
        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.StudentNumber)
            .IsUnique()
            .HasFilter("StudentNumber IS NOT NULL");
    }
}
