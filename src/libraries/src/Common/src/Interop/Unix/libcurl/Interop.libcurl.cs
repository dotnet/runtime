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

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern SafeCurlMultiHandle curl_multi_init();

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern void curl_multi_cleanup(
            IntPtr handle);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_multi_setopt(
            SafeCurlMultiHandle multi_handle,
            int option,
            curl_socket_callback value);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_multi_setopt(
            SafeCurlMultiHandle multi_handle,
            int option,
            curl_multi_timer_callback value);

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
        public static extern int curl_multi_assign(
            SafeCurlMultiHandle multi_handle,
            curl_socket_t sockfd,
            IntPtr sockptr);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_multi_socket_action(
            SafeCurlMultiHandle multi_handle,
            curl_socket_t sockfd,
            int ev_bitmask,
            out int running_handles);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern IntPtr curl_multi_info_read(
            SafeCurlMultiHandle multi_handle,
            out int msgs_in_queue);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern IntPtr curl_multi_strerror(
            int code);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern SafeCurlHandle curl_easy_init();

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern void curl_easy_cleanup(
            IntPtr handle);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern void curl_easy_reset(
            SafeCurlHandle curl);

        [DllImport(Interop.Libraries.LibCurl, CharSet = CharSet.Ansi)]
        public static extern int curl_easy_setopt(
            SafeCurlHandle curl,
            int option,
            string value);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_easy_setopt(
            SafeCurlHandle curl,
            int option,
            long value);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_easy_setopt(
            SafeCurlHandle curl,
            int option,
            ulong value);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_easy_setopt(
            SafeCurlHandle curl,
            int option,
            IntPtr value);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_easy_setopt(
            SafeCurlHandle curl,
            int option,
            curl_readwrite_callback callback);

        [DllImport(Interop.Libraries.LibCurl)]
        public unsafe static extern int curl_easy_setopt(
            SafeCurlHandle curl,
            int option,
            curl_unsafe_write_callback callback);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_easy_setopt(
            SafeCurlHandle curl,
            int option,
            curl_ioctl_callback callback);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern IntPtr curl_easy_strerror(
            int code);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_easy_getinfo(
            IntPtr handle,
            int info,
            out IntPtr value);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_easy_getinfo(
            SafeCurlHandle handle,
            int info,
            out ulong value);

        [DllImport(Interop.Libraries.LibCurl, CharSet = CharSet.Ansi)]
        public static extern IntPtr curl_slist_append(
            IntPtr curl_slist,
            string headerValue);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern void curl_slist_free_all(
            IntPtr curl_slist);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern IntPtr curl_version_info(int curlVersionStamp);
 }
}
