// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class DynamicChainTests
    {
        public static object[][] InvalidSignature3Cases { get; } =
            new object[][]
            {
                new object[]
                {
                    X509ChainStatusFlags.NotSignatureValid,
                    X509ChainStatusFlags.NoError,
                    X509ChainStatusFlags.UntrustedRoot,
                },
                new object[]
                {
                    X509ChainStatusFlags.NoError,
                    X509ChainStatusFlags.NotSignatureValid,
                    X509ChainStatusFlags.UntrustedRoot,
                },
                new object[]
                {
                    X509ChainStatusFlags.NoError,
                    X509ChainStatusFlags.NoError,
                    X509ChainStatusFlags.NotSignatureValid | X509ChainStatusFlags.UntrustedRoot,
                },
                new object[]
                {
                    X509ChainStatusFlags.NotSignatureValid | X509ChainStatusFlags.NotTimeValid,
                    X509ChainStatusFlags.NoError,
                    X509ChainStatusFlags.UntrustedRoot,
                },
                new object[]
                {
                    X509ChainStatusFlags.NotSignatureValid | X509ChainStatusFlags.NotTimeValid,
                    X509ChainStatusFlags.NotTimeValid,
                    X509ChainStatusFlags.UntrustedRoot,
                },
                new object[]
                {
                    X509ChainStatusFlags.NotSignatureValid | X509ChainStatusFlags.NotTimeValid,
                    X509ChainStatusFlags.NotTimeValid,
                    X509ChainStatusFlags.UntrustedRoot | X509ChainStatusFlags.NotTimeValid,
                },
            };

        [Theory]
        [MemberData(nameof(InvalidSignature3Cases))]
        public static void BuildInvalidSignatureTwice(
            X509ChainStatusFlags endEntityErrors,
            X509ChainStatusFlags intermediateErrors,
            X509ChainStatusFlags rootErrors)
        {
            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert);

            X509Certificate2 TamperIfNeeded(X509Certificate2 input, X509ChainStatusFlags flags)
            {
                if ((flags & X509ChainStatusFlags.NotSignatureValid) != 0)
                {
                    X509Certificate2 tampered = TamperSignature(input);
                    input.Dispose();
                    return tampered;
                }

                return input;
            }

            DateTime RewindIfNeeded(DateTime input, X509Certificate2 cert, X509ChainStatusFlags flags)
            {
                if ((flags & X509ChainStatusFlags.NotTimeValid) != 0)
                {
                    return cert.NotBefore.AddMinutes(-1);
                }

                return input;
            }

            int expectedCount = 3;

            DateTime verificationTime = endEntityCert.NotBefore.AddMinutes(1);
            verificationTime = RewindIfNeeded(verificationTime, endEntityCert, endEntityErrors);
            verificationTime = RewindIfNeeded(verificationTime, intermediateCert, intermediateErrors);
            verificationTime = RewindIfNeeded(verificationTime, rootCert, rootErrors);

            // Replace the certs for the scenario.
            endEntityCert = TamperIfNeeded(endEntityCert, endEntityErrors);
            intermediateCert = TamperIfNeeded(intermediateCert, intermediateErrors);
            rootCert = TamperIfNeeded(rootCert, rootErrors);

            if (OperatingSystem.IsMacOS())
            {
                // For the lower levels, turn NotSignatureValid into PartialChain,
                // and clear all errors at higher levels.

                if ((endEntityErrors & X509ChainStatusFlags.NotSignatureValid) != 0)
                {
                    expectedCount = 1;
                    endEntityErrors &= ~X509ChainStatusFlags.NotSignatureValid;
                    endEntityErrors |= X509ChainStatusFlags.PartialChain;
                    intermediateErrors = X509ChainStatusFlags.NoError;
                    rootErrors = X509ChainStatusFlags.NoError;
                }
                else if ((intermediateErrors & X509ChainStatusFlags.NotSignatureValid) != 0)
                {
                    expectedCount = 2;
                    intermediateErrors &= ~X509ChainStatusFlags.NotSignatureValid;
                    intermediateErrors |= X509ChainStatusFlags.PartialChain;
                    rootErrors = X509ChainStatusFlags.NoError;
                }
                else if ((rootErrors & X509ChainStatusFlags.NotSignatureValid) != 0)
                {
                    rootErrors &= ~X509ChainStatusFlags.NotSignatureValid;

                    // On 10.13+ it becomes PartialChain, and UntrustedRoot goes away.
                    if (PlatformDetection.IsOSX)
                    {
                        rootErrors &= ~X509ChainStatusFlags.UntrustedRoot;
                        rootErrors |= X509ChainStatusFlags.PartialChain;
                    }
                }
            }
            else if (OperatingSystem.IsWindows())
            {
                // Windows only reports NotTimeValid on the start-of-chain (end-entity in this case)
                // If it were possible in this suite to get only a higher-level cert as NotTimeValid
                // without the lower one, that would have resulted in NotTimeNested.
                intermediateErrors &= ~X509ChainStatusFlags.NotTimeValid;
                rootErrors &= ~X509ChainStatusFlags.NotTimeValid;
            }

            X509ChainStatusFlags expectedAllErrors = endEntityErrors | intermediateErrors | rootErrors;

            // If PartialChain or UntrustedRoot are the only remaining errors, the chain will succeed.
            const X509ChainStatusFlags SuccessCodes =
                X509ChainStatusFlags.UntrustedRoot | X509ChainStatusFlags.PartialChain;

            bool expectSuccess = (expectedAllErrors & ~SuccessCodes) == 0;

            using (endEntityCert)
            using (intermediateCert)
            using (rootCert)
            using (ChainHolder chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.VerificationTime = verificationTime;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                chain.ChainPolicy.ExtraStore.Add(rootCert);

                chain.ChainPolicy.VerificationFlags |=
                    X509VerificationFlags.AllowUnknownCertificateAuthority;

                int i = 0;

                void CheckChain()
                {
                    i++;

                    bool valid = chain.Build(endEntityCert);

                    if (expectSuccess)
                    {
                        Assert.True(valid, $"Chain build on iteration {i}");
                    }
                    else
                    {
                        Assert.False(valid, $"Chain build on iteration {i}");
                    }

                    Assert.Equal(expectedCount, chain.ChainElements.Count);
                    Assert.Equal(expectedAllErrors, chain.AllStatusFlags());

                    Assert.Equal(endEntityErrors, chain.ChainElements[0].AllStatusFlags());

                    if (expectedCount > 2)
                    {
                        Assert.Equal(rootErrors, chain.ChainElements[2].AllStatusFlags());
                    }

                    if (expectedCount > 1)
                    {
                        Assert.Equal(intermediateErrors, chain.ChainElements[1].AllStatusFlags());
                    }

                    chainHolder.DisposeChainElements();
                }

                CheckChain();
                CheckChain();
            }
        }

        [Fact]
        public static void BasicConstraints_ExceedMaximumPathLength()
        {
            X509Extension[] rootExtensions = new [] {
                new X509BasicConstraintsExtension(
                    certificateAuthority: true,
                    hasPathLengthConstraint: true,
                    pathLengthConstraint: 0,
                    critical: true)
            };

            X509Extension[] intermediateExtensions = new [] {
                new X509BasicConstraintsExtension(
                    certificateAuthority: true,
                    hasPathLengthConstraint: true,
                    pathLengthConstraint: 0,
                    critical: true)
            };

            TestDataGenerator.MakeTestChain4(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert1,
                out X509Certificate2 intermediateCert2,
                out X509Certificate2 rootCert,
                rootExtensions: rootExtensions,
                intermediateExtensions: intermediateExtensions);

            using (endEntityCert)
            using (intermediateCert1)
            using (intermediateCert2)
            using (rootCert)
            using (ChainHolder chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = endEntityCert.NotBefore.AddSeconds(1);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                chain.ChainPolicy.ExtraStore.Add(intermediateCert1);
                chain.ChainPolicy.ExtraStore.Add(intermediateCert2);

                Assert.False(chain.Build(endEntityCert));
                Assert.Equal(X509ChainStatusFlags.InvalidBasicConstraints, chain.AllStatusFlags());
            }
        }

        [Fact]
        public static void BasicConstraints_ViolatesCaFalse()
        {
            X509Extension[] intermediateExtensions = new [] {
                new X509BasicConstraintsExtension(
                    certificateAuthority: false,
                    hasPathLengthConstraint: false,
                    pathLengthConstraint: 0,
                    critical: true)
            };

            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert,
                intermediateExtensions: intermediateExtensions);

            using (endEntityCert)
            using (intermediateCert)
            using (rootCert)
            using (ChainHolder chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = endEntityCert.NotBefore.AddSeconds(1);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                chain.ChainPolicy.ExtraStore.Add(intermediateCert);

                Assert.False(chain.Build(endEntityCert));
                Assert.Equal(X509ChainStatusFlags.InvalidBasicConstraints, chain.AllStatusFlags());
            }
        }

        [Fact]
        public static void TestLeafCertificateWithUnknownCriticalExtension()
        {
            using (RSA key = RSA.Create())
            {
                CertificateRequest certReq = new CertificateRequest(
                    new X500DistinguishedName("CN=Cert"),
                    key,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                const string PrecertificatePoisonExtensionOid = "1.3.6.1.4.1.11129.2.4.3";
                certReq.CertificateExtensions.Add(new X509Extension(
                    new AsnEncodedData(
                        new Oid(PrecertificatePoisonExtensionOid),
                        new byte[] { 5, 0 }),
                    critical: true));

                DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddDays(-1);
                DateTimeOffset notAfter = notBefore.AddDays(30);

                using (X509Certificate2 cert = certReq.CreateSelfSigned(notBefore, notAfter))
                using (ChainHolder holder = new ChainHolder())
                {
                    X509Chain chain = holder.Chain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    Assert.False(chain.Build(cert));

                    X509ChainElement certElement = chain.ChainElements.OfType<X509ChainElement>().Single();
                    const X509ChainStatusFlags ExpectedFlag = X509ChainStatusFlags.HasNotSupportedCriticalExtension;
                    X509ChainStatusFlags actualFlags = certElement.AllStatusFlags();
                    Assert.True((actualFlags & ExpectedFlag) == ExpectedFlag, $"Has expected flag {ExpectedFlag} but was {actualFlags}");
                }
            }
        }

        [Fact]
        public static void TestInvalidAia()
        {
            using (RSA key = RSA.Create())
            {
                CertificateRequest rootReq = new CertificateRequest(
                    "CN=Root",
                    key,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                rootReq.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                CertificateRequest certReq = new CertificateRequest(
                    "CN=test",
                    key,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                certReq.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(false, false, 0, false));

                certReq.CertificateExtensions.Add(
                    new X509Extension(
                        "1.3.6.1.5.5.7.1.1",
                        new byte[] { 5 },
                        critical: false));

                DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddDays(-1);
                DateTimeOffset notAfter = notBefore.AddDays(30);

                using (X509Certificate2 root = rootReq.CreateSelfSigned(notBefore, notAfter))
                using (X509Certificate2 ee = certReq.Create(root, notBefore, notAfter, root.GetSerialNumber()))
                {
                    X509Chain chain = new X509Chain();
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    Assert.False(chain.Build(ee));
                    Assert.Equal(1, chain.ChainElements.Count);
                    Assert.Equal(X509ChainStatusFlags.PartialChain, chain.AllStatusFlags());
                }
            }
        }

        [Fact]
        // macOS (10.14) will not load certificates with NumericString in their subject
        // if the 0x12 (NumericString) is changed to 0x13 (PrintableString) then the cert
        // import doesn't fail.
        [PlatformSpecific(~TestPlatforms.OSX)]
        public static void VerifyNumericStringSubject()
        {
            X500DistinguishedName dn = new X500DistinguishedName(
                "30283117301506052901020203120C313233203635342037383930310D300B0603550403130454657374".HexToByteArray());

            using (RSA key = RSA.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    dn,
                    key,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = req.CreateSelfSigned(now.AddDays(-1), now.AddDays(1)))
                {
                    Assert.Equal("CN=Test, OID.1.1.1.2.2.3=123 654 7890", cert.Subject);
                }
            }
        }

        [Theory]
        // Test with intermediate certificates in CustomTrustStore
        [InlineData(true, X509ChainStatusFlags.NoError)]
        // Test with ExtraStore containing root certificate
        [InlineData(false, X509ChainStatusFlags.UntrustedRoot)]
        public static void CustomRootTrustDoesNotTrustIntermediates(
            bool saveAllInCustomTrustStore,
            X509ChainStatusFlags chainFlags)
        {
            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert);

            using (endEntityCert)
            using (intermediateCert)
            using (rootCert)
            using (ChainHolder chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = endEntityCert.NotBefore.AddSeconds(1);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(intermediateCert);

                if (saveAllInCustomTrustStore)
                {
                    chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                }
                else
                {
                    chain.ChainPolicy.ExtraStore.Add(rootCert);
                }

                Assert.Equal(saveAllInCustomTrustStore, chain.Build(endEntityCert));
                Assert.Equal(3, chain.ChainElements.Count);
                Assert.Equal(chainFlags, chain.AllStatusFlags());
            }
        }

        [Fact]
        public static void CustomTrustModeWithNoCustomTrustCerts()
        {
            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert);

            using (endEntityCert)
            using (intermediateCert)
            using (rootCert)
            using (ChainHolder chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = endEntityCert.NotBefore.AddSeconds(1);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                Assert.False(chain.Build(endEntityCert));
                Assert.Equal(1, chain.ChainElements.Count);
                Assert.Equal(X509ChainStatusFlags.PartialChain, chain.AllStatusFlags());
            }
        }

        private static X509Certificate2 TamperSignature(X509Certificate2 input)
        {
            byte[] cert = input.RawData;
            cert[cert.Length - 1] ^= 0xFF;
            return new X509Certificate2(cert);
        }
    }
}
