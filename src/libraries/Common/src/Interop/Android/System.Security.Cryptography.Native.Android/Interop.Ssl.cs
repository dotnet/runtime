// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SafeSslHandle = System.Net.SafeSslHandle;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        internal unsafe delegate int SSLReadCallback(byte* data, int offset, int length);
        internal unsafe delegate void SSLWriteCallback(byte* data, int offset, int length);

        [DllImport(Interop.Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamCreate")]
        internal static extern SafeSslHandle SSLStreamCreate(
            bool isServer,
            SSLReadCallback streamRead,
            SSLWriteCallback streamWrite,
            int appOutBufferSize,
            int appInBufferSize);

        [DllImport(Interop.Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamHandshake")]
        internal static extern SafeSslHandle SSLStreamHandshake(SafeSslHandle sslHandle);

        [DllImport(Interop.Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamCreateAndStartHandshake")]
        private static extern SafeSslHandle SSLStreamCreateAndStartHandshake(
            SSLReadCallback streamRead,
            SSLWriteCallback streamWrite,
            int tlsVersion,
            int appOutBufferSize,
            int appInBufferSize);

        [DllImport(Interop.Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamRead")]
        private static unsafe extern int SSLStreamRead(
            SafeSslHandle sslHandle,
            byte* buffer,
            int offset,
            int length);
        internal static unsafe bool SSLStreamRead(SafeSslHandle handle, byte* buffer, int count, out int read)
        {
            read = SSLStreamRead(handle, buffer, 0, count);
            return true;
        }

        [DllImport(Interop.Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamWrite")]
        private static unsafe extern void SSLStreamWrite(
            SafeSslHandle sslHandle,
            byte* buffer,
            int offset,
            int length);
        internal static unsafe bool SSLStreamWrite(SafeSslHandle handle, ReadOnlySpan<byte> buffer)
        {
            fixed (byte* bufferPtr = buffer)
            {
                SSLStreamWrite(handle, bufferPtr, 0, buffer.Length);
            }

            return true;
        }

        [DllImport(Interop.Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamRelease")]
        internal static extern void SSLStreamRelease(IntPtr ptr);

        internal sealed class SslException : Exception
        {
            internal SslException()
            {
            }

            internal SslException(int errorCode)
            {
                HResult = errorCode;
            }
        }
    }
}

namespace System.Net
{
    internal sealed class SafeSslHandle : SafeHandle
    {
        public SafeSslHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.AndroidCrypto.SSLStreamRelease(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
