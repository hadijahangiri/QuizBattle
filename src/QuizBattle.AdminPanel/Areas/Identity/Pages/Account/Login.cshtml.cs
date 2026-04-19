using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuizBattle.AdminPanel.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;
    private const string CaptchaSessionKey = "LoginCaptcha";

    public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string CaptchaText { get; set; } = "";

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "ایمیل الزامی است")]
        [EmailAddress(ErrorMessage = "فرمت ایمیل صحیح نیست")]
        [Display(Name = "ایمیل")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; } = "";

        [Display(Name = "مرا به خاطر بسپار")]
        public bool RememberMe { get; set; }

        [Required(ErrorMessage = "کد امنیتی الزامی است")]
        [Display(Name = "کد امنیتی")]
        public string CaptchaInput { get; set; } = "";
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ReturnUrl = returnUrl;
        CaptchaText = GenerateAndStoreCaptcha();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        // Get captcha from session
        var storedCaptcha = HttpContext.Session.GetString(CaptchaSessionKey);
        
        // Validate captcha
        if (string.IsNullOrEmpty(storedCaptcha) || 
            !string.Equals(Input.CaptchaInput?.Trim(), storedCaptcha.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Input.CaptchaInput", "کد امنیتی صحیح نیست");
            CaptchaText = GenerateAndStoreCaptcha();
            return Page();
        }

        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                HttpContext.Session.Remove(CaptchaSessionKey);
                return LocalRedirect(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                ErrorMessage = "حساب کاربری شما قفل شده است. لطفاً بعداً تلاش کنید.";
                CaptchaText = GenerateAndStoreCaptcha();
                return Page();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "ایمیل یا رمز عبور اشتباه است");
                CaptchaText = GenerateAndStoreCaptcha();
                return Page();
            }
        }

        // If we got this far, something failed, redisplay form
        CaptchaText = GenerateAndStoreCaptcha();
        return Page();
    }

    public IActionResult OnGetRefreshCaptcha()
    {
        var newCaptcha = GenerateAndStoreCaptcha();
        return new JsonResult(new { captcha = newCaptcha });
    }

    private string GenerateAndStoreCaptcha()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var captcha = new string(Enumerable.Range(0, 5).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        HttpContext.Session.SetString(CaptchaSessionKey, captcha);
        return captcha;
    }
}
