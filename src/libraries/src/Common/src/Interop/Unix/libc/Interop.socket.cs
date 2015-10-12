// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        internal const int SOCK_STREAM      = 1;
        internal const int SOCK_DGRAM       = 2;
        internal const int SOCK_RAW         = 3;
        internal const int SOCK_RDM         = 4;
        internal const int SOCK_SEQPACKET   = 5;

        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int socket(int domain, int type, int protocol);
    }
}
