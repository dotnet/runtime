// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
#if DEBUG
    internal sealed class SafeFreeCertContext : DebugSafeHandle
    {
#else
    internal sealed class SafeFreeCertContext : SafeHandle
    {
#endif
        public SafeFreeCertContext(IntPtr handle) : base(handle, true)
        {
        }

        public override bool IsInvalid
        {
            get
            {
                return IntPtr.Zero == handle;
            }
        }

        protected override bool ReleaseHandle()
        {
            if (IntPtr.Zero != handle)
            {
                Interop.libcrypto.X509_free(handle);
            }
            return true;
        }
    }

    //
    // Implementation of handles dependable on FreeCredentialsHandle
    //
#if DEBUG
    internal sealed class SafeFreeCredentials : DebugSafeHandle
    {
#else
    internal sealed class SafeFreeCredentials : SafeHandle
    {
#endif
        private SafeX509Handle _certHandle;
        private SslProtocols _protocols = SslProtocols.None;
        private EncryptionPolicy _policy;

        internal SafeX509Handle CertHandle
        {
            get { return _certHandle; }
        }

        internal SslProtocols Protocols
        {
            get { return _protocols; }
        }

        public SafeFreeCredentials(X509Certificate certificate, SslProtocols protocols, EncryptionPolicy policy)
            : base(IntPtr.Zero, true)
        {
            if (null != certificate)
            {
                _certHandle = Interop.libcrypto.X509_dup(certificate.Handle);
            }          
            _protocols = protocols;
            _policy = policy;
        }

        public override bool IsInvalid
        {
            get { return SslProtocols.None == _protocols; }
        }

        protected override bool ReleaseHandle()
        {
            if ((null != _certHandle) && !_certHandle.IsInvalid)
            {
                _certHandle.Dispose();
            }
            _protocols = SslProtocols.None;
            return true;
        }
    }

    //
    // This is a class holding a Credential handle reference, used for static handles cache
    //
#if DEBUG
    internal sealed class SafeCredentialReference : DebugCriticalHandleMinusOneIsInvalid
    {
#else
    internal sealed class SafeCredentialReference : CriticalHandleMinusOneIsInvalid
    {
#endif

        //
        // Static cache will return the target handle if found the reference in the table.
        //
        internal SafeFreeCredentials _Target;

        internal static SafeCredentialReference CreateReference(SafeFreeCredentials target)
        {
            SafeCredentialReference result = new SafeCredentialReference(target);
            if (result.IsInvalid)
            {
                return null;
            }

            return result;
        }
        private SafeCredentialReference(SafeFreeCredentials target) : base()
        {
            // Bumps up the refcount on Target to signify that target handle is statically cached so
            // its dispose should be postponed
            bool ignore = false;
            target.DangerousAddRef(ref ignore);
            _Target = target;
            SetHandle(new IntPtr(0));   // make this handle valid
        }

        protected override bool ReleaseHandle()
        {
            SafeFreeCredentials target = _Target;
            if (target != null)
            {
                target.DangerousRelease();
            }

            _Target = null;
            return true;
        }
    }

#if DEBUG
    internal sealed class SafeDeleteContext : DebugSafeHandle
    {
#else
    internal sealed class SafeDeleteContext : SafeHandle
    {
#endif
        public SafeDeleteContext(IntPtr handle) : base(handle, true)
        {
        }

        public override bool IsInvalid
        {
            get
            {
                return IntPtr.Zero == handle;
            }
        }

        protected override bool ReleaseHandle()
        {
            if (IntPtr.Zero != handle)
            {
                Interop.OpenSsl.FreeSslContext(handle);
            }
            return true;
        }

        public override string ToString()
        {
            return IsInvalid ? String.Empty : handle.ToString();
        }
    }

    internal abstract class SafeFreeContextBufferChannelBinding : ChannelBinding
    {
        // TODO: To be implemented
    }
}
