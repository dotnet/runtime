// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        internal delegate int SslCtxSetVerifyCallback(int preverify_ok, IntPtr x509_ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EnsureLibSslInitialized")]
        internal static extern void EnsureLibSslInitialized();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslV2_3Method")]
        internal static extern IntPtr SslV2_3Method();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslV3Method")]
        internal static extern IntPtr SslV3Method();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_TlsV1Method")]
        internal static extern IntPtr TlsV1Method();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_TlsV1_1Method")]
        internal static extern IntPtr TlsV1_1Method();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_TlsV1_2Method")]
        internal static extern IntPtr TlsV1_2Method();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCreate")]
        internal static extern SafeSslHandle SslCreate(SafeSslContextHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetError")]
        internal static extern SslErrorCode SslGetError(SafeSslHandle ssl, int ret);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetError")]
        internal static extern SslErrorCode SslGetError(IntPtr ssl, int ret);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslDestroy")]
        internal static extern void SslDestroy(IntPtr ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetConnectState")]
        internal static extern void SslSetConnectState(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetAcceptState")]
        internal static extern void SslSetAcceptState(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetVersion")]
        private static extern IntPtr SslGetVersion(SafeSslHandle ssl);

        internal static string GetProtocolVersion(SafeSslHandle ssl)
        {
            return Marshal.PtrToStringAnsi(SslGetVersion(ssl));
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetSslConnectionInfo")]
        internal static extern bool GetSslConnectionInfo(
            SafeSslHandle ssl,
            out int dataCipherAlg,
            out int keyExchangeAlg,
            out int dataHashAlg,
            out int dataKeySize,
            out int hashKeySize);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslWrite")]
        internal static unsafe extern int SslWrite(SafeSslHandle ssl, byte* buf, int num);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslRead")]
        internal static extern int SslRead(SafeSslHandle ssl, byte[] buf, int num);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_IsSslRenegotiatePending")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsSslRenegotiatePending(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslShutdown")]
        internal static extern int SslShutdown(IntPtr ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSetBio")]
        internal static extern void SslSetBio(SafeSslHandle ssl, SafeBioHandle rbio, SafeBioHandle wbio);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslDoHandshake")]
        internal static extern int SslDoHandshake(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_IsSslStateOK")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsSslStateOK(SafeSslHandle ssl);

        // NOTE: this is just an (unsafe) overload to the BioWrite method from Interop.Bio.cs.
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_BioWrite")]
        internal static unsafe extern int BioWrite(SafeBioHandle b, byte* data, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetPeerCertificate")]
        internal static extern SafeX509Handle SslGetPeerCertificate(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetPeerCertChain")]
        internal static extern SafeSharedX509StackHandle SslGetPeerCertChain(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetStreamSizes")]
        internal static extern void GetStreamSizes(out int header, out int trailer, out int maximumMessage);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetPeerFinished")]
        internal static extern int SslGetPeerFinished(SafeSslHandle ssl, IntPtr buf, int count);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetFinished")]
        internal static extern int SslGetFinished(SafeSslHandle ssl, IntPtr buf, int count);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslSessionReused")]
        internal static extern bool SslSessionReused(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslAddExtraChainCert")]
        internal static extern bool SslAddExtraChainCert(SafeSslHandle ssl, SafeX509Handle x509);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslGetClientCAList")]
        private static extern SafeSharedX509NameStackHandle SslGetClientCAList_private(SafeSslHandle ssl);

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
            internal static readonly IntPtr TLSv1_method = TlsV1Method();
            internal static readonly IntPtr TLSv1_1_method = TlsV1_1Method();
            internal static readonly IntPtr TLSv1_2_method = TlsV1_2Method();
            internal static readonly IntPtr SSLv3_method = SslV3Method();
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
        private SafeBioHandle _readBio;
        private SafeBioHandle _writeBio;
        private bool _isServer;
        private bool _handshakeCompleted = false;

        public bool IsServer
        {
            get { return _isServer; }
        }

        public SafeBioHandle InputBio
        {
            get
            {
                return _readBio;
            }
        }

        public SafeBioHandle OutputBio
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
            if (readBio.IsInvalid)
            {
                return new SafeSslHandle();
            }

            SafeBioHandle writeBio = Interop.Crypto.CreateMemoryBio();
            if (writeBio.IsInvalid)
            {
                readBio.Dispose();
                return new SafeSslHandle();
            }

            SafeSslHandle handle = Interop.Ssl.SslCreate(context);
            if (handle.IsInvalid)
            {
                readBio.Dispose();
                writeBio.Dispose();
                return handle;
            }
            handle._isServer = isServer;

            // After SSL_set_bio, the BIO handles are owned by SSL pointer
            // and are automatically freed by SSL_free. To prevent a double
            // free, we need to keep the ref counts bumped up till SSL_free
            bool gotRef = false;
            readBio.DangerousAddRef(ref gotRef);
            try
            {
                bool ignore = false;
                writeBio.DangerousAddRef(ref ignore);
            }
            catch
            {
                if (gotRef)
                {
                    readBio.DangerousRelease();
                }
                throw;
            }

            Interop.Ssl.SslSetBio(handle, readBio, writeBio);
            handle._readBio = readBio;
            handle._writeBio = writeBio;

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

        protected override bool ReleaseHandle()
        {
            if (_handshakeCompleted)
            {
                Disconnect();
            }

            Interop.Ssl.SslDestroy(handle);
            if (_readBio != null)
            {
                _readBio.SetHandleAsInvalid(); // BIO got freed in SslDestroy
            }
            if (_writeBio != null)
            {
                _writeBio.SetHandleAsInvalid(); // BIO got freed in SslDestroy
            }
            return true;
        }

        private void Disconnect()
        {
            Debug.Assert(!IsInvalid, "Expected a valid context in Disconnect");
            int retVal = Interop.Ssl.SslShutdown(handle);
            if (retVal < 0)
            {
                //TODO (Issue #4031) check this error
                Interop.Ssl.SslGetError(handle, retVal);
            }
        }

        private SafeSslHandle() : base(IntPtr.Zero, true)
        {
        }

        internal SafeSslHandle(IntPtr validSslPointer, bool ownsHandle) : base(IntPtr.Zero, ownsHandle)
        {
            handle = validSslPointer;
        }
    }

    internal sealed class SafeChannelBindingHandle : SafeHandle
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SecChannelBindings
        {
            internal int InitiatorLength;
            internal int InitiatorOffset;
            internal int AcceptorAddrType;
            internal int AcceptorLength;
            internal int AcceptorOffset;
            internal int ApplicationDataLength;
            internal int ApplicationDataOffset;
        }

        private static readonly byte[] s_tlsServerEndPointByteArray = Encoding.UTF8.GetBytes("tls-server-end-point:");
        private static readonly byte[] s_tlsUniqueByteArray = Encoding.UTF8.GetBytes("tls-unique:");
        private static readonly int s_secChannelBindingSize = Marshal.SizeOf<SecChannelBindings>();
        private readonly int _cbtPrefixByteArraySize;
        private const int CertHashMaxSize = 128;

        internal int Length
        {
            get;
            private set;
        }

        internal IntPtr CertHashPtr
        {
            get;
            private set;
        }

        internal void SetCertHash(byte[] certHashBytes)
        {
            Debug.Assert(certHashBytes != null, "check certHashBytes is not null");
            int length = certHashBytes.Length;
            Marshal.Copy(certHashBytes, 0, CertHashPtr, length);
            SetCertHashLength(length);
        }

        private byte[] GetPrefixBytes(ChannelBindingKind kind)
        {
            if (kind == ChannelBindingKind.Endpoint)
            {
                return s_tlsServerEndPointByteArray;
            }
            else if (kind == ChannelBindingKind.Unique)
            {
                return s_tlsUniqueByteArray;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        internal SafeChannelBindingHandle(ChannelBindingKind kind)
            : base(IntPtr.Zero, true)
        {
            byte[] cbtPrefix = GetPrefixBytes(kind);
            _cbtPrefixByteArraySize = cbtPrefix.Length;
            handle = Marshal.AllocHGlobal(s_secChannelBindingSize + _cbtPrefixByteArraySize + CertHashMaxSize);
            IntPtr cbtPrefixPtr = handle + s_secChannelBindingSize;
            Marshal.Copy(cbtPrefix, 0, cbtPrefixPtr, _cbtPrefixByteArraySize);
            CertHashPtr = cbtPrefixPtr + _cbtPrefixByteArraySize;
            Length = CertHashMaxSize;
        }

        internal void SetCertHashLength(int certHashLength)
        {
            int cbtLength = _cbtPrefixByteArraySize + certHashLength;
            Length = s_secChannelBindingSize + cbtLength;

            SecChannelBindings channelBindings = new SecChannelBindings()
            {
                ApplicationDataLength = cbtLength,
                ApplicationDataOffset = s_secChannelBindingSize
            };
            Marshal.StructureToPtr(channelBindings, handle, true);
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }
    }
}
