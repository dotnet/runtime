// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        [DllImport(Interop.Libraries.NtDll, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal extern static uint RtlIpv6StringToAddressExW(
            [In] string s,
            [Out] byte[] address,
            [Out] out uint scopeId,
            [Out] out ushort port);
    }
}
