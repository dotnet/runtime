// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "K32EnumProcessModules",  SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial bool EnumProcessModules(SafeProcessHandle handle, IntPtr[]? modules, int size, out int needed);
    }
}
