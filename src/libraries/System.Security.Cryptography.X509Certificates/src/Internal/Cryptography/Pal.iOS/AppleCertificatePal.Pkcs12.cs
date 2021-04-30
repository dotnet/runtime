// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs12;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class AppleCertificatePal : ICertificatePal
    {
        private static SafePasswordHandle s_passwordExportHandle = new SafePasswordHandle("DotnetExportPassphrase");

        private static AppleCertificatePal ImportPkcs12(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password)
        {
            using (ApplePkcs12Reader reader = new ApplePkcs12Reader(rawData))
            {
                reader.Decrypt(password);
                return ImportPkcs12(reader.GetSingleCert());
            }
        }

        internal static AppleCertificatePal ImportPkcs12(UnixPkcs12Reader.CertAndKey certAndKey)
        {
            AppleCertificatePal pal = (AppleCertificatePal)certAndKey.Cert!;

            if (certAndKey.Key != null)
            {
                Pkcs12SmallExport exporter = new Pkcs12SmallExport(new TempExportPal(pal), certAndKey.Key);
                byte[] smallPfx = exporter.Export(X509ContentType.Pkcs12, s_passwordExportHandle)!;

                SafeSecIdentityHandle identityHandle;
                SafeSecCertificateHandle certHandle = Interop.AppleCrypto.X509ImportCertificate(
                    smallPfx,
                    X509ContentType.Pkcs12,
                    s_passwordExportHandle,
                    out identityHandle);

                if (identityHandle.IsInvalid)
                {
                    identityHandle.Dispose();
                    return new AppleCertificatePal(certHandle);
                }

                certHandle.Dispose();
                return new AppleCertificatePal(identityHandle);
            }

            return pal;
        }

        private sealed class Pkcs12SmallExport : UnixExportProvider
        {
            private readonly AsymmetricAlgorithm _privateKey;

            internal Pkcs12SmallExport(ICertificatePalCore cert, AsymmetricAlgorithm privateKey)
                : base(cert)
            {
                _privateKey = privateKey;
            }

            protected override byte[] ExportPkcs7() => throw new NotImplementedException();

            protected override byte[] ExportPkcs8(ICertificatePalCore certificatePal, ReadOnlySpan<char> password)
            {
                return _privateKey.ExportEncryptedPkcs8PrivateKey(password, s_windowsPbe);
            }
        }

        private sealed class TempExportPal : ICertificatePalCore
        {
            private readonly ICertificatePal _realPal;

            internal TempExportPal(AppleCertificatePal realPal)
            {
                _realPal = realPal;
            }

            public bool HasPrivateKey => true;

            public void Dispose()
            {
                // No-op.
            }

            // Forwarders to make the interface compliant.
            public IntPtr Handle => _realPal.Handle;
            public string Issuer => _realPal.Issuer;
            public string Subject => _realPal.Subject;
            public string LegacyIssuer => _realPal.LegacyIssuer;
            public string LegacySubject => _realPal.LegacySubject;
            public byte[] Thumbprint => _realPal.Thumbprint;
            public string KeyAlgorithm => _realPal.KeyAlgorithm;
            public byte[] KeyAlgorithmParameters => _realPal.KeyAlgorithmParameters;
            public byte[] PublicKeyValue => _realPal.PublicKeyValue;
            public byte[] SerialNumber => _realPal.SerialNumber;
            public string SignatureAlgorithm => _realPal.SignatureAlgorithm;
            public DateTime NotAfter => _realPal.NotAfter;
            public DateTime NotBefore => _realPal.NotBefore;
            public byte[] RawData => _realPal.RawData;
            public byte[] Export(X509ContentType contentType, SafePasswordHandle password) =>
                _realPal.Export(contentType, password);
        }
    }
}
