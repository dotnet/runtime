// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Net.Test.Common
{
    public static partial class Capability
    {
        public static bool IsNtlmInstalled()
        {
            if (OperatingSystem.IsBrowser() || OperatingSystem.IsWasi())
            {
                return false;
            }
            return
                // Linux bionic uses managed NTLM implementation
                (OperatingSystem.IsLinux() && RuntimeInformation.RuntimeIdentifier.StartsWith("linux-bionic-", StringComparison.Ordinal)) ||
                // GSS on Linux does not work with OpenSSL 3.0. Fix was submitted to gss-ntlm (v 1.2.0+) but it will take a while to make to
                // all supported distributions. The second part of the check should be removed when it does. In the meantime, we whitelist
                // distributions containing the updated gss-ntlm package.
                Interop.NetSecurityNative.IsNtlmInstalled() && (!PlatformDetection.IsOpenSslSupported || PlatformDetection.OpenSslVersion.Major < 3
                    || PlatformDetection.IsUbuntu23OrLater
                    // || PlatformDetection.IsFedora40OrLater
                    // || PlatformDetection.IsDebian12OrLater
                    );
        }
    }
}
