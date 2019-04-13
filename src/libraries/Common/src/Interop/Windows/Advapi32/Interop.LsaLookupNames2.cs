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
        [DllImport(Interop.Libraries.Advapi32, EntryPoint = "LsaLookupNames2", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint LsaLookupNames2(
            SafeLsaPolicyHandle handle,
            int flags,
            int count,
            MARSHALLED_UNICODE_STRING[] names,
            out SafeLsaMemoryHandle referencedDomains,
            out SafeLsaMemoryHandle sids
        );
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct MARSHALLED_UNICODE_STRING
        {
            internal ushort Length;
            internal ushort MaximumLength;
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string Buffer;
        }
    }
}
