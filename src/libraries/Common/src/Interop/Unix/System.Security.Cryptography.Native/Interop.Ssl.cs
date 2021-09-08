// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        internal const int SSL_TLSEXT_ERR_OK = 0;
        internal const int OPENSSL_NPN_NEGOTIATED = 1;
        internal const int SSL_TLSEXT_ERR_ALERT_FATAL = 2;
        internal const int SSL_TLSEXT_ERR_NOACK = 3;

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EnsureLibSslInitialized")]
        internal static extern void EnsureLibSslInitialized();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslV2_3Method")]
        internal static extern IntPtr SslV2_3Method();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCreate")]
        internal static extern SafeSslHandle SslCreate(SafeSslContextHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetError")]
        internal static extern SslErrorCode SslGetError(SafeSslHandle ssl, int ret);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetError")]
        internal static extern SslErrorCode SslGetError(IntPtr ssl, int ret);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetQuietShutdown")]
        internal static extern void SslSetQuietShutdown(SafeSslHandle ssl, int mode);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslDestroy")]
        internal static extern void SslDestroy(IntPtr ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetConnectState")]
        internal static extern void SslSetConnectState(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetAcceptState")]
        internal static extern void SslSetAcceptState(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetAlpnProtos")]
        internal static extern int SslSetAlpnProtos(SafeSslHandle ssl, IntPtr protos, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetVersion")]
        internal static extern IntPtr SslGetVersion(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetTlsExtHostName")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SslSetTlsExtHostName(SafeSslHandle ssl, string host);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGet0AlpnSelected")]
        internal static extern void SslGetAlpnSelected(SafeSslHandle ssl, out IntPtr protocol, out int len);

        internal static byte[]? SslGetAlpnSelected(SafeSslHandle ssl)
        {
            IntPtr protocol;
            int len;
            SslGetAlpnSelected(ssl, out protocol, out len);

            if (len == 0)
                return null;

            byte[] result = new byte[len];
            Marshal.Copy(protocol, result, 0, len);
            return result;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslWrite", SetLastError = true)]
        internal static extern int SslWrite(SafeSslHandle ssl, ref byte buf, int num);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslRead", SetLastError = true)]
        internal static extern int SslRead(SafeSslHandle ssl, ref byte buf, int num);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslRenegotiate")]
        internal static extern int SslRenegotiate(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_IsSslRenegotiatePending")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsSslRenegotiatePending(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslShutdown")]
        internal static extern int SslShutdown(IntPtr ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslShutdown")]
        internal static extern int SslShutdown(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetBio")]
        internal static extern void SslSetBio(SafeSslHandle ssl, SafeBioHandle rbio, SafeBioHandle wbio);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslDoHandshake", SetLastError = true)]
        internal static extern int SslDoHandshake(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_IsSslStateOK")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsSslStateOK(SafeSslHandle ssl);

        // NOTE: this is just an (unsafe) overload to the BioWrite method from Interop.Bio.cs.
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioWrite")]
        internal static extern unsafe int BioWrite(SafeBioHandle b, byte* data, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioWrite")]
        internal static extern int BioWrite(SafeBioHandle b, ref byte data, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetPeerCertificate")]
        internal static extern SafeX509Handle SslGetPeerCertificate(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetPeerCertChain")]
        internal static extern SafeSharedX509StackHandle SslGetPeerCertChain(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetPeerFinished")]
        internal static extern int SslGetPeerFinished(SafeSslHandle ssl, IntPtr buf, int count);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetFinished")]
        internal static extern int SslGetFinished(SafeSslHandle ssl, IntPtr buf, int count);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSessionReused")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SslSessionReused(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetClientCAList")]
        private static extern SafeSharedX509NameStackHandle SslGetClientCAList_private(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetCurrentCipherId")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SslGetCurrentCipherId(SafeSslHandle ssl, out int cipherId);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetOpenSslCipherSuiteName")]
        private static extern IntPtr GetOpenSslCipherSuiteName(SafeSslHandle ssl, int cipherSuite, out int isTls12OrLower);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SetCiphers")]
        internal static extern unsafe bool SslSetCiphers(SafeSslHandle ssl, byte* cipherList, byte* cipherSuites);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetVerifyPeer")]
        internal static extern void SslSetVerifyPeer(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetData")]
        internal static extern IntPtr SslGetData(IntPtr ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetData")]
        internal static extern int SslSetData(SafeSslHandle ssl, IntPtr data);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetData")]
        internal static extern int SslSetData(IntPtr ssl, IntPtr data);

        internal static unsafe int SslSetAlpnProtos(SafeSslHandle ssl, List<SslApplicationProtocol> protocols)
        {
            byte[] buffer = ConvertAlpnProtocolListToByteArray(protocols);
            fixed (byte* b = buffer)
            {
                return SslSetAlpnProtos(ssl, (IntPtr)b, buffer.Length);
            }
        }

        internal static byte[] ConvertAlpnProtocolListToByteArray(List<SslApplicationProtocol> applicationProtocols)
        {
            int protocolSize = 0;
            foreach (SslApplicationProtocol protocol in applicationProtocols)
            {
                if (protocol.Protocol.Length == 0 || protocol.Protocol.Length > byte.MaxValue)
                {
                    throw new ArgumentException(SR.net_ssl_app_protocols_invalid, nameof(applicationProtocols));
                }

                protocolSize += protocol.Protocol.Length + 1;
            }

            byte[] buffer = new byte[protocolSize];
            var offset = 0;
            foreach (SslApplicationProtocol protocol in applicationProtocols)
            {
                buffer[offset++] = (byte)(protocol.Protocol.Length);
                protocol.Protocol.Span.CopyTo(buffer.AsSpan(offset));
                offset += protocol.Protocol.Length;
            }

            return buffer;
        }

        internal static string? GetOpenSslCipherSuiteName(SafeSslHandle ssl, TlsCipherSuite cipherSuite, out bool isTls12OrLower)
        {
            string? ret = Marshal.PtrToStringAnsi(GetOpenSslCipherSuiteName(ssl, (int)cipherSuite, out int isTls12OrLowerInt));
            isTls12OrLower = isTls12OrLowerInt != 0;
            return ret;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Tls13Supported")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Tls13SupportedImpl();
        internal static readonly bool Tls13Supported = Tls13SupportedImpl();

        internal static SafeSharedX509NameStackHandle SslGetClientCAList(SafeSslHandle ssl)
        {
            Crypto.CheckValidOpenSslHandle(ssl);

            SafeSharedX509NameStackHandle handle = SslGetClientCAList_private(ssl);

            if (!handle.IsInvalid)
            {
                handle.SetParent(ssl);
            }

            return handle;
        }

        internal static class SslMethods
        {
            internal static readonly IntPtr SSLv23_method = SslV2_3Method();
        }

        internal enum SslErrorCode
        {
            SSL_ERROR_NONE = 0,
            SSL_ERROR_SSL = 1,
            SSL_ERROR_WANT_READ = 2,
            SSL_ERROR_WANT_WRITE = 3,
            SSL_ERROR_SYSCALL = 5,
            SSL_ERROR_ZERO_RETURN = 6,

            // NOTE: this SslErrorCode value doesn't exist in OpenSSL, but
            // we use it to distinguish when a renegotiation is pending.
            // Choosing an arbitrarily large value that shouldn't conflict
            // with any actual OpenSSL error codes
            SSL_ERROR_RENEGOTIATE = 29304
        }
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeSslHandle : SafeHandle
    {
        private SafeBioHandle? _readBio;
        private SafeBioHandle? _writeBio;
        private bool _isServer;
        private bool _handshakeCompleted;

        public GCHandle AlpnHandle;

        public bool IsServer
        {
            get { return _isServer; }
        }

        public SafeBioHandle? InputBio
        {
            get
            {
                return _readBio;
            }
        }

        public SafeBioHandle? OutputBio
        {
            get
            {
                return _writeBio;
            }
        }

        internal void MarkHandshakeCompleted()
        {
            _handshakeCompleted = true;
        }

        public static SafeSslHandle Create(SafeSslContextHandle context, bool isServer)
        {
            SafeBioHandle readBio = Interop.Crypto.CreateMemoryBio();
            SafeBioHandle writeBio = Interop.Crypto.CreateMemoryBio();
            SafeSslHandle handle = Interop.Ssl.SslCreate(context);
            if (readBio.IsInvalid || writeBio.IsInvalid || handle.IsInvalid)
            {
                readBio.Dispose();
                writeBio.Dispose();
                handle.Dispose(); // will make IsInvalid==true if it's not already
                return handle;
            }
            handle._isServer = isServer;

            // SslSetBio will transfer ownership of the BIO handles to the SSL context
            try
            {
                readBio.TransferOwnershipToParent(handle);
                writeBio.TransferOwnershipToParent(handle);
                handle._readBio = readBio;
                handle._writeBio = writeBio;
                Interop.Ssl.SslSetBio(handle, readBio, writeBio);
            }
            catch (Exception exc)
            {
                // The only way this should be able to happen without thread aborts is if we hit OOMs while
                // manipulating the safe handles, in which case we may leak the bio handles.
                Debug.Fail("Unexpected exception while transferring SafeBioHandle ownership to SafeSslHandle", exc.ToString());
                throw;
            }

            if (isServer)
            {
                Interop.Ssl.SslSetAcceptState(handle);
            }
            else
            {
                Interop.Ssl.SslSetConnectState(handle);
            }
            return handle;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _readBio?.Dispose();
                _writeBio?.Dispose();
            }

            if (AlpnHandle.IsAllocated)
            {
                AlpnHandle.Free();
            }

            base.Dispose(disposing);
        }

        protected override bool ReleaseHandle()
        {
            if (_handshakeCompleted)
            {
                Disconnect();
            }

            IntPtr h = handle;
            SetHandle(IntPtr.Zero);
            Interop.Ssl.SslDestroy(h); // will free the handles underlying _readBio and _writeBio

            return true;
        }

        private void Disconnect()
        {
            Debug.Assert(!IsInvalid, "Expected a valid context in Disconnect");

            int retVal = Interop.Ssl.SslShutdown(handle);

            // Here, we are ignoring checking for <0 return values from Ssl_Shutdown,
            // since the underlying memory bio is already disposed, we are not
            // interested in reading or writing to it.
            if (retVal == 0)
            {
                // Do a bi-directional shutdown.
                retVal = Interop.Ssl.SslShutdown(handle);
            }

            if (retVal < 0)
            {
                // Clean up the errors
                Interop.Crypto.ErrClearError();
            }
        }

        public SafeSslHandle() : base(IntPtr.Zero, true)
        {
        }

        internal SafeSslHandle(IntPtr validSslPointer, bool ownsHandle) : base(IntPtr.Zero, ownsHandle)
        {
            handle = validSslPointer;
        }
    }
}
