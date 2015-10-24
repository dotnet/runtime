// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        internal delegate int SslCtxSetVerifyCallback(int preverify_ok, IntPtr x509_ctx);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void EnsureLibSslInitialized();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr SslV2_3Method();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr SslV3Method();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr TlsV1Method();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr TlsV1_1Method();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr TlsV1_2Method();

        [DllImport(Libraries.CryptoNative)]
        internal static extern void SetProtocolOptions(SafeSslContextHandle ctx, SslProtocols protocols);

        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeSslHandle SslCreate(SafeSslContextHandle ctx);

        [DllImport(Libraries.CryptoNative)]
        internal static extern SslErrorCode SslGetError(SafeSslHandle ssl, int ret);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void SslDestroy(IntPtr ssl);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void SslSetConnectState(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void SslSetAcceptState(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative)]
        private static extern IntPtr SslGetVersion(SafeSslHandle ssl);

        internal static string GetProtocolVersion(SafeSslHandle ssl)
        {
            return Marshal.PtrToStringAnsi(SslGetVersion(ssl));
        }

        [DllImport(Libraries.CryptoNative)]
        internal static extern bool GetSslConnectionInfo(
            SafeSslHandle ssl,
            out int dataCipherAlg,
            out int keyExchangeAlg,
            out int dataHashAlg,
            out int dataKeySize);

        [DllImport(Libraries.CryptoNative)]
        internal static unsafe extern int SslWrite(SafeSslHandle ssl, byte* buf, int num);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int SslRead(SafeSslHandle ssl, byte[] buf, int num);

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsSslRenegotiatePending(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int SslShutdown(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void SslSetBio(SafeSslHandle ssl, SafeBioHandle rbio, SafeBioHandle wbio);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int SslDoHandshake(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsSslStateOK(SafeSslHandle ssl);

        // NOTE: this is just an (unsafe) overload to the BioWrite method from Interop.Bio.cs.
        [DllImport(Libraries.CryptoNative)]
        internal static unsafe extern int BioWrite(SafeBioHandle b, byte* data, int len);

        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeX509Handle SslGetPeerCertificate(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeSharedX509StackHandle SslGetPeerCertChain(SafeSslHandle ssl);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int SslCtxUseCertificate(SafeSslContextHandle ctx, SafeX509Handle certPtr);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int SslCtxUsePrivateKey(SafeSslContextHandle ctx, SafeEvpPKeyHandle keyPtr);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int SslCtxCheckPrivateKey(SafeSslContextHandle ctx);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void SslCtxSetQuietShutdown(SafeSslContextHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "SslGetClientCAList")]
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

        [DllImport(Libraries.CryptoNative)]
        internal static extern void SslCtxSetVerify(SafeSslContextHandle ctx, SslCtxSetVerifyCallback callback);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void SetEncryptionPolicy(SafeSslContextHandle ctx, EncryptionPolicy policy);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void SslCtxSetClientCAList(SafeSslContextHandle ctx, SafeX509NameStackHandle x509NameStackPtr);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void GetStreamSizes(out int header, out int trailer, out int maximumMessage);

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

        private SafeSslHandle() : base(IntPtr.Zero, true)
        {
        }
    }
}
