// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs12;
using System.Text;
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
                AppleCertificateExporter exporter = new AppleCertificateExporter(new TempExportPal(pal, certAndKey.Key));
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

        private sealed class TempExportPal : ICertificatePal
        {
            private readonly ICertificatePal _realPal;
            private readonly AsymmetricAlgorithm _privateKey;

            internal TempExportPal(AppleCertificatePal realPal, AsymmetricAlgorithm privateKey)
            {
                Debug.Assert(privateKey != null);
                _realPal = realPal;
                _privateKey = privateKey;
            }

            public bool HasPrivateKey => true;
            public RSA? GetRSAPrivateKey() => _privateKey as RSA;
            public DSA? GetDSAPrivateKey() => _privateKey as DSA;
            public ECDsa? GetECDsaPrivateKey() => _privateKey as ECDsa;
            public ECDiffieHellman? GetECDiffieHellmanPrivateKey() => _privateKey as ECDiffieHellman;

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

            public int Version => _realPal.Version;
            public bool Archived { get => _realPal.Archived; set => _realPal.Archived = value; }
            public string FriendlyName { get => _realPal.FriendlyName; set => _realPal.FriendlyName = value; }
            public X500DistinguishedName SubjectName => _realPal.SubjectName;
            public X500DistinguishedName IssuerName => _realPal.IssuerName;
            public IEnumerable<X509Extension> Extensions => _realPal.Extensions;
            public string GetNameInfo(X509NameType nameType, bool forIssuer) => _realPal.GetNameInfo(nameType, forIssuer);
            public void AppendPrivateKeyInfo(StringBuilder sb) => _realPal.AppendPrivateKeyInfo(sb);
            public ICertificatePal CopyWithPrivateKey(DSA privateKey) => _realPal.CopyWithPrivateKey(privateKey);
            public ICertificatePal CopyWithPrivateKey(ECDsa privateKey) => _realPal.CopyWithPrivateKey(privateKey);
            public ICertificatePal CopyWithPrivateKey(RSA privateKey) => _realPal.CopyWithPrivateKey(privateKey);
            public ICertificatePal CopyWithPrivateKey(ECDiffieHellman privateKey) => _realPal.CopyWithPrivateKey(privateKey);
            public PolicyData GetPolicyData() => _realPal.GetPolicyData();
        }
    }
}
