using System.ComponentModel.DataAnnotations;
using AttendanceRegister.Data;
using AttendanceRegister.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceRegister.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required, StringLength(150)]
            [Display(Name = "Full name")]
            public string FullName { get; set; } = string.Empty;

            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            public string Role { get; set; } = SeedData.StudentRole;

            [Display(Name = "Student number")]
            public string? StudentNumber { get; set; }

            [Required, StringLength(100, MinimumLength = 8)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare(nameof(Password))]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Student number is only required for students - validated here rather than
            // with a blanket [Required] on the property, since the same form serves both roles.
            if (Input.Role == SeedData.StudentRole && string.IsNullOrWhiteSpace(Input.StudentNumber))
            {
                ModelState.AddModelError(nameof(Input.StudentNumber), "Student number is required for student accounts.");
            }

            // Two accounts sharing a student number silently splits that student's attendance
            // history across whichever account happens to get matched first (uploads and
            // history lookups both key off this field) - so it has to be unique, not just
            // "usually fine". Checked here (not just the DB constraint) for a clear message
            // instead of a raw save exception.
            if (Input.Role == SeedData.StudentRole && !string.IsNullOrWhiteSpace(Input.StudentNumber))
            {
                var normalized = Input.StudentNumber.Trim();
                var alreadyTaken = await _context.Users.AnyAsync(u => u.StudentNumber == normalized);
                if (alreadyTaken)
                {
                    ModelState.AddModelError(nameof(Input.StudentNumber),
                        "That student number is already registered to another account. If this is your account, log in instead.");
                }
                else
                {
                    Input.StudentNumber = normalized;
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FullName = Input.FullName,
                StudentNumber = Input.Role == SeedData.StudentRole ? Input.StudentNumber : null,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            await _userManager.AddToRoleAsync(user, Input.Role == SeedData.LecturerRole ? SeedData.LecturerRole : SeedData.StudentRole);
            await _signInManager.SignInAsync(user, isPersistent: false);

            return Input.Role == SeedData.LecturerRole
                ? RedirectToPage("/Lecturer/Index")
                : RedirectToPage("/Student/Index");
        }
    }
}
