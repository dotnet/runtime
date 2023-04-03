// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // https://learn.microsoft.com/windows/win32/api/sysinfoapi/nf-sysinfoapi-getsystemtimepreciseasfiletime
        [LibraryImport(Libraries.Kernel32)]
        [SuppressGCTransition]
        internal static unsafe partial void GetSystemTimePreciseAsFileTime(
            // [NativeTypeName("LPFILETIME")]
            ulong* lpSystemTimeAsFileTime);
    }
}
