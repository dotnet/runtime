// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libcurl
    {
        [DllImport(Interop.Libraries.LibCurl, CharSet = CharSet.Ansi)]
        private static extern IntPtr curl_slist_append(
            IntPtr curl_slist,
            string headerValue);

        internal static bool curl_slist_append(SafeCurlSlistHandle curl_slist, string headerValue)
        {
            bool gotRef = false;
            try
            {
                curl_slist.DangerousAddRef(ref gotRef);
                IntPtr newHandle = curl_slist_append(curl_slist.DangerousGetHandle(), headerValue);
                if (newHandle != IntPtr.Zero)
                {
                    curl_slist.SetHandle(newHandle);
                    return true;
                }
                return false;
            }
            finally
            {
                if (!gotRef)
                    curl_slist.DangerousRelease();
            }
        } 

        [DllImport(Interop.Libraries.LibCurl)]
        public static extern void curl_slist_free_all(
            IntPtr curl_slist);
    }
}
