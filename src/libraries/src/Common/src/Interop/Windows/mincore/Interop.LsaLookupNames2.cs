// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class mincore
    {
        [DllImport(Interop.Libraries.SecurityLsaPolicy, EntryPoint = "LsaLookupNames2", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint LsaLookupNames2(
            SafeLsaPolicyHandle handle,
            int flags,
            int count,
            UNICODE_STRING[] names,
            ref SafeLsaMemoryHandle referencedDomains,
            ref SafeLsaMemoryHandle sids
            );
    }
}
