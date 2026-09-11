// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    // Chain time validity must not depend on the process time zone.
    // OpenSSL/Linux only (Windows/macOS convert the verify time correctly).
    // RemoteExecutor + TZ isolates the time-zone change to a child process.
    [PlatformSpecific(TestPlatforms.Linux)]
    public static class ChainTimeZoneTests
    {
        private static readonly DateTimeOffset s_verify = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

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

        internal static void SetZone(string tz)
        {
            Environment.SetEnvironmentVariable("TZ", tz);
            TimeZoneInfo.ClearCachedData();
        }

        internal static X509Certificate2 MakeCert(DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            using RSA key = RSA.Create(2048);
            var req = new CertificateRequest("CN=109039", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
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
