// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

using SafeSslHandle = System.Net.SafeSslHandle;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        private const int INSUFFICIENT_BUFFER = -1;
        private const int SUCCESS = 1;

        internal unsafe delegate int SSLReadCallback(byte* data, int offset, int length);
        internal unsafe delegate void SSLWriteCallback(byte* data, int offset, int length);

        [DllImport(Interop.Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamCreate")]
        internal static extern SafeSslHandle SSLStreamCreate(
            [MarshalAs(UnmanagedType.U1)] bool isServer,
            SSLReadCallback streamRead,
            SSLWriteCallback streamWrite,
            int appOutBufferSize,
            int appInBufferSize);

        [DllImport(Interop.Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamConfigureParameters")]
        internal static extern int SSLStreamConfigureParameters(
            SafeSslHandle sslHandle,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string targetHost);

        [DllImport(Interop.Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamHandshake")]
        internal static extern int SSLStreamHandshake(SafeSslHandle sslHandle);

        [DllImport(Interop.Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamCreateAndStartHandshake")]
        private static extern SafeSslHandle SSLStreamCreateAndStartHandshake(
            SSLReadCallback streamRead,
            SSLWriteCallback streamWrite,
            int tlsVersion,
            int appOutBufferSize,
            int appInBufferSize);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamGetApplicationProtocol")]
        private static extern int SSLStreamGetApplicationProtocol(SafeSslHandle ssl, [Out] byte[]? buf, ref int len);
        internal static byte[]? SSLStreamGetApplicationProtocol(SafeSslHandle ssl)
        {
            int len = 0;
            int ret = SSLStreamGetApplicationProtocol(ssl, null, ref len);
            if (ret != INSUFFICIENT_BUFFER)
                return null;

            byte[] bytes = new byte[len];
            ret = SSLStreamGetApplicationProtocol(ssl, bytes, ref len);
            if (ret != SUCCESS)
                return null;

            return bytes;
        }

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

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamGetProtocol")]
        private static extern int SSLStreamGetProtocol(SafeSslHandle ssl, out IntPtr protocol);
        internal static string SSLStreamGetProtocol(SafeSslHandle ssl)
        {
            IntPtr protocolPtr;
            int ret = SSLStreamGetProtocol(ssl, out protocolPtr);
            if (ret != SUCCESS)
                throw new CryptographicException();

            if (protocolPtr == IntPtr.Zero)
                return string.Empty;

            string protocol = Marshal.PtrToStringUni(protocolPtr)!;
            Marshal.FreeHGlobal(protocolPtr);
            return protocol;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamGetCipherSuite")]
        private static extern int SSLStreamGetCipherSuite(SafeSslHandle ssl, out IntPtr cipherSuite);
        internal static string SSLStreamGetCipherSuite(SafeSslHandle ssl)
        {
            IntPtr cipherSuitePtr;
            int ret = SSLStreamGetCipherSuite(ssl, out cipherSuitePtr);
            if (ret != SUCCESS)
                throw new CryptographicException();

            if (cipherSuitePtr == IntPtr.Zero)
                return string.Empty;

            string cipherSuite = Marshal.PtrToStringUni(cipherSuitePtr)!;
            Marshal.FreeHGlobal(cipherSuitePtr);
            return cipherSuite;
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
