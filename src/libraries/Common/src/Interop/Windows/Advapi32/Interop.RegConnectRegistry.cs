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
        [LibraryImport(Libraries.Advapi32, EntryPoint = "RegConnectRegistryW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int RegConnectRegistry(
            string machineName,
            IntPtr key,
            out SafeRegistryHandle result);
    }
}
