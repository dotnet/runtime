// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

using SafeSslHandle = System.Net.SafeSslHandle;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        internal unsafe delegate PAL_SSLStreamStatus SSLReadCallback(byte* data, int* length);
        internal unsafe delegate void SSLWriteCallback(byte* data, int length);

        internal enum PAL_SSLStreamStatus
        {
            OK = 0,
            NeedData = 1,
            Error = 2
        };

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
        internal static extern PAL_SSLStreamStatus SSLStreamHandshake(SafeSslHandle sslHandle);

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
        internal static unsafe extern PAL_SSLStreamStatus SSLStreamRead(
            SafeSslHandle sslHandle,
            byte* buffer,
            int length,
            out int bytesRead);

        [DllImport(Interop.Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamWrite")]
        internal static unsafe extern PAL_SSLStreamStatus SSLStreamWrite(
            SafeSslHandle sslHandle,
            byte* buffer,
            int length);

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

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamGetPeerCertificate")]
        private static extern int SSLStreamGetPeerCertificate(SafeSslHandle ssl, out SafeX509Handle cert);
        internal static SafeX509Handle SSLStreamGetPeerCertificate(SafeSslHandle ssl)
        {
            SafeX509Handle cert;
            int ret = Interop.AndroidCrypto.SSLStreamGetPeerCertificate(ssl, out cert);
            if (ret != SUCCESS)
                throw new SslException();

            return cert;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamGetPeerCertificates")]
        private static extern int SSLStreamGetPeerCertificates(
            SafeSslHandle ssl,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] out IntPtr[] certs,
            out int count);
        internal static IntPtr[] SSLStreamGetPeerCertificates(SafeSslHandle ssl)
        {
            IntPtr[] ptrs;
            int count;
            int ret = Interop.AndroidCrypto.SSLStreamGetPeerCertificates(ssl, out ptrs, out count);
            if (ret != SUCCESS)
                throw new SslException();

            return ptrs;
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

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamVerifyHostname")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool SSLStreamVerifyHostname(
            SafeSslHandle ssl,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string hostname);
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
