// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrGetError")]
        internal static extern ulong ErrGetError();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrPeekError")]
        internal static extern ulong ErrPeekError();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrGetErrorAlloc")]
        private static extern ulong ErrGetErrorAlloc([MarshalAs(UnmanagedType.Bool)] out bool isAllocFailure);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrReasonErrorString")]
        internal static extern IntPtr ErrReasonErrorString(ulong error);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrErrorStringN")]
        private static extern unsafe void ErrErrorStringN(ulong e, byte* buf, int len);

        private static unsafe string ErrErrorStringN(ulong error)
        {
            var buffer = new byte[1024];
            fixed (byte* buf = &buffer[0])
            {
                ErrErrorStringN(error, buf, buffer.Length);
                return Marshal.PtrToStringAnsi((IntPtr)buf);
            }
        }

        internal static Exception CreateOpenSslCryptographicException()
        {
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
            bool isAllocFailure;
            ulong error = ErrGetErrorAlloc(out isAllocFailure);
            ulong lastRead = error;
            bool lastIsAllocFailure = isAllocFailure;

            // 0 (there's no named constant) is only returned when the calls
            // to ERR_get_error exceed the calls to ERR_set_error.
            while (lastRead != 0)
            {
                error = lastRead;
                isAllocFailure = lastIsAllocFailure;

                lastRead = ErrGetErrorAlloc(out lastIsAllocFailure);
            }

            // If we're in an error flow which results in an Exception, but
            // no calls to ERR_set_error were made, throw the unadorned
            // CryptographicException.
            if (error == 0)
            {
                return new CryptographicException();
            }

            if (isAllocFailure)
            {
                return new OutOfMemoryException();
            }

            // Even though ErrGetError returns ulong (C++ unsigned long), we 
            // really only expect error codes in the UInt32 range
            Debug.Assert(error <= uint.MaxValue, "ErrGetError should only return error codes in the UInt32 range.");

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
