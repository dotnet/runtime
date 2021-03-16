// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if REGISTRY_ASSEMBLY
using Microsoft.Win32.SafeHandles;
#else
using Internal.Win32.SafeHandles;
#endif
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegEnumKeyExW", ExactSpelling = true)]
        internal static extern int RegEnumKeyEx(
            SafeRegistryHandle hKey,
            int dwIndex,
            char[] lpName,
            ref int lpcbName,
            int[]? lpReserved,
            [Out] char[]? lpClass,
            int[]? lpcbClass,
            long[]? lpftLastWriteTime);
    }
}
