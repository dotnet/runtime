// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
#if REGISTRY_ASSEMBLY
using Microsoft.Win32.SafeHandles;
#else
using Internal.Win32.SafeHandles;
#endif
using System;
using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryInfoKeyW")]
        internal static extern int RegQueryInfoKey(
            SafeRegistryHandle hKey,
            [Out] char[]? lpClass,
            int[]? lpcbClass,
            IntPtr lpReserved_MustBeZero,
            ref int lpcSubKeys,
            int[]? lpcbMaxSubKeyLen,
            int[]? lpcbMaxClassLen,
            ref int lpcValues,
            int[]? lpcbMaxValueNameLen,
            int[]? lpcbMaxValueLen,
            int[]? lpcbSecurityDescriptor,
            int[]? lpftLastWriteTime);
    }
}
