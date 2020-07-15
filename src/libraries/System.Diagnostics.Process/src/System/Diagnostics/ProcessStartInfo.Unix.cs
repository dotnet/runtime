// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    // We know of no way to achieve this on Unix, particularly providing the password
    // without a prompt. If we find a way, we should implement it. It may make more sense to provide
    // similar functionality through an API specific to Unix.
    public sealed partial class ProcessStartInfo
    {
        private const bool CaseSensitiveEnvironmentVariables = true;

        [MinimumOSPlatform("windows7.0")]
        public string PasswordInClearText
        {
            get { throw new PlatformNotSupportedException(SR.Format(SR.ProcessStartSingleFeatureNotSupported, nameof(PasswordInClearText))); }
            set { throw new PlatformNotSupportedException(SR.Format(SR.ProcessStartSingleFeatureNotSupported, nameof(PasswordInClearText))); }
        }

        [MinimumOSPlatform("windows7.0")]
        public string Domain
        {
            get { throw new PlatformNotSupportedException(SR.Format(SR.ProcessStartSingleFeatureNotSupported, nameof(Domain))); }
            set { throw new PlatformNotSupportedException(SR.Format(SR.ProcessStartSingleFeatureNotSupported, nameof(Domain))); }
        }

        [MinimumOSPlatform("windows7.0")]
        public bool LoadUserProfile
        {
            get { throw new PlatformNotSupportedException(SR.Format(SR.ProcessStartSingleFeatureNotSupported, nameof(LoadUserProfile))); }
            set { throw new PlatformNotSupportedException(SR.Format(SR.ProcessStartSingleFeatureNotSupported, nameof(LoadUserProfile))); }
        }

        public bool UseShellExecute { get; set; }

        public string[] Verbs => Array.Empty<string>();

        [CLSCompliant(false)]
        [MinimumOSPlatform("windows7.0")]
        public SecureString Password
        {
            get { throw new PlatformNotSupportedException(SR.Format(SR.ProcessStartSingleFeatureNotSupported, nameof(Password))); }
            set { throw new PlatformNotSupportedException(SR.Format(SR.ProcessStartSingleFeatureNotSupported, nameof(Password))); }
        }
    }
}
