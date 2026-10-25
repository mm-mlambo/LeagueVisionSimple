using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeagueVisionSimple.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public LoginModel(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
        
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }

    public void OnGet() { }

    // This runs when the user clicks the "Log In" button
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            // Check if the email and password match a user in the database
            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, false, lockoutOnFailure: false);
            
            if (result.Succeeded)
            {
                // If successful, send them to the home page
                return RedirectToPage("/Index");
            }
            
            // If failed, show an error message
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }
        return Page();
    }

    // --- GUEST LOGIN LOGIC ---
    // This runs when someone clicks the "Continue as Guest" link
    public IActionResult OnGetGuest()
    {
        // Create a cookie called "IsGuest" that lasts for 1 day
        Response.Cookies.Append("IsGuest", "true", new CookieOptions { Expires = DateTime.Now.AddDays(1) });
        
        // Send them to the home page
        return RedirectToPage("/Index");
    }
}