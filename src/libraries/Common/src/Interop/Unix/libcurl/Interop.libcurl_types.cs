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
            internal const int CURLOPT_POST = CurlOptionLongBase + 47;
            internal const int CURLOPT_FOLLOWLOCATION = CurlOptionLongBase + 52;
            internal const int CURLOPT_PROXYPORT = CurlOptionLongBase + 59;
            internal const int CURLOPT_POSTFIELDSIZE = CurlOptionLongBase + 60;
            internal const int CURLOPT_MAXREDIRS = CurlOptionLongBase + 68;
            internal const int CURLOPT_PROXYTYPE = CurlOptionLongBase + 101;
            internal const int CURLOPT_HTTPAUTH = CurlOptionLongBase + 107;

            internal const int CURLOPT_WRITEDATA = CurlOptionObjectPointBase + 1;
            internal const int CURLOPT_URL = CurlOptionObjectPointBase + 2;
            internal const int CURLOPT_PROXY = CurlOptionObjectPointBase + 4;
            internal const int CURLOPT_PROXYUSERPWD = CurlOptionObjectPointBase + 6;
            internal const int CURLOPT_READDATA = CurlOptionObjectPointBase + 9;
            internal const int CURLOPT_POSTFIELDS = CurlOptionObjectPointBase + 15;
            internal const int CURLOPT_COOKIE = CurlOptionObjectPointBase + 22;
            internal const int CURLOPT_HTTPHEADER = CurlOptionObjectPointBase + 23;
            internal const int CURLOPT_HEADERDATA = CurlOptionObjectPointBase + 29;
            internal const int CURLOPT_ACCEPTENCODING = CurlOptionObjectPointBase + 102;
            internal const int CURLOPT_PRIVATE = CurlOptionObjectPointBase + 103;
            internal const int CURLOPT_IOCTLDATA = CurlOptionObjectPointBase + 131;
            internal const int CURLOPT_USERNAME = CurlOptionObjectPointBase + 173;
            internal const int CURLOPT_PASSWORD = CurlOptionObjectPointBase + 174;

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
            private const int CurlInfoLongBase   = 0x200000;

            internal const int CURLINFO_PRIVATE = CurlInfoStringBase + 21;
            internal const int CURLINFO_HTTPAUTH_AVAIL = CurlInfoLongBase + 23;
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

        // AUTH related constants
        internal static partial class CURLAUTH
        {
            internal const ulong None = 0;
            internal const ulong Basic = 1 << 0;
            internal const ulong Digest = 1 << 1;
            internal const ulong Negotiate = 1 << 2;
            internal const ulong DigestIE = 1 << 4;
            internal const ulong AuthAny = ~DigestIE;
        }

        internal static partial class CURL_VERSION_Features
        {
            internal const int CURL_VERSION_IPV6         = (1<<0);
            internal const int CURL_VERSION_KERBEROS4    = (1<<1);
            internal const int CURL_VERSION_SSL          = (1<<2);
            internal const int CURL_VERSION_LIBZ         = (1<<3);
            internal const int CURL_VERSION_NTLM         = (1<<4);
            internal const int CURL_VERSION_GSSNEGOTIATE = (1<<5);
            internal const int CURL_VERSION_DEBUG        = (1<<6);
            internal const int CURL_VERSION_ASYNCHDNS    = (1<<7);
            internal const int CURL_VERSION_SPNEGO       = (1<<8);
            internal const int CURL_VERSION_LARGEFILE    = (1<<9);
            internal const int CURL_VERSION_IDN          = (1<<10);
            internal const int CURL_VERSION_SSPI         = (1<<11);
            internal const int CURL_VERSION_CONV         = (1<<12);
            internal const int CURL_VERSION_CURLDEBUG    = (1<<13);
            internal const int CURL_VERSION_TLSAUTH_SRP  = (1<<14);
            internal const int CURL_VERSION_NTLM_WB      = (1<<15);
            internal const int CURL_VERSION_HTTP2        = (1<<16);
            internal const int CURL_VERSION_GSSAPI       = (1<<17);
            internal const int CURL_VERSION_KERBEROS5    = (1<<18);
            internal const int CURL_VERSION_UNIX_SOCKETS = (1<<19);
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

        // NOTE: The definition of this structure in Curl/curl.h is larger than
        // than what is defined below. This definition is only valid for use with
        // Marshal.PtrToStructure and not for general use in P/Invoke signatures.
        [StructLayout(LayoutKind.Sequential)]
        internal struct curl_version_info_data
        {
            internal int age;
            private unsafe char *version;
            private int versionNum;
            private unsafe char *host;
            internal int features;
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
