// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class FindTests
    {
        private static void RunTest(Action<X509Certificate2Collection> test)
        {
            RunTest((msCer, pfxCer, col1) => test(col1));
        }

        private static void RunTest(Action<X509Certificate2, X509Certificate2, X509Certificate2Collection> test)
        {
            using (var msCer = new X509Certificate2(TestData.MsCertificate))
            using (var pfxCer = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                X509Certificate2Collection col1 = new X509Certificate2Collection(new[] { msCer, pfxCer });

                test(msCer, pfxCer, col1);
            }
        }

        private static void RunExceptionTest<TException>(X509FindType findType, object findValue)
            where TException : Exception
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    Assert.Throws<TException>(() => col1.Find(findType, findValue, validOnly: false));
                });
        }

        private static void RunZeroMatchTest(X509FindType findType, object findValue)
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    X509Certificate2Collection col2 = col1.Find(findType, findValue, validOnly: false);

                    Assert.Equal(0, col2.Count);
                });
        }

        private static void RunSingleMatchTest_MsCer(X509FindType findType, object findValue)
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    EvaluateSingleMatch(msCer, col1, findType, findValue);
                });
        }

        private static void RunSingleMatchTest_PfxCer(X509FindType findType, object findValue)
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    EvaluateSingleMatch(pfxCer, col1, findType, findValue);
                });
        }

        private static void EvaluateSingleMatch(
            X509Certificate2 expected,
            X509Certificate2Collection col1,
            X509FindType findType,
            object findValue)
        {
            X509Certificate2Collection col2 =
                col1.Find(findType, findValue, validOnly: false);

            Assert.Equal(1, col2.Count);

            byte[] serialNumber;

            using (X509Certificate2 match = col2[0])
            {
                Assert.Equal(expected, match);
                Assert.NotSame(expected, match);

                // Verify that the find result and original are linked, not just equal.
                match.FriendlyName = "HAHA";
                Assert.Equal("HAHA", expected.FriendlyName);

                serialNumber = match.GetSerialNumber();
            }

            // Check that disposing match didn't dispose expected
            Assert.Equal(serialNumber, expected.GetSerialNumber());
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void FindByNullName()
        {
            RunExceptionTest<ArgumentNullException>(X509FindType.FindBySubjectName, null);
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void FindByThumbprint_Object()
        {
            RunExceptionTest<CryptographicException>(X509FindType.FindByThumbprint, new object());
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void FindByInvalidThumbprint()
        {
            RunZeroMatchTest(X509FindType.FindByThumbprint, "Nothing");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void FindByValidThumbprint()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    EvaluateSingleMatch(
                        pfxCer,
                        col1,
                        X509FindType.FindByThumbprint,
                        pfxCer.Thumbprint);
                });
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestSubjectName_Object()
        {
            RunExceptionTest<CryptographicException>(X509FindType.FindBySubjectName, new object());
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestSubjectName_NoMatch()
        {
            RunZeroMatchTest(X509FindType.FindBySubjectName, "Nothing");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestSubjectName_Match()
        {
            RunSingleMatchTest_MsCer(X509FindType.FindBySubjectName, "Microsoft Corporation");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestDistinguishedSubjectName_Object()
        {
            RunExceptionTest<CryptographicException>(X509FindType.FindBySubjectDistinguishedName, new object());
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestDistinguishedSubjectName_NoMatch()
        {
            RunZeroMatchTest(X509FindType.FindBySubjectDistinguishedName, "Nothing");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestDistinguishedSubjectName_Match()
        {
            RunSingleMatchTest_MsCer(
                X509FindType.FindBySubjectDistinguishedName,
                "CN=Microsoft Corporation, OU=MOPR, O=Microsoft Corporation, L=Redmond, S=Washington, C=US");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByTimeValid_Object()
        {
            RunExceptionTest<CryptographicException>(X509FindType.FindByTimeValid, new object());
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByTimeValid_Before()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    DateTime earliest = new[] { msCer.NotBefore, pfxCer.NotBefore }.Min();

                    X509Certificate2Collection col2 = col1.Find(
                        X509FindType.FindByTimeValid,
                        earliest - TimeSpan.FromSeconds(1),
                        validOnly: false);

                    Assert.Equal(0, col2.Count);
                });
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByTimeValid_After()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    DateTime latest = new[] { msCer.NotAfter, pfxCer.NotAfter }.Max();

                    X509Certificate2Collection col2 = col1.Find(
                        X509FindType.FindByTimeValid,
                        latest + TimeSpan.FromSeconds(1),
                        validOnly: false);

                    Assert.Equal(0, col2.Count);
                });
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByTimeValid_Between()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    DateTime earliestNotAfter = new[] { msCer.NotAfter, pfxCer.NotAfter }.Min();
                    DateTime latestNotBefore = new[] { msCer.NotBefore, pfxCer.NotBefore }.Max();

                    TimeSpan gap = latestNotBefore - earliestNotAfter;

                    // If this assert fails it means our test data was rebuilt and the constraint
                    // can no longer be satisifed
                    Assert.True(gap > TimeSpan.FromSeconds(1));

                    DateTime noMatchTime = earliestNotAfter + TimeSpan.FromSeconds(1);

                    X509Certificate2Collection col2 = col1.Find(
                        X509FindType.FindByTimeValid,
                        noMatchTime,
                        validOnly: false);

                    Assert.Equal(0, col2.Count);
                });
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByTimeValid_Match()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    EvaluateSingleMatch(
                        msCer,
                        col1,
                        X509FindType.FindByTimeValid,
                        msCer.NotBefore + TimeSpan.FromSeconds(1));
                });
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestBySerialNumber_Decimal()
        {
            // Decimal string is an allowed input format.
            RunSingleMatchTest_PfxCer(
                X509FindType.FindBySerialNumber,
                "284069184166497622998429950103047369500");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestBySerialNumber_DecimalLeadingZeros()
        {
            // Checking that leading zeros are ignored.
            RunSingleMatchTest_PfxCer(
               X509FindType.FindBySerialNumber,
               "000" + "284069184166497622998429950103047369500");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestBySerialNumber_DecimalNegative()
        {
            // This serial number has the high bit on which means that it's easier to misinterpret as a negative number.
            // Make sure we didn't fall into this trap.
            RunZeroMatchTest(
                X509FindType.FindBySerialNumber,
                "-56213182754440840464944657328720841956");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestBySerialNumber_Hex()
        {
            // Hex string is also an allowed input format.
            RunSingleMatchTest_PfxCer(
                X509FindType.FindBySerialNumber,
                "D5B5BC1C458A558845BFF51CB4DFF31C");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestBySerialNumber_HexIgnoreCase()
        {
            // Hex string is also an allowed input format and case-blind
            RunSingleMatchTest_PfxCer(
                X509FindType.FindBySerialNumber,
                "d5b5bc1c458a558845bff51cb4dff31c");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestBySerialNumber_HexLeadingZeros()
        {
            // Checking that leading zeros are ignored.
            RunSingleMatchTest_PfxCer(
                X509FindType.FindBySerialNumber,
                "0000" + "D5B5BC1C458A558845BFF51CB4DFF31C");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestBySerialNumber_NoMatch()
        {
            RunZeroMatchTest(
                X509FindType.FindBySerialNumber,
                "23000000B011AF0A8BD03B9FDD0001000000B0");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByExtension_FriendlyName()
        {
            RunSingleMatchTest_MsCer(X509FindType.FindByExtension, "Enhanced Key Usage");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByExtension_OidValue()
        {
            RunSingleMatchTest_MsCer(X509FindType.FindByExtension, "2.5.29.37");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByExtension_UnknownFriendlyName()
        {
            RunExceptionTest<ArgumentException>(X509FindType.FindByExtension, "BOGUS");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByExtension_NoMatch()
        {
            RunZeroMatchTest(X509FindType.FindByExtension, "2.9");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestBySubjectKeyIdentifier_MatchA()
        {
            RunSingleMatchTest_PfxCer(
                X509FindType.FindBySubjectKeyIdentifier,
                "B4D738B2D4978AFF290A0B02987BABD114FEE9C7");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestBySubjectKeyIdentifier_MatchB()
        {
            RunSingleMatchTest_MsCer(
                X509FindType.FindBySubjectKeyIdentifier,
                "5971A65A334DDA980780FF841EBE87F9723241F2");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestBySubjectKeyIdentifier_NoMatch()
        {
            RunZeroMatchTest(X509FindType.FindBySubjectKeyIdentifier, "");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByApplicationPolicy_MatchAll()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    X509Certificate2Collection results =
                        col1.Find(X509FindType.FindByApplicationPolicy, "1.3.6.1.5.5.7.3.3", false);

                    Assert.Equal(2, results.Count);

                    Assert.True(results.Contains(msCer));
                    Assert.True(results.Contains(pfxCer));

                    foreach (X509Certificate2 match in results)
                    {
                        match.Dispose();
                    }
                });
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByApplicationPolicy_NoPolicyAlwaysMatches()
        {
            // PfxCer doesn't have any application policies which means it's good for all usages (even nonsensical ones.)
            RunSingleMatchTest_PfxCer(X509FindType.FindByApplicationPolicy, "2.2");
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByApplicationPolicy_NoMatch()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    // Per TestByApplicationPolicy_NoPolicyAlwaysMatches we know that pfxCer will match, so remove it.
                    col1.Remove(pfxCer);

                    X509Certificate2Collection results =
                        col1.Find(X509FindType.FindByApplicationPolicy, "2.2", false);

                    Assert.Equal(0, results.Count);
                });
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByCertificatePolicies_MatchA()
        {
            using (var policyCert = new X509Certificate2(TestData.CertWithPolicies))
            {
                EvaluateSingleMatch(
                    policyCert,
                    new X509Certificate2Collection(policyCert),
                    X509FindType.FindByCertificatePolicy,
                    "2.18.19");
            }
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByCertificatePolicies_MatchB()
        {
            using (var policyCert = new X509Certificate2(TestData.CertWithPolicies))
            {
                EvaluateSingleMatch(
                    policyCert,
                    new X509Certificate2Collection(policyCert),
                    X509FindType.FindByCertificatePolicy,
                    "2.32.33");
            }
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByCertificatePolicies_NoMatch()
        {
            using (var policyCert = new X509Certificate2(TestData.CertWithPolicies))
            {
                X509Certificate2Collection col1 = new X509Certificate2Collection(policyCert);

                X509Certificate2Collection results = col1.Find(X509FindType.FindByCertificatePolicy, "2.999", false);
                Assert.Equal(0, results.Count);
            }
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByTemplate_MatchA()
        {
            using (var templatedCert = new X509Certificate2(TestData.CertWithTemplateData))
            {
                EvaluateSingleMatch(
                    templatedCert,
                    new X509Certificate2Collection(templatedCert),
                    X509FindType.FindByTemplateName,
                    "Hello");
            }
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByTemplate_MatchB()
        {
            using (var templatedCert = new X509Certificate2(TestData.CertWithTemplateData))
            {
                EvaluateSingleMatch(
                    templatedCert,
                    new X509Certificate2Collection(templatedCert),
                    X509FindType.FindByTemplateName,
                    "2.7.8.9");
            }
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestByTemplate_NoMatch()
        {
            using (var templatedCert = new X509Certificate2(TestData.CertWithTemplateData))
            {
                X509Certificate2Collection col1 = new X509Certificate2Collection(templatedCert);

                X509Certificate2Collection results = col1.Find(X509FindType.FindByTemplateName, "2.999", false);
                Assert.Equal(0, results.Count);
            }
        }
    }
}
