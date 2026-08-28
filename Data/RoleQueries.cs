using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Data
{
    public static class RoleQueries
    {
        // Joins AspNetUserRoles -> AspNetRoles by name, so "is this user a Student" is answered
        // from actual role membership instead of an incidental field like StudentNumber being set.
        public static async Task<List<string>> GetUserIdsInRoleAsync(ApplicationDbContext context, string roleName)
        {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role is null) return new List<string>();

            return await context.UserRoles
                .Where(ur => ur.RoleId == role.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();
        }
    }
}
