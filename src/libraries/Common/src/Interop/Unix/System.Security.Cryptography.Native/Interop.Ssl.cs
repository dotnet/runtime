// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslV2_3Method")]
        internal static partial IntPtr SslV2_3Method();

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCreate")]
        internal static partial SafeSslHandle SslCreate(SafeSslContextHandle ctx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetError")]
        internal static partial SslErrorCode SslGetError(SafeSslHandle ssl, int ret);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetError")]
        internal static partial SslErrorCode SslGetError(IntPtr ssl, int ret);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetQuietShutdown")]
        internal static partial void SslSetQuietShutdown(SafeSslHandle ssl, int mode);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslDestroy")]
        internal static partial void SslDestroy(IntPtr ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetConnectState")]
        internal static partial void SslSetConnectState(SafeSslHandle ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetAcceptState")]
        internal static partial void SslSetAcceptState(SafeSslHandle ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetAlpnProtos")]
        internal static unsafe partial int SslSetAlpnProtos(SafeSslHandle ssl, byte* protos, int len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetVersion")]
        internal static partial IntPtr SslGetVersion(SafeSslHandle ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetTlsExtHostName", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SslSetTlsExtHostName(SafeSslHandle ssl, string host);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetServerName")]
        internal static unsafe partial IntPtr SslGetServerName(IntPtr ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetSession")]
        internal static unsafe partial int SslSetSession(SafeSslHandle ssl, IntPtr session);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGet0AlpnSelected")]
        internal static partial void SslGetAlpnSelected(SafeSslHandle ssl, out IntPtr protocol, out int len);

        internal static unsafe ReadOnlySpan<byte> SslGetAlpnSelected(SafeSslHandle ssl)
        {
            IntPtr protocol;
            int len;
            SslGetAlpnSelected(ssl, out protocol, out len);

            if (len == 0)
                return ReadOnlySpan<byte>.Empty;

            return new ReadOnlySpan<byte>((void*)protocol, len);
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslWrite", SetLastError = true)]
        internal static partial int SslWrite(SafeSslHandle ssl, ref byte buf, int num, out SslErrorCode error);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslRead", SetLastError = true)]
        internal static partial int SslRead(SafeSslHandle ssl, ref byte buf, int num, out SslErrorCode error);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslRenegotiate")]
        internal static partial int SslRenegotiate(SafeSslHandle ssl, out SslErrorCode error);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_IsSslRenegotiatePending")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsSslRenegotiatePending(SafeSslHandle ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslShutdown")]
        internal static partial int SslShutdown(IntPtr ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslShutdown")]
        internal static partial int SslShutdown(SafeSslHandle ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetBio")]
        internal static partial void SslSetBio(SafeSslHandle ssl, SafeBioHandle rbio, SafeBioHandle wbio);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslDoHandshake", SetLastError = true)]
        internal static partial int SslDoHandshake(SafeSslHandle ssl, out SslErrorCode error);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_IsSslStateOK")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsSslStateOK(SafeSslHandle ssl);

        // NOTE: this is just an (unsafe) overload to the BioWrite method from Interop.Bio.cs.
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioWrite")]
        internal static unsafe partial int BioWrite(SafeBioHandle b, byte* data, int len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioWrite")]
        internal static partial int BioWrite(SafeBioHandle b, ref byte data, int len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetPeerCertificate")]
        internal static partial IntPtr SslGetPeerCertificate(SafeSslHandle ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetPeerCertChain")]
        internal static partial SafeSharedX509StackHandle SslGetPeerCertChain(SafeSslHandle ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetPeerFinished")]
        internal static partial int SslGetPeerFinished(SafeSslHandle ssl, IntPtr buf, int count);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetFinished")]
        internal static partial int SslGetFinished(SafeSslHandle ssl, IntPtr buf, int count);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSessionReused")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SslSessionReused(SafeSslHandle ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetClientCAList")]
        private static partial SafeSharedX509NameStackHandle SslGetClientCAList_private(SafeSslHandle ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetCurrentCipherId")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SslGetCurrentCipherId(SafeSslHandle ssl, out int cipherId);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetOpenSslCipherSuiteName")]
        private static partial IntPtr GetOpenSslCipherSuiteName(SafeSslHandle ssl, int cipherSuite, out int isTls12OrLower);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SetCiphers")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool SslSetCiphers(SafeSslHandle ssl, byte* cipherList, byte* cipherSuites);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetVerifyPeer")]
        internal static partial void SslSetVerifyPeer(SafeSslHandle ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetData")]
        internal static partial IntPtr SslGetData(IntPtr ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetData")]
        internal static partial IntPtr SslGetData(SafeSslHandle ssl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetData")]
        internal static partial int SslSetData(SafeSslHandle ssl, IntPtr data);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetData")]
        internal static partial int SslSetData(IntPtr ssl, IntPtr data);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslUseCertificate")]
        internal static partial int SslUseCertificate(SafeSslHandle ssl, SafeX509Handle certPtr);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslUsePrivateKey")]
        internal static partial int SslUsePrivateKey(SafeSslHandle ssl, SafeEvpPKeyHandle keyPtr);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetClientCertCallback")]
        internal static unsafe partial void SslSetClientCertCallback(SafeSslHandle ssl, int set);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetPostHandshakeAuth")]
        internal static partial void SslSetPostHandshakeAuth(SafeSslHandle ssl, int value);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Tls13Supported")]
        private static partial int Tls13SupportedImpl();

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSessionGetHostname")]
        internal static partial IntPtr SessionGetHostname(IntPtr session);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSessionFree")]
        internal static partial void SessionFree(IntPtr session);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSessionSetHostname")]
        internal static partial int SessionSetHostname(IntPtr session, IntPtr name);

        internal static class Capabilities
        {
            // needs separate type (separate static cctor) to be sure OpenSSL is initialized.
            internal static readonly bool Tls13Supported = Tls13SupportedImpl() != 0;
        }

        internal static int GetAlpnProtocolListSerializedLength(List<SslApplicationProtocol> applicationProtocols)
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

            return protocolSize;
        }

        internal static void SerializeAlpnProtocolList(List<SslApplicationProtocol> applicationProtocols, Span<byte> buffer)
        {
            Debug.Assert(GetAlpnProtocolListSerializedLength(applicationProtocols) == buffer.Length,
                "GetAlpnProtocolListSerializedSize(applicationProtocols) == buffer.Length");

            int offset = 0;
            foreach (SslApplicationProtocol protocol in applicationProtocols)
            {
                buffer[offset++] = (byte)protocol.Protocol.Length;
                protocol.Protocol.Span.CopyTo(buffer.Slice(offset));
                offset += protocol.Protocol.Length;
            }
        }

        internal static unsafe int SslSetAlpnProtos(SafeSslHandle ssl, List<SslApplicationProtocol> applicationProtocols)
        {
            int length = GetAlpnProtocolListSerializedLength(applicationProtocols);
            Span<byte> buffer = length <= 256 ? stackalloc byte[256].Slice(0, length) : new byte[length];
            SerializeAlpnProtocolList(applicationProtocols, buffer);
            return SslSetAlpnProtos(ssl, buffer);
        }

        internal static unsafe int SslSetAlpnProtos(SafeSslHandle ssl, Span<byte> serializedProtocols)
        {
            fixed (byte* pBuffer = &MemoryMarshal.GetReference(serializedProtocols))
            {
                return SslSetAlpnProtos(ssl, pBuffer, serializedProtocols.Length);
            }
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslAddExtraChainCert")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SslAddExtraChainCert(SafeSslHandle ssl, SafeX509Handle x509);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslAddClientCAs")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool SslAddClientCAs(SafeSslHandle ssl, IntPtr* x509s, int count);

        internal static unsafe bool SslAddClientCAs(SafeSslHandle ssl, Span<IntPtr> x509handles)
        {
            fixed (IntPtr* pHandles = &MemoryMarshal.GetReference(x509handles))
            {
                return SslAddClientCAs(ssl, pHandles, x509handles.Length);
            }
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static unsafe partial void CryptoNative_SslStapleOcsp(SafeSslHandle ssl, byte* buf, int len);

        internal static unsafe void SslStapleOcsp(SafeSslHandle ssl, ReadOnlySpan<byte> stapledResponse)
        {
            Debug.Assert(stapledResponse.Length > 0);

            fixed (byte* ptr = stapledResponse)
            {
                CryptoNative_SslStapleOcsp(ssl, ptr, stapledResponse.Length);
            }
        }

        internal static bool AddExtraChainCertificates(SafeSslHandle ssl, ReadOnlyCollection<X509Certificate2> chain)
        {
            // send pre-computed list of intermediates.
            for (int i = 0; i < chain.Count; i++)
            {
                SafeX509Handle dupCertHandle = Crypto.X509UpRef(chain[i].Handle);
                Crypto.CheckValidOpenSslHandle(dupCertHandle);
                if (!SslAddExtraChainCert(ssl, dupCertHandle))
                {
                    Crypto.ErrClearError();
                    dupCertHandle.Dispose(); // we still own the safe handle; clean it up
                    return false;
                }
                dupCertHandle.SetHandleAsInvalid(); // ownership has been transferred to sslHandle; do not free via this safe handle
            }

            return true;
        }

        internal static string? GetOpenSslCipherSuiteName(SafeSslHandle ssl, TlsCipherSuite cipherSuite, out bool isTls12OrLower)
        {
            string? ret = Marshal.PtrToStringUTF8(GetOpenSslCipherSuiteName(ssl, (int)cipherSuite, out int isTls12OrLowerInt));
            isTls12OrLower = isTls12OrLowerInt != 0;
            return ret;
        }

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
            SSL_ERROR_WANT_X509_LOOKUP = 4,
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
    internal sealed class SafeSslHandle : SafeDeleteSslContext
    {
        private SafeBioHandle? _readBio;
        private SafeBioHandle? _writeBio;
        private bool _isServer;
        private bool _handshakeCompleted;

        public GCHandle AlpnHandle;
        public SafeSslContextHandle? SslContextHandle;

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
                Interop.Ssl.SslSetData(handle, IntPtr.Zero);
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

            SslContextHandle?.DangerousRelease();

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
