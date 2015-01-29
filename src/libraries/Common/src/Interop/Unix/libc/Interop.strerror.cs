// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using size_t = System.IntPtr;

internal static partial class Interop
{
    internal static partial class libc
    {
        internal unsafe static string strerror(int errno) // thread-safe version of strerror that internally uses strerror_r, which is thread-safe
        {
            const int bufferLength = 1024; // length long enough for most any Unix error messages
            byte* buffer = stackalloc byte[bufferLength];
            IntPtr errorPtr = (IntPtr)strerror_r(errno, buffer, (IntPtr)bufferLength);
            return Marshal.PtrToStringAnsi(errorPtr); // TODO: Use the proper Encoding
        }

        [DllImport(Libraries.Libc)]
        private static extern unsafe byte* strerror_r(int errnum, byte* buf, size_t buflen); // GNU-specific
    }
}
