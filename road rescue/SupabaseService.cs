// SupabaseService.cs  (keep at project root)
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

// Avoid collisions via aliases
using Sb = Supabase;
using SbAuth = Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Supabase.Postgrest;       // queries
using road_rescue.Models;       // AppUser

namespace road_rescue   
{
    /// Persist Supabase auth session with SecureStorage.
    public sealed class MauiSessionHandler : IGotrueSessionPersistence<SbAuth.Session>
    {
        private const string AccessKey = "sb_access";
        private const string RefreshKey = "sb_refresh";

        public void SaveSession(SbAuth.Session session)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(session.AccessToken))
                    SecureStorage.Default.SetAsync(AccessKey, session.AccessToken).GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(session.RefreshToken))
                    SecureStorage.Default.SetAsync(RefreshKey, session.RefreshToken).GetAwaiter().GetResult();
            }
            catch { /* ignore */ }
        }

        public SbAuth.Session? LoadSession()
        {
            try
            {
                var access = SecureStorage.Default.GetAsync(AccessKey).GetAwaiter().GetResult();
                var refresh = SecureStorage.Default.GetAsync(RefreshKey).GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(refresh)) return null;
                return new SbAuth.Session { AccessToken = access, RefreshToken = refresh };
            }
            catch { return null; }
        }

        public void DestroySession()
        {
            SecureStorage.Default.Remove(AccessKey);
            SecureStorage.Default.Remove(RefreshKey);
        }
    }

    public static class SupabaseService
    {
        public static Sb.Client? Client { get; private set; }

        // ✅ Put your real project URL/key here
        private const string SupabaseUrl = "https://ewrlmlsetyinyhjwgoko.supabase.co";
        private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImV3cmxtbHNldHlpbnloandnb2tvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTUzOTk3OTgsImV4cCI6MjA3MDk3NTc5OH0.Xqt8WHv58IX-bGQt4bo887DUb0L-_4H20KLqH76DtJ8";

        public static async Task InitializeAsync()
        {
            if (Client is not null) return;

            var options = new Sb.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false,
                SessionHandler = new MauiSessionHandler()
            };

            Client = new Sb.Client(SupabaseUrl, SupabaseKey, options);

            // Load any saved tokens, then refresh so CurrentUser is valid
            Client.Auth.LoadSession();                 // via SessionHandler
            await Client.Auth.RetrieveSessionAsync();  // refresh/validate
        }

        public static async Task SignOutAsync()
        {
            if (Client is null) return;
            try { await Client.Auth.SignOut(); }
            finally { new MauiSessionHandler().DestroySession(); }
        }

        /// Create (if needed) and return the app_user row for this UID.
        public static async Task<AppUser> GetOrCreateAppUserAsync(string uidStr, string? emailFallback = null)
        {
            if (Client is null) throw new InvalidOperationException("Supabase not initialized.");

            var result = await Client
                .From<AppUser>()
                .Select("*")
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, uidStr)
                .Get();

            var existing = result.Models.FirstOrDefault();
            if (existing != null) return existing;

            var uid = Guid.TryParse(uidStr, out var g) ? g : Guid.Empty;

            var newUser = new AppUser
            {
                UserId = uid,
                FullName = emailFallback ?? "User",
                Email = emailFallback ?? string.Empty,
                Role = "Driver"
            };

            var insert = await Client
                .From<AppUser>()
                .Insert(newUser, new QueryOptions { Returning = QueryOptions.ReturnType.Representation });

            return insert.Models.First();
        }
    }
}
