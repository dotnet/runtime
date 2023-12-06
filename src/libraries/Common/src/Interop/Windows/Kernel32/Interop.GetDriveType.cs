// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "GetDriveTypeW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int GetDriveType(string drive);

    }
}
