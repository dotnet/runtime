// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;

namespace System.Diagnostics
{
    public sealed partial class ProcessStartInfo
    {
        private string _userName;
        private string _domain;
        private string _passwordInClearText;
        private bool _loadUserProfile;

        private const bool CaseSensitiveEnvironmentVariables = false;

        public string UserName
        {
            get { return _userName ?? string.Empty; }
            set { _userName = value; }
        }

        public string PasswordInClearText
        {
            get { return _passwordInClearText; }
            set { _passwordInClearText = value; }
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