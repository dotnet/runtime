// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    internal sealed class SafeFreeSslCredentials : SafeFreeCredentials
    {
        private SafeX509Handle? _certHandle;
        private SafeEvpPKeyHandle? _certKeyHandle;
        private SslProtocols _protocols = SslProtocols.None;
        private EncryptionPolicy _policy;
        private bool _isInvalid;
        private SslStreamCertificateContext? _context;

        internal SafeX509Handle? CertHandle
        {
            get { return _certHandle; }
        }

        internal SafeEvpPKeyHandle? CertKeyHandle
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

        public SafeFreeSslCredentials(SslStreamCertificateContext? context, SslProtocols protocols, EncryptionPolicy policy, bool isServer)
            : base(IntPtr.Zero, true)
        {

            Debug.Assert(
                context == null || context.Certificate is X509Certificate2,
                "Only X509Certificate2 certificates are supported at this time");

            X509Certificate2? cert = context?.Certificate;

            if (cert != null)
            {
                Debug.Assert(cert.HasPrivateKey, "cert.HasPrivateKey");

                using (RSAOpenSsl? rsa = (RSAOpenSsl?)cert.GetRSAPrivateKey())
                {
                    if (rsa != null)
                    {
                        _certKeyHandle = rsa.DuplicateKeyHandle();
                        Interop.Crypto.CheckValidOpenSslHandle(_certKeyHandle);
                    }
                }

                if (_certKeyHandle == null)
                {
                    using (ECDsaOpenSsl? ecdsa = (ECDsaOpenSsl?)cert.GetECDsaPrivateKey())
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

                _certHandle = Interop.Crypto.X509UpRef(cert.Handle);
                Interop.Crypto.CheckValidOpenSslHandle(_certHandle);
            }

            _protocols = protocols;
            _policy = policy;
            _context = context;
        }

        public override bool IsInvalid
        {
            get { return _isInvalid; }
        }

        protected override bool ReleaseHandle()
        {
            _certHandle?.Dispose();
            _certKeyHandle?.Dispose();

            _isInvalid = true;
            return true;
        }
    }
}
