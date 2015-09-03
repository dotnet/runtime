// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;


namespace System.Net.Http
{
    internal sealed class CurlException : Exception
    {
        internal CurlException(int error, string message) : base(message)
        {
            HResult = error;
        }

        internal CurlException(int error, bool isMulti) : this(error, GetCurlErrorString(error, isMulti))
        {
        }

        internal static string GetCurlErrorString(int code, bool isMulti)
        {
            IntPtr ptr = isMulti ? Interop.libcurl.curl_multi_strerror(code) : Interop.libcurl.curl_easy_strerror(code);
            return Marshal.PtrToStringAnsi(ptr);
        }
    }
}
