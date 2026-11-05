// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [Flags]
        internal enum UnlinkAtFlags
        {
            None = 0,
            AT_REMOVEDIR = 0x0001,
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_UnlinkAt", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial int UnlinkAt(SafeFileHandle directoryHandle, string path, UnlinkAtFlags flags);
    }
}
