// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [Flags]
        internal enum TRUST_ATTRIBUTE
        {
            TRUST_ATTRIBUTE_NON_TRANSITIVE = 0x00000001,
            TRUST_ATTRIBUTE_UPLEVEL_ONLY = 0x00000002,
            TRUST_ATTRIBUTE_QUARANTINED_DOMAIN = 0x00000004,
            TRUST_ATTRIBUTE_FOREST_TRANSITIVE = 0x00000008,
            TRUST_ATTRIBUTE_CROSS_ORGANIZATION = 0x00000010,
            TRUST_ATTRIBUTE_WITHIN_FOREST = 0x00000020,
            TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL = 0x00000040
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TRUSTED_DOMAIN_INFORMATION_EX
        {
            public UNICODE_STRING Name;
            public UNICODE_STRING FlatName;
            public IntPtr Sid;
            public int TrustDirection;
            public int TrustType;
            public TRUST_ATTRIBUTE TrustAttributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TRUSTED_DOMAIN_AUTH_INFORMATION
        {
            public int IncomingAuthInfos;
            public IntPtr IncomingAuthenticationInformation;
            public IntPtr IncomingPreviousAuthenticationInformation;
            public int OutgoingAuthInfos;
            public IntPtr OutgoingAuthenticationInformation;
            public IntPtr OutgoingPreviousAuthenticationInformation;
        }

        [LibraryImport(Libraries.Advapi32)]
        internal static partial uint LsaCreateTrustedDomainEx(SafeLsaPolicyHandle handle, in TRUSTED_DOMAIN_INFORMATION_EX domainEx, in TRUSTED_DOMAIN_AUTH_INFORMATION authInfo, int desiredAccess, out IntPtr domainHandle);
    }
}
