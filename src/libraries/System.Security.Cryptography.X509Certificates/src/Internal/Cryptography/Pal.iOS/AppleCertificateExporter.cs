// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed class AppleCertificateExporter : UnixExportProvider
    {
        private AsymmetricAlgorithm? _privateKey;

        public AppleCertificateExporter(ICertificatePalCore cert)
            : base(cert)
        {
        }

        public AppleCertificateExporter(ICertificatePalCore cert, AsymmetricAlgorithm privateKey)
            : base(cert)
        {
            _privateKey = privateKey;
        }

        public AppleCertificateExporter(X509Certificate2Collection certs)
            : base(certs)
        {
        }

        protected override byte[] ExportPkcs7()
        {
            throw new CryptographicException(
                SR.Cryptography_X509_PKCS7_Unsupported,
                new PlatformNotSupportedException(SR.Cryptography_X509_PKCS7_Unsupported));
        }

        protected override byte[] ExportPkcs8(ICertificatePalCore certificatePal, ReadOnlySpan<char> password)
        {
            if (_privateKey != null)
            {
                return _privateKey.ExportEncryptedPkcs8PrivateKey(password, s_windowsPbe);
            }

            Debug.Assert(certificatePal.HasPrivateKey);
            ICertificatePal pal = (ICertificatePal)certificatePal;
            AsymmetricAlgorithm algorithm;

            switch (pal.KeyAlgorithm)
            {
                case Oids.Rsa:
                    algorithm = pal.GetRSAPrivateKey()!;
                    break;
                case Oids.EcPublicKey:
                    algorithm = pal.GetECDsaPrivateKey()!;
                    break;
                case Oids.Dsa:
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownKeyAlgorithm, pal.KeyAlgorithm));
            };

            using (algorithm)
            {
                return algorithm.ExportEncryptedPkcs8PrivateKey(password, s_windowsPbe);
            }
        }
    }
}
