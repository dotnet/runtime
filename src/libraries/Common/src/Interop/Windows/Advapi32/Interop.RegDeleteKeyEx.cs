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
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "RegDeleteKeyExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static partial int RegDeleteKeyEx(
#else
        [DllImport(Libraries.Advapi32, EntryPoint = "RegDeleteKeyExW", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int RegDeleteKeyEx(
#endif
            SafeRegistryHandle hKey,
            string lpSubKey,
            int samDesired,
            int Reserved);
    }
}
