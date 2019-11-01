// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class SspiCli
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct KERB_S4U_LOGON
        {
            internal KERB_LOGON_SUBMIT_TYPE MessageType;
            internal KerbS4uLogonFlags Flags;
            internal UNICODE_STRING ClientUpn;
            internal UNICODE_STRING ClientRealm;
        }

        [Flags]
        internal enum KerbS4uLogonFlags : int
        {
            None = 0x00000000,
            KERB_S4U_LOGON_FLAG_CHECK_LOGONHOURS = 0x00000002,
            KERB_S4U_LOGON_FLAG_IDENTITY = 0x00000008,
        }
    }
}
