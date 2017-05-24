// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;

namespace System.Diagnostics
{
    public sealed partial class ProcessStartInfo
    {
        private string _userName;
        private string _domain;

        private const bool CaseSensitiveEnvironmentVariables = false;

        public string UserName
        {
            get => _userName ?? string.Empty;
            set => _userName = value;
        }

        public string PasswordInClearText { get; set; }

        public string Domain
        {
            get => _domain ?? string.Empty;
            set => _domain = value;
        }

        public bool LoadUserProfile { get; set; }

        [CLSCompliant(false)]
        public SecureString Password { get; set; }
    }
}
