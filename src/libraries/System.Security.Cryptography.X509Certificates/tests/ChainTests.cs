// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class ChainTests
    {
        [Fact]
        public static void BuildChain()
        {
            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            using (var microsoftDotComIssuer = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            using (var microsoftDotComRoot = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
            using (var unrelated = new X509Certificate2(TestData.DssCer))
            {
                X509Chain chain = new X509Chain();

                chain.ChainPolicy.ExtraStore.Add(unrelated);
                chain.ChainPolicy.ExtraStore.Add(microsoftDotComRoot);
                chain.ChainPolicy.ExtraStore.Add(microsoftDotComIssuer);
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

                // Halfway between microsoftDotCom's NotBefore and NotAfter
                // This isn't a boundary condition test.
                chain.ChainPolicy.VerificationTime = new DateTime(2015, 10, 15, 12, 01, 01, DateTimeKind.Local);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                bool valid = chain.Build(microsoftDotCom);
                Assert.True(valid, "Chain built validly");

                // The chain should have 3 members
                Assert.Equal(3, chain.ChainElements.Count);

                // These are the three specific members.
                Assert.Equal(microsoftDotCom, chain.ChainElements[0].Certificate);
                Assert.Equal(microsoftDotComIssuer, chain.ChainElements[1].Certificate);
                Assert.Equal(microsoftDotComRoot, chain.ChainElements[2].Certificate);
            }
        }

        /// <summary>
        /// Tests that when a certificate chain has a root certification which is not trusted by the trust provider,
        /// Build returns false and a ChainStatus returns UntrustedRoot
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void BuildChainExtraStoreUntrustedRoot()
        {
            using (var testCert = new X509Certificate2(Path.Combine("TestData", "test.pfx"), TestData.ChainPfxPassword))
            {
                X509Certificate2Collection collection = new X509Certificate2Collection();
                collection.Import(Path.Combine("TestData", "test.pfx"), TestData.ChainPfxPassword, X509KeyStorageFlags.DefaultKeySet);

                X509Chain chain = new X509Chain();
                chain.ChainPolicy.ExtraStore.AddRange(collection);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = new DateTime(2015, 9, 22, 12, 25, 0);

                bool valid = chain.Build(testCert);

                Assert.False(valid);
                Assert.Contains(chain.ChainStatus, s => s.Status == X509ChainStatusFlags.UntrustedRoot);
            }
        }

        public static IEnumerable<object[]> VerifyExpressionData()
        {
            // The test will be using the chain for TestData.MicrosoftDotComSslCertBytes
            // The leaf cert (microsoft.com) is valid from 2014-10-15 00:00:00Z to 2016-10-15 23:59:59Z
            DateTime[] validTimes =
            {
                // The NotBefore value
                new DateTime(2014, 10, 15, 0, 0, 0, DateTimeKind.Utc),

                // One second before the NotAfter value
                new DateTime(2016, 10, 15, 23, 59, 58, DateTimeKind.Utc),
            };

            // The NotAfter value as a boundary condition differs on Windows and OpenSSL.
            // Windows considers it valid (<= NotAfter).
            // OpenSSL considers it invalid (< NotAfter), with a comment along the lines of
            //   "it'll be invalid in a millisecond, why bother with the <="
            // So that boundary condition is not being tested.

            DateTime[] invalidTimes =
            {
                // One second before the NotBefore time
                new DateTime(2014, 10, 14, 23, 59, 59, DateTimeKind.Utc),

                // One second after the NotAfter time
                new DateTime(2016, 10, 16, 0, 0, 0, DateTimeKind.Utc),
            };

            List<object[]> testCases = new List<object[]>((validTimes.Length + invalidTimes.Length) * 3);

            // Build (date, result, kind) tuples.  The kind is used to help describe the test case.
            // The DateTime format that xunit uses does show a difference in the DateTime itself, but
            // having the Kind be a separate parameter just helps.

            foreach (DateTime utcTime in validTimes)
            {
                DateTime local = utcTime.ToLocalTime();
                DateTime unspecified = new DateTime(local.Ticks);

                testCases.Add(new object[] { utcTime, true, utcTime.Kind });
                testCases.Add(new object[] { local, true, local.Kind });
                testCases.Add(new object[] { unspecified, true, unspecified.Kind });
            }

            foreach (DateTime utcTime in invalidTimes)
            {
                DateTime local = utcTime.ToLocalTime();
                DateTime unspecified = new DateTime(local.Ticks);

                testCases.Add(new object[] { utcTime, false, utcTime.Kind });
                testCases.Add(new object[] { local, false, local.Kind });
                testCases.Add(new object[] { unspecified, false, unspecified.Kind });
            }

            return testCases;
        }

        [Theory]
        [MemberData("VerifyExpressionData")]
        public static void VerifyExpiration_LocalTime(DateTime verificationTime, bool shouldBeValid, DateTimeKind kind)
        {
            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            using (var microsoftDotComIssuer = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            using (var microsoftDotComRoot = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
            {
                X509Chain chain = new X509Chain();

                chain.ChainPolicy.ExtraStore.Add(microsoftDotComIssuer);
                chain.ChainPolicy.ExtraStore.Add(microsoftDotComRoot);

                // Ignore anything except NotTimeValid
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags & ~X509VerificationFlags.IgnoreNotTimeValid;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = verificationTime;

                bool builtSuccessfully = chain.Build(microsoftDotCom);

                Assert.Equal(shouldBeValid, builtSuccessfully);

                // If we failed to build the chain, ensure that NotTimeValid is one of the reasons.
                if (!shouldBeValid)
                {
                    Assert.Contains(chain.ChainStatus, s => s.Status == X509ChainStatusFlags.NotTimeValid);
                }
            }
        }
    }
}
