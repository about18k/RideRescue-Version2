namespace road_rescue.Models
{
    public class SignupData
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Password { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string Role { get; set; } = "Driver";
        public bool IsMechanicFlow { get; set; }

        public string? GoogleSub { get; set; }
        public string? PhotoUrl { get; set; }

        // Mechanic extras
        public string? CertificateUrl { get; set; }
        public string? Services { get; set; }
        public string? TimeOpen { get; set; }
        public string? TimeClose { get; set; }
        public string? Days { get; set; }
    }
}
