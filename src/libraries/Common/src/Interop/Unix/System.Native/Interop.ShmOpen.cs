// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ShmOpen", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial SafeFileHandle ShmOpen(string name, OpenFlags flags, int mode);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ShmUnlink", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial int ShmUnlink(string name);
    }
}
