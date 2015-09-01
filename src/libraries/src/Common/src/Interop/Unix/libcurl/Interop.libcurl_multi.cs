// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libcurl
    {
        [DllImport(Interop.Libraries.LibCurl)]
        public static extern SafeCurlMultiHandle curl_multi_init();

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_multi_cleanup(
            IntPtr handle);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_multi_setopt(
            SafeCurlMultiHandle multi_handle,
            int option,
            IntPtr value);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_multi_add_handle(
            SafeCurlMultiHandle multi_handle,
            SafeCurlHandle easy_handle);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_multi_remove_handle(
            SafeCurlMultiHandle multi_handle,
            SafeCurlHandle easy_handle);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern unsafe int curl_multi_wait(
            SafeCurlMultiHandle multi_handle,
            curl_waitfd* extra_fds,
            uint extra_nfds,
            int timeout_ms,
            out int numfds);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_multi_perform(
            SafeCurlMultiHandle multi_handle,
            out int running_handles);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern IntPtr curl_multi_info_read(
            SafeCurlMultiHandle multi_handle,
            out int msgs_in_queue);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern IntPtr curl_multi_strerror(
            int code);
    }
}
