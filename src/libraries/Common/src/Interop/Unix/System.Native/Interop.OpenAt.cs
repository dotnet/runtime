// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_OpenAt", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial SafeFileHandle OpenAt(SafeFileHandle directoryHandle, string path, OpenFlags flags, int mode);
    }
}
