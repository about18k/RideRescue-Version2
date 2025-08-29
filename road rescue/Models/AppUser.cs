using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace road_rescue.Models
{
    [Table("app_user")]
    public class AppUser : BaseModel
    {
        [PrimaryKey("user_id", false)]
        public Guid UserId { get; set; }

        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("password")]
        public string? Password { get; set; }

        [Column("google_sub")]
        public string? GoogleSub { get; set; }

        [Column("photo_url")]
        public string? PhotoUrl { get; set; }

        [Column("role")]
        public string Role { get; set; } = "Driver";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
