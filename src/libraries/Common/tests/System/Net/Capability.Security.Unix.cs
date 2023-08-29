// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Test.Common
{
    public static partial class Capability
    {
        public static bool IsNtlmInstalled()
        {
            // GSS on Linux does not work with OpenSSL 3.0. Fix was submitted to gss-ntlm but it will take a while to make to
            // all supported distributions. The second part of the check should be removed when it does.
            return Interop.NetSecurityNative.IsNtlmInstalled() && (!PlatformDetection.IsOpenSslSupported || PlatformDetection.OpenSslVersion.Major < 3);
        }
    }
}
