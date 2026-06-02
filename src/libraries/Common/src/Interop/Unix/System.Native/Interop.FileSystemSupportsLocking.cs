// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_FileSystemSupportsLocking")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool FileSystemSupportsLocking(SafeFileHandle fd, LockOperations lockOperation, [MarshalAs(UnmanagedType.Bool)] bool accessWrite);
    }
}
