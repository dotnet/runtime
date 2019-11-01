// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class SspiCli
    {
        [DllImport(Libraries.SspiCli)]
        internal static extern int LsaLogonUser(
            [In]  SafeLsaHandle LsaHandle,
            [In]  ref Advapi32.LSA_STRING OriginName,
            [In]  SECURITY_LOGON_TYPE LogonType,
            [In]  int AuthenticationPackage,
            [In]  IntPtr AuthenticationInformation,
            [In]  int AuthenticationInformationLength,
            [In]  IntPtr LocalGroups,
            [In]  ref TOKEN_SOURCE SourceContext,
            [Out] out SafeLsaReturnBufferHandle ProfileBuffer,
            [Out] out int ProfileBufferLength,
            [Out] out LUID LogonId,
            [Out] out SafeAccessTokenHandle Token,
            [Out] out QUOTA_LIMITS Quotas,
            [Out] out int SubStatus
            );
    }
}
