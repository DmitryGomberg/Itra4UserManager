using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManager.Data;
using UserManager.Models;

namespace UserManager.Controllers;

public class UsersController : Controller
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return null;
        return await _db.Users.FindAsync(userId);
    }

    private async Task<bool> IsAuthorized()
    {
        var user = await GetCurrentUserAsync();
        if (user == null || user.Status == "blocked")
        {
            HttpContext.Session.Clear();
            return false;
        }
        return true;
    }

    public async Task<IActionResult> Index(string sort = "lastlogin", string dir = "desc", string? search = null)
    {
        if (!await IsAuthorized())
            return RedirectToAction("Login", "Auth");

        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }

        query = (sort, dir) switch
        {
            ("name", "asc") => query.OrderBy(u => u.Name),
            ("name", "desc") => query.OrderByDescending(u => u.Name),
            ("email", "asc") => query.OrderBy(u => u.Email),
            ("email", "desc") => query.OrderByDescending(u => u.Email),
            ("status", "asc") => query.OrderBy(u => u.Status),
            ("status", "desc") => query.OrderByDescending(u => u.Status),
            ("registered", "asc") => query.OrderBy(u => u.RegisteredAt),
            ("registered", "desc") => query.OrderByDescending(u => u.RegisteredAt),
            ("lastlogin", "asc") => query.OrderBy(u => u.LastLoginAt),
            _ => query.OrderByDescending(u => u.LastLoginAt)
        };

        ViewBag.Sort = sort;
        ViewBag.Dir = dir;
        ViewBag.Search = search;

        var users = await query.ToListAsync();
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> Block(List<int> ids)
    {
        if (!await IsAuthorized())
            return RedirectToAction("Login", "Auth");

        var users = await _db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
        foreach (var user in users)
        {
            if (user.Status != "blocked")
                user.PreviousStatus = user.Status;
            user.Status = "blocked";
        }

        await _db.SaveChangesAsync();

        var currentUserId = HttpContext.Session.GetInt32("UserId");
        if (ids.Contains(currentUserId ?? -1))
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        TempData["Success"] = "Выбранный пользователь был заблокирован.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Unblock(List<int> ids)
    {
        if (!await IsAuthorized())
            return RedirectToAction("Login", "Auth");

        var users = await _db.Users
        .Where(u => ids.Contains(u.Id) && u.Status == "blocked")
        .ToListAsync();
        foreach (var user in users)
        {
            user.Status = user.PreviousStatus ?? "active";
            user.PreviousStatus = null;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Выбранные пользователи были разблокированы.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(List<int> ids)
    {
        if (!await IsAuthorized())
            return RedirectToAction("Login", "Auth");

        var currentUserId = HttpContext.Session.GetInt32("UserId");
        var users = await _db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
        _db.Users.RemoveRange(users);
        await _db.SaveChangesAsync();

        if (ids.Contains(currentUserId ?? -1))
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        TempData["Success"] = "Выбранные пользователи были удалены.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUnverified(List<int> ids)
    {
        if (!await IsAuthorized())
            return RedirectToAction("Login", "Auth");

        var currentUserId = HttpContext.Session.GetInt32("UserId");
        var users = await _db.Users
            .Where(u => ids.Contains(u.Id) && u.Status == "unverified")
            .ToListAsync();

        _db.Users.RemoveRange(users);
        await _db.SaveChangesAsync();

        if (ids.Contains(currentUserId ?? -1))
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        TempData["Success"] = "Выбранные неподтвержденные пользователи были удалены.";
        return RedirectToAction("Index");
    }
}