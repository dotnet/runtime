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
        // Class for constants defined for the global flags in curl.h
        internal static partial class CurlGlobalFlags
        {
            internal const long CURL_GLOBAL_SSL = 1L;
            internal const long CURL_GLOBAL_WIN32 = 2L;
            internal const long CURL_GLOBAL_ALL = (CURL_GLOBAL_SSL | CURL_GLOBAL_WIN32);
        }

        // Class for constants defined for the enum CURLoption in curl.h
        internal static partial class CURLoption
        {
            // Curl options are of the format <type base> + <n>
            private const int CurlOptionLongBase = 0;
            private const int CurlOptionObjectPointBase = 10000;
            private const int CurlOptionFunctionPointBase = 20000;

            internal const int CURLOPT_NOBODY = CurlOptionLongBase + 44;
            internal const int CURLOPT_UPLOAD = CurlOptionLongBase + 46;
            internal const int CURLOPT_FOLLOWLOCATION = CurlOptionLongBase + 52;
            internal const int CURLOPT_PROXYPORT = CurlOptionLongBase + 59;
            internal const int CURLOPT_PROXYTYPE = CurlOptionLongBase + 101;

            internal const int CURLOPT_WRITEDATA = CurlOptionObjectPointBase + 1;
            internal const int CURLOPT_URL = CurlOptionObjectPointBase + 2;
            internal const int CURLOPT_PROXY = CurlOptionObjectPointBase + 4;
            internal const int CURLOPT_PROXYUSERPWD = CurlOptionObjectPointBase + 6;
            internal const int CURLOPT_READDATA = CurlOptionObjectPointBase + 9;
            internal const int CURLOPT_HTTPHEADER = CurlOptionObjectPointBase + 23;
            internal const int CURLOPT_HEADERDATA = CurlOptionObjectPointBase + 29;
            internal const int CURLOPT_ACCEPTENCODING = CurlOptionObjectPointBase + 102;
            internal const int CURLOPT_PRIVATE = CurlOptionObjectPointBase + 103;
            internal const int CURLOPT_IOCTLDATA = CurlOptionObjectPointBase + 131;

            internal const int CURLOPT_WRITEFUNCTION = CurlOptionFunctionPointBase + 11;
            internal const int CURLOPT_READFUNCTION = CurlOptionFunctionPointBase + 12;
            internal const int CURLOPT_HEADERFUNCTION = CurlOptionFunctionPointBase + 79;
            internal const int CURLOPT_IOCTLFUNCTION = CurlOptionFunctionPointBase + 130;
        }

        // Class for constants defined for the enum CURLMoption in multi.h
        internal static partial class CURLMoption
        {
            // Curl options are of the format <type base> + <n>
            private const int CurlOptionObjectPointBase = 10000;
            private const int CurlOptionFunctionPointBase = 20000;

            internal const int CURLMOPT_TIMERDATA = CurlOptionObjectPointBase + 5;

            internal const int CURLMOPT_SOCKETFUNCTION = CurlOptionFunctionPointBase + 1;
            internal const int CURLMOPT_TIMERFUNCTION = CurlOptionFunctionPointBase + 4;
        }

        // Class for constants defined for the enum CURLINFO in curl.h
        internal static partial class CURLINFO
        {
            // Curl info are of the format <type base> + <n>
            private const int CurlInfoStringBase = 0x100000;

            internal const int CURLINFO_PRIVATE = CurlInfoStringBase + 21;
        }

        // Class for constants defined for the enum curl_proxytype in curl.h
        internal static partial class curl_proxytype
        {
            internal const int CURLPROXY_HTTP = 0;
        }

        // Class for constants defined for the enum CURLcode in curl.h
        internal static partial class CURLcode
        {
            internal const int CURLE_OK = 0;
        }

        // Class for constants defined for the enum CURLMcode in multi.h
        internal static partial class CURLMcode
        {
            internal const int CURLM_OK = 0;
        }

        // Class for constants defined for the enum curlioerr in curl.h
        internal static partial class curlioerr
        {
            internal const int CURLIOE_OK = 0;
            internal const int CURLIOE_UNKNOWNCMD = 1;
            internal const int CURLIOE_FAILRESTART = 2;
        }

        // Class for constants defined for the enum curliocmd in curl.h
        internal static partial class curliocmd
        {
            internal const int CURLIOCMD_RESTARTREAD = 1;
        }

        // Class for CURL_POLL_* macros in multi.h
        internal static partial class CurlPoll
        {
            internal const int CURL_POLL_REMOVE = 4;
        }

        // Class for CURL_CSELECT_* macros in multi.h
        internal static partial class CurlSelect
        {
            internal const int CURL_CSELECT_IN = 1;
            internal const int CURL_CSELECT_OUT = 2;
        }

        // Class for constants defined for the enum CURLMSG in multi.h
        internal static partial class CURLMSG
        {
            internal const int CURLMSG_DONE = 1;
        }

        // Type definition of CURLMsg from multi.h
        [StructLayout(LayoutKind.Explicit)]
        internal struct CURLMsg
        {
            [FieldOffset(0)]
            internal int msg;
            [FieldOffset(8)]
            internal IntPtr easy_handle;
            [FieldOffset(16)]
            internal IntPtr data;
            [FieldOffset(16)]
            internal int result;
        }

        public delegate int curl_socket_callback(
            IntPtr handle,
            curl_socket_t sockfd,
            int what,
            IntPtr context,
            IntPtr sockptr);

        public delegate int curl_multi_timer_callback(
            IntPtr handle,
            long timeout_ms,
            IntPtr context);

        public delegate size_t curl_readwrite_callback(
            IntPtr buffer,
            size_t size,
            size_t nitems,
            IntPtr context);

        public unsafe delegate size_t curl_unsafe_write_callback(
            byte* buffer,
            size_t size,
            size_t nitems,
            IntPtr context);

        public delegate int curl_ioctl_callback(
            IntPtr handle,
            int cmd,
            IntPtr context);
    }
}
