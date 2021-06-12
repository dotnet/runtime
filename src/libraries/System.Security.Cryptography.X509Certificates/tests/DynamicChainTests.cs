// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class DynamicChainTests
    {
        private static X509Extension BasicConstraintsCA => new X509BasicConstraintsExtension(
            certificateAuthority: true,
            hasPathLengthConstraint: false,
            pathLengthConstraint: 0,
            critical: true);

        private static X509Extension BasicConstraintsEndEntity => new X509BasicConstraintsExtension(
            certificateAuthority: false,
            hasPathLengthConstraint: false,
            pathLengthConstraint: 0,
            critical: true);

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
            string testName = $"{nameof(BuildInvalidSignatureTwice)} {endEntityErrors} {intermediateErrors} {rootErrors}";
            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert,
                testName: testName);

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

            if (PlatformDetection.UsesAppleCrypto)
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
                    if (PlatformDetection.UsesAppleCrypto)
                    {
                        rootErrors &= ~X509ChainStatusFlags.UntrustedRoot;
                        rootErrors |= X509ChainStatusFlags.PartialChain;
                    }
                }
            }
            else if (OperatingSystem.IsAndroid())
            {
                // Android always validates signature as part of building a path,
                // so invalid signature comes back as PartialChain with no elements
                expectedCount = 0;
                endEntityErrors = X509ChainStatusFlags.PartialChain;
                intermediateErrors = X509ChainStatusFlags.PartialChain;
                rootErrors = X509ChainStatusFlags.PartialChain;
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

            bool expectSuccess;
            if (PlatformDetection.IsAndroid)
            {
                // Android always validates signature as part of building a path, so chain
                // building is expected to fail
                expectSuccess = false;
            }
            else
            {
                // If PartialChain or UntrustedRoot are the only remaining errors, the chain will succeed.
                const X509ChainStatusFlags SuccessCodes =
                    X509ChainStatusFlags.UntrustedRoot | X509ChainStatusFlags.PartialChain;

                expectSuccess = (expectedAllErrors & ~SuccessCodes) == 0;
            }

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

                // Android doesn't respect AllowUnknownCertificateAuthority
                if (!PlatformDetection.IsAndroid)
                {
                    chain.ChainPolicy.VerificationFlags |=
                        X509VerificationFlags.AllowUnknownCertificateAuthority;
                }

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

                    if (expectedCount > 0)
                    {
                        Assert.Equal(endEntityErrors, chain.ChainElements[0].AllStatusFlags());
                    }

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
            X509Extension[] rootExtensions = new []
            {
                new X509BasicConstraintsExtension(
                    certificateAuthority: true,
                    hasPathLengthConstraint: true,
                    pathLengthConstraint: 0,
                    critical: true),
            };

            X509Extension[] intermediateExtensions = new []
            {
                new X509BasicConstraintsExtension(
                    certificateAuthority: true,
                    hasPathLengthConstraint: true,
                    pathLengthConstraint: 0,
                    critical: true),
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
                Assert.Equal(PlatformBasicConstraints(X509ChainStatusFlags.InvalidBasicConstraints), chain.AllStatusFlags());
            }
        }

        [Fact]
        public static void BasicConstraints_ViolatesCaFalse()
        {
            X509Extension[] intermediateExtensions = new []
            {
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
            {
                TestChain3(
                    rootCert,
                    intermediateCert,
                    endEntityCert,
                    expectedFlags: PlatformBasicConstraints(X509ChainStatusFlags.InvalidBasicConstraints));
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

                    if (PlatformDetection.IsAndroid)
                    {
                        // Android always unsupported critical extensions as part of building a path,
                        // so errors comes back as PartialChain with no elements
                        Assert.Equal(X509ChainStatusFlags.PartialChain, chain.AllStatusFlags());
                        Assert.Equal(0, chain.ChainElements.Count);
                    }
                    else
                    {
                        X509ChainElement certElement = chain.ChainElements.Single();
                        const X509ChainStatusFlags ExpectedFlag = X509ChainStatusFlags.HasNotSupportedCriticalExtension;
                        X509ChainStatusFlags actualFlags = certElement.AllStatusFlags();
                        Assert.True((actualFlags & ExpectedFlag) == ExpectedFlag, $"Has expected flag {ExpectedFlag} but was {actualFlags}");
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Android, "Android does not support AIA fetching")]
        public static void TestInvalidAia()
        {
            using (RSA key = RSA.Create())
            {
                CertificateRequest rootReq = new CertificateRequest(
                    "CN=Root",
                    key,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                rootReq.CertificateExtensions.Add(BasicConstraintsCA);

                CertificateRequest certReq = new CertificateRequest(
                    "CN=test",
                    key,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                certReq.CertificateExtensions.Add(BasicConstraintsEndEntity);

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
        [SkipOnPlatform(TestPlatforms.OSX, "Not supported on OSX.")]
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
            string testName = $"{nameof(CustomRootTrustDoesNotTrustIntermediates)} {saveAllInCustomTrustStore} {chainFlags}";
            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert,
                testName: testName);

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

                if (PlatformDetection.IsAndroid && !saveAllInCustomTrustStore)
                {
                    // Android does not support an empty custom root trust
                    // Only self-issued certs are treated as trusted anchors, so building the chain
                    // should through PNSE even though the intermediate cert is added to the store
                    Assert.Throws<PlatformNotSupportedException>(() => chain.Build(endEntityCert));
                }
                else
                {
                    Assert.Equal(saveAllInCustomTrustStore, chain.Build(endEntityCert));
                    Assert.Equal(3, chain.ChainElements.Count);
                    Assert.Equal(chainFlags, chain.AllStatusFlags());
                }
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

                if (PlatformDetection.IsAndroid)
                {
                    // Android does not support an empty custom root trust
                    Assert.Throws<PlatformNotSupportedException>(() => chain.Build(endEntityCert));
                }
                else
                {
                    Assert.False(chain.Build(endEntityCert));
                    Assert.Equal(1, chain.ChainElements.Count);
                    Assert.Equal(X509ChainStatusFlags.PartialChain, chain.AllStatusFlags());
                }
            }
        }

        [Fact]
        public static void NameConstraintViolation_PermittedTree_Dns()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
            builder.AddDnsName("microsoft.com");

            // permitted DNS name constraint for .example.com
            string nameConstraints = "3012A010300E820C2E6578616D706C652E636F6D";

            TestNameConstrainedChain(nameConstraints, builder, (bool result, X509Chain chain) => {
                Assert.False(result, "chain.Build");
                Assert.Equal(PlatformNameConstraints(X509ChainStatusFlags.HasNotPermittedNameConstraint), chain.AllStatusFlags());
            });
        }

        [Fact]
        public static void NameConstraintViolation_ExcludedTree_Dns()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
            builder.AddDnsName("www.example.com");

            // excluded DNS name constraint for .example.com.
            string nameConstraints = "3012A110300E820C2E6578616D706C652E636F6D";
            if (PlatformDetection.IsAndroid)
            {
                // Android does not consider the constraint as being violated when it has
                // the leading period. It checks expects the period as part of the left-side
                // labels and not the constraint when doing validation.
                // Use an excluded DNS name constraint without the period: example.com
                nameConstraints = "3011A10F300D820B6578616D706C652E636F6D";
            }

            TestNameConstrainedChain(nameConstraints, builder, (bool result, X509Chain chain) => {
                Assert.False(result, "chain.Build");
                Assert.Equal(PlatformNameConstraints(X509ChainStatusFlags.HasExcludedNameConstraint), chain.AllStatusFlags());
            });
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.AppleCrypto, "macOS appears to just completely ignore min/max.")]
        public static void NameConstraintViolation_PermittedTree_HasMin()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
            builder.AddDnsName("example.com");

            // permitted DNS name constraint for example.com with a MIN of 9.
            string nameConstraints = "3015A0133011820C2E6578616D706C652E636F6D800109";

            TestNameConstrainedChain(nameConstraints, builder, (bool result, X509Chain chain) => {
                Assert.False(result, "chain.Build");
                Assert.Equal(PlatformNameConstraints(X509ChainStatusFlags.HasNotSupportedNameConstraint), chain.AllStatusFlags());
            });
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "Windows seems to skip over nonsense GeneralNames.")]
        [SkipOnPlatform(TestPlatforms.Android, "Android will check for a match. Since the permitted name does match the subject alt name, it succeeds.")]
        public static void NameConstraintViolation_InvalidGeneralNames()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
            builder.AddEmailAddress("///");

            // permitted RFC822 name constraint with GeneralName of ///.
            string nameConstraints = "3009A007300581032F2F2F";

            TestNameConstrainedChain(nameConstraints, builder, (bool result, X509Chain chain) => {
                Assert.False(result, "chain.Build");
                Assert.Equal(PlatformNameConstraints(X509ChainStatusFlags.InvalidNameConstraints), chain.AllStatusFlags());
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52976", TestPlatforms.Android)]
        public static void MismatchKeyIdentifiers()
        {
            X509Extension[] intermediateExtensions = new []
            {
                BasicConstraintsCA,
                new X509Extension(
                    "2.5.29.14",
                    "0414C7AC28EFB300F46F9406ED155628A123633E556F".HexToByteArray(),
                    critical: false),
            };

            X509Extension[] endEntityExtensions = new []
            {
                BasicConstraintsEndEntity,
                new X509Extension(
                    "2.5.29.35",
                    "30168014A84A6A63047DDDBAE6D139B7A64565EFF3A8ECA1".HexToByteArray(),
                    critical: false),
            };

            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert,
                intermediateExtensions: intermediateExtensions,
                endEntityExtensions: endEntityExtensions);

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

                if (OperatingSystem.IsLinux())
                {
                    Assert.False(chain.Build(endEntityCert), "chain.Build");
                    Assert.Equal(X509ChainStatusFlags.PartialChain, chain.AllStatusFlags());
                }
                else
                {
                    Assert.True(chain.Build(endEntityCert), "chain.Build");
                    Assert.Equal(3, chain.ChainElements.Count);
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Linux, "Not supported on Linux.")]
        public static void PolicyConstraints_RequireExplicitPolicy()
        {
            X509Extension[] intermediateExtensions = new []
            {
                BasicConstraintsCA,
                BuildPolicyConstraints(requireExplicitPolicySkipCerts: 0),
            };

            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert,
                intermediateExtensions: intermediateExtensions);

            using (endEntityCert)
            using (intermediateCert)
            using (rootCert)
            {
                TestChain3(
                    rootCert,
                    intermediateCert,
                    endEntityCert,
                    expectedFlags: PlatformPolicyConstraints(X509ChainStatusFlags.NoIssuanceChainPolicy));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void PolicyConstraints_Malformed()
        {
            X509Extension[] intermediateExtensions = new []
            {
                BasicConstraintsCA,
                // Nonsense ContextSpecific 3.
                new X509Extension("2.5.29.36", "3003830102".HexToByteArray(), critical: true),
            };

            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert,
                intermediateExtensions: intermediateExtensions);

            using (endEntityCert)
            using (intermediateCert)
            using (rootCert)
            {
                TestChain3(
                    rootCert,
                    intermediateCert,
                    endEntityCert,
                    expectedFlags: X509ChainStatusFlags.InvalidPolicyConstraints);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Linux, "Not supported on Linux.")]
        public static void PolicyConstraints_Valid()
        {
            X509Extension[] intermediateExtensions = new []
            {
                BasicConstraintsCA,
                BuildPolicyConstraints(requireExplicitPolicySkipCerts: 0),
                BuildPolicyByIdentifiers("2.23.140.1.2.1"), // CABF DV OID
            };
            X509Extension[] endEntityExtensions = new []
            {
                BasicConstraintsEndEntity,
                BuildPolicyByIdentifiers("2.23.140.1.2.1"), // CABF DV OID
            };

            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert,
                intermediateExtensions: intermediateExtensions,
                endEntityExtensions: endEntityExtensions);

            using (endEntityCert)
            using (intermediateCert)
            using (rootCert)
            {
                TestChain3(rootCert, intermediateCert, endEntityCert);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Linux, "Not supported on Linux.")]
        public static void PolicyConstraints_Mismatch()
        {
            X509Extension[] intermediateExtensions = new []
            {
                BasicConstraintsCA,
                BuildPolicyConstraints(requireExplicitPolicySkipCerts: 0),
                BuildPolicyByIdentifiers("2.23.140.1.2.1"), // CABF DV OID
            };
            X509Extension[] endEntityExtensions = new []
            {
                BasicConstraintsEndEntity,
                BuildPolicyByIdentifiers("1.2.3.4"),
            };

            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert,
                intermediateExtensions: intermediateExtensions,
                endEntityExtensions: endEntityExtensions);

            using (endEntityCert)
            using (intermediateCert)
            using (rootCert)
            {
                TestChain3(
                    rootCert,
                    intermediateCert,
                    endEntityCert,
                    expectedFlags: PlatformPolicyConstraints(X509ChainStatusFlags.NoIssuanceChainPolicy));
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Linux, "Not supported on Linux.")]
        public static void PolicyConstraints_AnyPolicy()
        {
            X509Extension[] intermediateExtensions = new []
            {
                BasicConstraintsCA,
                BuildPolicyConstraints(requireExplicitPolicySkipCerts: 0),
                BuildPolicyByIdentifiers("2.5.29.32.0"), // anyPolicy special OID.
            };
            X509Extension[] endEntityExtensions = new []
            {
                BasicConstraintsEndEntity,
                BuildPolicyByIdentifiers("1.2.3.4"),
            };

            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert,
                intermediateExtensions: intermediateExtensions,
                endEntityExtensions: endEntityExtensions);

            using (endEntityCert)
            using (intermediateCert)
            using (rootCert)
            {
                TestChain3(rootCert, intermediateCert, endEntityCert);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Linux, "Not supported on Linux.")]
        public static void PolicyConstraints_Mapped()
        {
            X509Extension[] intermediateExtensions = new []
            {
                BasicConstraintsCA,
                BuildPolicyConstraints(requireExplicitPolicySkipCerts: 0),
                BuildPolicyByIdentifiers("2.23.140.1.2.1"),
                BuildPolicyMappings(("2.23.140.1.2.1", "1.2.3.4")),
            };
            X509Extension[] endEntityExtensions = new []
            {
                BasicConstraintsEndEntity,
                BuildPolicyByIdentifiers("1.2.3.4"),
            };

            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert,
                intermediateExtensions: intermediateExtensions,
                endEntityExtensions: endEntityExtensions);

            using (endEntityCert)
            using (intermediateCert)
            using (rootCert)
            {
                TestChain3(rootCert, intermediateCert, endEntityCert);
            }
        }

        private static X509ChainStatusFlags PlatformBasicConstraints(X509ChainStatusFlags flags)
        {
            if (OperatingSystem.IsAndroid())
            {
                // Android always validates basic constraints as part of building a path
                // so violations comes back as PartialChain with no elements.
                flags = X509ChainStatusFlags.PartialChain;
            }

            return flags;
        }

        private static X509ChainStatusFlags PlatformNameConstraints(X509ChainStatusFlags flags)
        {
            if (PlatformDetection.UsesAppleCrypto)
            {
                const X509ChainStatusFlags AnyNameConstraintFlags =
                    X509ChainStatusFlags.HasExcludedNameConstraint |
                    X509ChainStatusFlags.HasNotDefinedNameConstraint |
                    X509ChainStatusFlags.HasNotPermittedNameConstraint |
                    X509ChainStatusFlags.HasNotSupportedNameConstraint |
                    X509ChainStatusFlags.InvalidNameConstraints;

                if ((flags & AnyNameConstraintFlags) != 0)
                {
                    flags &= ~AnyNameConstraintFlags;
                    flags |= X509ChainStatusFlags.InvalidNameConstraints;
                }
            }
            else if (OperatingSystem.IsAndroid())
            {
                // Android always validates name constraints as part of building a path
                // so violations comes back as PartialChain with no elements.
                flags = X509ChainStatusFlags.PartialChain;
            }

            return flags;
        }

        private static X509ChainStatusFlags PlatformPolicyConstraints(X509ChainStatusFlags flags)
        {
            if (PlatformDetection.UsesAppleCrypto)
            {
                const X509ChainStatusFlags AnyPolicyConstraintFlags =
                    X509ChainStatusFlags.NoIssuanceChainPolicy;

                if ((flags & AnyPolicyConstraintFlags) != 0)
                {
                    flags &= ~AnyPolicyConstraintFlags;
                    flags |= X509ChainStatusFlags.InvalidPolicyConstraints;
                }
            }
            else if (OperatingSystem.IsAndroid())
            {
                // Android always validates policy constraints as part of building a path
                // so violations comes back as PartialChain with no elements.
                flags = X509ChainStatusFlags.PartialChain;
            }

            return flags;
        }

        private static void TestNameConstrainedChain(
            string intermediateNameConstraints,
            SubjectAlternativeNameBuilder endEntitySanBuilder,
            Action<bool, X509Chain> body,
            [CallerMemberName] string testName = null)
        {
            X509Extension[] endEntityExtensions = new []
            {
                new X509BasicConstraintsExtension(
                    certificateAuthority: false,
                    hasPathLengthConstraint: false,
                    pathLengthConstraint: 0,
                    critical: true),
                endEntitySanBuilder.Build(),
            };

            X509Extension[] intermediateExtensions = new []
            {
                new X509BasicConstraintsExtension(
                    certificateAuthority: true,
                    hasPathLengthConstraint: false,
                    pathLengthConstraint: 0,
                    critical: true),
                new X509Extension(
                    "2.5.29.30",
                    intermediateNameConstraints.HexToByteArray(),
                    critical: true),
            };

            TestDataGenerator.MakeTestChain3(
                out X509Certificate2 endEntityCert,
                out X509Certificate2 intermediateCert,
                out X509Certificate2 rootCert,
                intermediateExtensions: intermediateExtensions,
                endEntityExtensions: endEntityExtensions,
                testName: testName);

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

                bool result = chain.Build(endEntityCert);
                body(result, chain);
            }
        }

        private static X509Certificate2 TamperSignature(X509Certificate2 input)
        {
            byte[] cert = input.RawData;
            cert[cert.Length - 1] ^= 0xFF;
            return new X509Certificate2(cert);
        }

        private static X509Extension BuildPolicyConstraints(
            int? requireExplicitPolicySkipCerts = null,
            int? inhibitPolicyMappingSkipCerts = null)
        {
            // RFC 5280 4.2.1.11
            //    id-ce-policyConstraints OBJECT IDENTIFIER ::=  { id-ce 36 }
            //    PolicyConstraints ::= SEQUENCE {
            //         requireExplicitPolicy           [0] SkipCerts OPTIONAL,
            //         inhibitPolicyMapping            [1] SkipCerts OPTIONAL }
            //    SkipCerts ::= INTEGER (0..MAX)
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSequence())
            {
                if (requireExplicitPolicySkipCerts.HasValue)
                {
                    Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 0);
                    writer.WriteInteger(requireExplicitPolicySkipCerts.Value, tag);
                }

                if (inhibitPolicyMappingSkipCerts.HasValue)
                {
                    Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 1);
                    writer.WriteInteger(inhibitPolicyMappingSkipCerts.Value, tag);
                }
            }

            // Conforming CAs MUST mark this extension as critical.
            return new X509Extension("2.5.29.36", writer.Encode(), critical: true);
        }

        private static X509Extension BuildPolicyByIdentifiers(params string[] policyOids)
        {
            // id-ce-certificatePolicies OBJECT IDENTIFIER ::=  { id-ce 32 }

            // anyPolicy OBJECT IDENTIFIER ::= { id-ce-certificatePolicies 0 }

            // CertificatePolicies ::= SEQUENCE SIZE (1..MAX) OF PolicyInformation

            // PolicyInformation ::= SEQUENCE {
            //      policyIdentifier   CertPolicyId,
            //      policyQualifiers   SEQUENCE SIZE (1..MAX) OF
            //              PolicyQualifierInfo OPTIONAL }

            // CertPolicyId ::= OBJECT IDENTIFIER
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSequence()) //CertificatePolicies
            {
                foreach (string policyOid in policyOids)
                {
                    using (writer.PushSequence()) // PolicyInformation
                    {
                        writer.WriteObjectIdentifier(policyOid);
                    }
                }
            }

            return new X509Extension("2.5.29.32", writer.Encode(), critical: false);
        }

        private static X509Extension BuildPolicyMappings(
            params (string IssuerDomainPolicy, string SubjectDomainPolicy)[] policyMappings)
        {
            //    PolicyMappings ::= SEQUENCE SIZE (1..MAX) OF SEQUENCE {
            //         issuerDomainPolicy      CertPolicyId,
            //         subjectDomainPolicy     CertPolicyId }
            //    CertPolicyId ::= OBJECT IDENTIFIER

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSequence())
            {
                foreach ((string issuerDomainPolicy, string subjectDomainPolicy) in policyMappings)
                {
                    using (writer.PushSequence())
                    {
                        writer.WriteObjectIdentifier(issuerDomainPolicy);
                        writer.WriteObjectIdentifier(subjectDomainPolicy);
                    }
                }
            }

            return new X509Extension("2.5.29.33", writer.Encode(), critical: true);
        }

        private static void TestChain3(
            X509Certificate2 rootCertificate,
            X509Certificate2 intermediateCertificate,
            X509Certificate2 endEntityCertificate,
            X509ChainStatusFlags expectedFlags = X509ChainStatusFlags.NoError)
        {
            using (ChainHolder chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = endEntityCertificate.NotBefore.AddSeconds(1);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(rootCertificate);
                chain.ChainPolicy.ExtraStore.Add(intermediateCertificate);

                bool result = chain.Build(endEntityCertificate);
                X509ChainStatusFlags actualFlags = chain.AllStatusFlags();
                Assert.True(result == (expectedFlags == X509ChainStatusFlags.NoError), $"chain.Build ({actualFlags})");

                Assert.True(
                    actualFlags.HasFlag(expectedFlags),
                    $"Expected Flags: \"{expectedFlags}\"; Actual Flags: \"{actualFlags}\"");
            }
        }
    }
}
