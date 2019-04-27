// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
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

                    // On 10.12 this is just UntrustedRoot.
                    // On 10.13+ it becomes PartialChain, and UntrustedRoot goes away.
                    if (PlatformDetection.IsMacOsHighSierraOrHigher)
                    {
                        rootErrors &= ~X509ChainStatusFlags.UntrustedRoot;
                        rootErrors |= X509ChainStatusFlags.PartialChain;
                    }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                    Assert.False(chain.Build(ee));
                    Assert.Equal(1, chain.ChainElements.Count);
                }
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
