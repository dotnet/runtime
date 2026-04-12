using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
    public static class ApplePartialChainTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public static void TamperedSignature_WithAllowUnknownCertificateAuthority_IsAccepted_OnApple()
        {
            using X509Certificate2 root = CreateRoot(out ECDsa rootKey);
            using X509Certificate2 intermediate = CreateIntermediate(root, out ECDsa intermediateKey);
            using X509Certificate2 leaf = CreateLeaf(intermediate, intermediateKey);

            using X509Certificate2 tamperedLeaf = new X509Certificate2(TamperSignature(leaf.Export(X509ContentType.Cert)));

            using (ChainHolder chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(root);
                chain.ChainPolicy.ExtraStore.Add(intermediate);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

                bool valid = chain.Build(tamperedLeaf);

                Assert.True(valid, $"Expected Apple to accept the tampered chain with AllowUnknownCertificateAuthority, but got '{chain.AllStatusFlags()}'.");
                Assert.Equal(1, chain.ChainElements.Count);
                Assert.Equal(X509ChainStatusFlags.PartialChain, chain.AllStatusFlags());
            }
        }

        private static X509Certificate2 CreateRoot(out ECDsa key)
        {
            key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var req = new CertificateRequest("CN=Root", key, HashAlgorithmName.SHA256);
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
            return req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        }

        private static X509Certificate2 CreateIntermediate(X509Certificate2 root, out ECDsa key)
        {
            key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var req = new CertificateRequest("CN=Intermediate", key, HashAlgorithmName.SHA256);
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
            X509Certificate2 cert = req.Create(root, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(20), Guid.NewGuid().ToByteArray());
            return cert.CopyWithPrivateKey(key);
        }

        private static X509Certificate2 CreateLeaf(X509Certificate2 intermediate, ECDsa intermediateKey)
        {
            using ECDsa leafKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var req = new CertificateRequest("CN=Leaf", leafKey, HashAlgorithmName.SHA256);
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
            req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
            req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, true));
            X509Certificate2 cert = req.Create(intermediate, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(10), Guid.NewGuid().ToByteArray());
            return cert.CopyWithPrivateKey(leafKey);
        }

        private static byte[] TamperSignature(byte[] cert)
        {
            byte[] mutated = cert.ToArray();
            mutated[^10] ^= 0x01;
            return mutated;
        }
    }
}
