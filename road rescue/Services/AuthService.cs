using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Supabase;

namespace road_rescue.Services
{
    public static class AuthService
    {
        private const string Key = "auth_session_v2";
        private const string PendingSignupKey = "pending_signup_data"; // optional cleanup if used elsewhere

        public class SavedSession
        {
            public Guid UserId { get; set; }
            public string Role { get; set; } = "";   // "Driver" or "Mechanic"
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string? PhotoUrl { get; set; }    // ✅ Google profile or uploaded certificate
            public string? AccessToken { get; set; }
            public string? RefreshToken { get; set; }
            public DateTime? ExpiresAtUtc { get; set; }
        }

        public static async Task SaveSessionAsync(SavedSession session)
        {
            var json = JsonSerializer.Serialize(session);
            await SecureStorage.SetAsync(Key, json);
        }

        public static async Task<SavedSession?> LoadSessionAsync()
        {
            var json = await SecureStorage.GetAsync(Key);
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                return JsonSerializer.Deserialize<SavedSession>(json);
            }
            catch
            {
                // If somehow corrupted, remove it
                DeleteSession();
                return null;
            }
        }

        public static void DeleteSession()
        {
            SecureStorage.Remove(Key);
            SecureStorage.Remove(PendingSignupKey); // optional
        }

        /// <summary>
        /// Signs out from Supabase (revokes/clears access + refresh) and deletes local session.
        /// </summary>
        public static async Task LogoutAsync()
        {
            try
            {
                await SupabaseService.InitializeAsync();
                if (SupabaseService.Client?.Auth != null)
                    await SupabaseService.Client.Auth.SignOut(); // revokes Supabase tokens
            }
            catch
            {
                // ignore network/edge errors – still clear local state
            }

            DeleteSession();
        }

        /// <summary>
        /// Save session directly from Supabase auth user.
        /// </summary>
        public static async Task SaveFromSupabaseUserAsync(
            string uid, string email, string fullName, string role,
            string? photoUrl = null, string? accessToken = null, string? refreshToken = null,
            DateTime? expiresAt = null)
        {
            var session = new SavedSession
            {
                UserId = Guid.TryParse(uid, out var g) ? g : Guid.Empty,
                Email = email,
                FullName = fullName,
                Role = role,
                PhotoUrl = photoUrl,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAtUtc = expiresAt
            };

            await SaveSessionAsync(session);
        }
    }
}
