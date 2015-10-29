// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using SafeCurlMultiHandle = Interop.Http.SafeCurlMultiHandle;
using CURLMcode = Interop.Http.CURLMcode;

internal static partial class Interop
{
    internal static partial class libcurl
    {
        [DllImport(Libraries.LibCurl)]
        public static extern unsafe CURLMcode curl_multi_wait(
            SafeCurlMultiHandle multi_handle,
            curl_waitfd* extra_fds,
            uint extra_nfds,
            int timeout_ms,
            out int numfds);
    }
}
