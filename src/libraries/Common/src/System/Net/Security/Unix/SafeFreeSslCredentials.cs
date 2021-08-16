// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#pragma warning disable CA1419 // TODO https://github.com/dotnet/roslyn-analyzers/issues/5232: not intended for use with P/Invoke

namespace System.Net.Security
{
    internal sealed class SafeFreeSslCredentials : SafeFreeCredentials
    {
        private SafeX509Handle? _certHandle;
        private SafeEvpPKeyHandle? _certKeyHandle;
        private SslProtocols _protocols = SslProtocols.None;
        private EncryptionPolicy _policy;
        private bool _isInvalid;

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

        public SafeFreeSslCredentials(X509Certificate? certificate, SslProtocols protocols, EncryptionPolicy policy)
            : base(IntPtr.Zero, true)
        {
            Debug.Assert(
                certificate == null || certificate is X509Certificate2,
                "Only X509Certificate2 certificates are supported at this time");

            X509Certificate2? cert = (X509Certificate2?)certificate;

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
        }

        public override bool IsInvalid
        {
            get { return _isInvalid; }
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

            _isInvalid = true;
            return true;
        }
    }
}
