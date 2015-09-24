// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using socklen_t = System.UInt32;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        public static extern unsafe int getsockopt(int sockfd, int level, int optname, void* optval, socklen_t* optlen);
    }
}
