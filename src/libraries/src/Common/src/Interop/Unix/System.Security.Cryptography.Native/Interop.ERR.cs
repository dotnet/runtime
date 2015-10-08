// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative)]
        private static extern ulong ErrGetError();

        [DllImport(Libraries.CryptoNative, CharSet = CharSet.Ansi)]
        private static extern void ErrErrorStringN(ulong e, StringBuilder buf, int len);

        private static string ErrErrorStringN(ulong error)
        {
            StringBuilder buf = new StringBuilder(1024);

            ErrErrorStringN(error, buf, buf.Capacity);
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
            ulong error = ErrGetError();
            ulong lastRead = error;

            // 0 (there's no named constant) is only returned when the calls
            // to ERR_get_error exceed the calls to ERR_set_error.
            while (lastRead != 0)
            {
                error = lastRead;
                lastRead = ErrGetError();
            }

            // If we're in an error flow which results in an Exception, but
            // no calls to ERR_set_error were made, throw the unadorned
            // CryptographicException.
            if (error == 0)
            {
                return new CryptographicException();
            }

            // Even though ErrGetError returns ulong (C++ unsigned long), we 
            // really only expect error codes in the UInt32 range
            Debug.Assert(error <= uint.MaxValue, "ErrGetError should only return error codes in the UInt32 range.");

            // Inline version of the ERR_GET_REASON macro.
            ulong reason = error & ReasonMask;

            if (reason == ERR_R_MALLOC_FAILURE)
            {
                return new OutOfMemoryException();
            }

            // If there was an error code, and it wasn't something handled specially,
            // use the OpenSSL error string as the message to a CryptographicException.
            return new OpenSslCryptographicException(unchecked((int)error), ErrErrorStringN(error));
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

        private sealed class OpenSslCryptographicException : CryptographicException
        {
            internal OpenSslCryptographicException(int errorCode, string message)
                : base(message)
            {
                HResult = errorCode;
            }
        }
    }
}
