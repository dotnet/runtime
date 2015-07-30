// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

using socklen_t = System.UInt32;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern unsafe int getnameinfo(sockaddr* sa, socklen_t salen, StringBuilder host, socklen_t hostlen, StringBuilder serv, socklen_t servlen, int flags);
    }
}
