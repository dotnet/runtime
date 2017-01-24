// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        [DllImport(Interop.Libraries.NtDll, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal extern static unsafe uint RtlIpv6StringToAddressExW(
            [In] string s,
            [Out] byte* address,
            [Out] out uint scopeId,
            [Out] out ushort port);
    }
}
