// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using socklen_t = System.UInt32;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern unsafe int gethostbyaddr_r(void* addr, socklen_t len, int type, hostent* ret, byte* buf, IntPtr buflen, hostent** result, int* h_errnop);
    }
}
