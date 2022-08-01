// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class SspiCli
    {
        [LibraryImport(Libraries.SspiCli)]
        internal static partial int LsaLogonUser(
            SafeLsaHandle LsaHandle,
            in Advapi32.LSA_STRING OriginName,
            SECURITY_LOGON_TYPE LogonType,
            int AuthenticationPackage,
            IntPtr AuthenticationInformation,
            int AuthenticationInformationLength,
            IntPtr LocalGroups,
            in TOKEN_SOURCE SourceContext,
            out SafeLsaReturnBufferHandle ProfileBuffer,
            out int ProfileBufferLength,
            out LUID LogonId,
            out SafeAccessTokenHandle Token,
            out QUOTA_LIMITS Quotas,
            out int SubStatus
            );
    }
}
