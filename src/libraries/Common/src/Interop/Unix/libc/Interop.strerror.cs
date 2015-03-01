// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using size_t = System.IntPtr;

internal static partial class Interop
{
    internal static partial class libc
    {
        internal static string strerror(int errno)
        {
            string result = s_isGnu.Value ? strerror_gnu(errno) : strerror_xsi(errno);
            // System.Diagnostics.Debug.WriteLine("strerror: " + result + "\n" + System.Environment.StackTrace); // uncomment to aid debugging
            return result;
        }

        // strerror is not thread-safe; instead, there is the strerror_r function, which is thread-safe.
        // However, there are two versions of strerror_r:
        // - GNU: char* strerror_r(int, char*, size_t);
        // - XSI: int   strerror_r(int, char*, size_T);
        // The former may or may not use the supplied buffer, and returns the error message string.
        // The latter stores the error message string into the supplied buffer.
        // Due to the different return values, we don't want to just choose one signature
        // and try to use a heuristic to deduce which version we're dealing with, as that
        // could result in a stack imbalance due to the varied return result size on 64-bit.  Instead,
        // we detect whether we're compiled against GNU's libc by trying to invoke its
        // version-getting method, and invoke the right signature based on that.

        private static Lazy<bool> s_isGnu = new Lazy<bool>(() =>
        {
            try
            {
                // Just try to call the P/Invoke.  If it succeeds, this is GNU libc.
                gnu_get_libc_version();
                return true;
            }
            catch
            {
                // Otherwise, it's not.
                return false;
            }
        });

        private const int MaxErrorMessageLength = 1024; // length long enough for most any Unix error messages

        private static unsafe string strerror_gnu(int errno)
        {
            byte* buffer = stackalloc byte[MaxErrorMessageLength];
            IntPtr result = strerror_r_gnu(errno, buffer, (IntPtr)MaxErrorMessageLength);
            return Marshal.PtrToStringAnsi(result);
        }

        private static unsafe string strerror_xsi(int errno)
        {
            byte* buffer = stackalloc byte[MaxErrorMessageLength];
            int ignored = strerror_r_xsi(errno, buffer, (size_t)MaxErrorMessageLength);

            // We ignore the return value of strerror_r.  The only three valid return values are 0
            // for success, EINVAL for an unknown errno value, and ERANGE if there's not enough
            // buffer space provided.  For EINVAL, it'll still fill the buffer with a reasonable error
            // string (e.g. "Unknown error: 0x123"), and for ERANGE, it'll fill the buffer with as much 
            // of the error as can it, null-terminated.  For now we don't grow the buffer, we could
            // in the future if desired.

            return Marshal.PtrToStringAnsi((IntPtr)buffer);
        }

        [DllImport(Libraries.Libc, EntryPoint = "strerror_r")]
        private static extern unsafe IntPtr strerror_r_gnu(int errnum, byte* buf, size_t buflen);

        [DllImport(Libraries.Libc, EntryPoint = "strerror_r")]
        private static extern unsafe int    strerror_r_xsi(int errnum, byte* buf, size_t buflen);

        [DllImport(Libraries.Libc)]
        private static extern IntPtr gnu_get_libc_version();
    }
}
