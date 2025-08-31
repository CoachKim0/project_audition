using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DbServer.Data.Entities;

[Table("UserSessions")]
public class UserSession
{
    [Key]
    [StringLength(128)]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(512)]
    public string Token { get; set; } = string.Empty;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime")]
    public DateTime ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(45)]
    [Column(TypeName = "varchar(45)")]
    public string? IpAddress { get; set; }

    [StringLength(512)]
    public string? UserAgent { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}