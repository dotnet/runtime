// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class mincore
    {
        [DllImport(Interop.Libraries.SecurityLsaPolicy, EntryPoint = "LsaLookupSids", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint LsaLookupSids(
            SafeLsaPolicyHandle handle,
            int count,
            IntPtr[] sids,
            ref SafeLsaMemoryHandle referencedDomains,
            ref SafeLsaMemoryHandle names
            );
    }
}
