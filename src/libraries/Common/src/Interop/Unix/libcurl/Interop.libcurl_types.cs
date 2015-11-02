// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using size_t = System.UInt64;
using curl_off_t = System.Int64;

internal static partial class Interop
{
    internal static partial class libcurl
    {
        // Class for constants defined for the results of CURL_SEEKFUNCTION
        internal static partial class CURL_SEEKFUNC
        {
            internal const int CURL_SEEKFUNC_OK = 0;
            internal const int CURL_SEEKFUNC_FAIL = 1;
            internal const int CURL_SEEKFUNC_CANTSEEK = 2;
        }

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
