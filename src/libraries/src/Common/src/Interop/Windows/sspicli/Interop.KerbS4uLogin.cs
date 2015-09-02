// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            internal LSA_UNICODE_STRING ClientUpn;
            internal LSA_UNICODE_STRING ClientRealm;
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
