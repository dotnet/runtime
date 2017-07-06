// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    public static class CertificateRequestChainTests
    {
        public static bool PlatformSupportsPss { get; } = DetectPssSupport();

        [Fact]
        public static void CreateChain_ECC()
        {
            using (ECDsa rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP521))
            using (ECDsa intermed1Key = ECDsa.Create(ECCurve.NamedCurves.nistP384))
            using (ECDsa intermed2Key = ECDsa.Create(ECCurve.NamedCurves.nistP384))
            using (ECDsa leafKey = ECDsa.Create(ECCurve.NamedCurves.nistP256))
            using (ECDsa leafPubKey = ECDsa.Create(leafKey.ExportParameters(false)))
            {
                CreateAndTestChain(
                    rootKey,
                    intermed1Key,
                    intermed2Key,
                    leafPubKey);
            }
        }

        [Fact]
        public static void CreateChain_RSA()
        {
            using (RSA rootKey = RSA.Create(3072))
            using (RSA intermed1Key = RSA.Create(2048))
            using (RSA intermed2Key = RSA.Create(2048))
            using (RSA leafKey = RSA.Create(1536))
            using (RSA leafPubKey = RSA.Create(leafKey.ExportParameters(false)))
            {
                leafPubKey.ImportParameters(leafKey.ExportParameters(false));

                CreateAndTestChain(
                    rootKey,
                    intermed1Key,
                    intermed2Key,
                    leafPubKey);
            }
        }

        [Fact]
        public static void CreateChain_Hybrid()
        {
            using (ECDsa rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP521))
            using (RSA intermed1Key = RSA.Create(2048))
            using (RSA intermed2Key = RSA.Create(2048))
            using (ECDsa leafKey = ECDsa.Create(ECCurve.NamedCurves.nistP256))
            using (ECDsa leafPubKey = ECDsa.Create(leafKey.ExportParameters(false)))
            {
                CreateAndTestChain(
                    rootKey,
                    intermed1Key,
                    intermed2Key,
                    leafPubKey);
            }
        }

        [Theory]
        // A root cert doing the issuing
        [InlineData(false, null, X509KeyUsageFlags.None, false)]
        [InlineData(false, null, X509KeyUsageFlags.KeyCertSign, false)]
        [InlineData(false, null, X509KeyUsageFlags.DigitalSignature, false)]
        [InlineData(false, null, X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DigitalSignature, false)]
        [InlineData(false, false, X509KeyUsageFlags.None, false)]
        [InlineData(false, false, X509KeyUsageFlags.KeyCertSign, false)]
        [InlineData(false, false, X509KeyUsageFlags.DigitalSignature, false)]
        [InlineData(false, false, X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DigitalSignature, false)]
        [InlineData(false, true, X509KeyUsageFlags.None, true)]
        [InlineData(false, true, X509KeyUsageFlags.KeyCertSign, true)]
        [InlineData(false, true, X509KeyUsageFlags.DigitalSignature, false)]
        [InlineData(false, true, X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DigitalSignature, true)]

        // An intermediate doing the issuing
        [InlineData(true, null, X509KeyUsageFlags.None, false)]
        [InlineData(true, null, X509KeyUsageFlags.KeyCertSign, false)]
        [InlineData(true, null, X509KeyUsageFlags.DigitalSignature, false)]
        [InlineData(true, null, X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DigitalSignature, false)]
        [InlineData(true, false, X509KeyUsageFlags.None, false)]
        [InlineData(true, false, X509KeyUsageFlags.KeyCertSign, false)]
        [InlineData(true, false, X509KeyUsageFlags.DigitalSignature, false)]
        [InlineData(true, false, X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DigitalSignature, false)]
        [InlineData(true, true, X509KeyUsageFlags.None, true)]
        [InlineData(true, true, X509KeyUsageFlags.KeyCertSign, true)]
        [InlineData(true, true, X509KeyUsageFlags.DigitalSignature, false)]
        [InlineData(true, true, X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DigitalSignature, true)]
        public static void ChainCertRequirements(bool useIntermed, bool? isCA, X509KeyUsageFlags keyUsage, bool expectSuccess)
        {
            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA384;

            ECDsa rootKey = null;
            ECDsa intermedKey = null;
            ECDsa leafKey = null;

            X509Certificate2 rootCert = null;
            X509Certificate2 intermedCert = null;
            X509Certificate2 leafCert = null;

            try
            {
                rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);

                var request = new CertificateRequest("CN=Root", rootKey, hashAlgorithm);

                if (useIntermed || isCA.HasValue)
                {
                    request.CertificateExtensions.Add(
                        new X509BasicConstraintsExtension(useIntermed || isCA.Value, false, 0, true));
                }

                X509KeyUsageFlags rootFlags = useIntermed ? X509KeyUsageFlags.KeyCertSign : keyUsage;

                if (rootFlags != X509KeyUsageFlags.None)
                {
                    request.CertificateExtensions.Add(new X509KeyUsageExtension(rootFlags, true));
                }

                DateTimeOffset start = DateTimeOffset.UtcNow.AddHours(-1);
                DateTimeOffset end = start.AddHours(2);

                rootCert = request.CreateSelfSigned(start, end);

                X509Certificate2 signerCert = rootCert;

                if (useIntermed)
                {
                    intermedKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
                    request = new CertificateRequest("CN=Intermediate", intermedKey, hashAlgorithm);

                    if (isCA.HasValue)
                    {
                        request.CertificateExtensions.Add(
                            new X509BasicConstraintsExtension(isCA.Value, false, 0, true));
                    }

                    if (keyUsage != X509KeyUsageFlags.None)
                    {
                        request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsage, true));
                    }

                    using (X509Certificate2 tmp = request.Create(rootCert, start, end, new byte[] { 6, 0, 2, 2, 10, 23 }))
                    {
                        intermedCert = tmp.CopyWithPrivateKey(intermedKey);
                    }

                    signerCert = intermedCert;
                }

                leafKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                request = new CertificateRequest("CN=Leaf", leafKey, hashAlgorithm);

                byte[] leafSerialNumber = { 2, 4, 6, 0, 1 };

                if (!expectSuccess)
                {
                    AssertExtensions.Throws<ArgumentException>(
                        "issuerCertificate",
                        () =>
                        {
                            request.Create(signerCert, start, end, leafSerialNumber)?.Dispose();
                        });

                    return;
                }

                leafCert = request.Create(signerCert, start, end, leafSerialNumber);

                using (X509Chain chain = new X509Chain())
                {
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    chain.ChainPolicy.ExtraStore.Add(rootCert);
                    chain.ChainPolicy.VerificationTime = start.ToLocalTime().DateTime;

                    if (useIntermed)
                    {
                        chain.ChainPolicy.ExtraStore.Add(intermedCert);
                    }

                    RunChain(chain, leafCert, true, "Chain verification");
                }
            }
            finally
            {
                leafCert?.Dispose();
                leafKey?.Dispose();
                intermedCert?.Dispose();
                intermedKey?.Dispose();
                rootCert?.Dispose();
                rootKey?.Dispose();
            }
        }

        private static CertificateRequest OpenCertRequest(
            string dn,
            AsymmetricAlgorithm key,
            HashAlgorithmName hashAlgorithm)
        {
            RSA rsa = key as RSA;

            if (rsa != null)
                return new CertificateRequest(dn, rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);

            ECDsa ecdsa = key as ECDsa;

            if (ecdsa != null)
                return new CertificateRequest(dn, ecdsa, hashAlgorithm);

            throw new InvalidOperationException(
                $"Had no handler for key of type {key?.GetType().FullName ?? "null"}");
        }

        private static X509SignatureGenerator OpenGenerator(AsymmetricAlgorithm key)
        {
            RSA rsa = key as RSA;

            if (rsa != null)
                return X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1);

            ECDsa ecdsa = key as ECDsa;

            if (ecdsa != null)
                return X509SignatureGenerator.CreateForECDsa(ecdsa);

            throw new InvalidOperationException(
                $"Had no handler for key of type {key?.GetType().FullName ?? "null"}");
        }

        private static CertificateRequest CreateChainRequest(
            string dn,
            AsymmetricAlgorithm key,
            HashAlgorithmName hashAlgorithm,
            bool isCa,
            int? pathLen)
        {
            const X509KeyUsageFlags CAFlags = X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.KeyCertSign;
            const X509KeyUsageFlags EEFlags =
                X509KeyUsageFlags.DataEncipherment |
                X509KeyUsageFlags.KeyEncipherment |
                X509KeyUsageFlags.DigitalSignature |
                X509KeyUsageFlags.NonRepudiation;

            CertificateRequest request = OpenCertRequest(dn, key, hashAlgorithm);

            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(
                    request.PublicKey,
                    X509SubjectKeyIdentifierHashAlgorithm.Sha1,
                    false));

            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    isCa ? CAFlags : EEFlags,
                    true));

            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(
                    isCa,
                    pathLen.HasValue,
                    pathLen.GetValueOrDefault(),
                    true));

            return request;
        }

        private static void RunChain(
            X509Chain chain,
            X509Certificate2 cert,
            bool expectSuccess,
            string msg)
        {
            bool success = chain.Build(cert);

            FormattableString errMsg = null;

            if (expectSuccess && !success)
            {
                for (int i = 0; i < chain.ChainElements.Count; i++)
                {
                    X509ChainElement element = chain.ChainElements[i];

                    if (element.ChainElementStatus.Length != 0)
                    {
                        X509ChainStatusFlags flags =
                            element.ChainElementStatus.Select(ces => ces.Status).Aggregate((a, b) => a | b);

                        errMsg = $"{msg}: Initial chain error at depth {i}: {flags}";
                        break;
                    }
                }
            }
            else if (!expectSuccess && success)
            {
                errMsg = $"{msg}: Chain fails when expected";
            }

            if (errMsg != null)
            {
                DisposeChainCerts(chain);
            }

            if (expectSuccess)
            {
                Assert.True(success, errMsg?.ToString());
            }
            else
            {
                Assert.False(success, errMsg?.ToString());
            }
        }

        private static void DisposeChainCerts(X509Chain chain)
        {
            foreach (X509ChainElement element in chain.ChainElements)
            {
                element.Certificate.Dispose();
            }
        }

        private static X509Certificate2 CloneWithPrivateKey(X509Certificate2 cert, AsymmetricAlgorithm key)
        {
            RSA rsa = key as RSA;

            if (rsa != null)
                return cert.CopyWithPrivateKey(rsa);

            ECDsa ecdsa = key as ECDsa;

            if (ecdsa != null)
                return cert.CopyWithPrivateKey(ecdsa);

            DSA dsa = key as DSA;

            if (dsa != null)
                return cert.CopyWithPrivateKey(dsa);

            throw new InvalidOperationException(
                $"Had no handler for key of type {key?.GetType().FullName ?? "null"}");
        }

        private static void CreateAndTestChain(
            AsymmetricAlgorithm rootPrivKey,
            AsymmetricAlgorithm intermed1PrivKey,
            AsymmetricAlgorithm intermed2PrivKey,
            AsymmetricAlgorithm leafPubKey)
        {
            const string RootDN = "CN=Experimental Root Certificate";
            const string Intermed1DN = "CN=First Intermediate Certificate, O=Experimental";
            const string Intermed2DN = "CN=Second Intermediate Certificate, O=Experimental";
            const string LeafDN = "CN=End-Entity Certificate, O=Experimental";

            CertificateRequest rootRequest =
                CreateChainRequest(RootDN, rootPrivKey, HashAlgorithmName.SHA512, true, null);

            CertificateRequest intermed1Request =
                CreateChainRequest(Intermed1DN, intermed1PrivKey, HashAlgorithmName.SHA384, true, null);

            CertificateRequest intermed2Request =
                CreateChainRequest(Intermed2DN, intermed2PrivKey, HashAlgorithmName.SHA384, true, 0);

            CertificateRequest leafRequest =
                CreateChainRequest(LeafDN, leafPubKey, HashAlgorithmName.SHA256, false, null);

            leafRequest.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

            X509SignatureGenerator rootGenerator = OpenGenerator(rootPrivKey);
            X509SignatureGenerator intermed2Generator = OpenGenerator(intermed2PrivKey);

            X509Certificate2 rootCertWithKey = null;
            X509Certificate2 intermed1CertWithKey = null;
            X509Certificate2 intermed2CertWithKey = null;
            X509Certificate2 leafCert = null;

            try
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset rootEnd = now + TimeSpan.FromDays(10000);
                DateTimeOffset intermedEnd = now + TimeSpan.FromDays(366 * 4);
                DateTimeOffset leafEnd = now + TimeSpan.FromDays(366 * 1.3);

                rootCertWithKey = rootRequest.CreateSelfSigned(now, rootEnd);

                byte[] intermed1Serial = new byte[10];
                byte[] intermed2Serial = new byte[10];
                byte[] leafSerial = new byte[10];

                intermed1Serial[1] = 1;
                intermed2Serial[1] = 2;
                leafSerial[1] = 1;

                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(intermed1Serial, 2, intermed1Serial.Length - 2);
                    rng.GetBytes(intermed2Serial, 2, intermed2Serial.Length - 2);
                    rng.GetBytes(leafSerial, 2, leafSerial.Length - 2);
                }

                X509Certificate2 intermed1Tmp =
                    intermed1Request.Create(rootCertWithKey.SubjectName, rootGenerator, now, intermedEnd, intermed1Serial);

                X509Certificate2 intermed2Tmp =
                    intermed2Request.Create(rootCertWithKey.SubjectName, rootGenerator, now, intermedEnd, intermed1Serial);

                intermed1CertWithKey = CloneWithPrivateKey(intermed1Tmp, intermed1PrivKey);
                intermed2CertWithKey = CloneWithPrivateKey(intermed2Tmp, intermed2PrivKey);

                intermed1Tmp.Dispose();
                intermed2Tmp.Dispose();

                leafCert = leafRequest.Create(
                    intermed2CertWithKey.SubjectName, intermed2Generator, now, leafEnd, leafSerial);

                using (X509Chain chain = new X509Chain())
                {
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    chain.ChainPolicy.ExtraStore.Add(intermed1CertWithKey);
                    chain.ChainPolicy.ExtraStore.Add(intermed2CertWithKey);
                    chain.ChainPolicy.ExtraStore.Add(rootCertWithKey);
                    chain.ChainPolicy.VerificationTime = now.ToLocalTime().DateTime;

                    RunChain(chain, leafCert, true, "Initial chain build");

                    try
                    {
                        // Intermediate 1 plays no part.
                        Assert.Equal(3, chain.ChainElements.Count);
                        Assert.Equal(LeafDN, chain.ChainElements[0].Certificate.Subject);
                        Assert.Equal(Intermed2DN, chain.ChainElements[1].Certificate.Subject);
                        Assert.Equal(RootDN, chain.ChainElements[2].Certificate.Subject);
                    }
                    finally
                    {
                        DisposeChainCerts(chain);
                    }

                    // Server Auth EKU, expect true.
                    chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1"));
                    RunChain(chain, leafCert, true, "Server auth EKU chain build");
                    DisposeChainCerts(chain);

                    // Client Auth EKU, expect false
                    chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.2"));
                    RunChain(chain, leafCert, false, "Server and Client auth EKU chain build");
                    DisposeChainCerts(chain);
                }
            }
            finally
            {
                leafCert?.Dispose();
                intermed2CertWithKey?.Dispose();
                intermed1CertWithKey?.Dispose();
                rootCertWithKey?.Dispose();
            }
        }

        [ConditionalFact(nameof(PlatformSupportsPss))]
        public static void CreateChain_RSAPSS()
        {
            using (RSA rootKey = RSA.Create())
            using (RSA intermedKey = RSA.Create())
            using (RSA leafKey = RSA.Create(TestData.RsaBigExponentParams))
            {
                X509Certificate2 rootCertWithKey = null;
                X509Certificate2 intermedCertWithKey = null;
                X509Certificate2 leafCert = null;
                CertificateRequest request;

                RSASignaturePadding padding = RSASignaturePadding.Pss;

                DateTimeOffset notBefore = DateTimeOffset.UtcNow;
                DateTimeOffset notAfter = notBefore.AddHours(1);

                try
                {
                    request = new CertificateRequest("CN=Root", rootKey, HashAlgorithmName.SHA512, padding);
                    request.CertificateExtensions.Add(
                        new X509BasicConstraintsExtension(true, false, 0, true));

                    rootCertWithKey = request.CreateSelfSigned(notBefore, notAfter);

                    byte[] intermedSerial = { 1, 2, 3, 5, 7, 11, 13 };

                    request = new CertificateRequest("CN=Intermediate", intermedKey, HashAlgorithmName.SHA384, padding);
                    request.CertificateExtensions.Add(
                        new X509BasicConstraintsExtension(true, true, 1, true));

                    X509Certificate2 intermedPublic = request.Create(rootCertWithKey, notBefore, notAfter, intermedSerial);
                    intermedCertWithKey = intermedPublic.CopyWithPrivateKey(intermedKey);
                    intermedPublic.Dispose();

                    request = new CertificateRequest("CN=Leaf", leafKey, HashAlgorithmName.SHA256, padding);
                    request.CertificateExtensions.Add(
                        new X509BasicConstraintsExtension(false, false, 0, true));

                    byte[] leafSerial = { 1, 1, 2, 6, 12, 60, 60, };

                    leafCert = request.Create(intermedCertWithKey, notBefore, notAfter, leafSerial);

                    using (X509Chain chain = new X509Chain())
                    {
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                        chain.ChainPolicy.ExtraStore.Add(intermedCertWithKey);
                        chain.ChainPolicy.ExtraStore.Add(rootCertWithKey);
                        chain.ChainPolicy.VerificationTime = notBefore.ToLocalTime().DateTime;

                        RunChain(chain, leafCert, true, "Chain build");
                    }
                }
                finally
                {
                    leafCert?.Dispose();
                    intermedCertWithKey?.Dispose();
                    rootCertWithKey?.Dispose();
                }
            }
        }

        private static bool DetectPssSupport()
        {
            using (X509Certificate2 cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            using (RSA rsa = cert.GetRSAPrivateKey())
            {
                try
                {
                    rsa.SignData(Array.Empty<byte>(), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                }
                catch (CryptographicException)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
