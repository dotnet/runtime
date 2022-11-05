// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static unsafe partial int WaitForMultipleObjects(uint nCount, nint* handles, [MarshalAs(UnmanagedType.Bool)] bool waitAll, uint timeout);
    }
}
