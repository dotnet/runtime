// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class SspiCli
    {
        [DllImport(Libraries.Sspi)]
        internal static extern int LsaLogonUser(
            [In]  SafeLsaHandle LsaHandle,
            [In]  ref LSA_STRING OriginName,
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
