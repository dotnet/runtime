// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        private static extern uint ERR_get_error();

        [DllImport(Libraries.LibCrypto, CharSet = CharSet.Ansi)]
        private static extern void ERR_error_string_n(uint e, StringBuilder buf, int len);

        private static string ERR_error_string_n(uint error)
        {
            StringBuilder buf = new StringBuilder(1024);

            ERR_error_string_n(error, buf, buf.Capacity);
            return buf.ToString();
        }

        internal static Exception CreateOpenSslCryptographicException()
        {
            const uint ReasonMask = 0x0FFF;
            const uint ERR_R_MALLOC_FAILURE = 64 | 1;

            // The Windows cryptography library reports error codes through
            // Marshal.GetLastWin32Error, which has a single value when the
            // function exits, last writer wins.
            //
            // OpenSSL maintains an error queue. Calls to ERR_get_error read
            // values out of the queue in the order that ERR_set_error wrote
            // them. Nothing enforces that a single call into an OpenSSL
            // function will guarantee at-most one error being set.
            //
            // In order to maintain parity in how error flows look between the
            // Windows code and the OpenSSL-calling code, drain the queue
            // whenever an Exception is desired, and report the exception
            // related to the last value in the queue.
            uint error = ERR_get_error();
            uint lastRead = error;

            // 0 (there's no named constant) is only returned when the calls
            // to ERR_get_error exceed the calls to ERR_set_error.
            while (lastRead != 0)
            {
                error = lastRead;
                lastRead = ERR_get_error();
            }

            // If we're in an error flow which results in an Exception, but
            // no calls to ERR_set_error were made, throw the unadorned
            // CryptographicException.
            if (error == 0)
            {
                return new CryptographicException();
            }

            // Inline version of the ERR_GET_REASON macro.
            uint reason = error & ReasonMask;

            if (reason == ERR_R_MALLOC_FAILURE)
            {
                return new OutOfMemoryException();
            }

            // If there was an error code, and it wasn't something handled specially,
            // use the OpenSSL error string as the message to a CryptographicException.
            return new CryptographicException(ERR_error_string_n(error));
        }

        internal static void CheckValidOpenSslHandle(SafeHandle handle)
        {
            if (handle == null || handle.IsInvalid)
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        internal static void CheckValidOpenSslHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw CreateOpenSslCryptographicException();
            }
        }
    }
}
