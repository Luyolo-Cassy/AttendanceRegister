using AttendanceRegister.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Data
{
    // Runs once at startup. Keeps "how do I get a first lecturer account" out of the manual
    // testing/marking process - the department shouldn't need to know a magic SQL insert.
    public static class SeedData
    {
        public const string StudentRole = "Student";
        public const string LecturerRole = "Lecturer";

        public static async Task InitializeAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in new[] { StudentRole, LecturerRole })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            const string demoLecturerEmail = "lecturer@uct.ac.za";

            if (await userManager.FindByEmailAsync(demoLecturerEmail) is null)
            {
                var lecturer = new ApplicationUser
                {
                    UserName = demoLecturerEmail,
                    Email = demoLecturerEmail,
                    FullName = "Demo Lecturer",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(lecturer, "ChangeMe123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(lecturer, LecturerRole);
                }
            }
        }
    }
}
