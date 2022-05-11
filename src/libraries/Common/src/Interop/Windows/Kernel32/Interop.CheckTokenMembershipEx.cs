// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const uint CTMF_INCLUDE_APPCONTAINER = 0x00000001;

        [LibraryImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static partial bool CheckTokenMembershipEx(SafeAccessTokenHandle TokenHandle, byte[] SidToCheck, uint Flags, ref bool IsMember);
    }
}
