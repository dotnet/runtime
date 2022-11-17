// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrClearError")]
        internal static partial ulong ErrClearError();

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrGetExceptionError")]
        private static partial ulong ErrGetExceptionError([MarshalAs(UnmanagedType.Bool)] out bool isAllocFailure);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrPeekError")]
        internal static partial ulong ErrPeekError();

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrPeekLastError")]
        internal static partial ulong ErrPeekLastError();

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrReasonErrorString")]
        internal static partial IntPtr ErrReasonErrorString(ulong error);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrErrorStringN")]
        private static unsafe partial void ErrErrorStringN(ulong e, byte* buf, int len);

        private static unsafe string ErrErrorStringN(ulong error)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            string ret;

            fixed (byte* buf = &buffer[0])
            {
                ErrErrorStringN(error, buf, buffer.Length);
                ret = Marshal.PtrToStringUTF8((IntPtr)buf)!;
            }

            ArrayPool<byte>.Shared.Return(buffer);
            return ret;
        }

        internal static Exception CreateOpenSslCryptographicException()
        {
            // The Windows cryptography libraries reports error codes through
            // return values, or Marshal.GetLastWin32Error, either of which
            // has a single value when the function exits.
            //
            // OpenSSL maintains an error queue. Calls to ERR_get_error read
            // values out of the queue in the order that ERR_set_error wrote
            // them. Nothing enforces that a single call into an OpenSSL
            // function will guarantee at-most one error being set, and there
            // are well-known cases where multiple errors are emitted.
            //
            // In older versions of .NET, we collected the last error in the
            // queue, by repeatedly calling into ERR_get_error from managed code
            // and using the last error as the basis of the exception.
            // Now, we call into the shim once, which is responsible for
            // maintaining the error state and informing us of the one value to report.
            // (and when fetching that error we go ahead and clear out the rest).
            ulong error = ErrGetExceptionError(out bool isAllocFailure);

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
            // really only expect error codes in the UInt32 range, since that
            // type is only 32 bits on x86 Linux.
            Debug.Assert(error <= uint.MaxValue, "ErrGetError should only return error codes in the UInt32 range.");

            // If there was an error code, and it wasn't something handled specially,
            // use the OpenSSL error string as the message to a CryptographicException.
            return new OpenSslCryptographicException(unchecked((int)error), ErrErrorStringN(error));
        }

        internal static void CheckValidOpenSslHandle(SafeHandle handle)
        {
            if (handle == null || handle.IsInvalid)
            {
                Exception e = CreateOpenSslCryptographicException();
                handle?.Dispose();
                throw e;
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
