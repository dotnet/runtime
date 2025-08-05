// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.X509Certificates.Tests.Common;

namespace System.Net.Test.Common
{
    public static partial class Configuration
    {
        public static partial class Certificates
        {
            private static readonly X509KeyUsageExtension s_eeKeyUsage =
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment,
                critical: false);

            private static readonly X509EnhancedKeyUsageExtension s_tlsServerEku =
                new X509EnhancedKeyUsageExtension(
                    new OidCollection
                    {
                    new Oid("1.3.6.1.5.5.7.3.1", null)
                    },
                    false);

            private static readonly X509EnhancedKeyUsageExtension s_tlsClientEku =
                new X509EnhancedKeyUsageExtension(
                    new OidCollection
                    {
                    new Oid("1.3.6.1.5.5.7.3.2", null)
                    },
                    false);

            private static readonly X509BasicConstraintsExtension s_eeConstraints =
                new X509BasicConstraintsExtension(false, false, 0, false);

            public static void CleanupCertificates([CallerMemberName] string? testName = null, StoreName storeName = StoreName.CertificateAuthority)
            {
                string caName = $"O={testName}";
                try
                {
                    using (X509Store store = new X509Store(storeName, StoreLocation.LocalMachine))
                    {
                        store.Open(OpenFlags.ReadWrite);
                        foreach (X509Certificate2 cert in store.Certificates)
                        {
                            if (cert.Subject.Contains(caName))
                            {
                                store.Remove(cert);
                            }
                            cert.Dispose();
                        }
                    }
                }
                catch
                {
                }

                try
                {
                    using (X509Store store = new X509Store(storeName, StoreLocation.CurrentUser))
                    {
                        store.Open(OpenFlags.ReadWrite);
                        foreach (X509Certificate2 cert in store.Certificates)
                        {
                            if (cert.Subject.Contains(caName))
                            {
                                store.Remove(cert);
                            }
                            cert.Dispose();
                        }
                    }
                }
                catch
                {
                }
            }

            internal static X509ExtensionCollection BuildTlsServerCertExtensions(string serverName)
            {
                return BuildTlsCertExtensions(serverName, true);
            }

            private static X509ExtensionCollection BuildTlsCertExtensions(string targetName, bool serverCertificate)
            {
                X509ExtensionCollection extensions = new X509ExtensionCollection();

                SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
                builder.AddDnsName(targetName);
                builder.AddIpAddress(IPAddress.Loopback);
                builder.AddIpAddress(IPAddress.IPv6Loopback);
                extensions.Add(builder.Build());
                extensions.Add(s_eeConstraints);
                extensions.Add(s_eeKeyUsage);
                extensions.Add(serverCertificate ? s_tlsServerEku : s_tlsClientEku);

                return extensions;
            }

            internal class PkiHolder : IDisposable
            {
                internal CertificateAuthority Root { get; }
                internal CertificateAuthority[] Intermediates { get; }
                public X509Certificate2 EndEntity { get; }
                public X509Certificate2Collection IssuerChain { get; }
                internal RevocationResponder Responder { get; }

                private readonly string? _testName;

                public PkiHolder(string? testName, CertificateAuthority root, CertificateAuthority[] intermediates, X509Certificate2 endEntity, RevocationResponder responder)
                {
                    _testName = testName;
                    Root = root;
                    Intermediates = intermediates;
                    EndEntity = endEntity;
                    Responder = responder;

                    // Walk the intermediates backwards so we build the chain collection as
                    // Issuer3
                    // Issuer2
                    // Issuer1
                    // Root
                    IssuerChain = new X509Certificate2Collection();
                    for (int i = intermediates.Length - 1; i >= 0; i--)
                    {
                        CertificateAuthority authority = intermediates[i];

                        IssuerChain.Add(authority.CloneIssuerCert());
                    }

                    IssuerChain.Add(root.CloneIssuerCert());
                }

                public SslStreamCertificateContext CreateSslStreamCertificateContext()
                {
                    return SslStreamCertificateContext.Create(EndEntity, IssuerChain);
                }

                public void Dispose()
                {
                    foreach (CertificateAuthority authority in Intermediates)
                    {
                        authority.Dispose();
                    }
                    Root.Dispose();
                    EndEntity.Dispose();
                    Responder.Dispose();

                    foreach (X509Certificate2 authority in IssuerChain)
                    {
                        authority.Dispose();
                    }

                    if (PlatformDetection.IsWindows && _testName != null)
                    {
                        CleanupCertificates(_testName);
                    }
                }
            }

            internal static PkiHolder GenerateCertificates(string targetName, [CallerMemberName] string? testName = null, bool longChain = false, bool serverCertificate = true, bool ephemeralKey = false)
            {
                const int keySize = 2048;
                if (PlatformDetection.IsWindows && testName != null)
                {
                    CleanupCertificates(testName);
                }

                X509Certificate2Collection chain = new X509Certificate2Collection();
                X509ExtensionCollection extensions = BuildTlsCertExtensions(targetName, serverCertificate);

                CertificateAuthority.BuildPrivatePki(
                    PkiOptions.AllRevocation,
                    out RevocationResponder responder,
                    out CertificateAuthority root,
                    out CertificateAuthority[] intermediates,
                    out X509Certificate2 endEntity,
                    intermediateAuthorityCount: longChain ? 3 : 1,
                    subjectName: targetName,
                    testName: testName,
                    keyFactory: CertificateAuthority.KeyFactory.RSASize(keySize),
                    extensions: extensions);

                if (!ephemeralKey && PlatformDetection.IsWindows)
                {
                    X509Certificate2 ephemeral = endEntity;
                    endEntity = X509CertificateLoader.LoadPkcs12(endEntity.Export(X509ContentType.Pfx), (string?)null, X509KeyStorageFlags.Exportable);
                    ephemeral.Dispose();
                }

                return new PkiHolder(testName, root, intermediates, endEntity, responder);
            }
        }
    }
}
