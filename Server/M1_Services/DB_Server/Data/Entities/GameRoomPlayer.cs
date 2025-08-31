using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DbServer.Data.Entities;

[Table("GameRoomPlayers")]
public class GameRoomPlayer
{
    [Key]
    [StringLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    public string RoomId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string UserId { get; set; } = string.Empty;

    [Column(TypeName = "datetime")]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime")]
    public DateTime? LeftAt { get; set; }

    public bool IsActive { get; set; } = true;

    public int PlayerSlot { get; set; } = 0; // 0-7 for 8-player games

    [Column(TypeName = "json")]
    public string? PlayerData { get; set; } // JSON for player stats, position, etc.

    // Navigation properties
    [ForeignKey("RoomId")]
    public virtual GameRoom Room { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}