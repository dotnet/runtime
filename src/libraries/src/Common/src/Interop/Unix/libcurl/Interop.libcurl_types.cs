// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using size_t = System.UInt64;
using curl_socket_t = System.Int32;
using curl_off_t = System.Int64;

internal static partial class Interop
{
    internal static partial class libcurl
    {
        // Class for constants defined for the enum CURLoption in curl.h
        internal static partial class CURLoption
        {
            // Curl options are of the format <type base> + <n>
            private const int CurlOptionLongBase = 0;
            private const int CurlOptionObjectPointBase = 10000;
            private const int CurlOptionFunctionPointBase = 20000;

            internal const int CURLOPT_INFILESIZE = CurlOptionLongBase + 14;
            internal const int CURLOPT_VERBOSE = CurlOptionLongBase + 41;            
            internal const int CURLOPT_NOBODY = CurlOptionLongBase + 44;
            internal const int CURLOPT_UPLOAD = CurlOptionLongBase + 46;
            internal const int CURLOPT_POST = CurlOptionLongBase + 47;
            internal const int CURLOPT_FOLLOWLOCATION = CurlOptionLongBase + 52;
            internal const int CURLOPT_PROXYPORT = CurlOptionLongBase + 59;
            internal const int CURLOPT_POSTFIELDSIZE = CurlOptionLongBase + 60;
            internal const int CURLOPT_MAXREDIRS = CurlOptionLongBase + 68;
            internal const int CURLOPT_NOSIGNAL = CurlOptionLongBase + 99;
            internal const int CURLOPT_PROXYTYPE = CurlOptionLongBase + 101;
            internal const int CURLOPT_HTTPAUTH = CurlOptionLongBase + 107;

            internal const int CURLOPT_WRITEDATA = CurlOptionObjectPointBase + 1;
            internal const int CURLOPT_URL = CurlOptionObjectPointBase + 2;
            internal const int CURLOPT_PROXY = CurlOptionObjectPointBase + 4;
            internal const int CURLOPT_PROXYUSERPWD = CurlOptionObjectPointBase + 6;
            internal const int CURLOPT_READDATA = CurlOptionObjectPointBase + 9;
            internal const int CURLOPT_COOKIE = CurlOptionObjectPointBase + 22;
            internal const int CURLOPT_HTTPHEADER = CurlOptionObjectPointBase + 23;
            internal const int CURLOPT_HEADERDATA = CurlOptionObjectPointBase + 29;
            internal const int CURLOPT_ACCEPTENCODING = CurlOptionObjectPointBase + 102;
            internal const int CURLOPT_PRIVATE = CurlOptionObjectPointBase + 103;
            internal const int CURLOPT_COPYPOSTFIELDS = CurlOptionObjectPointBase + 165;
            internal const int CURLOPT_SEEKDATA = CurlOptionObjectPointBase + 168;
            internal const int CURLOPT_USERNAME = CurlOptionObjectPointBase + 173;
            internal const int CURLOPT_PASSWORD = CurlOptionObjectPointBase + 174;

            internal const int CURLOPT_WRITEFUNCTION = CurlOptionFunctionPointBase + 11;
            internal const int CURLOPT_READFUNCTION = CurlOptionFunctionPointBase + 12;
            internal const int CURLOPT_HEADERFUNCTION = CurlOptionFunctionPointBase + 79;
            internal const int CURLOPT_SEEKFUNCTION = CurlOptionFunctionPointBase + 167;
        }

        // Class for constants defined for the enum CURLINFO in curl.h
        internal static partial class CURLINFO
        {
            // Curl info are of the format <type base> + <n>
            private const int CurlInfoStringBase = 0x100000;
            private const int CurlInfoLongBase = 0x200000;

            internal const int CURLINFO_PRIVATE = CurlInfoStringBase + 21;
            internal const int CURLINFO_HTTPAUTH_AVAIL = CurlInfoLongBase + 23;
        }

        // Class for constants defined for the enum curl_proxytype in curl.h
        internal static partial class curl_proxytype
        {
            internal const int CURLPROXY_HTTP = 0;
        }

        // Class for constants defined for the enum CURLMcode in multi.h
        internal static partial class CURLMcode
        {
            internal const int CURLM_OK = 0;
            internal const int CURLM_BAD_HANDLE = 1;
            internal const int CURLM_BAD_EASY_HANDLE = 2;
            internal const int CURLM_OUT_OF_MEMORY = 3;
            internal const int CURLM_INTERNAL_ERROR = 4;
            internal const int CURLM_BAD_SOCKET = 5;
            internal const int CURLM_UNKNOWN_OPTION = 6;
            internal const int CURLM_ADDED_ALREADY = 7;
        }

        // Class for constants defined for the results of CURL_SEEKFUNCTION
        internal static partial class CURL_SEEKFUNC
        {
            internal const int CURL_SEEKFUNC_OK = 0;
            internal const int CURL_SEEKFUNC_FAIL = 1;
            internal const int CURL_SEEKFUNC_CANTSEEK = 2;
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

        // Poll values used with curl_multi_wait and curl_waitfd.events/revents
        internal const int CURL_WAIT_POLLIN = 0x0001;
        internal const int CURL_WAIT_POLLPRI = 0x0002;
        internal const int CURL_WAIT_POLLOUT = 0x0004;

#pragma warning disable 0649 // until this file is split up, this produces a warning in the X509 project due to being unused
        internal struct curl_waitfd
        {
            internal int fd;
            internal short events;
            internal short revents;
        };
#pragma warning restore 0649

        public delegate size_t curl_readwrite_callback(
            IntPtr buffer,
            size_t size,
            size_t nitems,
            IntPtr context);

        public delegate int seek_callback(
            IntPtr userp, 
            curl_off_t offset, 
            int origin);

        public const int CURL_READFUNC_ABORT = 0x10000000;
        public const int CURL_READFUNC_PAUSE = 0x10000001;
        public const int CURL_WRITEFUNC_PAUSE = 0x10000001;
        public const int CURLPAUSE_CONT = 0;
    }
}
