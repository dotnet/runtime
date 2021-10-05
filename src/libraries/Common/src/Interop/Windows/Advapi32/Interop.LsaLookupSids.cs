// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.Advapi32, EntryPoint = "LsaLookupSids", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial uint LsaLookupSids(
#else
        [DllImport(Interop.Libraries.Advapi32, EntryPoint = "LsaLookupSids", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint LsaLookupSids(
#endif
            SafeLsaPolicyHandle handle,
            int count,
            IntPtr[] sids,
            out SafeLsaMemoryHandle referencedDomains,
            out SafeLsaMemoryHandle names
        );
    }
}
