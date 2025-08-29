using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace road_rescue.Models
{
    [Table("Driver")]
    public class Driver : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid UserId { get; set; }

        [Column("fullname")]
        public string FullName { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Column("photo_url")]
        public string? PhotoUrl { get; set; }
    }
}