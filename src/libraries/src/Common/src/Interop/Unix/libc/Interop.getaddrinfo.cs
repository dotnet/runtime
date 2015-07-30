// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern unsafe int getaddrinfo(string node, string service, addrinfo* hints, addrinfo** res);
    }
}
