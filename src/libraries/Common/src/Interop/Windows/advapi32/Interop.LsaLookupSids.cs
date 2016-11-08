// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Interop.Libraries.Advapi32, EntryPoint = "LsaLookupSids", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint LsaLookupSids(
            SafeLsaPolicyHandle handle,
            int count,
            IntPtr[] sids,
            ref SafeLsaMemoryHandle referencedDomains,
            ref SafeLsaMemoryHandle names
            );
    }
}
