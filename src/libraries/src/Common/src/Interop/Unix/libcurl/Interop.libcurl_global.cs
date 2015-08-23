// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using size_t = System.UInt64;
using curl_socket_t = System.Int32;

internal static partial class Interop
{
    internal static partial class libcurl
    {
        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_global_init(
            long flags);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern void curl_global_cleanup();
    }
}