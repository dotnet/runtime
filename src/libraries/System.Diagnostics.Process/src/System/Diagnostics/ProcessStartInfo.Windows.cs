using System.Security;

namespace System.Diagnostics
{
    public sealed partial class ProcessStartInfo
    {
        private string _userName;
        private string _domain;
        private SecureString _password;
        private bool _loadUserProfile;

        private const bool CaseSensitiveEnvironmentVariables = false;

        public string UserName
        {
            get { return _userName ?? string.Empty; }
            set { _userName = value; }
        }

        public SecureString Password
        {
            get { return _password; }
            set { _password = value; }
        }

        public string Domain
        {
            get { return _domain ?? string.Empty; }
            set { _domain = value; }
        }

        public bool LoadUserProfile
        {
            get { return _loadUserProfile; }
            set { _loadUserProfile = value; }
        }
    }
}