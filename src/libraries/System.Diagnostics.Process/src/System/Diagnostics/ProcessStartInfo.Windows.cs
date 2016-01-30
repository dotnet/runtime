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

        // CoreCLR can't correctly support UseShellExecute=true for the following reasons
        // 1. ShellExecuteEx is not supported on onecore.
        // 2. ShellExecuteEx needs to run as STA but managed code runs as MTA by default and Thread.SetApartmentState() is not supported on all platforms.
        //
        // Irrespective of the limited functionality of the property we still expose it in the contract as it can be implemented
        // on other platforms.  Further, the default value of UseShellExecute is true on desktop and scenarios like redirection mandates 
        // the value to be false. So in order to provide maximum code portability we expose UseShellExecute in the contract 
        // and throw PlatformNotSupportedException in portable library in case it is set to true.
        public bool UseShellExecute
        {
            get { return false; }
            set { if (value == true) throw new PlatformNotSupportedException(SR.UseShellExecute); }
        }
    }
}
