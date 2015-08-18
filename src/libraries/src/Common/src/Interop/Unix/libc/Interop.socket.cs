// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        public const int SOCK_DGRAM = 2;

        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int socket(int domain, int type, int protocol);
    }
}
