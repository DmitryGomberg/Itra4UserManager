using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManager.Data;
using UserManager.Models;
using UserManager.Services;

namespace UserManager.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _db;
    private readonly EmailService _emailService;

    public AuthController(AppDbContext db, EmailService EmailService)
    {
        _db = db;
        _emailService = EmailService;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name) ||
            string.IsNullOrWhiteSpace(model.Email) ||
            string.IsNullOrWhiteSpace(model.Password))
        {
            ViewBag.Error = "Все поля обязательны для заполнения.";
            return View(model);
        }

        var user = new User
        {
            Name = model.Name,
            Email = model.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Status = "unverified",
            RegisteredAt = DateTime.UtcNow,
            VerificationToken = Guid.NewGuid().ToString()
        };

        try
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ViewBag.Error = "Этот email уже зарегистрирован.";
            return View(model);
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendVerificationEmailAsync(user.Email, user.VerificationToken!);
                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EMAIL ERROR: {ex.Message}");
                Console.WriteLine($"STACK: {ex.StackTrace}");
            }
        });

        ViewBag.Success = "Регистрация прошла успешно! Пожалуйста, проверьте вашу электронную почту для подтверждения аккаунта.";
        return View();
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == model.Email.ToLower().Trim());

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ViewBag.Error = "Неверный email или пароль.";
            return View(model);
        }

        if (user.Status == "blocked")
        {
            ViewBag.Error = "Ваш аккаунт был заблокирован.";
            return View(model);
        }

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserName", user.Name);

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return RedirectToAction("Index", "Users");
    }
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> Verify(string token)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);

        if (user == null)
        {
            ViewBag.Error = "Недействительная ссылка подтверждения.";
            return View();
        }

        if (user.Status == "unverified")
        {
            user.Status = "active";
            user.VerificationToken = null;
            await _db.SaveChangesAsync();
        }

        ViewBag.Success = "Email подтверждён! Теперь вы можете войти.";
        return View();
    }
}