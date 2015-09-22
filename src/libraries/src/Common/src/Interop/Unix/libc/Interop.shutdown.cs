// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        public const int SHUT_RD = 0;
        public const int SHUT_WR = 1;
        public const int SHUT_RDWR = 2;

        [DllImport(Libraries.Libc, SetLastError = true)]
        public static extern int shutdown(int sockfd, int how);
    }
}
