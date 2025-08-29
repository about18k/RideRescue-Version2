using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace road_rescue.Models
{
    [Table("mechanic_details")]
    public class MechanicDetails : BaseModel
    {
        [PrimaryKey("mechanic_id", false)]
        public Guid MechanicId { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("services")]
        public string? Services { get; set; }

        [Column("certificate_url")]
        public string? CertificateUrl { get; set; }

        [Column("time_open")]
        public string? TimeOpen { get; set; }

        [Column("time_close")]
        public string? TimeClose { get; set; }

        [Column("days")]
        public string? Days { get; set; }

        [Column("is_verified")]
        public bool IsVerified { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
