// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "No X.509 support on browser")]
    public static class ChainPolicyTests
    {
        private static readonly Oid s_emailProtectionEku = new Oid("1.3.6.1.5.5.7.3.4", null);
        private static readonly Oid s_timestampEku = new Oid("1.3.6.1.5.5.7.3.8", null);

        [Fact]
        public static void DefaultCtorState()
        {
            X509ChainPolicy policy = new X509ChainPolicy();
            AssertDefaultState(policy);
        }

        [Fact]
        public static void ResetAfterChanges()
        {
            using (X509Certificate2 cert = new X509Certificate2(SelfSigned1PemBytes))
            {
                X509ChainPolicy policy = new X509ChainPolicy();
                policy.CertificatePolicy.Add(s_emailProtectionEku);
                policy.ApplicationPolicy.Add(s_timestampEku);
                policy.ExtraStore.Add(cert);
                policy.CustomTrustStore.Add(cert);
                policy.DisableCertificateDownloads = true;
                policy.VerificationTime = DateTime.MinValue;
                policy.VerificationTimeIgnored = false;
                policy.UrlRetrievalTimeout = TimeSpan.MaxValue;
                policy.VerificationFlags = X509VerificationFlags.IgnoreCtlNotTimeValid;
                policy.RevocationMode = X509RevocationMode.Offline;
                policy.RevocationFlag = X509RevocationFlag.EntireChain;

                policy.Reset();
                AssertDefaultState(policy);
            }
        }

        [Fact]
        public static void SetVerificationTimeClearsIgnored()
        {
            X509ChainPolicy policy = new X509ChainPolicy();
            Assert.True(policy.VerificationTimeIgnored, "policy.VerificationTimeIgnored (Before)");
            policy.VerificationTime = DateTime.MinValue;
            Assert.False(policy.VerificationTimeIgnored, "policy.VerificationTimeIgnored (After)");
        }

        [Fact]
        public static void VerifyCloneBehavior()
        {
            using (X509Certificate2 cert = new X509Certificate2(SelfSigned1PemBytes))
            using (X509Certificate2 cert2 = new X509Certificate2(SelfSigned1PemBytes))
            {
                X509ChainPolicy source = new X509ChainPolicy();
                source.CertificatePolicy.Add(s_timestampEku);
                source.ApplicationPolicy.Add(s_emailProtectionEku);
                source.ExtraStore.Add(cert);
                source.CustomTrustStore.Add(cert2);
                source.DisableCertificateDownloads = true;
                source.VerificationTime = DateTime.MinValue;
                source.VerificationTimeIgnored = false;
                source.UrlRetrievalTimeout = TimeSpan.MaxValue;
                source.VerificationFlags = X509VerificationFlags.IgnoreCtlNotTimeValid;
                source.RevocationMode = X509RevocationMode.Offline;
                source.RevocationFlag = X509RevocationFlag.EntireChain;

                X509ChainPolicy clone = source.Clone();
                Assert.Equal(source.VerificationTime, clone.VerificationTime);
                Assert.Equal(source.VerificationTimeIgnored, clone.VerificationTimeIgnored);
                Assert.Equal(source.VerificationFlags, clone.VerificationFlags);
                Assert.Equal(source.RevocationFlag, clone.RevocationFlag);
                Assert.Equal(source.RevocationMode, clone.RevocationMode);
                Assert.Equal(source.UrlRetrievalTimeout, clone.UrlRetrievalTimeout);
                Assert.Equal(source.DisableCertificateDownloads, clone.DisableCertificateDownloads);

                Assert.NotSame(source.CertificatePolicy, clone.CertificatePolicy);
                Assert.Equal(source.CertificatePolicy, clone.CertificatePolicy);
                Assert.Same(source.CertificatePolicy[0], clone.CertificatePolicy[0]);

                Assert.NotSame(source.ApplicationPolicy, clone.ApplicationPolicy);
                Assert.Equal(source.ApplicationPolicy, clone.ApplicationPolicy);
                Assert.Same(source.ApplicationPolicy[0], clone.ApplicationPolicy[0]);

                Assert.NotSame(source.ExtraStore, clone.ExtraStore);
                Assert.Equal(source.ExtraStore, clone.ExtraStore);
                Assert.Same(source.ExtraStore[0], clone.ExtraStore[0]);

                Assert.NotSame(source.CustomTrustStore, clone.CustomTrustStore);
                Assert.Equal(source.CustomTrustStore, clone.CustomTrustStore);
                Assert.Same(source.CustomTrustStore[0], clone.CustomTrustStore[0]);

                Assert.NotSame(source.ExtraStore[0], clone.CustomTrustStore[0]);
            }
        }

        private static void AssertDefaultState(X509ChainPolicy policy)
        {
            Assert.Equal(X509RevocationFlag.ExcludeRoot, policy.RevocationFlag);
            Assert.Equal(X509RevocationMode.Online, policy.RevocationMode);
            Assert.Equal(X509VerificationFlags.NoFlag, policy.VerificationFlags);
            Assert.Equal(X509ChainTrustMode.System, policy.TrustMode);
            Assert.False(policy.DisableCertificateDownloads, "policy.DisableCertificateDownloads");
            Assert.True(policy.VerificationTimeIgnored, "policy.VerificationTimeIgnored");
            Assert.Equal(TimeSpan.Zero, policy.UrlRetrievalTimeout);

            // A DST adjustment will make the two Now values differ by the jump time, which
            // is never more than an hour.
            // An NTP jump is usually limited to 5 minutes.
            // Add another 10 minutes for never seeing this fail.
            Assert.Equal(DateTime.Now, policy.VerificationTime, TimeSpan.FromMinutes(75));

            Assert.Empty(policy.ApplicationPolicy);
            Assert.Empty(policy.CertificatePolicy);
            Assert.Empty(policy.ExtraStore);
            Assert.Empty(policy.CustomTrustStore);
        }

        // TODO (66338): Unify with TestData.SelfSigned1PemBytes after all X509 tests are unified in.
        private static readonly byte[] SelfSigned1PemBytes = ByteUtils.AsciiBytes(
            @"-----BEGIN CERTIFICATE-----
MIIDWjCCAkKgAwIBAgIJAJpCQ7mtFWHeMA0GCSqGSIb3DQEBCwUAMEIxCzAJBgNV
BAYTAlhYMRUwEwYDVQQHDAxEZWZhdWx0IENpdHkxHDAaBgNVBAoME0RlZmF1bHQg
Q29tcGFueSBMdGQwHhcNMTgwNTMwMTgyNjM1WhcNMTkwNTMwMTgyNjM1WjBCMQsw
CQYDVQQGEwJYWDEVMBMGA1UEBwwMRGVmYXVsdCBDaXR5MRwwGgYDVQQKDBNEZWZh
dWx0IENvbXBhbnkgTHRkMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA
pfYZTHjzei9U3QxiIIjESsf9z3Bfl8FAQLIU+OeICN3upnDvTgeWM/Jw7LwiuHhu
XvSawPwQ8ONvUeSG/wfyjYyTB7VBpVnNi6oTR6E1WSuiu0iT3qlDHVwArTI5DvIM
FzP3/AT1Ub5SvwVbWiR2za6wuUIsryyLz5+zCwGr+J/Xbmta/H9IT9NLwmDJCZQe
4Q4hCWhf7FKdXWt59y9PofVnE7R8CKNfUKr6GA+gy+SEtM/cHgqox5PErnV9b14U
uVROnRUyo1bFwTOdoW3zf5S4VZ4pFPJHNYACHEMiE0eNgfJf+QeyPUPN50neEAbf
kQYkeEET8dW6JlDFrAI4wwIDAQABo1MwUTAdBgNVHQ4EFgQUK+C/eGYPlV+KaTvj
tF6lJaKmo3EwHwYDVR0jBBgwFoAUK+C/eGYPlV+KaTvjtF6lJaKmo3EwDwYDVR0T
AQH/BAUwAwEB/zANBgkqhkiG9w0BAQsFAAOCAQEAZUjvDMhGc45TLRHKO5rsyifN
g7qb3dO5vtD/JWeo+wyMYcBHIANIIxYrkT0dRBQWQerVDBvsAESahM3f0SdszGac
6y1qxQWxfjxRiCwrEQ7JVZkmspYLbOxaS1T2IZUo3D7VJReyna6r11EKy7i49Toa
KmrhTLBsHV+MUgPRtupiOOu0fXqfxpXE7XEvi0hyv8PKli+Oww2Zyt1jTTvv2RTA
eJRqTUNUbWEDesXAOh5CY6Xjfg7Gt6IYQHt0JMw29pXB3TV2uyXuvFNsc725cPbW
JCuC9TGQRUAUj+LZ43tTrfaZ7g5L80/eRrvlx5MIJSsX8cev8pZYx224WRtk/w==
-----END CERTIFICATE-----
");
    }
}
