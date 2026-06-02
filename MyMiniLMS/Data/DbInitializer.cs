using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMiniLMS.Models;

namespace MyMiniLMS.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager =
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager);
        await EnsureAdminHasOnlyAdminRoleAsync(userManager);
        await SeedCoursesAsync(userManager, dbContext);
        await MergeLegacyDemoCoursesAsync(userManager, dbContext);
        await SeedAssignmentsAsync(dbContext);
        await MergeLegacyDemoAssignmentsAsync(dbContext);
        await SeedStudentCoursesAsync(userManager, dbContext);
        await RemoveTeachersFromOwnCoursesAsync(dbContext);
        await SyncStudentAssignmentsAsync(dbContext);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles =
        [
            "Admin",
            "Teacher",
            "Student"
        ];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        await CreateUserIfNotExistsAsync(userManager, DemoSeedData.Admin);

        foreach (var seedUser in DemoSeedData.Users)
        {
            await CreateUserIfNotExistsAsync(userManager, seedUser);
        }
    }

    private static async Task CreateUserIfNotExistsAsync(
        UserManager<ApplicationUser> userManager,
        DemoUser seedUser)
    {
        var existingUser = await userManager.FindByEmailAsync(seedUser.Email);

        if (existingUser != null)
        {
            if (existingUser.FullName != seedUser.FullName)
            {
                existingUser.FullName = seedUser.FullName;
                await userManager.UpdateAsync(existingUser);
            }

            if (!await userManager.IsInRoleAsync(existingUser, seedUser.Role))
            {
                await userManager.AddToRoleAsync(existingUser, seedUser.Role);
            }

            return;
        }

        var user = new ApplicationUser
        {
            UserName = seedUser.Email,
            Email = seedUser.Email,
            FullName = seedUser.FullName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, seedUser.Password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, seedUser.Role);
        }
    }

    private static async Task SeedCoursesAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        foreach (var seedCourse in DemoSeedData.Courses)
        {
            var teacher = await userManager.FindByEmailAsync(seedCourse.TeacherEmail);

            if (teacher == null)
            {
                continue;
            }

            var exists = await dbContext.Courses
                .AnyAsync(c => c.Name == seedCourse.Name && c.TeacherId == teacher.Id);

            if (exists)
            {
                continue;
            }

            dbContext.Courses.Add(new Course
            {
                Name = seedCourse.Name,
                Description = seedCourse.Description,
                TeacherId = teacher.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureAdminHasOnlyAdminRoleAsync(UserManager<ApplicationUser> userManager)
    {
        var admin = await userManager.FindByEmailAsync(DemoSeedData.Admin.Email);

        if (admin == null)
        {
            return;
        }

        foreach (var role in new[] { "Teacher", "Student" })
        {
            if (await userManager.IsInRoleAsync(admin, role))
            {
                await userManager.RemoveFromRoleAsync(admin, role);
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }

    private static async Task SeedAssignmentsAsync(ApplicationDbContext dbContext)
    {
        foreach (var seedAssignment in DemoSeedData.Assignments)
        {
            var course = await dbContext.Courses
                .FirstOrDefaultAsync(c => c.Name == seedAssignment.CourseName);

            if (course == null)
            {
                continue;
            }

            var exists = await dbContext.Assignments
                .AnyAsync(a => a.CourseId == course.Id && a.Title == seedAssignment.Title);

            if (exists)
            {
                continue;
            }

            dbContext.Assignments.Add(new Assignment
            {
                CourseId = course.Id,
                Title = seedAssignment.Title,
                Description = seedAssignment.Description,
                Deadline = DateTime.UtcNow.AddDays(seedAssignment.DeadlineDaysFromNow),
                CreatedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task MergeLegacyDemoCoursesAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        foreach (var seedCourse in DemoSeedData.Courses.Where(c => c.LegacyName != null))
        {
            var teacher = await userManager.FindByEmailAsync(seedCourse.TeacherEmail);

            if (teacher == null)
            {
                continue;
            }

            var targetCourse = await dbContext.Courses
                .FirstOrDefaultAsync(c => c.Name == seedCourse.Name && c.TeacherId == teacher.Id);

            var legacyCourse = await dbContext.Courses
                .FirstOrDefaultAsync(c => c.Name == seedCourse.LegacyName && c.TeacherId == teacher.Id);

            if (legacyCourse == null)
            {
                continue;
            }

            if (targetCourse == null)
            {
                legacyCourse.Name = seedCourse.Name;
                legacyCourse.Description = seedCourse.Description;
                continue;
            }

            await MoveStudentCoursesAsync(dbContext, legacyCourse, targetCourse);
            await MoveAssignmentsAsync(dbContext, legacyCourse, targetCourse);
            dbContext.Courses.Remove(legacyCourse);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task MergeLegacyDemoAssignmentsAsync(ApplicationDbContext dbContext)
    {
        foreach (var seedAssignment in DemoSeedData.Assignments.Where(a => a.LegacyTitle != null))
        {
            var course = await dbContext.Courses
                .FirstOrDefaultAsync(c => c.Name == seedAssignment.CourseName);

            if (course == null)
            {
                continue;
            }

            var targetAssignment = await dbContext.Assignments
                .FirstOrDefaultAsync(a => a.CourseId == course.Id && a.Title == seedAssignment.Title);

            var legacyAssignment = await dbContext.Assignments
                .FirstOrDefaultAsync(a => a.CourseId == course.Id && a.Title == seedAssignment.LegacyTitle);

            if (legacyAssignment == null)
            {
                continue;
            }

            if (targetAssignment?.Id == legacyAssignment.Id)
            {
                continue;
            }

            if (targetAssignment == null)
            {
                legacyAssignment.Title = seedAssignment.Title;
                legacyAssignment.Description = seedAssignment.Description;
                continue;
            }

            await MoveStudentAssignmentsAsync(dbContext, legacyAssignment, targetAssignment);
            dbContext.Assignments.Remove(legacyAssignment);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task MoveStudentCoursesAsync(
        ApplicationDbContext dbContext,
        Course sourceCourse,
        Course targetCourse)
    {
        var sourceStudentCourses = await dbContext.StudentCourses
            .Where(sc => sc.CourseId == sourceCourse.Id)
            .ToListAsync();

        foreach (var sourceStudentCourse in sourceStudentCourses)
        {
            var exists = await dbContext.StudentCourses
                .AnyAsync(sc =>
                    sc.StudentId == sourceStudentCourse.StudentId &&
                    sc.CourseId == targetCourse.Id);

            if (!exists)
            {
                dbContext.StudentCourses.Add(new StudentCourse
                {
                    StudentId = sourceStudentCourse.StudentId,
                    CourseId = targetCourse.Id,
                    AssignedAt = sourceStudentCourse.AssignedAt
                });
            }

            dbContext.StudentCourses.Remove(sourceStudentCourse);
        }
    }

    private static async Task MoveAssignmentsAsync(
        ApplicationDbContext dbContext,
        Course sourceCourse,
        Course targetCourse)
    {
        var sourceAssignments = await dbContext.Assignments
            .Where(a => a.CourseId == sourceCourse.Id)
            .ToListAsync();

        foreach (var sourceAssignment in sourceAssignments)
        {
            var targetAssignment = await dbContext.Assignments
                .FirstOrDefaultAsync(a =>
                    a.CourseId == targetCourse.Id &&
                    a.Title == sourceAssignment.Title);

            if (targetAssignment == null)
            {
                sourceAssignment.CourseId = targetCourse.Id;
            }
            else
            {
                await MoveStudentAssignmentsAsync(dbContext, sourceAssignment, targetAssignment);
                dbContext.Assignments.Remove(sourceAssignment);
            }
        }
    }

    private static async Task MoveStudentAssignmentsAsync(
        ApplicationDbContext dbContext,
        Assignment sourceAssignment,
        Assignment targetAssignment)
    {
        var sourceStudentAssignments = await dbContext.StudentAssignments
            .Where(sa => sa.AssignmentId == sourceAssignment.Id)
            .ToListAsync();

        foreach (var sourceStudentAssignment in sourceStudentAssignments)
        {
            var exists = await dbContext.StudentAssignments
                .AnyAsync(sa =>
                    sa.StudentId == sourceStudentAssignment.StudentId &&
                    sa.AssignmentId == targetAssignment.Id);

            if (!exists)
            {
                dbContext.StudentAssignments.Add(new StudentAssignment
                {
                    StudentId = sourceStudentAssignment.StudentId,
                    AssignmentId = targetAssignment.Id,
                    Status = sourceStudentAssignment.Status,
                    StartedAt = sourceStudentAssignment.StartedAt,
                    CompletedAt = sourceStudentAssignment.CompletedAt
                });
            }

            dbContext.StudentAssignments.Remove(sourceStudentAssignment);
        }
    }

    private static async Task SeedStudentCoursesAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        foreach (var seedStudentCourse in DemoSeedData.StudentCourses)
        {
            var student = await userManager.FindByEmailAsync(seedStudentCourse.StudentEmail);

            var course = await dbContext.Courses
                .FirstOrDefaultAsync(c => c.Name == seedStudentCourse.CourseName);

            if (student == null || course == null)
            {
                continue;
            }

            if (course.TeacherId == student.Id)
            {
                continue;
            }

            var exists = await dbContext.StudentCourses
                .AnyAsync(sc => sc.StudentId == student.Id && sc.CourseId == course.Id);

            if (exists)
            {
                continue;
            }

            dbContext.StudentCourses.Add(new StudentCourse
            {
                StudentId = student.Id,
                CourseId = course.Id,
                AssignedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task RemoveTeachersFromOwnCoursesAsync(ApplicationDbContext dbContext)
    {
        var invalidStudentCourses = await dbContext.StudentCourses
            .Include(sc => sc.Course)
            .Where(sc => sc.Course.TeacherId == sc.StudentId)
            .ToListAsync();

        if (!invalidStudentCourses.Any())
        {
            return;
        }

        foreach (var invalidStudentCourse in invalidStudentCourses)
        {
            var assignmentIds = await dbContext.Assignments
                .Where(a => a.CourseId == invalidStudentCourse.CourseId)
                .Select(a => a.Id)
                .ToListAsync();

            var invalidStudentAssignments = await dbContext.StudentAssignments
                .Where(sa =>
                    sa.StudentId == invalidStudentCourse.StudentId &&
                    assignmentIds.Contains(sa.AssignmentId))
                .ToListAsync();

            dbContext.StudentAssignments.RemoveRange(invalidStudentAssignments);
            dbContext.StudentCourses.Remove(invalidStudentCourse);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SyncStudentAssignmentsAsync(ApplicationDbContext dbContext)
    {
        var studentCourses = await dbContext.StudentCourses.ToListAsync();

        foreach (var studentCourse in studentCourses)
        {
            var assignmentIds = await dbContext.Assignments
                .Where(a => a.CourseId == studentCourse.CourseId)
                .Select(a => a.Id)
                .ToListAsync();

            foreach (var assignmentId in assignmentIds)
            {
                var exists = await dbContext.StudentAssignments
                    .AnyAsync(sa =>
                        sa.StudentId == studentCourse.StudentId &&
                        sa.AssignmentId == assignmentId);

                if (exists)
                {
                    continue;
                }

                dbContext.StudentAssignments.Add(new StudentAssignment
                {
                    StudentId = studentCourse.StudentId,
                    AssignmentId = assignmentId,
                    Status = AssignmentStatus.NotStarted
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
