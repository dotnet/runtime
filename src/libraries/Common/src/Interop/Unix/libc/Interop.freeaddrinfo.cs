// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc)]
        public static extern unsafe void freeaddrinfo(addrinfo* res);
    }
}
