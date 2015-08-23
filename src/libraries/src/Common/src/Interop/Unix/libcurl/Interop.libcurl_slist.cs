// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libcurl
    {
        [DllImport(Interop.Libraries.LibCurl, CharSet = CharSet.Ansi)]
        public static extern IntPtr curl_slist_append(
            IntPtr curl_slist,
            string headerValue);

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern void curl_slist_free_all(
            IntPtr curl_slist);
    }
}
