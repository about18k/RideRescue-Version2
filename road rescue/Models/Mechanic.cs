using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace road_rescue.Models
{
    [Table("Mechanic")]
    public class Mechanic : BaseModel
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

        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Column("certificate")]
        public string CertificatePath { get; set; } = string.Empty;

        [Column("services")]
        public string Services { get; set; } = string.Empty;

        [Column("schedule")]
        public string Schedule { get; set; } = string.Empty;
    }
}
