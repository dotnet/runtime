// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography.X509Certificates.Tests.Common;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.RevocationTests
{
    [OuterLoop("These tests exercise timeout properties which take a lot of time.")]
    public static class TimeoutTests
    {
        [Theory]
        [InlineData(PkiOptions.OcspEverywhere)]
        [InlineData(PkiOptions.CrlEverywhere)]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public static void RevocationCheckingDelayed(PkiOptions pkiOptions)
        {
            RetryHelper.Execute(() => {
                CertificateAuthority.BuildPrivatePki(
                    pkiOptions,
                    out RevocationResponder responder,
                    out CertificateAuthority rootAuthority,
                    out CertificateAuthority intermediateAuthority,
                    out X509Certificate2 endEntityCert,
                    nameof(RevocationCheckingDelayed));

                using (responder)
                using (rootAuthority)
                using (intermediateAuthority)
                using (endEntityCert)
                using (ChainHolder holder = new ChainHolder())
                using (X509Certificate2 rootCert = rootAuthority.CloneIssuerCert())
                using (X509Certificate2 intermediateCert = intermediateAuthority.CloneIssuerCert())
                {
                    TimeSpan delay = TimeSpan.FromSeconds(8);

                    X509Chain chain = holder.Chain;
                    responder.ResponseDelay = delay;
                    responder.DelayedActions = DelayedActionsFlag.All;

                    // This needs to be greater than delay, but less than 2x delay to ensure
                    // that the time is a timeout for individual fetches, not a running total.
                    chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(15);
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                    chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;

                    chain.ChainPolicy.DisableCertificateDownloads = true;

                    Stopwatch watch = Stopwatch.StartNew();
                    Assert.True(chain.Build(endEntityCert),  $"chain.Build; Chain status: {chain.AllStatusFlags()}");
                    watch.Stop();

                    // There should be two network fetches, OCSP/CRL to intermediate to get leaf status,
                    // OCSP/CRL to root to get intermediate statuses. It should take at least 2x the delay
                    // plus other non-network time, so we can at least ensure it took as long as
                    // the delay for each fetch.
                    // We expect the chain to build in at least 16 seconds (2 * delay) since each fetch
                    // should take `delay` number of seconds, and there are two fetchs that need to be
                    // performed. We allow a small amount of leeway to account for differences between
                    // how long the the delay is performed and the stopwatch.
                    Assert.True(watch.Elapsed >= delay * 2 - TimeSpan.FromSeconds(1), $"watch.Elapsed: {watch.Elapsed}");
                }
            });
        }

        [Theory]
        [InlineData(PkiOptions.OcspEverywhere)]
        [InlineData(PkiOptions.CrlEverywhere)]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public static void RevocationCheckingTimeout(PkiOptions pkiOptions)
        {
            CertificateAuthority.BuildPrivatePki(
                pkiOptions,
                out RevocationResponder responder,
                out CertificateAuthority rootAuthority,
                out CertificateAuthority intermediateAuthority,
                out X509Certificate2 endEntityCert,
                nameof(RevocationCheckingTimeout));

            using (responder)
            using (rootAuthority)
            using (intermediateAuthority)
            using (endEntityCert)
            using (ChainHolder holder = new ChainHolder())
            using (X509Certificate2 rootCert = rootAuthority.CloneIssuerCert())
            using (X509Certificate2 intermediateCert = intermediateAuthority.CloneIssuerCert())
            {
                TimeSpan delay = TimeSpan.FromSeconds(3);

                X509Chain chain = holder.Chain;
                responder.ResponseDelay = delay;
                responder.DelayedActions = DelayedActionsFlag.All;

                chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(1);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;

                chain.ChainPolicy.DisableCertificateDownloads = true;

                Assert.False(chain.Build(endEntityCert), "chain.Build");

                const X509ChainStatusFlags ExpectedFlags =
                    X509ChainStatusFlags.RevocationStatusUnknown |
                    X509ChainStatusFlags.OfflineRevocation;

                X509ChainStatusFlags eeFlags = GetFlags(chain, endEntityCert.Thumbprint);
                X509ChainStatusFlags intermediateFlags = GetFlags(chain, intermediateCert.Thumbprint);
                Assert.Equal(ExpectedFlags, eeFlags);
                Assert.Equal(ExpectedFlags, intermediateFlags);
            }
        }

        [Theory]
        [InlineData(PkiOptions.OcspEverywhere)]
        [InlineData(PkiOptions.CrlEverywhere)]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public static void RevocationCheckingMaximum(PkiOptions pkiOptions)
        {
            // Windows 10 has a different maximum from previous versions of Windows.
            // We are primarily testing that Linux behavior matches some behavior of
            // Windows, so we won't test except on Windows 10.
            if (PlatformDetection.WindowsVersion < 10)
            {
                return;
            }

            CertificateAuthority.BuildPrivatePki(
                pkiOptions,
                out RevocationResponder responder,
                out CertificateAuthority rootAuthority,
                out CertificateAuthority intermediateAuthority,
                out X509Certificate2 endEntityCert,
                nameof(RevocationCheckingMaximum));

            using (responder)
            using (rootAuthority)
            using (intermediateAuthority)
            using (endEntityCert)
            using (ChainHolder holder = new ChainHolder())
            using (X509Certificate2 rootCert = rootAuthority.CloneIssuerCert())
            using (X509Certificate2 intermediateCert = intermediateAuthority.CloneIssuerCert())
            {
                TimeSpan delay = TimeSpan.FromMinutes(1.5);

                X509Chain chain = holder.Chain;
                responder.ResponseDelay = delay;
                responder.DelayedActions = DelayedActionsFlag.All;

                chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromMinutes(2);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

                chain.ChainPolicy.DisableCertificateDownloads = true;

                // Even though UrlRetrievalTimeout is more than the delay, it should
                // get clamped to 1 minute, and thus less than the actual delay.
                Assert.False(chain.Build(endEntityCert), "chain.Build");

                const X509ChainStatusFlags ExpectedFlags =
                    X509ChainStatusFlags.RevocationStatusUnknown |
                    X509ChainStatusFlags.OfflineRevocation;

                X509ChainStatusFlags eeFlags = GetFlags(chain, endEntityCert.Thumbprint);
                Assert.Equal(ExpectedFlags, eeFlags);
            }
        }

        [Theory]
        [InlineData(PkiOptions.OcspEverywhere)]
        [InlineData(PkiOptions.CrlEverywhere)]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public static void RevocationCheckingNegativeTimeout(PkiOptions pkiOptions)
        {
            RetryHelper.Execute(() => {
                CertificateAuthority.BuildPrivatePki(
                    pkiOptions,
                    out RevocationResponder responder,
                    out CertificateAuthority rootAuthority,
                    out CertificateAuthority intermediateAuthority,
                    out X509Certificate2 endEntityCert,
                    nameof(RevocationCheckingNegativeTimeout));

                using (responder)
                using (rootAuthority)
                using (intermediateAuthority)
                using (endEntityCert)
                using (ChainHolder holder = new ChainHolder())
                using (X509Certificate2 rootCert = rootAuthority.CloneIssuerCert())
                using (X509Certificate2 intermediateCert = intermediateAuthority.CloneIssuerCert())
                {
                    // Delay is more than the 15 second default.
                    TimeSpan delay = TimeSpan.FromSeconds(25);

                    X509Chain chain = holder.Chain;
                    responder.ResponseDelay = delay;
                    responder.DelayedActions = DelayedActionsFlag.All;

                    chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromMinutes(-1);
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                    chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

                    chain.ChainPolicy.DisableCertificateDownloads = true;

                    Assert.True(chain.Build(endEntityCert), $"chain.Build; Chain status: {chain.AllStatusFlags()}");
                }
            });
        }

        [Theory]
        [InlineData(DelayedActionsFlag.Ocsp)]
        [InlineData(DelayedActionsFlag.Crl)]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public static void RevocationCheckingTimeoutFallbackToOther(DelayedActionsFlag delayFlags)
        {
            RetryHelper.Execute(() => {
                CertificateAuthority.BuildPrivatePki(
                    PkiOptions.AllRevocation,
                    out RevocationResponder responder,
                    out CertificateAuthority rootAuthority,
                    out CertificateAuthority intermediateAuthority,
                    out X509Certificate2 endEntityCert,
                    nameof(RevocationCheckingTimeoutFallbackToOther));

                using (responder)
                using (rootAuthority)
                using (intermediateAuthority)
                using (endEntityCert)
                using (ChainHolder holder = new ChainHolder())
                using (X509Certificate2 rootCert = rootAuthority.CloneIssuerCert())
                using (X509Certificate2 intermediateCert = intermediateAuthority.CloneIssuerCert())
                {
                    X509Chain chain = holder.Chain;
                    responder.ResponseDelay = TimeSpan.FromSeconds(8);
                    responder.DelayedActions = delayFlags;

                    chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(4);
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                    chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

                    chain.ChainPolicy.DisableCertificateDownloads = true;

                    Assert.True(chain.Build(endEntityCert), $"chain.Build; Chain status: {chain.AllStatusFlags()}");
                }
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public static void AiaFetchDelayed()
        {
            RetryHelper.Execute(() => {
                CertificateAuthority.BuildPrivatePki(
                    PkiOptions.OcspEverywhere,
                    out RevocationResponder responder,
                    out CertificateAuthority rootAuthority,
                    out CertificateAuthority intermediateAuthority,
                    out X509Certificate2 endEntityCert,
                    nameof(AiaFetchDelayed));

                using (responder)
                using (rootAuthority)
                using (intermediateAuthority)
                using (endEntityCert)
                using (ChainHolder holder = new ChainHolder())
                using (X509Certificate2 rootCert = rootAuthority.CloneIssuerCert())
                using (X509Certificate2 intermediateCert = intermediateAuthority.CloneIssuerCert())
                {
                    TimeSpan delay = TimeSpan.FromSeconds(1);

                    X509Chain chain = holder.Chain;
                    responder.ResponseDelay = delay;
                    responder.DelayedActions = DelayedActionsFlag.All;

                    chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(15);
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                    Stopwatch watch = Stopwatch.StartNew();
                    Assert.True(chain.Build(endEntityCert), GetFlags(chain, endEntityCert.Thumbprint).ToString());
                    watch.Stop();

                    Assert.True(watch.Elapsed >= delay, $"watch.Elapsed: {watch.Elapsed}");
                }
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public static void AiaFetchTimeout()
        {
            CertificateAuthority.BuildPrivatePki(
                PkiOptions.AllRevocation,
                out RevocationResponder responder,
                out CertificateAuthority rootAuthority,
                out CertificateAuthority intermediateAuthority,
                out X509Certificate2 endEntityCert,
                nameof(AiaFetchTimeout));

            using (responder)
            using (rootAuthority)
            using (intermediateAuthority)
            using (endEntityCert)
            using (ChainHolder holder = new ChainHolder())
            using (X509Certificate2 rootCert = rootAuthority.CloneIssuerCert())
            using (X509Certificate2 intermediateCert = intermediateAuthority.CloneIssuerCert())
            {
                TimeSpan delay = TimeSpan.FromSeconds(10);

                X509Chain chain = holder.Chain;
                responder.ResponseDelay = delay;
                responder.DelayedActions = DelayedActionsFlag.All;

                chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(2);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                Assert.False(chain.Build(endEntityCert), "chain.Build");

                const X509ChainStatusFlags ExpectedFlags =
                    X509ChainStatusFlags.PartialChain;

                X509ChainStatusFlags eeFlags = GetFlags(chain, endEntityCert.Thumbprint);
                Assert.Equal(ExpectedFlags, eeFlags);
            }
        }

        private static X509ChainStatusFlags GetFlags(X509Chain chain, string thumbprint) =>
            chain.ChainElements.
                Single(e => e.Certificate.Thumbprint == thumbprint).
                ChainElementStatus.Aggregate((X509ChainStatusFlags)0, (a, e) => a | e.Status);
    }
}
