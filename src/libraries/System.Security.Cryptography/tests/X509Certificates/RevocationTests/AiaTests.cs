// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography.X509Certificates.Tests.Common;
using Microsoft.DotNet.RemoteExecutor;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.RevocationTests
{
    [SkipOnPlatform(TestPlatforms.Android, "Android does not support AIA fetching")]
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
    public static class AiaTests
    {
        [Fact]
        public static void EmptyAiaResponseIsIgnored()
        {
            CertificateAuthority.BuildPrivatePki(
                PkiOptions.AllRevocation,
                out RevocationResponder responder,
                out CertificateAuthority root,
                out CertificateAuthority[] intermediates,
                out X509Certificate2 endEntity,
                intermediateAuthorityCount: 2,
                pkiOptionsInSubject: false,
                testName: nameof(EmptyAiaResponseIsIgnored));

            using (responder)
            using (root)
            using (CertificateAuthority intermediate1 = intermediates[0])
            using (CertificateAuthority intermediate2 = intermediates[1])
            using (endEntity)
            using (X509Certificate2 intermediate2Cert = intermediate2.CloneIssuerCert())
            {
                responder.RespondKind = RespondKind.Empty;

                RetryHelper.Execute(() => {
                    using (ChainHolder holder = new ChainHolder())
                    {
                        X509Chain chain = holder.Chain;
                        chain.ChainPolicy.ExtraStore.Add(intermediate2Cert);
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        chain.ChainPolicy.VerificationTime = endEntity.NotBefore.AddMinutes(1);
                        chain.ChainPolicy.UrlRetrievalTimeout = DynamicRevocationTests.s_urlRetrievalLimit;
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                        Assert.False(chain.Build(endEntity));
                        X509ChainStatusFlags chainFlags = chain.AllStatusFlags();
                        Assert.True(chainFlags.HasFlag(X509ChainStatusFlags.PartialChain), $"expected partial chain flags, got {chainFlags}");
                        Assert.Equal(2, chain.ChainElements.Count);
                    }
                });
            }
        }

        [Theory]
        [InlineData(AiaResponseKind.Pkcs12, true)]
        [InlineData(AiaResponseKind.Cert, false)]
        public static void AiaAcceptsCertTypesAndIgnoresNonCertTypes(AiaResponseKind aiaResponseKind, bool mustIgnore)
        {
            CertificateAuthority.BuildPrivatePki(
                PkiOptions.AllRevocation,
                out RevocationResponder responder,
                out CertificateAuthority root,
                out CertificateAuthority intermediate,
                out X509Certificate2 endEntity,
                pkiOptionsInSubject: false,
                testName: Guid.NewGuid().ToString());

            using (responder)
            using (root)
            using (intermediate)
            using (endEntity)
            using (X509Certificate2 rootCert = root.CloneIssuerCert())
            {
                responder.AiaResponseKind = aiaResponseKind;

                RetryHelper.Execute(() => {
                    using (ChainHolder holder = new ChainHolder())
                    {
                        X509Chain chain = holder.Chain;
                        chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        chain.ChainPolicy.VerificationTime = endEntity.NotBefore.AddMinutes(1);
                        chain.ChainPolicy.UrlRetrievalTimeout = DynamicRevocationTests.s_urlRetrievalLimit;

                        Assert.NotEqual(mustIgnore, chain.Build(endEntity));
                    }
                });
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "CA store is not available")]
        public static void DisableAiaOptionWorks()
        {
            CertificateAuthority.BuildPrivatePki(
                PkiOptions.AllRevocation,
                out RevocationResponder responder,
                out CertificateAuthority root,
                out CertificateAuthority intermediate,
                out X509Certificate2 endEntity,
                pkiOptionsInSubject: false,
                testName: nameof(DisableAiaOptionWorks));

            using (responder)
            using (root)
            using (intermediate)
            using (endEntity)
            using (X509Certificate2 rootCert = root.CloneIssuerCert())
            using (X509Certificate2 intermediateCert = intermediate.CloneIssuerCert())
            {
                RetryHelper.Execute(() => {
                    using (ChainHolder holder = new ChainHolder())
                    using (var cuCaStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser))
                    {
                        cuCaStore.Open(OpenFlags.ReadWrite);

                        X509Chain chain = holder.Chain;

                        // macOS combines revocation and AIA fetching in to a single flag. Both need to be disabled
                        // to prevent AIA fetches.
                        if (PlatformDetection.IsOSX)
                        {
                            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        }

                        chain.ChainPolicy.DisableCertificateDownloads = true;
                        chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        chain.ChainPolicy.VerificationTime = endEntity.NotBefore.AddMinutes(1);
                        chain.ChainPolicy.UrlRetrievalTimeout = DynamicRevocationTests.s_urlRetrievalLimit;

                        Assert.False(chain.Build(endEntity), "Chain build with no intermediate, AIA disabled");

                        // If a previous run of this test leaves contamination in the CU\CA store on Windows
                        // the Windows chain engine will match the bad issuer and report NotSignatureValid instead
                        // of PartialChain.
                        X509ChainStatusFlags chainFlags = chain.AllStatusFlags();

                        if (chainFlags.HasFlag(X509ChainStatusFlags.NotSignatureValid))
                        {
                            Assert.Equal(3, chain.ChainElements.Count);

                            foreach (X509Certificate2 storeCert in cuCaStore.Certificates)
                            {
                                if (storeCert.Subject.Equals(intermediateCert.Subject))
                                {
                                    cuCaStore.Remove(storeCert);
                                }

                                storeCert.Dispose();
                            }

                            holder.DisposeChainElements();

                            // Try again, with no caching side effect.
                            Assert.False(chain.Build(endEntity), "Chain build 2 with no intermediate, AIA disabled");
                        }

                        Assert.Equal(1, chain.ChainElements.Count);
                        Assert.Contains(X509ChainStatusFlags.PartialChain, chain.ChainStatus.Select(s => s.Status));
                        holder.DisposeChainElements();

                        chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                        Assert.True(chain.Build(endEntity), "Chain build with intermediate, AIA disabled");
                        Assert.Equal(3, chain.ChainElements.Count);
                        Assert.Equal(X509ChainStatusFlags.NoError, chain.AllStatusFlags());
                        holder.DisposeChainElements();

                        chain.ChainPolicy.DisableCertificateDownloads = false;
                        chain.ChainPolicy.ExtraStore.Clear();
                        Assert.True(chain.Build(endEntity), "Chain build with no intermediate, AIA enabled");
                        Assert.Equal(3, chain.ChainElements.Count);
                        Assert.Equal(X509ChainStatusFlags.NoError, chain.AllStatusFlags());

                        cuCaStore.Remove(intermediateCert);
                    }
                });
            }
        }

        [PlatformSpecific(TestPlatforms.Linux)]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void AiaIgnoresCertOverLimit()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppContext.SetData("System.Security.Cryptography.AiaDownloadLimit", 100);
                CertificateAuthority.BuildPrivatePki(
                    PkiOptions.AllRevocation,
                    out RevocationResponder responder,
                    out CertificateAuthority root,
                    out CertificateAuthority intermediate,
                    out X509Certificate2 endEntity,
                    pkiOptionsInSubject: false,
                    testName: Guid.NewGuid().ToString());
                using (responder)
                using (root)
                using (intermediate)
                using (endEntity)
                using (X509Certificate2 rootCert = root.CloneIssuerCert())
                {
                    responder.AiaResponseKind = AiaResponseKind.Cert;
                    using (ChainHolder holder = new ChainHolder())
                    {
                        X509Chain chain = holder.Chain;
                        chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        chain.ChainPolicy.VerificationTime = endEntity.NotBefore.AddMinutes(1);
                        chain.ChainPolicy.UrlRetrievalTimeout = DynamicRevocationTests.s_urlRetrievalLimit;
                        Assert.False(chain.Build(endEntity));
                    }
                }
            }).Dispose();
        }

        [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.Windows)]
        [Fact]
        public static void AiaCompletionHasLimits()
        {
            const int IntermediateCount = 8;
            int iteration = 0;

            RetryHelper.Execute(
                () =>
                {
                    iteration++;

                    CertificateAuthority.BuildPrivatePki(
                        PkiOptions.AllRevocation,
                        out RevocationResponder responder,
                        out CertificateAuthority root,
                        out CertificateAuthority[] intermediates,
                        out X509Certificate2 endEntity,
                        intermediateAuthorityCount: IntermediateCount,
                        pkiOptionsInSubject: false,
                        testName: $"{nameof(AiaCompletionHasLimits)}_{iteration}");

                    using (responder)
                    using (root)
                    using (endEntity)
                    {
                        try
                        {
                            using (ChainHolder holder = new ChainHolder())
                            {
                                // This test shows that we only download two certificates at a time.
                                // This is a Windows black-box behavior that we're matching.
                                // White-box suggests it's supposed to be 3, but maybe there's an off-by-one.
                                // Windows also allow registry customization... but this test can't tolerate it.

                                try
                                {
                                    X509Chain chain = holder.Chain;
                                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                                    chain.ChainPolicy.VerificationFlags |=
                                        X509VerificationFlags.AllowUnknownCertificateAuthority;
                                    chain.ChainPolicy.VerificationTime = endEntity.NotBefore.AddMinutes(1);
                                    chain.ChainPolicy.UrlRetrievalTimeout = DynamicRevocationTests.s_urlRetrievalLimit;
                                    chain.ChainPolicy.ExtraStore.Add(intermediates[^2].CloneIssuerCert());

                                    // EE, intermediate0 (AIA), intermediate1 (ExtraStore), intermediate2 (AIA).
                                    AssertExtensions.TrueExpression(chain.Build(endEntity));

                                    // Current Windows only allows 2, by black box testing, but 3 does seem to happen
                                    // on some builds.  So, variant test for it being over-sized
                                    if (chain.ChainElements.Count == 5)
                                    {
                                        // The ones described above, plus intermediate3(AIA).
                                        AssertExtensions.HasFlag(X509ChainStatusFlags.PartialChain, chain.AllStatusFlags());

                                        CloneIntoExtraStore(chain, 1);
                                        CloneIntoExtraStore(chain, 3);
                                        CloneIntoExtraStore(chain, 4);
                                        holder.DisposeChainElements();

                                        // Previous 5 plus intermediate4, intermediate5, and intermediate6.
                                        AssertExtensions.TrueExpression(chain.Build(endEntity));
                                        Assert.Equal(8, chain.ChainElements.Count);
                                        AssertExtensions.HasFlag(X509ChainStatusFlags.PartialChain, chain.AllStatusFlags());

                                        CloneIntoExtraStore(chain, 5);
                                        CloneIntoExtraStore(chain, 6);
                                        CloneIntoExtraStore(chain, 7);
                                        holder.DisposeChainElements();
                                    }
                                    else
                                    {
                                        Assert.Equal(4, chain.ChainElements.Count);
                                        AssertExtensions.HasFlag(X509ChainStatusFlags.PartialChain, chain.AllStatusFlags());

                                        CloneIntoExtraStore(chain, 1);
                                        CloneIntoExtraStore(chain, 3);
                                        holder.DisposeChainElements();

                                        // Previous 4 plus intermediate3 and intermediate4.
                                        AssertExtensions.TrueExpression(chain.Build(endEntity));
                                        Assert.Equal(6, chain.ChainElements.Count);
                                        AssertExtensions.HasFlag(X509ChainStatusFlags.PartialChain, chain.AllStatusFlags());

                                        CloneIntoExtraStore(chain, 4);
                                        CloneIntoExtraStore(chain, 5);
                                        holder.DisposeChainElements();

                                        // Previous 6 plus intermediate5 and intermediate6.
                                        AssertExtensions.TrueExpression(chain.Build(endEntity));
                                        Assert.Equal(8, chain.ChainElements.Count);
                                        AssertExtensions.HasFlag(X509ChainStatusFlags.PartialChain, chain.AllStatusFlags());

                                        CloneIntoExtraStore(chain, 6);
                                        CloneIntoExtraStore(chain, 7);
                                        holder.DisposeChainElements();
                                    }

                                    // AIA fetches intermediate7 and root, chain finishes.
                                    AssertExtensions.TrueExpression(chain.Build(endEntity));
                                    Assert.Equal(10, chain.ChainElements.Count);
                                    Assert.Equal(X509ChainStatusFlags.UntrustedRoot, chain.AllStatusFlags());
                                }
                                finally
                                {
                                    foreach (X509Certificate2 cert in holder.Chain.ChainPolicy.ExtraStore)
                                    {
                                        cert.Dispose();
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (intermediates is not null)
                            {
                                foreach (CertificateAuthority intermediate in intermediates)
                                {
                                    intermediate.Dispose();
                                }
                            }
                        }
                    }
                });

            static void CloneIntoExtraStore(X509Chain chain, int index)
            {
                ReadOnlySpan<byte> source = chain.ChainElements[index].Certificate.RawDataMemory.Span;
                X509Certificate2 cert = X509CertificateLoader.LoadCertificate(source);
                chain.ChainPolicy.ExtraStore.Add(cert);
            }
        }
    }
}
