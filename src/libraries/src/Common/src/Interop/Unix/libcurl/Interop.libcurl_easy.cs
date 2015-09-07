// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libcurl
    {
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
            SafeHandle value);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_easy_setopt(
            SafeCurlHandle curl,
            int option,
            Delegate callback);

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

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_easy_perform(
            SafeCurlHandle curl);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern int curl_easy_pause(SafeCurlHandle easy, int bitmask);
    }
}
