using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebDevStd2531.AppData;
using WebDevStd2531.Models;
using WebDevStd2531.Services;

namespace WebDevStd2531.Controllers;

public class UserController : Controller
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly IUserAccountGrpcClient _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IUserAccountGrpcClient userService,
        ILogger<UserController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userService = userService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    public IActionResult Login(string? returnUrl = null)
    {
        var model = new LoginViewModel
        {
            UserName = string.Empty,
            Password = string.Empty,
            ReturnUrl = returnUrl
        };

        return View(model);
    }

    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        _logger.LogInformation("Logout requested.");
        await _signInManager.SignOutAsync();

        if (returnUrl != null)
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = "~/")
    {
        returnUrl = Url.Content("~/");

        if (ModelState.IsValid)
        {
            _logger.LogInformation("Register submit received for user {UserName}.", model.UserName);
            var result = await _userService.RegisterAsync(model);
            if (result.Success)
            {
                _logger.LogInformation("Register succeeded for user {UserName}.", model.UserName);
                var localUser = await _userManager.FindByNameAsync(model.UserName);
                if (localUser != null)
                {
                    await _signInManager.SignInAsync(localUser, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                ModelState.AddModelError(string.Empty, "Registration succeeded, but the local login cookie could not be created.");
                return View(model);
            }

            _logger.LogWarning("Register failed for user {UserName}: {Message}", model.UserName, result.Message);
            ModelState.AddModelError(string.Empty, result.Message);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        returnUrl = Url.Content("~/");

        if (ModelState.IsValid)
        {
            _logger.LogInformation("Login submit received for user {UserName}.", model.UserName);
            var result = await _userService.LoginAsync(model);
            if (result.Success)
            {
                _logger.LogInformation("Login succeeded for user {UserName}.", model.UserName);
                var localUser = await _userManager.FindByNameAsync(model.UserName);
                if (localUser != null)
                {
                    await _signInManager.SignInAsync(localUser, isPersistent: model.RememberMe);
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Login succeeded, but the local login cookie could not be created.");
                return View(model);
            }

            _logger.LogWarning("Login failed for user {UserName}: {Message}", model.UserName, result.Message);
            ModelState.AddModelError(string.Empty, result.Message);
        }

        return View(model);
    }
}
