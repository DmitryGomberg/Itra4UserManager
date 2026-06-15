namespace UserManager.Models;

public class User
{
  public int Id { get; set; }

  public string Name { get; set; } = string.Empty;

  public string Email { get; set; } = string.Empty;

  public string PasswordHash { get; set; } = string.Empty;

  public string Status { get; set; } = "unverified";

  public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

  public DateTime? LastLoginAt { get; set; }

  public string? VerificationToken { get; set; }
}