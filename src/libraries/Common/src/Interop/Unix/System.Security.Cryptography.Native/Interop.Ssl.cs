// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
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
        internal static extern SafeSslContextHandle SslCtxCreate(IntPtr method);

        [DllImport(Libraries.CryptoNative)]
        internal static extern long SslCtxCtrl(SafeSslContextHandle ctx, int cmd, long larg, IntPtr parg);

        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeSslHandle SslCreate(SafeSslContextHandle ctx);

        [DllImport(Libraries.CryptoNative)]
        internal static extern libssl.SslErrorCode SslGetError(SafeSslHandle ssl, int ret);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void SslDestroy(IntPtr ssl);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void SslCtxDestroy(IntPtr ctx);

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

        // NOTE: this is just an (unsafe) overload to the BioWrite method from Interop.Bio.cs.
        [DllImport(Libraries.CryptoNative)]
        internal static unsafe extern int BioWrite(SafeBioHandle b, byte* data, int len);
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeSslContextHandle : SafeHandle
    {
        private SafeSslContextHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            Interop.Ssl.SslCtxDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }
    }

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

            Interop.libssl.SSL_set_bio(handle, readBio, writeBio);
            handle._readBio = readBio;
            handle._writeBio = writeBio;

            if (isServer)
            {
                Interop.libssl.SSL_set_accept_state(handle);
            }
            else
            {
                Interop.libssl.SSL_set_connect_state(handle);
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
