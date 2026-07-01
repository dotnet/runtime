// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    // Chain and managed certificate time validity must not depend on the process time zone.
    // OpenSSL/Linux only (Windows/macOS convert the verify time correctly).
    // RemoteExecutor + TZ isolates the time-zone change to a child process.
    [PlatformSpecific(TestPlatforms.Linux)]
    public static class ChainTimeZoneTests
    {
        private static readonly DateTimeOffset s_verify = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

        // Validity window of +/-2h around the query, narrower than the UTC+14 shift below.
        private static readonly DateTimeOffset s_managedNotBefore = new(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset s_managedNotAfter = new(2024, 6, 15, 14, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset s_managedQuery = new(2024, 6, 15, 13, 0, 0, TimeSpan.Zero);

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public static void ValidCert_StaysTimeValidAfterTimeZoneChange(bool asLocal)
        {
            RemoteExecutor.Invoke(static asLocalStr =>
            {
                // Validity margin (+/-2h) is narrower than the UTC+14 shift below, so the
                // bug (if present) moves the effective verify time outside the window.
                using X509Certificate2 cert = MakeCert(s_verify.AddHours(-2), s_verify.AddHours(2));
                bool asLocal = bool.Parse(asLocalStr);

                SetZone("UTC");
                Assert.True(IsTimeValid(cert, s_verify, asLocal));

                SetZone("Pacific/Kiritimati"); // UTC+14
                Assert.True(IsTimeValid(cert, s_verify, asLocal));
            }, asLocal.ToString()).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public static void ExpiredCert_StaysNotTimeValidAfterTimeZoneChange(bool asLocal)
        {
            RemoteExecutor.Invoke(static asLocalStr =>
            {
                // Expired 30 min before s_verify. NotBefore is >14h earlier so the
                // westward shift (~14h) lands back inside the window (re-entering
                // validity from the expiry side), not before NotBefore.
                using X509Certificate2 cert = MakeCert(s_verify.AddHours(-20), s_verify.AddMinutes(-30));
                bool asLocal = bool.Parse(asLocalStr);

                SetZone("Pacific/Kiritimati"); // UTC+14
                Assert.False(IsTimeValid(cert, s_verify, asLocal));

                SetZone("UTC"); // westward: "now" moves back ~14h
                Assert.False(IsTimeValid(cert, s_verify, asLocal));
            }, asLocal.ToString()).Dispose();
        }

        // Find(FindByTimeValid) for an instant inside the validity window must keep matching
        // across a mid-process time-zone change.
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void FindByTimeValid_StableAcrossTimeZoneChange()
        {
            RemoteExecutor.Invoke(static () =>
            {
                SetZone("UTC");

                using X509Certificate2 cert = MakeCert(s_managedNotBefore, s_managedNotAfter, "CN=managed-tz");

                // Read the validity dates before the zone change.
                _ = cert.NotBefore;
                _ = cert.NotAfter;

                var coll = new X509Certificate2Collection(cert);
                Assert.Equal(1, FindCount(coll, s_managedQuery));

                SetZone("Pacific/Kiritimati"); // UTC+14
                Assert.Equal(1, FindCount(coll, s_managedQuery));
            }).Dispose();
        }

        // CertificateRequest.Create checks that the leaf validity nests inside the issuer's.
        // A request that nests must keep succeeding across a mid-process time-zone change.
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void CreateNesting_StableAcrossTimeZoneChange()
        {
            RemoteExecutor.Invoke(static () =>
            {
                SetZone("UTC");

                DateTimeOffset issuerNotBefore = new(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
                DateTimeOffset issuerNotAfter = new(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);

                using RSA issuerKey = RSA.Create(2048);
                var issuerReq = new CertificateRequest("CN=issuer", issuerKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                issuerReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
                using X509Certificate2 issuer = issuerReq.CreateSelfSigned(issuerNotBefore, issuerNotAfter);

                // Leaf nests 1h inside the issuer on each boundary.
                DateTimeOffset leafNotBefore = issuerNotBefore.AddHours(1);
                DateTimeOffset leafNotAfter = issuerNotAfter.AddHours(-1);

                using RSA leafKey = RSA.Create(2048);
                var leafReq = new CertificateRequest("CN=leaf", leafKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                byte[] serial = [1, 2, 3, 4, 5, 6, 7, 8];

                // Read the issuer's validity dates before the zone change.
                _ = issuer.NotBefore;
                _ = issuer.NotAfter;

                using (X509Certificate2 leaf = leafReq.Create(issuer, leafNotBefore, leafNotAfter, serial))
                {
                    Assert.NotNull(leaf);
                }

                SetZone("Pacific/Kiritimati"); // UTC+14

                using X509Certificate2 leaf2 = leafReq.Create(issuer, leafNotBefore, leafNotAfter, serial);
                Assert.NotNull(leaf2);
            }).Dispose();
        }

        private static int FindCount(X509Certificate2Collection coll, DateTimeOffset when)
        {
            X509Certificate2Collection found = coll.Find(X509FindType.FindByTimeValid, when.UtcDateTime, validOnly: false);
            int count = found.Count;
            foreach (X509Certificate2 c in found)
            {
                c.Dispose();
            }
            return count;
        }

        internal static void SetZone(string tz)
        {
            Environment.SetEnvironmentVariable("TZ", tz);
            TimeZoneInfo.ClearCachedData();
        }

        internal static X509Certificate2 MakeCert(DateTimeOffset notBefore, DateTimeOffset notAfter, string subjectName = "CN=109039")
        {
            using RSA key = RSA.Create(2048);
            var req = new CertificateRequest(subjectName, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return req.CreateSelfSigned(notBefore, notAfter);
        }

        // Verify-time source (Utc vs Local) must not change the verdict.
        internal static bool IsTimeValid(X509Certificate2 cert, DateTimeOffset verify, bool asLocal)
        {
            using ChainHolder holder = new();
            X509Chain chain = holder.Chain;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(cert);
            chain.ChainPolicy.VerificationTime = asLocal ? verify.LocalDateTime : verify.UtcDateTime;

            chain.Build(cert);
            return (chain.AllStatusFlags() & X509ChainStatusFlags.NotTimeValid) == 0;
        }
    }
}
