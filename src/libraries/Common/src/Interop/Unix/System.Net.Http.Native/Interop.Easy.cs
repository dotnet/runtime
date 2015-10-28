// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class LibCurl
    {
        [DllImport(Libraries.HttpNative)]
        public static extern SafeCurlHandle EasyCreate();

        [DllImport(Libraries.HttpNative)]
        private static extern void EasyDestroy(IntPtr handle);

        [DllImport(Libraries.HttpNative, CharSet = CharSet.Ansi)]
        public static extern int EasySetOptionString(SafeCurlHandle curl, int option, string value);

        [DllImport(Libraries.HttpNative)]
        public static extern int EasySetOptionLong(SafeCurlHandle curl, int option, long value);

        [DllImport(Libraries.HttpNative)]
        public static extern int EasySetOptionPointer(SafeCurlHandle curl, int option, IntPtr value);

        [DllImport(Libraries.HttpNative)]
        public static extern int EasySetOptionPointer(SafeCurlHandle curl, int option, SafeHandle value);

        [DllImport(Libraries.HttpNative)]
        public static extern int EasySetOptionPointer(SafeCurlHandle curl, int option, Delegate callback);

        [DllImport(Libraries.HttpNative)]
        public static extern IntPtr EasyGetErrorString(int code);

        [DllImport(Libraries.HttpNative)]
        public static extern int EasyGetInfoPointer(IntPtr handle, int info, out IntPtr value);

        [DllImport(Libraries.HttpNative)]
        public static extern int EasyGetInfoLong(SafeCurlHandle handle, int info, out long value);

        [DllImport(Libraries.HttpNative)]
        public static extern int EasyPerform(SafeCurlHandle curl);

        [DllImport(Libraries.HttpNative)]
        public static extern int EasyPause(SafeCurlHandle easy);

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
            internal const int CURLOPT_PROTOCOLS = CurlOptionLongBase + 181;
            internal const int CURLOPT_REDIR_PROTOCOLS = CurlOptionLongBase + 182;

            internal const int CURLOPT_WRITEDATA = CurlOptionObjectPointBase + 1;
            internal const int CURLOPT_URL = CurlOptionObjectPointBase + 2;
            internal const int CURLOPT_PROXY = CurlOptionObjectPointBase + 4;
            internal const int CURLOPT_PROXYUSERPWD = CurlOptionObjectPointBase + 6;
            internal const int CURLOPT_READDATA = CurlOptionObjectPointBase + 9;
            internal const int CURLOPT_COOKIE = CurlOptionObjectPointBase + 22;
            internal const int CURLOPT_HTTPHEADER = CurlOptionObjectPointBase + 23;
            internal const int CURLOPT_HEADERDATA = CurlOptionObjectPointBase + 29;
            internal const int CURLOPT_CUSTOMREQUEST = CurlOptionObjectPointBase + 36;
            internal const int CURLOPT_ACCEPT_ENCODING = CurlOptionObjectPointBase + 102;
            internal const int CURLOPT_PRIVATE = CurlOptionObjectPointBase + 103;
            internal const int CURLOPT_COPYPOSTFIELDS = CurlOptionObjectPointBase + 165;
            internal const int CURLOPT_SEEKDATA = CurlOptionObjectPointBase + 168;
            internal const int CURLOPT_USERNAME = CurlOptionObjectPointBase + 173;
            internal const int CURLOPT_PASSWORD = CurlOptionObjectPointBase + 174;

            internal const int CURLOPT_WRITEFUNCTION = CurlOptionFunctionPointBase + 11;
            internal const int CURLOPT_READFUNCTION = CurlOptionFunctionPointBase + 12;
            internal const int CURLOPT_HEADERFUNCTION = CurlOptionFunctionPointBase + 79;
            internal const int CURLOPT_SSL_CTX_FUNCTION = CurlOptionFunctionPointBase + 108;
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

        // AUTH related constants
        internal static partial class CURLAUTH
        {
            internal const long None = 0;
            internal const long Basic = 1 << 0;
            internal const long Digest = 1 << 1;
            internal const long Negotiate = 1 << 2;
        }

        // Class for constants defined for the enum curl_proxytype in curl.h
        internal static partial class curl_proxytype
        {
            internal const int CURLPROXY_HTTP = 0;
        }

        internal static partial class CURLPROTO_Definitions
        {
            internal const int CURLPROTO_HTTP = (1 << 0);
            internal const int CURLPROTO_HTTPS = (1 << 1);
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
