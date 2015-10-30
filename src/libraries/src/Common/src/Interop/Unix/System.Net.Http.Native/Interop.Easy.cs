// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Http
    {
        [DllImport(Libraries.HttpNative)]
        public static extern SafeCurlHandle EasyCreate();

        [DllImport(Libraries.HttpNative)]
        private static extern void EasyDestroy(IntPtr handle);

        [DllImport(Libraries.HttpNative, CharSet = CharSet.Ansi)]
        public static extern CURLcode EasySetOptionString(SafeCurlHandle curl, CURLoption option, string value);

        [DllImport(Libraries.HttpNative)]
        public static extern CURLcode EasySetOptionLong(SafeCurlHandle curl, CURLoption option, long value);

        [DllImport(Libraries.HttpNative)]
        public static extern CURLcode EasySetOptionPointer(SafeCurlHandle curl, CURLoption option, IntPtr value);

        [DllImport(Libraries.HttpNative)]
        public static extern CURLcode EasySetOptionPointer(SafeCurlHandle curl, CURLoption option, SafeHandle value);

        [DllImport(Libraries.HttpNative)]
        public static extern CURLcode EasySetOptionPointer(SafeCurlHandle curl, CURLoption option, Delegate callback);

        [DllImport(Libraries.HttpNative)]
        public static extern IntPtr EasyGetErrorString(int code);

        [DllImport(Libraries.HttpNative)]
        public static extern CURLcode EasyGetInfoPointer(IntPtr handle, CURLINFO info, out IntPtr value);

        [DllImport(Libraries.HttpNative)]
        public static extern CURLcode EasyGetInfoLong(SafeCurlHandle handle, CURLINFO info, out long value);

        [DllImport(Libraries.HttpNative)]
        public static extern CURLcode EasyPerform(SafeCurlHandle curl);

        [DllImport(Libraries.HttpNative)]
        public static extern CURLcode EasyUnpause(SafeCurlHandle easy);

        // Curl options are of the format <type base> + <n>
        private const int CurlOptionLongBase = 0;
        private const int CurlOptionObjectPointBase = 10000;
        private const int CurlOptionFunctionPointBase = 20000;

        // Enum for constants defined for the enum CURLoption in curl.h
        internal enum CURLoption
        {
            CURLOPT_INFILESIZE = CurlOptionLongBase + 14,
            CURLOPT_VERBOSE = CurlOptionLongBase + 41,
            CURLOPT_NOBODY = CurlOptionLongBase + 44,
            CURLOPT_UPLOAD = CurlOptionLongBase + 46,
            CURLOPT_POST = CurlOptionLongBase + 47,
            CURLOPT_FOLLOWLOCATION = CurlOptionLongBase + 52,
            CURLOPT_PROXYPORT = CurlOptionLongBase + 59,
            CURLOPT_POSTFIELDSIZE = CurlOptionLongBase + 60,
            CURLOPT_MAXREDIRS = CurlOptionLongBase + 68,
            CURLOPT_NOSIGNAL = CurlOptionLongBase + 99,
            CURLOPT_PROXYTYPE = CurlOptionLongBase + 101,
            CURLOPT_HTTPAUTH = CurlOptionLongBase + 107,
            CURLOPT_PROTOCOLS = CurlOptionLongBase + 181,
            CURLOPT_REDIR_PROTOCOLS = CurlOptionLongBase + 182,

            CURLOPT_WRITEDATA = CurlOptionObjectPointBase + 1,
            CURLOPT_URL = CurlOptionObjectPointBase + 2,
            CURLOPT_PROXY = CurlOptionObjectPointBase + 4,
            CURLOPT_PROXYUSERPWD = CurlOptionObjectPointBase + 6,
            CURLOPT_READDATA = CurlOptionObjectPointBase + 9,
            CURLOPT_COOKIE = CurlOptionObjectPointBase + 22,
            CURLOPT_HTTPHEADER = CurlOptionObjectPointBase + 23,
            CURLOPT_HEADERDATA = CurlOptionObjectPointBase + 29,
            CURLOPT_CUSTOMREQUEST = CurlOptionObjectPointBase + 36,
            CURLOPT_ACCEPT_ENCODING = CurlOptionObjectPointBase + 102,
            CURLOPT_PRIVATE = CurlOptionObjectPointBase + 103,
            CURLOPT_COPYPOSTFIELDS = CurlOptionObjectPointBase + 165,
            CURLOPT_SEEKDATA = CurlOptionObjectPointBase + 168,
            CURLOPT_USERNAME = CurlOptionObjectPointBase + 173,
            CURLOPT_PASSWORD = CurlOptionObjectPointBase + 174,

            CURLOPT_WRITEFUNCTION = CurlOptionFunctionPointBase + 11,
            CURLOPT_READFUNCTION = CurlOptionFunctionPointBase + 12,
            CURLOPT_HEADERFUNCTION = CurlOptionFunctionPointBase + 79,
            CURLOPT_SSL_CTX_FUNCTION = CurlOptionFunctionPointBase + 108,
            CURLOPT_SEEKFUNCTION = CurlOptionFunctionPointBase + 167,
        }

        // Curl info are of the format <type base> + <n>
        private const int CurlInfoStringBase = 0x100000;
        private const int CurlInfoLongBase = 0x200000;

        // Enum for constants defined for the enum CURLINFO in curl.h
        internal enum CURLINFO
        {
            CURLINFO_PRIVATE = CurlInfoStringBase + 21,
            CURLINFO_HTTPAUTH_AVAIL = CurlInfoLongBase + 23,
        }

        // AUTH related constants
        [Flags]
        internal enum CURLAUTH
        {
            None = 0,
            Basic = 1 << 0,
            Digest = 1 << 1,
            Negotiate = 1 << 2,
        }

        // Class for constants defined for the enum curl_proxytype in curl.h
        internal enum curl_proxytype
        {
            CURLPROXY_HTTP = 0,
        }

        [Flags]
        internal enum CurlProtocols
        {
            CURLPROTO_HTTP = (1 << 0),
            CURLPROTO_HTTPS = (1 << 1),
        }

        internal sealed class SafeCurlHandle : SafeHandle
        {
            public SafeCurlHandle() : base(IntPtr.Zero, true)
            {
            }

            public override bool IsInvalid
            {
                get { return handle == IntPtr.Zero; }
            }

            protected override bool ReleaseHandle()
            {
                EasyDestroy(handle);
                SetHandle(IntPtr.Zero);
                return true;
            }
        }
    }
}
