// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        [DllImport(Interop.Libraries.NtDll, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal extern static uint RtlIpv6AddressToStringExW(
            [In] byte[] address,
            [In] uint scopeId,
            [In] ushort port,
            [In, Out] StringBuilder addressString,
            [In, Out] ref uint addressStringLength);
    }
}
