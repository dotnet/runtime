// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class ChainTests
    {
        private static bool TrustsMicrosoftDotComRoot
        {
            get
            {
                // Verifies that the microsoft.com certs build with only the certificates in the root store

                using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
                using (var chainHolder = new ChainHolder())
                {
                    X509Chain chain = chainHolder.Chain;
                    chain.ChainPolicy.VerificationTime = new DateTime(2015, 10, 15, 12, 01, 01, DateTimeKind.Local);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                    return chain.Build(microsoftDotCom);
                }
            }
        }

        [Fact]
        public static void BuildChain()
        {
            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            using (var microsoftDotComIssuer = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            using (var microsoftDotComRoot = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
            using (var unrelated = new X509Certificate2(TestData.DssCer))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;

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

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public static void VerifyChainFromHandle()
        {
            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            using (var microsoftDotComIssuer = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            using (var microsoftDotComRoot = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
            using (var unrelated = new X509Certificate2(TestData.DssCer))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;

                chain.ChainPolicy.ExtraStore.Add(unrelated);
                chain.ChainPolicy.ExtraStore.Add(microsoftDotComRoot);
                chain.ChainPolicy.ExtraStore.Add(microsoftDotComIssuer);
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                chain.ChainPolicy.VerificationTime = new DateTime(2015, 10, 15, 12, 01, 01, DateTimeKind.Local);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                bool valid = chain.Build(microsoftDotCom);
                Assert.True(valid, "Source chain built validly");
                Assert.Equal(3, chain.ChainElements.Count);

                using (var chainHolder2 = new ChainHolder(chain.ChainContext))
                {
                    X509Chain chain2 = chainHolder2.Chain;
                    Assert.NotSame(chain, chain2);
                    Assert.Equal(chain.ChainContext, chain2.ChainContext);

                    Assert.Equal(3, chain2.ChainElements.Count);

                    Assert.NotSame(chain.ChainElements[0], chain2.ChainElements[0]);
                    Assert.NotSame(chain.ChainElements[1], chain2.ChainElements[1]);
                    Assert.NotSame(chain.ChainElements[2], chain2.ChainElements[2]);

                    Assert.Equal(microsoftDotCom, chain2.ChainElements[0].Certificate);
                    Assert.Equal(microsoftDotComIssuer, chain2.ChainElements[1].Certificate);
                    Assert.Equal(microsoftDotComRoot, chain2.ChainElements[2].Certificate);

                    // ChainPolicy is not carried over from the Chain(IntPtr) constructor
                    Assert.NotEqual(chain.ChainPolicy.VerificationFlags, chain2.ChainPolicy.VerificationFlags);
                    Assert.NotEqual(chain.ChainPolicy.VerificationTime, chain2.ChainPolicy.VerificationTime);
                    Assert.NotEqual(chain.ChainPolicy.RevocationMode, chain2.ChainPolicy.RevocationMode);
                    Assert.Equal(X509VerificationFlags.NoFlag, chain2.ChainPolicy.VerificationFlags);

                    // Re-set the ChainPolicy properties
                    chain2.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    chain2.ChainPolicy.VerificationTime = new DateTime(2015, 10, 15, 12, 01, 01, DateTimeKind.Local);
                    chain2.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                    valid = chain2.Build(microsoftDotCom);
                    Assert.True(valid, "Cloned chain built validly");
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void VerifyChainFromHandle_Unix()
        {
            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                chain.ChainPolicy.VerificationTime = new DateTime(2015, 10, 15, 12, 01, 01, DateTimeKind.Local);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                bool valid = chain.Build(microsoftDotCom);

                Assert.Equal(IntPtr.Zero, chain.ChainContext);
            }

            Assert.Throws<PlatformNotSupportedException>(() => new X509Chain(IntPtr.Zero));
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public static void TestDispose()
        {
            X509Chain chain;

            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            using (var chainHolder = new ChainHolder())
            {
                chain = chainHolder.Chain;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                chain.ChainPolicy.VerificationTime = new DateTime(2015, 10, 15, 12, 01, 01, DateTimeKind.Local);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.Build(microsoftDotCom);

                Assert.NotEqual(IntPtr.Zero, chain.ChainContext);
            }
            // No exception thrown for accessing ChainContext on disposed chain
            Assert.Equal(IntPtr.Zero, chain.ChainContext);
        }

        [Fact]
        public static void TestResetMethod()
        {
            using (var sampleCert = new X509Certificate2(TestData.DssCer))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;

                chain.ChainPolicy.ExtraStore.Add(sampleCert);
                bool valid = chain.Build(sampleCert);
                Assert.False(valid);
                chainHolder.DisposeChainElements();

                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                chain.ChainPolicy.VerificationTime = new DateTime(2015, 10, 15, 12, 01, 01, DateTimeKind.Local);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                valid = chain.Build(sampleCert);
                Assert.True(valid, "Chain built validly");

                Assert.Equal(1, chain.ChainElements.Count);
                chainHolder.DisposeChainElements();

                chain.Reset();
                Assert.Equal(0, chain.ChainElements.Count);

                // ChainPolicy did not reset (for .NET Framework compat)
                Assert.Equal(X509VerificationFlags.AllowUnknownCertificateAuthority, chain.ChainPolicy.VerificationFlags);

                valid = chain.Build(sampleCert);
                Assert.Equal(1, chain.ChainElements.Count);
                // This succeeds because ChainPolicy did not reset
                Assert.True(valid, "Chain built validly after reset");
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
            using (ImportedCollection ic = Cert.Import(Path.Combine("TestData", "test.pfx"), TestData.ChainPfxPassword, X509KeyStorageFlags.DefaultKeySet))
            using (var chainHolder = new ChainHolder())
            {
                X509Certificate2Collection collection = ic.Collection;

                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.ExtraStore.AddRange(collection);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = new DateTime(2015, 9, 22, 12, 25, 0);

                bool valid = chain.Build(testCert);

                Assert.False(valid);
                Assert.Contains(chain.ChainStatus, s => s.Status == X509ChainStatusFlags.UntrustedRoot);
            }
        }

        [Theory]
        // Tests that the chain fails when a system trust certificate is added to the custom root trust, but its root isn't.
        [InlineData(true)]
        // Tests that the chain fails when no certificates are added to the custom root trust.
        [InlineData(false)]
        public static void SystemTrustCertificateWithCustomRootTrust(bool addCertificateToCustomRootTrust)
        {
            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            using (var testCert = new X509Certificate2(Path.Combine("TestData", "test.pfx"), TestData.ChainPfxPassword))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = microsoftDotCom.NotBefore.AddSeconds(1);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                if (addCertificateToCustomRootTrust)
                {
                    chain.ChainPolicy.CustomTrustStore.Add(testCert);
                }

                Assert.False(chain.Build(microsoftDotCom));

                // Linux and Windows do not search the default system root stores when CustomRootTrust is enabled
                if (OperatingSystem.IsMacOS())
                {
                    Assert.Equal(3, chain.ChainElements.Count);
                    Assert.Equal(X509ChainStatusFlags.UntrustedRoot, chain.AllStatusFlags());
                }
                else
                {
                    Assert.Equal(2, chain.ChainElements.Count);
                    Assert.Equal(X509ChainStatusFlags.PartialChain, chain.AllStatusFlags());
                }
            }
        }

        public enum BuildChainCustomTrustStoreTestArguments : int
        {
            TrustedIntermediateUntrustedRoot,
            UntrustedIntermediateTrustedRoot,
            TrustedIntermediateTrustedRoot,
            MultipleCalls
        }

        [Theory]
        [InlineData(false, X509ChainStatusFlags.UntrustedRoot, BuildChainCustomTrustStoreTestArguments.TrustedIntermediateUntrustedRoot)]
        [InlineData(true, X509ChainStatusFlags.NoError, BuildChainCustomTrustStoreTestArguments.UntrustedIntermediateTrustedRoot)]
        [InlineData(true, X509ChainStatusFlags.NoError, BuildChainCustomTrustStoreTestArguments.TrustedIntermediateTrustedRoot)]
        [InlineData(true, X509ChainStatusFlags.NoError, BuildChainCustomTrustStoreTestArguments.MultipleCalls)]
        public static void BuildChainCustomTrustStore(
            bool chainBuildsSuccessfully,
            X509ChainStatusFlags chainFlags,
            BuildChainCustomTrustStoreTestArguments testArguments)
        {
            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            using (var chainHolderPrep = new ChainHolder())
            {
                X509Chain chainPrep = chainHolderPrep.Chain;
                chainPrep.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chainPrep.ChainPolicy.VerificationTime = microsoftDotCom.NotBefore.AddSeconds(1);

                chainPrep.Build(microsoftDotCom);
                X509Certificate2 rootCert = chainPrep.ChainElements[2].Certificate;

                using (var chainHolderTest = new ChainHolder())
                {
                    X509Chain chainTest = chainHolderTest.Chain;
                    chainTest.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chainTest.ChainPolicy.VerificationTime = microsoftDotCom.NotBefore.AddSeconds(1);
                    chainTest.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                    switch (testArguments)
                    {
                        case BuildChainCustomTrustStoreTestArguments.TrustedIntermediateUntrustedRoot:
                            chainTest.ChainPolicy.ExtraStore.Add(rootCert);
                            break;
                        case BuildChainCustomTrustStoreTestArguments.UntrustedIntermediateTrustedRoot:
                            chainTest.ChainPolicy.CustomTrustStore.Add(rootCert);
                            break;
                        case BuildChainCustomTrustStoreTestArguments.TrustedIntermediateTrustedRoot:
                            chainTest.ChainPolicy.CustomTrustStore.Add(rootCert);
                            break;
                        case BuildChainCustomTrustStoreTestArguments.MultipleCalls:
                            chainTest.ChainPolicy.CustomTrustStore.Add(rootCert);
                            chainTest.Build(microsoftDotCom);
                            chainHolderTest.DisposeChainElements();
                            chainTest.ChainPolicy.CustomTrustStore.Remove(rootCert);
                            chainTest.ChainPolicy.TrustMode = X509ChainTrustMode.System;
                            break;
                        default:
                            throw new InvalidDataException();
                    }

                    Assert.Equal(chainBuildsSuccessfully, chainTest.Build(microsoftDotCom));
                    Assert.Equal(3, chainTest.ChainElements.Count);
                    Assert.Equal(chainFlags, chainTest.AllStatusFlags());
                }
            }
        }

        [Fact]
        public static void BuildChainWithSystemTrustAndCustomTrustCertificates()
        {
            using (var testCert = new X509Certificate2(Path.Combine("TestData", "test.pfx"), TestData.ChainPfxPassword))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = testCert.NotBefore.AddSeconds(1);
                chain.ChainPolicy.CustomTrustStore.Add(new X509Certificate2());

                Assert.Throws<CryptographicException>(() => chain.Build(testCert));
            }
        }

        [Fact]
        public static void BuildChainWithCustomRootTrustAndInvalidCustomCertificates()
        {
            using (var testCert = new X509Certificate2(Path.Combine("TestData", "test.pfx"), TestData.ChainPfxPassword))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = testCert.NotBefore.AddSeconds(1);
                chain.ChainPolicy.CustomTrustStore.Add(new X509Certificate2());
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                Assert.Throws<CryptographicException>(() => chain.Build(testCert));
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public static void Invalidx509ChainTrustMode(int trustMode)
        {
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                Assert.Throws<ArgumentException>(() => chain.ChainPolicy.TrustMode = (X509ChainTrustMode)trustMode);
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

                testCases.Add(new object[] { utcTime, true });
                testCases.Add(new object[] { local, true });
                testCases.Add(new object[] { unspecified, true });
            }

            foreach (DateTime utcTime in invalidTimes)
            {
                DateTime local = utcTime.ToLocalTime();
                DateTime unspecified = new DateTime(local.Ticks);

                testCases.Add(new object[] { utcTime, false });
                testCases.Add(new object[] { local, false });
                testCases.Add(new object[] { unspecified, false });
            }

            return testCases;
        }

        [Theory]
        [MemberData(nameof(VerifyExpressionData))]
        public static void VerifyExpiration_LocalTime(DateTime verificationTime, bool shouldBeValid)
        {
            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            using (var microsoftDotComIssuer = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            using (var microsoftDotComRoot = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;

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

        [Fact]
        public static void BuildChain_WithApplicationPolicy_Match()
        {
            using (var msCer = new X509Certificate2(TestData.MsCertificate))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;

                // Code Signing
                chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.3"));
                chain.ChainPolicy.VerificationTime = msCer.NotBefore.AddHours(2);
                chain.ChainPolicy.VerificationFlags =
                    X509VerificationFlags.AllowUnknownCertificateAuthority;

                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                bool valid = chain.Build(msCer);
                Assert.True(valid, "Chain built validly");
            }
        }

        [Fact]
        public static void BuildChain_WithApplicationPolicy_NoMatch()
        {
            using (var cert = new X509Certificate2(TestData.MsCertificate))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;

                // Gibberish.  (Code Signing + ".1")
                chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.3.1"));
                chain.ChainPolicy.VerificationFlags =
                    X509VerificationFlags.AllowUnknownCertificateAuthority;

                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = cert.NotBefore.AddHours(2);

                bool valid = chain.Build(cert);
                Assert.False(valid, "Chain built validly");

                Assert.InRange(chain.ChainElements.Count, 1, int.MaxValue);

                Assert.NotSame(cert, chain.ChainElements[0].Certificate);
                Assert.Equal(cert, chain.ChainElements[0].Certificate);

                X509ChainStatus[] chainElementStatus = chain.ChainElements[0].ChainElementStatus;
                Assert.InRange(chainElementStatus.Length, 1, int.MaxValue);
                Assert.Contains(chainElementStatus, x => x.Status == X509ChainStatusFlags.NotValidForUsage);
            }
        }

        [Fact]
        public static void BuildChain_WithCertificatePolicy_Match()
        {
            using (var cert = new X509Certificate2(TestData.CertWithPolicies))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;

                // Code Signing
                chain.ChainPolicy.CertificatePolicy.Add(new Oid("2.18.19"));
                chain.ChainPolicy.VerificationFlags =
                    X509VerificationFlags.AllowUnknownCertificateAuthority;
                chain.ChainPolicy.VerificationTime = cert.NotBefore.AddHours(2);

                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                bool valid = chain.Build(cert);
                Assert.True(valid, "Chain built validly");
            }
        }

        [Fact]
        public static void BuildChain_WithCertificatePolicy_NoMatch()
        {
            using (var cert = new X509Certificate2(TestData.CertWithPolicies))
            using (var chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;

                chain.ChainPolicy.CertificatePolicy.Add(new Oid("2.999"));
                chain.ChainPolicy.VerificationFlags =
                    X509VerificationFlags.AllowUnknownCertificateAuthority;

                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = cert.NotBefore.AddHours(2);

                bool valid = chain.Build(cert);
                Assert.False(valid, "Chain built validly");

                Assert.InRange(chain.ChainElements.Count, 1, int.MaxValue);

                Assert.NotSame(cert, chain.ChainElements[0].Certificate);
                Assert.Equal(cert, chain.ChainElements[0].Certificate);

                X509ChainStatus[] chainElementStatus = chain.ChainElements[0].ChainElementStatus;
                Assert.InRange(chainElementStatus.Length, 1, int.MaxValue);
                Assert.Contains(chainElementStatus, x => x.Status == X509ChainStatusFlags.NotValidForUsage);
            }
        }

        [ConditionalFact(nameof(TrustsMicrosoftDotComRoot))]
        public static void BuildChain_FailOnlyApplicationPolicy()
        {
            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            using (var microsoftDotComRoot = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
            using (ChainHolder holder = new ChainHolder())
            {
                holder.Chain.ChainPolicy.ApplicationPolicy.Add(new Oid("0.1.2.3.4", null));
                holder.Chain.ChainPolicy.VerificationTime = microsoftDotCom.NotBefore.AddDays(1);
                holder.Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                Assert.False(holder.Chain.Build(microsoftDotCom));

                Assert.Equal(
                    X509ChainStatusFlags.NotValidForUsage,
                    holder.Chain.ChainStatus.Aggregate(
                        X509ChainStatusFlags.NoError,
                        (a, status) => a | status.Status));

                Assert.Equal(3, holder.Chain.ChainElements.Count);

                Assert.Equal(microsoftDotCom.RawData, holder.Chain.ChainElements[0].Certificate.RawData);
                Assert.Equal(microsoftDotComRoot.RawData, holder.Chain.ChainElements[2].Certificate.RawData);

                Assert.Equal(
                    X509ChainStatusFlags.NotValidForUsage,
                    holder.Chain.ChainElements[0].ChainElementStatus.Aggregate(
                        X509ChainStatusFlags.NoError,
                        (a, status) => a | status.Status));

                Assert.Equal(
                    X509ChainStatusFlags.NotValidForUsage,
                    holder.Chain.ChainElements[1].ChainElementStatus.Aggregate(
                        X509ChainStatusFlags.NoError,
                        (a, status) => a | status.Status));

                Assert.Equal(
                    X509ChainStatusFlags.NotValidForUsage,
                    holder.Chain.ChainElements[2].ChainElementStatus.Aggregate(
                        X509ChainStatusFlags.NoError,
                        (a, status) => a | status.Status));
            }
        }

        [ConditionalFact(nameof(TrustsMicrosoftDotComRoot))]
        [OuterLoop(/* Modifies user certificate store */)]
        public static void BuildChain_MicrosoftDotCom_WithRootCertInUserAndSystemRootCertStores()
        {
            // Verifies that when the same root cert is placed in both a user and machine root certificate store,
            // any certs chain building to that root cert will build correctly
            //
            // We use a copy of the microsoft.com SSL certs and root certs to validate that the chain can build
            // successfully

            bool shouldInstallCertToUserStore = true;
            bool installedCertToUserStore = false;

            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            using (var microsoftDotComRoot = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
            {
                // Check that microsoft.com's root certificate IS installed in the machine root store as a sanity step
                using (var machineRootStore = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
                {
                    machineRootStore.Open(OpenFlags.ReadOnly);
                    bool foundCert = false;

                    foreach (var machineCert in machineRootStore.Certificates)
                    {
                        if (machineCert.Equals(microsoftDotComRoot))
                        {
                            foundCert = true;
                        }

                        machineCert.Dispose();
                    }

                    Assert.True(foundCert, string.Format("Did not find expected certificate with thumbprint '{0}' in the machine root store", microsoftDotComRoot.Thumbprint));
                }

                // Concievably at this point there could still be something wrong and we still don't chain build correctly - if that's
                // the case, then there's likely something wrong with the machine. Validating that happy path is out of scope
                // of this particular test.

                // Check that microsoft.com's root certificate is NOT installed on in the user cert store as a sanity step
                // We won't try to install the microsoft.com root cert into the user root store if it's already there
                using (var userRootStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
                {
                    userRootStore.Open(OpenFlags.ReadOnly);

                    foreach (var userCert in userRootStore.Certificates)
                    {
                        bool foundCert = false;
                        if (userCert.Equals(microsoftDotComRoot))
                        {
                            foundCert = true;
                        }

                        userCert.Dispose();

                        if (foundCert)
                        {
                            shouldInstallCertToUserStore = false;
                        }
                    }
                }

                using (var userRootStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
                using (var chainHolder = new ChainHolder())
                {
                    try
                    {
                        if (shouldInstallCertToUserStore)
                        {
                            try
                            {
                                userRootStore.Open(OpenFlags.ReadWrite);
                            }
                            catch (CryptographicException)
                            {
                                return;
                            }

                            userRootStore.Add(microsoftDotComRoot); // throws CryptographicException
                            installedCertToUserStore = true;
                        }

                        X509Chain chainValidator = chainHolder.Chain;
                        chainValidator.ChainPolicy.VerificationTime = new DateTime(2015, 10, 15, 12, 01, 01, DateTimeKind.Local);
                        chainValidator.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                        bool chainBuildResult = chainValidator.Build(microsoftDotCom);

                        StringBuilder builder = new StringBuilder();
                        foreach (var status in chainValidator.ChainStatus)
                        {
                            builder.AppendFormat("{0} {1}{2}", status.Status, status.StatusInformation, Environment.NewLine);
                        }

                        Assert.True(chainBuildResult,
                            string.Format("Certificate chain build failed. ChainStatus is:{0}{1}", Environment.NewLine, builder.ToString()));
                    }
                    finally
                    {
                        if (installedCertToUserStore)
                        {
                            userRootStore.Remove(microsoftDotComRoot);
                        }
                    }
                }
            }
        }

        [Fact]
        [OuterLoop( /* May require using the network, to download CRLs and intermediates */)]
        public static void VerifyWithRevocation()
        {
            using (var cert = new X509Certificate2(Path.Combine("TestData", "MS.cer")))
            using (var onlineChainHolder = new ChainHolder())
            using (var offlineChainHolder = new ChainHolder())
            {
                X509Chain onlineChain = onlineChainHolder.Chain;
                X509Chain offlineChain = offlineChainHolder.Chain;

                onlineChain.ChainPolicy.VerificationFlags =
                    X509VerificationFlags.AllowUnknownCertificateAuthority;

                onlineChain.ChainPolicy.VerificationTime = cert.NotBefore.AddHours(2);
                onlineChain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                onlineChain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;

                // Attempt the online test a couple of times, in case there was just a CRL
                // download failure.
                const int RetryLimit = 3;
                bool valid = false;

                for (int i = 0; i < RetryLimit; i++)
                {
                    valid = onlineChain.Build(cert);

                    if (valid)
                    {
                        break;
                    }

                    for (int j = 0; j < onlineChain.ChainElements.Count; j++)
                    {
                        X509ChainStatusFlags chainFlags = onlineChain.AllStatusFlags();

                        const X509ChainStatusFlags WontCheck =
                            X509ChainStatusFlags.RevocationStatusUnknown | X509ChainStatusFlags.UntrustedRoot;

                        if (chainFlags == WontCheck)
                        {
                            Console.WriteLine($"{nameof(VerifyWithRevocation)}: online chain failed with {{{chainFlags}}}, skipping");
                            return;
                        }

                        X509ChainElement chainElement = onlineChain.ChainElements[j];

                        // Since `NoError` gets mapped as the empty array, just look for non-empty arrays
                        if (chainElement.ChainElementStatus.Length > 0)
                        {
                            X509ChainStatusFlags allFlags = chainElement.AllStatusFlags();

                            Console.WriteLine(
                                $"{nameof(VerifyWithRevocation)}: online attempt {i} - errors at depth {j}: {allFlags}");
                        }

                        chainElement.Certificate.Dispose();
                    }

                    Thread.Sleep(1000); // For network flakiness
                }

                if (TestEnvironmentConfiguration.RunManualTests)
                {
                    Assert.True(valid, $"Online Chain Built Validly within {RetryLimit} tries");
                }
                else if (!valid)
                {
                    Console.WriteLine($"SKIP [{nameof(VerifyWithRevocation)}]: Chain failed to build within {RetryLimit} tries.");
                    return;
                }

                // Since the network was enabled, we should get the whole chain.
                Assert.Equal(3, onlineChain.ChainElements.Count);

                Assert.Equal(0, onlineChain.ChainElements[0].ChainElementStatus.Length);
                Assert.Equal(0, onlineChain.ChainElements[1].ChainElementStatus.Length);

                // The root CA is not expected to be installed on everyone's machines,
                // so allow for it to report UntrustedRoot, but nothing else..
                X509ChainStatus[] rootElementStatus = onlineChain.ChainElements[2].ChainElementStatus;

                if (rootElementStatus.Length != 0)
                {
                    Assert.Equal(1, rootElementStatus.Length);
                    Assert.Equal(X509ChainStatusFlags.UntrustedRoot, rootElementStatus[0].Status);
                }

                // Now that everything is cached, try again in Offline mode.
                offlineChain.ChainPolicy.VerificationFlags = onlineChain.ChainPolicy.VerificationFlags;
                offlineChain.ChainPolicy.VerificationTime = onlineChain.ChainPolicy.VerificationTime;
                offlineChain.ChainPolicy.RevocationMode = X509RevocationMode.Offline;
                offlineChain.ChainPolicy.RevocationFlag = onlineChain.ChainPolicy.RevocationFlag;

                valid = offlineChain.Build(cert);
                Assert.True(valid, "Offline Chain Built Validly");

                // Everything should look just like the online chain:
                Assert.Equal(onlineChain.ChainElements.Count, offlineChain.ChainElements.Count);

                for (int i = 0; i < offlineChain.ChainElements.Count; i++)
                {
                    X509ChainElement onlineElement = onlineChain.ChainElements[i];
                    X509ChainElement offlineElement = offlineChain.ChainElements[i];

                    Assert.Equal(onlineElement.ChainElementStatus, offlineElement.ChainElementStatus);
                    Assert.Equal(onlineElement.Certificate, offlineElement.Certificate);
                }
            }
        }

        [Fact]
        public static void Create()
        {
            using (var chain = X509Chain.Create())
                Assert.NotNull(chain);
        }

        [Fact]
        public static void BuildChainInvalidValues()
        {
            using (var chain = X509Chain.Create())
            {
                AssertExtensions.Throws<ArgumentException>("certificate", () => chain.Build(null));
                AssertExtensions.Throws<ArgumentException>("certificate", () => chain.Build(new X509Certificate2()));
            }
        }

        [Fact]
        public static void InvalidSelfSignedSignature()
        {
            X509ChainStatusFlags expectedFlags;

            if (OperatingSystem.IsWindows())
            {
                expectedFlags = X509ChainStatusFlags.NotSignatureValid;
            }
            else if (OperatingSystem.IsMacOS())
            {
                // For OSX alone expectedFlags here means OR instead of AND.
                // Because the error code changed in 10.13.4 from UntrustedRoot to PartialChain
                // and we handle that later in this test.
                expectedFlags =
                    X509ChainStatusFlags.UntrustedRoot |
                    X509ChainStatusFlags.PartialChain;
            }
            else
            {
                expectedFlags =
                    X509ChainStatusFlags.NotSignatureValid |
                    X509ChainStatusFlags.UntrustedRoot;
            }

            byte[] certBytes = (byte[])TestData.MicrosoftDotComRootBytes.Clone();
            // The signature goes up to the very last byte, so flip some bits in it.
            certBytes[certBytes.Length - 1] ^= 0xFF;

            using (var cert = new X509Certificate2(certBytes))
            using (ChainHolder holder = new ChainHolder())
            {
                X509Chain chain = holder.Chain;
                X509ChainPolicy policy = chain.ChainPolicy;
                policy.VerificationTime = cert.NotBefore.AddDays(3);
                policy.RevocationMode = X509RevocationMode.NoCheck;

                chain.Build(cert);

                X509ChainStatusFlags allFlags = chain.AllStatusFlags();

                if (OperatingSystem.IsMacOS())
                {
                    // If we're on 10.13.3 or older we get UntrustedRoot.
                    // If we're on 10.13.4 or newer we get PartialChain.
                    //
                    // So make the expectedValue be whichever of those two is set.
                    expectedFlags = (expectedFlags & allFlags);
                    // One of them has to be set.
                    Assert.NotEqual(X509ChainStatusFlags.NoError, expectedFlags);
                    // Continue executing now to ensure that no other unexpected flags were set.
                }

                Assert.Equal(expectedFlags, allFlags);
            }
        }

        [Fact]
        public static void ChainErrorsAtMultipleLayers()
        {
            // These certificates were generated for this test using CertificateRequest
            // but the netstandard(2.0) version of this test library doesn't have
            // CertificateRequest available.
            //
            // These certificates have been hard-coded to enable the scenario on
            // netstandard.
            byte[] endEntityBytes = Encoding.ASCII.GetBytes(@"
-----BEGIN CERTIFICATE-----
MIIC6DCCAdCgAwIBAgIQAKjmD7+TWUwQN2ucajn9kTANBgkqhkiG9w0BAQsFADAXMRUwEwYDVQQD
EwxJbnRlcm1lZGlhdGUwHhcNMTkwMzAzMjM1NzA3WhcNMTkwNjAzMjM1NzA3WjAVMRMwEQYDVQQD
EwpFbmQtRW50aXR5MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxTybBkpMdQ8IeL1C
jG755+ifQfqjNt4+Xhm3pbMi+nCRD68tym1xviUka1hQmx+I1mptswW0Laq1owur0r2KanKoIP2F
i2h6orOOdslMFPMWqCuNTU4C7cUxokaWah0R7FihwW+aBeWgxG948Cvt+ByQeR1ns9yo7wa8f8kT
IwzOUu0v1Yj5oW5bOn/cmIBE1C5CD5RivPMGUXX8mZ/myNh16dLQqJW5yQt/uvfr7lkNWC0qq+v7
Ely4+X27acwMTdtk4chcr5/bTS5FXV7HVqwhajOmm6WrzagPZBELWKRk2EaJkha/MLrBqNfHExs4
sx2ks+TTclrOrRzG+AUBuQIDAQABozIwMDAMBgNVHRMBAf8EAjAAMAsGA1UdDwQEAwIF4DATBgNV
HSUEDDAKBggrBgEFBQcDAjANBgkqhkiG9w0BAQsFAAOCAQEAbrEbiw4gpgWi3SJ+sGrfcWCAldpx
0735hkkYz94OsJjIwWfgQ03pYZwjcnIE4Ln0PU2E52D2ldsJlAE376hpNxdO0X4RLpZVZPEjKGTF
v2Rf+d0cpqha5J//mqcTTm7F58JRKyfEQn0pqfxx4VyXeLfEsqYbT3kY7ufK0km3Jst0DGw2AGue
MPmiZicaNlXPVO9vyW4s6J23+kol6X8K2rnVht9jagfnOQ990Ux2xXGyDGM4I0pvW1Zo4vid/eli
psHHsU9xg0o7L2WXD5qYhD2JCQIVWNRmRZCf1luWlKqUaqWWONMJ44hk8Md+ohxpyCRmbtLRZPzd
wlkQzPsc9A==
-----END CERTIFICATE-----");
            byte[] intermediateBytes = Encoding.ASCII.GetBytes(@"
-----BEGIN CERTIFICATE-----
MIIC1DCCAbygAwIBAgIPRoY1rB2tMVJeYB4GILkNMA0GCSqGSIb3DQEBCwUAMBQxEjAQBgNVBAMT
CVRlc3QgUm9vdDAeFw0xOTAyMTgyMzU3MDdaFw0yMDAyMTgyMzU3MDdaMBcxFTATBgNVBAMTDElu
dGVybWVkaWF0ZTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALxYzEN6nYvQ0TOg/jOF
wdBGRUYhTiJpYGFBh9826X5vKlbCS1UAcjFRXmKtJ4WZ8v3peCBPxvVe/1KR38+MWNVtO4B1GBvr
qR2T9k1ewgn0lO3i6krnIAhJQ+F94xGcsRAfZjXBh7lOmTE9ZlDhDJWkehBIs5TteiBOfbGDml2S
v7x81cmm2o/sDoP1oVGhezOkFtI2/NdZYKxRthnjDywN3W4KFataJFATVv/yq+QjcLEWrXFRpzDE
rpVdYmj66kaAnu9D4sHhFqOk1SX3JvcB361stVuUPp2ri75MaaXakweH6X/Yb4nPNV6m1ENwMoDy
HqrZrHSK8SpzfhY9aB0CAwEAAaMgMB4wDwYDVR0TAQH/BAUwAwEB/zALBgNVHQ8EBAMCAYYwDQYJ
KoZIhvcNAQELBQADggEBAC4oJ2SH+Ov4QIMXo7mwGSrwONkdMuKyyM9shZiGEH+zIO9SVuPuvtQG
cePR2bijSz2DtjySi+ST8y3Ql7A3isfbXYPDFmnkzKP6hGvLkctc8eO8U1x7ny+QW1max0gm3UA8
CY0IMP8pCHUZH9OX/K0N9L+GItqlBK8G4grJ4o43da2x9L0hIrdauPadaGcJalf8k1ymhJ4VDj7t
ueuTl2qTtbBh015GuEld61EBXSBLIUqwOAeFYrNJbC4J2mXgnLTWC380cBf5KWeSdjLYgk2sZ1V4
FKKQecZIhxdlDGzMAbbmEV+2EqS+As2C7+y4dkpG4nnbQe/4AFr8vekHdrI=
-----END CERTIFICATE-----");
            byte[] rootBytes = Encoding.ASCII.GetBytes(@"
-----BEGIN CERTIFICATE-----
MIICyjCCAbKgAwIBAgIIKKt3K3rRbvQwDQYJKoZIhvcNAQELBQAwFDESMBAGA1UEAxMJVGVzdCBS
b290MB4XDTE5MDIwNDIzNTcwN1oXDTIxMDIwNDIzNTcwN1owFDESMBAGA1UEAxMJVGVzdCBSb290
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAiM7tv4YvqmWYGF1vbeM2cQWV1NVBxKU4
ZK5XEJHZirzE2HCiA0+hI/UD7xnfBrzGQRLsHnp9vfhBi/0wenSIKTckxcGGpuM+JzNoVF97uFSd
bKvfIwQZzbdRGyTF1eoQWCylsZsnZOXg8c/yoFhG2TJB38l09RYn+HkMkapQERFKSXPZ7taNVJNb
Sedp3l9jO0aVmh9rmJ7taBXBfWDmSWqhkxjkEcbiRxB7z5K8YxZBlHQCLqf43JiCbKIMBHdzTg+N
lEBkBGp6T2hoJ4/A1uwvhesjmyqagZrC2NnzOWOxUQ/WujIUfS62ii/yDkP4Jo3745lJ9XXoPbIw
AwvWYQIDAQABoyAwHjAPBgNVHRMBAf8EBTADAQH/MAsGA1UdDwQEAwIBhjANBgkqhkiG9w0BAQsF
AAOCAQEAA/pfswrUzcLP5UfmHgQDc1slJjh0btnkN+4dxCCTLcnteJCTumYw+/82qL+O4t1KlzlS
2Eqgyx0u48YmwDp/5jWAvT8RX8pvV3Prd7T8/dp/ucES7R9r3zF2Rmw5Me9iq1yaLAypGyBGqV1J
HAwJjH/eKZ5iuOMhFljs2R5Gh5rRsQjNVUCRsolCds4d1f+76fi2SGaKqkAA4gzg1c71SPTAaUPR
ythjxnoCBDVFmwV5opXZj9qIZoUdH92gCVFgMWkxWCYWzyH78uIUzV1oo+KNwK1SCTnfVHcfWRIL
tHP28fj0LUop/QFojSZPsaPAW6JvoQ0t4hd6WoyX6z7FsA==
-----END CERTIFICATE-----");

            using (X509Certificate2 endEntityCert = new X509Certificate2(endEntityBytes))
            using (X509Certificate2 intermediateCert = new X509Certificate2(intermediateBytes))
            using (X509Certificate2 rootCert = new X509Certificate2(rootBytes))
            using (ChainHolder chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationFlags |= X509VerificationFlags.AllowUnknownCertificateAuthority;
                chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                chain.ChainPolicy.ExtraStore.Add(rootCert);
                chain.ChainPolicy.VerificationTime = endEntityCert.NotAfter.AddDays(1);

                Assert.False(chain.Build(endEntityCert));

                Assert.Equal(3, chain.ChainElements.Count);
                Assert.Equal(X509ChainStatusFlags.NotTimeValid, chain.ChainElements[0].AllStatusFlags());
                Assert.Equal(X509ChainStatusFlags.NoError, chain.ChainElements[1].AllStatusFlags());
                Assert.Equal(X509ChainStatusFlags.UntrustedRoot, chain.ChainElements[2].AllStatusFlags());

                Assert.Equal(
                    X509ChainStatusFlags.NotTimeValid | X509ChainStatusFlags.UntrustedRoot,
                    chain.AllStatusFlags());
            }
        }

        [Fact]
        public static void ChainWithEmptySubject()
        {
            using (var cert = new X509Certificate2(TestData.EmptySubjectCertificate))
            using (var issuer = new X509Certificate2(TestData.EmptySubjectIssuerCertificate))
            using (ChainHolder chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationFlags |= X509VerificationFlags.AllowUnknownCertificateAuthority;
                chain.ChainPolicy.ExtraStore.Add(issuer);

                Assert.True(chain.Build(cert), "chain.Build(cert)");
                Assert.Equal(2, chain.ChainElements.Count);
                Assert.Equal(string.Empty, cert.Subject);
                Assert.Equal(cert.RawData, chain.ChainElements[0].Certificate.RawData);
                Assert.Equal(issuer.RawData, chain.ChainElements[1].Certificate.RawData);
            }
        }

        [Fact]
        public static void BuildInvalidSignatureTwice()
        {
            byte[] bytes = (byte[])TestData.MsCertificate.Clone();
            bytes[bytes.Length - 1] ^= 0xFF;

            using (X509Certificate2 cert = new X509Certificate2(bytes))
            using (ChainHolder chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.VerificationTime = cert.NotBefore.AddHours(2);
                chain.ChainPolicy.VerificationFlags =
                    X509VerificationFlags.AllowUnknownCertificateAuthority;

                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                int iter = 0;

                void CheckChain()
                {
                    iter++;
                    bool valid = chain.Build(cert);
                    X509ChainStatusFlags allFlags = chain.AllStatusFlags();

                    if (OperatingSystem.IsMacOS())
                    {
                        // OSX considers this to be valid because it doesn't report NotSignatureValid,
                        // just PartialChain ("I couldn't find an issuer that made the signature work"),
                        // and PartialChain + AllowUnknownCertificateAuthority == pass.
                        Assert.True(valid, $"Chain is valid on execution {iter}");

                        Assert.Equal(1, chain.ChainElements.Count);

                        Assert.Equal(
                            X509ChainStatusFlags.PartialChain,
                            allFlags);
                    }
                    else
                    {
                        // These asserts are "most informative first".
                        // (There was an interval where valid was reporting as true in CI, but
                        // that is the least informative failure)

                        // Clear UntrustedRoot, if it happened.
                        allFlags &= ~X509ChainStatusFlags.UntrustedRoot;

                        Assert.Equal(
                            X509ChainStatusFlags.NotSignatureValid,
                            allFlags);

                        Assert.Equal(3, chain.ChainElements.Count);

                        Assert.False(valid, $"Chain is valid on execution {iter}");
                    }

                    chainHolder.DisposeChainElements();
                }

                CheckChain();
                CheckChain();
            }
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.Linux)]
        public static void BuildChainForFraudulentCertificate()
        {
            // This certificate is a misissued certificate for a "high-value"
            // domain, mail.google.com. Windows and macOS give this certificate
            // special distrust treatment beyond normal revocation, resulting
            // in ExplicitDistrust. OpenSSL relies on normal revocation routines
            // to distrust this certificate, so we skip this test on Linux.

            byte[] certBytes = Convert.FromBase64String(@"
MIIF7jCCBNagAwIBAgIQBH7L6fylX3vQnq424QyuHjANBgkqhkiG9w0BAQUFADCB
lzELMAkGA1UEBhMCVVMxCzAJBgNVBAgTAlVUMRcwFQYDVQQHEw5TYWx0IExha2Ug
Q2l0eTEeMBwGA1UEChMVVGhlIFVTRVJUUlVTVCBOZXR3b3JrMSEwHwYDVQQLExho
dHRwOi8vd3d3LnVzZXJ0cnVzdC5jb20xHzAdBgNVBAMTFlVUTi1VU0VSRmlyc3Qt
SGFyZHdhcmUwHhcNMTEwMzE1MDAwMDAwWhcNMTQwMzE0MjM1OTU5WjCB3zELMAkG
A1UEBhMCVVMxDjAMBgNVBBETBTM4NDc3MRAwDgYDVQQIEwdGbG9yaWRhMRAwDgYD
VQQHEwdFbmdsaXNoMRcwFQYDVQQJEw5TZWEgVmlsbGFnZSAxMDEUMBIGA1UEChML
R29vZ2xlIEx0ZC4xEzARBgNVBAsTClRlY2ggRGVwdC4xKDAmBgNVBAsTH0hvc3Rl
ZCBieSBHVEkgR3JvdXAgQ29ycG9yYXRpb24xFDASBgNVBAsTC1BsYXRpbnVtU1NM
MRgwFgYDVQQDEw9tYWlsLmdvb2dsZS5jb20wggEiMA0GCSqGSIb3DQEBAQUAA4IB
DwAwggEKAoIBAQCwc/DyBO7CokbKNCqqu2Aj0RF2Hx860GWDTppFqENwhXbwH4cA
Ah9uOxcXxLXpGUaikiWNYiq0YzAfuYX4NeEWWnZJzFBIUzlZidaEAvua7BvHUdV2
lZDUOiq4pt4CTQb7ze2lRkFfVXTl7H5A3FCcteQ1XR5oIPjp3qNqKL9B0qGz4iWN
DBvKPZMMGK7fxbz9vIK6aADXFjJxn2W1EdpoWdCmV2Qbyf6Y5fWlZerh2+70s52z
juqHrhbSHqB8fGk/KRaFAVOnbPFgq92i/CVH1DLREt33SBLg/Jyid5jpiZm4+Djx
jAbCeiM2bZudzTDIxzQXHrt9Qsir5xUW9nO1AgMBAAGjggHqMIIB5jAfBgNVHSME
GDAWgBShcl8mGyiYQ5VdBzfVhZadS9LDRTAdBgNVHQ4EFgQUGCqiyNR6P3utBIu9
b54QRhN4cZ0wDgYDVR0PAQH/BAQDAgWgMAwGA1UdEwEB/wQCMAAwHQYDVR0lBBYw
FAYIKwYBBQUHAwEGCCsGAQUFBwMCMEYGA1UdIAQ/MD0wOwYMKwYBBAGyMQECAQME
MCswKQYIKwYBBQUHAgEWHWh0dHBzOi8vc2VjdXJlLmNvbW9kby5jb20vQ1BTMHsG
A1UdHwR0MHIwOKA2oDSGMmh0dHA6Ly9jcmwuY29tb2RvY2EuY29tL1VUTi1VU0VS
Rmlyc3QtSGFyZHdhcmUuY3JsMDagNKAyhjBodHRwOi8vY3JsLmNvbW9kby5uZXQv
VVROLVVTRVJGaXJzdC1IYXJkd2FyZS5jcmwwcQYIKwYBBQUHAQEEZTBjMDsGCCsG
AQUFBzAChi9odHRwOi8vY3J0LmNvbW9kb2NhLmNvbS9VVE5BZGRUcnVzdFNlcnZl
ckNBLmNydDAkBggrBgEFBQcwAYYYaHR0cDovL29jc3AuY29tb2RvY2EuY29tMC8G
A1UdEQQoMCaCD21haWwuZ29vZ2xlLmNvbYITd3d3Lm1haWwuZ29vZ2xlLmNvbTAN
BgkqhkiG9w0BAQUFAAOCAQEAZwYICifFk24C8t4XP9DTG3z/tc16x3fHvt8Syhne
sBNXDAORxHlSz3+3XlUghEnd9dApLw4E2lmeDhOf9MAym/+hESQql6PyPz0qa6it
jBl1lQ4dJf1PxHoVwx3HE0DIDb6XYHKm/iW+j+zVpobDIVxZUtlqC1yfS961+ezi
9MXMYlN2iWXkKdq3v5bgYI0NtwlV1kBVHcHyliF1r4mGH12BlykoHinXlsEgAzJ7
ADtqNxdao7MabzI7bvGjXaurzCrLMAwfNSOLaURc6qwoYO2ra2Oe9pK8vZpaJkzF
mLgOGT78BTHjFtn9kAUDhsZXAR9/eKDPM2qqZmsi0KdJIw==");

            using (X509Certificate2 cert = new X509Certificate2(certBytes))
            using (ChainHolder chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = cert.NotBefore.AddHours(2);
                Assert.False(chain.Build(cert));

                X509ChainElement certElement = chain.ChainElements
                    .OfType<X509ChainElement>()
                    .Single(e => e.Certificate.Subject == cert.Subject);

                const X509ChainStatusFlags ExpectedFlag = X509ChainStatusFlags.ExplicitDistrust;
                X509ChainStatusFlags actualFlags = certElement.AllStatusFlags();
                Assert.True((actualFlags & ExpectedFlag) == ExpectedFlag, $"Has expected flag {ExpectedFlag} but was {actualFlags}");
            }
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.Linux)]
        public static void BuildChainForCertificateSignedWithDisallowedKey()
        {
            // The intermediate certificate is from the now defunct CA DigiNotar.
            // This intermediate is disallowed on the macOS on Windows platforms
            // which result in an ExplicitDistrust result. OpenSSL does not treat
            // this intermediate differently, and distributions have removed the
            // root CA from the trust store anyway, resulting in a partial chain.
            // Since OpenSSL isn't going out of its way to give the CA or its
            // intermediates any special distrust, we skip this test on Linux.

            byte[] intermediateBytes = Convert.FromBase64String(@"
MIIDzTCCAzagAwIBAgIERpwssDANBgkqhkiG9w0BAQUFADCBwzELMAkGA1UEBhMC
VVMxFDASBgNVBAoTC0VudHJ1c3QubmV0MTswOQYDVQQLEzJ3d3cuZW50cnVzdC5u
ZXQvQ1BTIGluY29ycC4gYnkgcmVmLiAobGltaXRzIGxpYWIuKTElMCMGA1UECxMc
KGMpIDE5OTkgRW50cnVzdC5uZXQgTGltaXRlZDE6MDgGA1UEAxMxRW50cnVzdC5u
ZXQgU2VjdXJlIFNlcnZlciBDZXJ0aWZpY2F0aW9uIEF1dGhvcml0eTAeFw0wNzA3
MjYxNTU5MDBaFw0xMzA4MjYxNjI5MDBaMGgxCzAJBgNVBAYTAk5MMRIwEAYDVQQK
EwlEaWdpTm90YXIxIzAhBgNVBAMTGkRpZ2lOb3RhciBTZXJ2aWNlcyAxMDI0IENB
MSAwHgYJKoZIhvcNAQkBFhFpbmZvQGRpZ2lub3Rhci5ubDCBnzANBgkqhkiG9w0B
AQEFAAOBjQAwgYkCgYEA2ptNXTz50eKLxsYIIMXZHkjsZlhneWIrQWP0iY1o2q+4
lDaLGSSkoJPSmQ+yrS01Tc0vauH5mxkrvAQafi09UmTN8T5nD4ku6PJPrqYIoYX+
oakJ5sarPkP8r3oDkdqmOaZh7phPGKjTs69mgumfvN1y+QYEvRLZGCTnq5NTi1kC
AwEAAaOCASYwggEiMBIGA1UdEwEB/wQIMAYBAf8CAQAwJwYDVR0lBCAwHgYIKwYB
BQUHAwEGCCsGAQUFBwMCBggrBgEFBQcDBDARBgNVHSAECjAIMAYGBFUdIAAwMwYI
KwYBBQUHAQEEJzAlMCMGCCsGAQUFBzABhhdodHRwOi8vb2NzcC5lbnRydXN0Lm5l
dDAzBgNVHR8ELDAqMCigJqAkhiJodHRwOi8vY3JsLmVudHJ1c3QubmV0L3NlcnZl
cjEuY3JsMB0GA1UdDgQWBBT+3JRJDG/vXH/G8RKZTxZJrfuCZTALBgNVHQ8EBAMC
AQYwHwYDVR0jBBgwFoAU8BdiE1U9s/8KAGv7UISX8+1i0BowGQYJKoZIhvZ9B0EA
BAwwChsEVjcuMQMCAIEwDQYJKoZIhvcNAQEFBQADgYEAY3RqN6k/lpxmyFisCcnv
9WWUf6MCxDgxvV0jh+zUVrLJsm7kBQb87PX6iHBZ1O7m3bV6oKNgLwIMq94SXa/w
NUuqikeRGvWFLELHHe+VQ7NeuJWTpdrFKKqtci0xrZlrbP+MISevrZqRK8fdWMNu
B8WfedLHjFW/TMcnXlEWKz4=");
            byte[] leafBytes = Convert.FromBase64String(@"
MIID3zCCA0igAwIBAgIRAK91OcqDBdcxtsg6T03CzCQwDQYJKoZIhvcNAQEFBQAw
aDELMAkGA1UEBhMCTkwxEjAQBgNVBAoTCURpZ2lOb3RhcjEjMCEGA1UEAxMaRGln
aU5vdGFyIFNlcnZpY2VzIDEwMjQgQ0ExIDAeBgkqhkiG9w0BCQEWEWluZm9AZGln
aW5vdGFyLm5sMB4XDTA5MDQyNDExMTUyNVoXDTEzMDQyMzExMTUyNVowgckxCzAJ
BgNVBAYTAk5MMSwwKgYDVQQKEyNDdXJyZW5jZSBTZXJ2aWNlcyBCLlYuICgwMDMw
MTkzNjE0KTEuMCwGA1UEBxMlQW1zdGVyZGFtIEJlZXRob3ZlbnN0cmFhdCAzMDAg
ICgwMDAwKTEoMCYGA1UECxMfU1NMIFNlcnZlcmNlcnRpZmljYWF0IC0gWmllIENQ
UzEaMBgGA1UEBRMRUlAwNzAwMDEwMDIyOTkxNjExFjAUBgNVBAMTDSouY3VycmVu
Y2UubmwwgZ8wDQYJKoZIhvcNAQEBBQADgY0AMIGJAoGBAK7b2TiO+0EuQnHlFl8Z
h2R6yEUIhvnpjQOHLnvN+7QicZ2Qe44sMk1hWdxvILwtdBBRN1jBkQh2zcB17fqm
bbGEb6E/i1sN1w1cFs3M1PJ+zTdgRACZ9yUl2Yh3C0PQqgI6tDmONvb1hqAdKgU4
dlFwUK1cz/YAzgg3HkEi2eB3AgMBAAGjggElMIIBITAfBgNVHSMEGDAWgBT+3JRJ
DG/vXH/G8RKZTxZJrfuCZTAJBgNVHRMEAjAAMIHDBgNVHSAEgbswgbgwgbUGC2CE
EAGHaQEBAQoBMIGlMCcGCCsGAQUFBwIBFhtodHRwOi8vd3d3LmRpZ2lub3Rhci5u
bC9jcHMwegYIKwYBBQUHAgIwbhpsQ29uZGl0aW9ucywgYXMgbWVudGlvbmVkIG9u
IG91ciB3ZWJzaXRlICh3d3cuZGlnaW5vdGFyLm5sKSwgYXJlIGFwcGxpY2FibGUg
dG8gYWxsIG91ciBwcm9kdWN0cyBhbmQgc2VydmljZXMuMA4GA1UdDwEB/wQEAwIE
sDAdBgNVHSUEFjAUBggrBgEFBQcDAgYIKwYBBQUHAwEwDQYJKoZIhvcNAQEFBQAD
gYEAGZH8mFA+TMlGMqXifNKs713LQ8bWv4j7bNNBsySUROa0+uhhKtGhh8089Cnn
lWOt5PxA7mHNbkGVwPbvPwg32LedZ6nRgpjHE8BJe57z2YmoawxLhxtzLyhOzfe8
yY1kePIfwE+GFWvagZ2ehANB/6LgBTT8jFhR95Tw2oE3N0I=");

            using (X509Certificate2 intermediateCert = new X509Certificate2(intermediateBytes))
            using (X509Certificate2 cert = new X509Certificate2(leafBytes))
            using (ChainHolder chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = cert.NotBefore.AddHours(2);
                chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                Assert.False(chain.Build(cert));

                X509ChainElement certElement = chain.ChainElements
                    .OfType<X509ChainElement>()
                    .Single(e => e.Certificate.Subject == intermediateCert.Subject);

                const X509ChainStatusFlags ExpectedFlag = X509ChainStatusFlags.ExplicitDistrust;
                X509ChainStatusFlags actualFlags = certElement.AllStatusFlags();
                Assert.True((actualFlags & ExpectedFlag) == ExpectedFlag, $"Has expected flag {ExpectedFlag} but was {actualFlags}");
            }
        }

        [Fact]
        public static void BuildChainForCertificateWithMD5Signature()
        {
            byte[] issuerCert = Convert.FromBase64String(@"
MIIDgzCCAmsCFGTFpNWP/ick4s4VCF1MafVWpWr+MA0GCSqGSIb3DQEBCwUAMH0x
CzAJBgNVBAYTAlVTMR0wGwYDVQQIDBREaXN0cmljdCBvZiBDb2x1bWJpYTETMBEG
A1UEBwwKV2FzaGluZ3RvbjEQMA4GA1UECgwHVGVzdCBDQTEUMBIGA1UECwwLRGV2
ZWxvcG1lbnQxEjAQBgNVBAMMCVRlc3QgUm9vdDAgFw0yMDA0MjgwMDQwNDZaGA8y
MTIwMDQwNDAwNDA0NlowfTELMAkGA1UEBhMCVVMxHTAbBgNVBAgMFERpc3RyaWN0
IG9mIENvbHVtYmlhMRMwEQYDVQQHDApXYXNoaW5ndG9uMRAwDgYDVQQKDAdUZXN0
IENBMRQwEgYDVQQLDAtEZXZlbG9wbWVudDESMBAGA1UEAwwJVGVzdCBSb290MIIB
IjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAsUBPPdECxj8DWbmjkhtnjxjd
LZluHyRpb0+favXLuXHeFtTy/92wuHUSHTr45TGKDxI0qevMLaNqEiy9yBkjNPTz
ctjZTHwbOhxuGEz3Mv2n3IJ7XoIPcn2ZQbhEcTDI/FeF06B+OQKNLigYMHR+L/qd
KlNBpaUaGG0FNpZ0zGJl1n+CizECWOh3PYaVLKmIS9RjEmNmOMqP737u8W6d3sRZ
vb6etsNKRmwRjpWUdk4/LzjSJSiNbIQv5c/cGSv6sXFKDixXxIugwreQ/F/JwJ3/
x2xTIJt0nHSKbK8zVKIGkSmZ3+bdeve889Mjwu0kN7EW+labAuf8VwzQ0c9qUQID
AQABMA0GCSqGSIb3DQEBCwUAA4IBAQAjGr0pZOxiCa+52S94WvR0WQVNMje3ZL+m
f4/FyaaDUCqrNv8Tt4m3vYtr8bkT+0uC4rcYx5/9iwLzI6oK1+JddoprAQ+17ZPw
Cg8ISgn8PuBzvaOQJxpc1nvWvvQpOiYxpsZPWABdE4xl3YAdcuu43x1mtFphn7Aw
KFTcvxF03RVZPSuZ0k6l1WBRNZFJFoTo2XlhUiLXN4vjxIEDXTCyi/kOzlYu98kZ
pzDlSoMBAu6CHSBygS51IaimM48qtdQjxZIYVZhFL9QaBa2zQ+qsEF0gz+mG0an9
0BMCvSA9GZA0VBrQJjQLQLjv0rpZkw0i9FypOicu2Zv9d5UF+IXZ");

            byte[] md5SignedLeafCert = Convert.FromBase64String(@"
MIIEezCCA2OgAwIBAgIUf1ubwalzwcn4DQ2X5hUbYPqateowDQYJKoZIhvcNAQEE
BQAwfTELMAkGA1UEBhMCVVMxHTAbBgNVBAgMFERpc3RyaWN0IG9mIENvbHVtYmlh
MRMwEQYDVQQHDApXYXNoaW5ndG9uMRAwDgYDVQQKDAdUZXN0IENBMRQwEgYDVQQL
DAtEZXZlbG9wbWVudDESMBAGA1UEAwwJVGVzdCBSb290MCAXDTIwMDQyODAwNDI1
OFoYDzIxMTYwMjI1MDA0MjU4WjBoMQswCQYDVQQGEwJVUzEdMBsGA1UECAwURGlz
dHJpY3Qgb2YgQ29sdW1iaWExEDAOBgNVBAoMB1Rlc3QgQ0ExFDASBgNVBAsMC0Rl
dmVsb3BtZW50MRIwEAYDVQQDDAlUZXN0IExlYWYwggEiMA0GCSqGSIb3DQEBAQUA
A4IBDwAwggEKAoIBAQDGHz60IeCSpN1qQbdLHO2VSlQbOn2fBV5qGK/82+a4xiZf
xO0wZ6p9Tb7/rOnF7P7YlaOrY9zc6O5vPxatcv2FcZxwrR8zDnslWUg39WzFnz4M
8eiiGBpxlbUfcUq8FvqfGQ6MxMAwA0kgUjegaVXN1Zgq+J+HcLSJm8EADNOD46nS
TkTVXvEMCBmrl17LyYEnxLogUgWve9QMNz0+XpJq90MlygPmxuvUnWduDGnVgrJq
blkwFqaLIh94vmc8rQJ9WSy+1FRknDoDcy3KveYW3ii9uD9B7YvXdmFVEjGXPcUv
9aFoRiqq/E8oYUhWJXTr9omyA3iJawcn0Kkl3IT7AgMBAAGjggEEMIIBADAJBgNV
HRMEAjAAMCwGCWCGSAGG+EIBDQQfFh1PcGVuU1NMIEdlbmVyYXRlZCBDZXJ0aWZp
Y2F0ZTAdBgNVHQ4EFgQUE9CCY2wzRzuH3lvEENkvBxDRFx8wgaUGA1UdIwSBnTCB
mqGBgaR/MH0xCzAJBgNVBAYTAlVTMR0wGwYDVQQIDBREaXN0cmljdCBvZiBDb2x1
bWJpYTETMBEGA1UEBwwKV2FzaGluZ3RvbjEQMA4GA1UECgwHVGVzdCBDQTEUMBIG
A1UECwwLRGV2ZWxvcG1lbnQxEjAQBgNVBAMMCVRlc3QgUm9vdIIUZMWk1Y/+JyTi
zhUIXUxp9Valav4wDQYJKoZIhvcNAQEEBQADggEBAGbrB50Gf9FQ1lTbtKQBlrpF
M01/mHvqDioqjP6hcvDRMvxWcnX8kIq7Idb2uv1fByBPQdBTH2yzGc1adCXtBqrb
ueIjvYVDoXZMqRa7vZjaMA+8szK9lgm2dzSfa3xFKCIT7Twfq6FKGJ7o4TRbopmr
3MsjTMLjfGUnKdxtcYb/FGxB4NRdIyCaaRgtYOIFkOGgA3UTEAJuOAqwY8RdQywR
lHBlkA0wrbydD3FzxYHUJgx0HGO6CcyAzXJLhZVbuBW4expq4Qhi0jDV4d8Otskv
LjCvFGJ+RiZCbxIZfUZEuJ5vAH5WOa2S0tYoEAeyfzuLMIqY9xK74nlZ/vzz1cY=");

            using (X509Certificate2 issuer = new X509Certificate2(issuerCert))
            using (X509Certificate2 cert = new X509Certificate2(md5SignedLeafCert))
            using (ChainHolder chainHolder = new ChainHolder())
            {
                X509Chain chain = chainHolder.Chain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationTime = cert.NotBefore.AddHours(2);
                chain.ChainPolicy.ExtraStore.Add(issuer);

                // Should not throw, don't care about the validity of the chain.
                chain.Build(cert);
            }
        }

        internal static X509ChainStatusFlags AllStatusFlags(this X509Chain chain)
        {
            return chain.ChainStatus.Aggregate(
                X509ChainStatusFlags.NoError,
                (f, s) => f | s.Status);
        }

        internal static X509ChainStatusFlags AllStatusFlags(this X509ChainElement chainElement)
        {
            return chainElement.ChainElementStatus.Aggregate(
                X509ChainStatusFlags.NoError,
                (f, s) => f | s.Status);
        }
    }
}
