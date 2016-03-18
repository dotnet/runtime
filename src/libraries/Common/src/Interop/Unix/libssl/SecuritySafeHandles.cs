// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
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
        private readonly SafeX509Handle _certificate;

        public SafeFreeCertContext(SafeX509Handle certificate) : base(IntPtr.Zero, true)
        {
            // In certain scenarios (eg. server querying for a client cert), the
            // input certificate may be invalid and this is OK
            if ((null != certificate) && !certificate.IsInvalid)
            {
                bool gotRef = false;
                certificate.DangerousAddRef(ref gotRef);
                Debug.Assert(gotRef, "Unexpected failure in AddRef of certificate");
                _certificate = certificate;
                handle = _certificate.DangerousGetHandle();
            }
        }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }

        protected override bool ReleaseHandle()
        {
            _certificate.DangerousRelease();
            _certificate.Dispose();
            return true;
        }
    }

    //
    // Implementation of handles dependable on FreeCredentialsHandle
    //
#if DEBUG
    internal abstract class SafeFreeCredentials : DebugSafeHandle
    {
#else
    internal abstract class SafeFreeCredentials : SafeHandle
    {
#endif
        protected SafeFreeCredentials(IntPtr handle, bool ownsHandle) : base(handle, ownsHandle)
        {
        }
    }

    internal sealed class SafeFreeSslCredentials : SafeFreeCredentials
    {
        private SafeX509Handle _certHandle;
        private SafeEvpPKeyHandle _certKeyHandle;
        private SslProtocols _protocols = SslProtocols.None;
        private EncryptionPolicy _policy;

        internal SafeX509Handle CertHandle
        {
            get { return _certHandle; }
        }

        internal SafeEvpPKeyHandle CertKeyHandle
        {
            get { return _certKeyHandle; }
        }

        internal SslProtocols Protocols
        {
            get { return _protocols; }
        }

        internal EncryptionPolicy Policy
        {
            get { return _policy; }
        }

        public SafeFreeSslCredentials(X509Certificate certificate, SslProtocols protocols, EncryptionPolicy policy)
            : base(IntPtr.Zero, true)
        {
            Debug.Assert(
                certificate == null || certificate is X509Certificate2,
                "Only X509Certificate2 certificates are supported at this time");

            X509Certificate2 cert = (X509Certificate2)certificate;

            if (cert != null)
            {
                Debug.Assert(cert.HasPrivateKey, "cert.HasPrivateKey");

                using (RSAOpenSsl rsa = (RSAOpenSsl)cert.GetRSAPrivateKey())
                {
                    if (rsa != null)
                    {
                        _certKeyHandle = rsa.DuplicateKeyHandle();
                        Interop.Crypto.CheckValidOpenSslHandle(_certKeyHandle);
                    }
                }

                if (_certKeyHandle == null)
                {
                    using (ECDsaOpenSsl ecdsa = (ECDsaOpenSsl)cert.GetECDsaPrivateKey())
                    {
                        if (ecdsa != null)
                        {
                            _certKeyHandle = ecdsa.DuplicateKeyHandle();
                            Interop.Crypto.CheckValidOpenSslHandle(_certKeyHandle);
                        }
                    }
                }

                if (_certKeyHandle == null)
                {
                    throw new NotSupportedException(SR.net_ssl_io_no_server_cert);
                }

                _certHandle = Interop.Crypto.X509Duplicate(cert.Handle);
                Interop.Crypto.CheckValidOpenSslHandle(_certHandle);
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
            if (_certHandle != null)
            {
                _certHandle.Dispose();
            }

            if (_certKeyHandle != null)
            {
                _certKeyHandle.Dispose();
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
        internal SafeFreeCredentials Target;

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
            Target = target;
            SetHandle(new IntPtr(0));   // make this handle valid
        }

        protected override bool ReleaseHandle()
        {
            SafeFreeCredentials target = Target;
            if (target != null)
            {
                target.DangerousRelease();
            }

            Target = null;
            return true;
        }
    }

#if DEBUG
    internal abstract class SafeDeleteContext : DebugSafeHandle
    {
#else
    internal abstract class SafeDeleteContext : SafeHandle
    {
#endif
        private SafeFreeCredentials _credential;

        protected SafeDeleteContext(SafeFreeCredentials credential)
            : base(IntPtr.Zero, true)
        {
            Debug.Assert((null != credential), "Invalid credential passed to SafeDeleteContext");

            // When a credential handle is first associated with the context we keep credential
            // ref count bumped up to ensure ordered finalization. The credential properties
            // are used in the SSL/NEGO data structures and should survive the lifetime of
            // the SSL/NEGO context
            bool ignore = false;
            _credential = credential;
            _credential.DangerousAddRef(ref ignore);
        }

        public override bool IsInvalid
        {
            get { return (null == _credential); }
        }

        protected override bool ReleaseHandle()
        {
            Debug.Assert((null != _credential), "Null credential in SafeDeleteContext");
            _credential.DangerousRelease();
            _credential = null;
            return true;
        }
    }

    internal sealed class SafeDeleteSslContext : SafeDeleteContext
    {
        private SafeSslHandle _sslContext;

        public SafeSslHandle SslContext
        {
            get
            {
                return _sslContext;
            }
        }

        public SafeDeleteSslContext(SafeFreeSslCredentials credential, bool isServer, bool remoteCertRequired)
            : base(credential)
        {
            Debug.Assert((null != credential) && !credential.IsInvalid, "Invalid credential used in SafeDeleteSslContext");

            try
            {
                _sslContext = Interop.OpenSsl.AllocateSslContext(
                    credential.Protocols,
                    credential.CertHandle,
                    credential.CertKeyHandle,
                    credential.Policy,
                    isServer,
                    remoteCertRequired);
            }
            catch(Exception ex)
            {
                Debug.Write("Exception Caught. - " + ex);
                Dispose();
                throw;
            }
        }

        public override bool IsInvalid
        {
            get
            {
                return (null == _sslContext) || _sslContext.IsInvalid;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _sslContext)
                {
                    _sslContext.Dispose();
                    _sslContext = null;
                }
            }

            base.Dispose(disposing);
        }
    }

    internal sealed class SafeFreeContextBufferChannelBinding : ChannelBinding
    {
        private readonly SafeChannelBindingHandle _channelBinding = null;

        public override int Size
        {
            get { return _channelBinding.Length; }
        }

        public override bool IsInvalid
        {
            get { return _channelBinding.IsInvalid; }
        }

        public SafeFreeContextBufferChannelBinding(SafeChannelBindingHandle binding)
        {
            Debug.Assert(null != binding && !binding.IsInvalid, "input channelBinding is invalid");
            bool gotRef = false;
            binding.DangerousAddRef(ref gotRef);
            handle = binding.DangerousGetHandle();
            _channelBinding = binding;
        }

        protected override bool ReleaseHandle()
        {
            _channelBinding.DangerousRelease();
            _channelBinding.Dispose();
            return true;
        }
    }
}
