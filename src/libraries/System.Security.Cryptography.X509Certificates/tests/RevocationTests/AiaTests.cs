// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography.X509Certificates.Tests.Common;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.RevocationTests
{
    public static class AiaTests
    {
        [Fact]
        public static void EmptyAiaResponseIsIgnored()
        {
            CertificateAuthority.BuildPrivatePki(
                PkiOptions.AllRevocation,
                out RevocationResponder responder,
                out CertificateAuthority root,
                out CertificateAuthority intermediate,
                out X509Certificate2 endEntity,
                pkiOptionsInSubject: false);

            using (responder)
            using (root)
            using (intermediate)
            using (endEntity)
            using (ChainHolder holder = new ChainHolder())
            using (X509Certificate2 rootCert = root.CloneIssuerCert())
            using (X509Certificate2 intermediateCert = intermediate.CloneIssuerCert())
            {
                responder.RespondEmpty = true;

                X509Chain chain = holder.Chain;
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.VerificationTime = endEntity.NotBefore.AddMinutes(1);
                chain.ChainPolicy.UrlRetrievalTimeout = DynamicRevocationTests.s_urlRetrievalLimit;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                Assert.False(chain.Build(endEntity));
                Assert.True(chain.AllStatusFlags().HasFlag(X509ChainStatusFlags.PartialChain), "expected partial chain");
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/47492", TestPlatforms.OSX)]
        public static void DisableAiaOptionWorks()
        {
            CertificateAuthority.BuildPrivatePki(
                PkiOptions.AllRevocation,
                out RevocationResponder responder,
                out CertificateAuthority root,
                out CertificateAuthority intermediate,
                out X509Certificate2 endEntity,
                pkiOptionsInSubject: false);

            using (responder)
            using (root)
            using (intermediate)
            using (endEntity)
            using (ChainHolder holder = new ChainHolder())
            using (X509Certificate2 rootCert = root.CloneIssuerCert())
            using (X509Certificate2 intermediateCert = intermediate.CloneIssuerCert())
            using (var cuCaStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser))
            {
                cuCaStore.Open(OpenFlags.ReadWrite);

                X509Chain chain = holder.Chain;
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

                // macOS doesn't like our revocation responder, so disable revocation checks there.
                if (PlatformDetection.IsOSX)
                {
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                }

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
        }
    }
}
