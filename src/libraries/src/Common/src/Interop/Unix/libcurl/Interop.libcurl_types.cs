// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using size_t = System.UInt64;
using curl_off_t = System.Int64;

internal static partial class Interop
{
    internal static partial class libcurl
    {
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
            internal Http.CURLcode result;
        }

        // Poll values used with curl_multi_wait and curl_waitfd.events/revents
        internal const int CURL_WAIT_POLLIN = 0x0001;

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
    }
}
