// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

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
            Interop.Crypto.SslCtxDestroy(handle);
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

            SafeSslHandle handle = Interop.Crypto.SslCreate(context);
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
            Interop.Crypto.SslDestroy(handle);
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