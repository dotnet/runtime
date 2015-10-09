// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using size_t = System.IntPtr;
using socklen_t = System.UInt32;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        public static extern unsafe size_t recvfrom(int sockfd, void* buf, size_t len, int flags, byte* dest_addr, socklen_t* addrlen);
    }
}
