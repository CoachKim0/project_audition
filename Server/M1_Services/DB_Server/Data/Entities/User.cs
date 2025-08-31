using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DbServer.Data.Entities;

[Table("Users")]
public class User
{
    [Key]
    [StringLength(36)]
    public string UserId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string Nickname { get; set; } = string.Empty;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime")]
    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}