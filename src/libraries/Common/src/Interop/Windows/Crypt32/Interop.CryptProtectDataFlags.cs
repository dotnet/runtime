// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [Flags]
        internal enum CryptProtectDataFlags : int
        {
            CRYPTPROTECT_UI_FORBIDDEN       = 0x1,
            CRYPTPROTECT_LOCAL_MACHINE      = 0x4,
            CRYPTPROTECT_CRED_SYNC          = 0x8,
            CRYPTPROTECT_AUDIT              = 0x10,
            CRYPTPROTECT_NO_RECOVERY        = 0x20,
            CRYPTPROTECT_VERIFY_PROTECTION  = 0x40,
        }
    }
}
