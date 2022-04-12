// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if REGISTRY_ASSEMBLY
using Microsoft.Win32.SafeHandles;
#else
using Internal.Win32.SafeHandles;
#endif
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Libraries.Advapi32, EntryPoint = "RegOpenKeyExW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int RegOpenKeyEx(
            SafeRegistryHandle hKey,
            string? lpSubKey,
            int ulOptions,
            int samDesired,
            out SafeRegistryHandle hkResult);

        [LibraryImport(Libraries.Advapi32, EntryPoint = "RegOpenKeyExW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int RegOpenKeyEx(
            IntPtr hKey,
            string? lpSubKey,
            int ulOptions,
            int samDesired,
            out SafeRegistryHandle hkResult);
    }
}
