// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [ThreadStatic]
        private static byte[]? t_msgBuf;

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrClearError")]
        internal static partial ulong ErrClearError();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrGetErrorAlloc")]
        private static partial ulong ErrGetErrorAlloc([MarshalAs(UnmanagedType.Bool)] out bool isAllocFailure);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrPeekError")]
        internal static partial ulong ErrPeekError();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrPeekLastError")]
        internal static partial ulong ErrPeekLastError();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrReasonErrorString")]
        internal static partial IntPtr ErrReasonErrorString(ulong error);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ErrErrorStringN")]
        private static unsafe partial void ErrErrorStringN(ulong e, byte* buf, int len);

        private static unsafe string ErrErrorStringN(ulong error)
        {
            if (t_msgBuf is null)
            {
                t_msgBuf = new byte[1024];
            }

            byte[] buffer = t_msgBuf;

            fixed (byte* buf = &buffer[0])
            {
                ErrErrorStringN(error, buf, buffer.Length);
                return Marshal.PtrToStringAnsi((IntPtr)buf)!;
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
            // In older versions of .NET, we collected the last error in the
            // queue, and did not habitually clear the error pipeline.
            // Some of the flows in OpenSSL 3 showed that what we often want
            // is the first error code that was set during the operation that
            // failed (and that the preferred API often reports multiple/cascaded
            // errors). So now we clear habitually, and we take the first error
            // (and when fetching that error we go ahead and clear out the rest).
            ulong error = ErrGetErrorAlloc(out bool isAllocFailure);

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
