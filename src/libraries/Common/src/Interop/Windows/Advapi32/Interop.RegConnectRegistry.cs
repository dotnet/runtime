// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

#if REGISTRY_ASSEMBLY
using Microsoft.Win32.SafeHandles;
#else
using Internal.Win32.SafeHandles;
#endif

internal static partial class Interop
{
    internal static partial class Advapi32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "RegConnectRegistryW", CharSet = CharSet.Unicode)]
        internal static partial int RegConnectRegistry(
#else
        [DllImport(Libraries.Advapi32, EntryPoint = "RegConnectRegistryW", CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern int RegConnectRegistry(
#endif
            string machineName,
            SafeRegistryHandle key,
            out SafeRegistryHandle result);
    }
}
