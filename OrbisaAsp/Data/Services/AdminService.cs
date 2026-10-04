using Microsoft.Extensions.Options;
using OrbisaAsp.Data.Configuration;

namespace OrbisaAsp.Data.Services
{
    public class AdminService : IAdminService
    {
        private readonly AdminSettings _settings;

        public AdminService(
            IOptions<AdminSettings> options)
        {
            _settings = options.Value;
        }

        public bool IsAdmin(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return _settings.Emails.Any(
                adminEmail =>
                    string.Equals(
                        adminEmail.Trim(),
                        email.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
            );
        }
    }
}
