using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DbServer.Data.Entities;

[Table("GameRooms")]
public class GameRoom
{
    [Key]
    [StringLength(36)]
    public string RoomId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string RoomName { get; set; } = string.Empty;

    [StringLength(36)]
    public string CreatedBy { get; set; } = string.Empty;

    public int MaxPlayers { get; set; } = 8;

    public int CurrentPlayers { get; set; } = 0;

    [StringLength(20)]
    [Column(TypeName = "varchar(20)")]
    public string Status { get; set; } = "Waiting"; // Waiting, Playing, Finished

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime")]
    public DateTime? StartedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? FinishedAt { get; set; }

    [Column(TypeName = "json")]
    public string? GameSettings { get; set; }

    // Navigation properties
    [ForeignKey("CreatedBy")]
    public virtual User? Creator { get; set; }

    public virtual ICollection<GameRoomPlayer> Players { get; set; } = new List<GameRoomPlayer>();
}