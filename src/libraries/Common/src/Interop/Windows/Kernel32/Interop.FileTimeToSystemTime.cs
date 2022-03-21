// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32)]
        [SuppressGCTransition]
        internal static unsafe partial Interop.BOOL FileTimeToSystemTime(ulong* lpFileTime, Interop.Kernel32.SYSTEMTIME* lpSystemTime);
    }
}
