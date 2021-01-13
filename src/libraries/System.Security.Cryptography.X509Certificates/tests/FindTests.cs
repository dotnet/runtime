// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class FindTests
    {
        private const string LeftToRightMark = "\u200E";

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

                    using (new ImportedCollection(col2))
                    {
                        Assert.Equal(0, col2.Count);
                    }
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

            using (new ImportedCollection(col2))
            {
                Assert.Equal(1, col2.Count);

                byte[] serialNumber;

                using (X509Certificate2 match = col2[0])
                {
                    Assert.Equal(expected, match);
                    Assert.NotSame(expected, match);

                    // FriendlyName is Windows-only.
                    if (OperatingSystem.IsWindows())
                    {
                        // Verify that the find result and original are linked, not just equal.
                        match.FriendlyName = "HAHA";
                        Assert.Equal("HAHA", expected.FriendlyName);
                    }

                    serialNumber = match.GetSerialNumber();
                }

                // Check that disposing match didn't dispose expected
                Assert.Equal(serialNumber, expected.GetSerialNumber());
            }
        }

        [Theory]
        [MemberData(nameof(GenerateInvalidInputs))]
        public static void FindWithWrongTypeValue(X509FindType findType, Type badValueType)
        {
            object badValue;

            if (badValueType == typeof(object))
            {
                badValue = new object();
            }
            else if (badValueType == typeof(DateTime))
            {
                badValue = DateTime.Now;
            }
            else if (badValueType == typeof(byte[]))
            {
                badValue = Array.Empty<byte>();
            }
            else if (badValueType == typeof(string))
            {
                badValue = "Hello, world";
            }
            else
            {
                throw new InvalidOperationException("No creator exists for type " + badValueType);
            }

            RunExceptionTest<CryptographicException>(findType, badValue);
        }

        [Theory]
        [MemberData(nameof(GenerateInvalidOidInputs))]
        public static void FindWithBadOids(X509FindType findType, string badOid)
        {
            RunExceptionTest<ArgumentException>(findType, badOid);
        }

        [Fact]
        public static void FindByNullName()
        {
            RunExceptionTest<ArgumentNullException>(X509FindType.FindBySubjectName, null);
        }

        [Fact]
        public static void FindByInvalidThumbprint()
        {
            RunZeroMatchTest(X509FindType.FindByThumbprint, "Nothing");
        }

        [Fact]
        public static void FindByInvalidThumbprint_RightLength()
        {
            RunZeroMatchTest(X509FindType.FindByThumbprint, "ffffffffffffffffffffffffffffffffffffffff");
        }

        [Fact]
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
        public static void FindByThumbprint_WithLrm()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    EvaluateSingleMatch(
                        pfxCer,
                        col1,
                        X509FindType.FindByThumbprint,
                        LeftToRightMark + pfxCer.Thumbprint);
                });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void FindByValidThumbprint_ValidOnly(bool validOnly)
        {
            using (var msCer = new X509Certificate2(TestData.MsCertificate))
            {
                var col1 = new X509Certificate2Collection(msCer);

                X509Certificate2Collection col2 =
                    col1.Find(X509FindType.FindByThumbprint, msCer.Thumbprint, validOnly);

                using (new ImportedCollection(col2))
                {
                    // The certificate is expired. Unless we invent time travel
                    // (or roll the computer clock back significantly), the validOnly
                    // criteria should filter it out.
                    //
                    // This test runs both ways to make sure that the precondition of
                    // "would match otherwise" is met (in the validOnly=false case it is
                    // effectively the same as FindByValidThumbprint, but does less inspection)
                    int expectedMatchCount = validOnly ? 0 : 1;

                    Assert.Equal(expectedMatchCount, col2.Count);

                    if (!validOnly)
                    {
                        // Double check that turning on validOnly doesn't prevent the cloning
                        // behavior of Find.
                        using (X509Certificate2 match = col2[0])
                        {
                            Assert.Equal(msCer, match);
                            Assert.NotSame(msCer, match);
                        }
                    }
                }
            }
        }

        [Fact]
        public static void FindByValidThumbprint_RootCert()
        {
            using (X509Store machineRoot = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
            {
                machineRoot.Open(OpenFlags.ReadOnly);

                using (var watchedStoreCerts = new ImportedCollection(machineRoot.Certificates))
                {
                    X509Certificate2Collection storeCerts = watchedStoreCerts.Collection;
                    X509Certificate2 rootCert = null;
                    TimeSpan tolerance = TimeSpan.FromHours(12);

                    // These APIs use local time, so use DateTime.Now, not DateTime.UtcNow.
                    DateTime notBefore = DateTime.Now;
                    DateTime notAfter = DateTime.Now.Subtract(tolerance);

                    foreach (X509Certificate2 cert in storeCerts)
                    {
                        if (cert.NotBefore >= notBefore || cert.NotAfter <= notAfter)
                        {
                            // Not (safely) valid, skip.
                            continue;
                        }

                        X509KeyUsageExtension keyUsageExtension = null;

                        foreach (X509Extension extension in cert.Extensions)
                        {
                            keyUsageExtension = extension as X509KeyUsageExtension;

                            if (keyUsageExtension != null)
                            {
                                break;
                            }
                        }

                        // Some tool is putting the com.apple.systemdefault utility cert in the
                        // LM\Root store on OSX machines; but it gets rejected by OpenSSL as an
                        // invalid root for not having the Certificate Signing key usage value.
                        //
                        // While the real problem seems to be with whatever tool is putting it
                        // in the bundle; we can work around it here.
                        const X509KeyUsageFlags RequiredFlags = X509KeyUsageFlags.KeyCertSign;

                        // No key usage extension means "good for all usages"
                        if (keyUsageExtension != null &&
                            (keyUsageExtension.KeyUsages & RequiredFlags) != RequiredFlags)
                        {
                            // Not a valid KeyUsage, skip.
                            continue;
                        }

                        using (ChainHolder chainHolder = new ChainHolder())
                        {
                            chainHolder.Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                            if (!chainHolder.Chain.Build(cert))
                            {
                                // Despite being not expired and having a valid KeyUsage, it's
                                // not considered a valid root/chain.
                                continue;
                            }
                        }

                        rootCert = cert;
                        break;
                    }

                    // Just in case someone has a system with no valid trusted root certs whatsoever.
                    if (rootCert != null)
                    {
                        X509Certificate2Collection matches =
                            storeCerts.Find(X509FindType.FindByThumbprint, rootCert.Thumbprint, true);

                        using (new ImportedCollection(matches))
                        {
                            // Improve the debuggability, since the root cert found on each
                            // machine might be different
                            if (matches.Count == 0)
                            {
                                Assert.True(
                                    false,
                                    $"Root certificate '{rootCert.Subject}' ({rootCert.NotBefore} - {rootCert.NotAfter}) is findable with thumbprint '{rootCert.Thumbprint}' and validOnly=true");
                            }

                            Assert.NotSame(rootCert, matches[0]);
                            Assert.Equal(rootCert, matches[0]);
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData("Nothing")]
        [InlineData("US, Redmond, Microsoft Corporation, MOPR, Microsoft Corporation")]
        public static void TestSubjectName_NoMatch(string subjectQualifier)
        {
            RunZeroMatchTest(X509FindType.FindBySubjectName, subjectQualifier);
        }

        [Theory]
        [InlineData("Microsoft Corporation")]
        [InlineData("MOPR")]
        [InlineData("Redmond")]
        [InlineData("Washington")]
        [InlineData("US")]
        [InlineData("US, Washington")]
        [InlineData("Washington, Redmond")]
        [InlineData("US, Washington, Redmond, Microsoft Corporation, MOPR, Microsoft Corporation")]
        [InlineData("us, washington, redmond, microsoft corporation, mopr, microsoft corporation")]
        public static void TestSubjectName_Match(string subjectQualifier)
        {
            RunSingleMatchTest_MsCer(X509FindType.FindBySubjectName, subjectQualifier);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Nothing")]
        [InlineData("ou=mopr, o=microsoft corporation")]
        [InlineData("CN=Microsoft Corporation")]
        [InlineData("CN=Microsoft Corporation,     OU=MOPR, O=Microsoft Corporation, L=Redmond, S=Washington, C=US")]
        [InlineData("CN=Microsoft Corporation, OU=MOPR, O=Microsoft Corporation, L=Redmond, S=Washington, C=US    ")]
        [InlineData("    CN=Microsoft Corporation, OU=MOPR, O=Microsoft Corporation, L=Redmond, S=Washington, C=US")]
        public static void TestDistinguishedSubjectName_NoMatch(string distinguishedSubjectName)
        {
            RunZeroMatchTest(X509FindType.FindBySubjectDistinguishedName, distinguishedSubjectName);
        }

        [Theory]
        [InlineData("CN=Microsoft Corporation, OU=MOPR, O=Microsoft Corporation, L=Redmond, S=Washington, C=US")]
        [InlineData("CN=microsoft corporation, OU=MOPR, O=Microsoft Corporation, L=Redmond, S=Washington, C=US")]
        [InlineData("cn=microsoft corporation, ou=mopr, o=microsoft corporation, l=redmond, s=washington, c=us")]
        public static void TestDistinguishedSubjectName_Match(string distinguishedSubjectName)
        {
            RunSingleMatchTest_MsCer(X509FindType.FindBySubjectDistinguishedName, distinguishedSubjectName);
        }

        [Fact]
        public static void TestIssuerName_NoMatch()
        {
            RunZeroMatchTest(X509FindType.FindByIssuerName, "Nothing");
        }

        [Theory]
        [InlineData("Microsoft Code Signing PCA")]
        [InlineData("Microsoft Corporation")]
        [InlineData("Redmond")]
        [InlineData("Washington")]
        [InlineData("US")]
        [InlineData("US, Washington")]
        [InlineData("Washington, Redmond")]
        [InlineData("US, Washington, Redmond, Microsoft Corporation, Microsoft Code Signing PCA")]
        [InlineData("us, washington, redmond, microsoft corporation, microsoft code signing pca")]
        public static void TestIssuerName_Match(string issuerQualifier)
        {
            RunSingleMatchTest_MsCer(X509FindType.FindByIssuerName, issuerQualifier);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Nothing")]
        [InlineData("CN=Microsoft Code Signing PCA")]
        [InlineData("CN=Microsoft Code Signing PCA,     O=Microsoft Corporation, L=Redmond, S=Washington, C=US")]
        [InlineData("CN=Microsoft Code Signing PCA, O=Microsoft Corporation, L=Redmond, S=Washington, C=US    ")]
        [InlineData("    CN=Microsoft Code Signing PCA, O=Microsoft Corporation, L=Redmond, S=Washington, C=US")]
        public static void TestDistinguishedIssuerName_NoMatch(string issuerDistinguishedName)
        {
            RunZeroMatchTest(X509FindType.FindByIssuerDistinguishedName, issuerDistinguishedName);
        }

        [Theory]
        [InlineData("CN=Microsoft Code Signing PCA, O=Microsoft Corporation, L=Redmond, S=Washington, C=US")]
        [InlineData("CN=microsoft Code signing pca, O=Microsoft Corporation, L=Redmond, S=Washington, C=US")]
        [InlineData("cn=microsoft code signing pca, o=microsoft corporation, l=redmond, s=washington, c=us")]
        public static void TestDistinguishedIssuerName_Match(string issuerDistinguishedName)
        {
            RunSingleMatchTest_MsCer(X509FindType.FindByIssuerDistinguishedName, issuerDistinguishedName);
        }

        [Fact]
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

                    using (new ImportedCollection(col2))
                    {
                        Assert.Equal(0, col2.Count);
                    }
                });
        }

        [Fact]
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

                    using (new ImportedCollection(col2))
                    {
                        Assert.Equal(0, col2.Count);
                    }
                });
        }

        [Fact]
        public static void TestByTimeValid_Between()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    DateTime earliestNotAfter = new[] { msCer.NotAfter, pfxCer.NotAfter }.Min();
                    DateTime latestNotBefore = new[] { msCer.NotBefore, pfxCer.NotBefore }.Max();

                    TimeSpan gap = latestNotBefore - earliestNotAfter;

                    // If this assert fails it means our test data was rebuilt and the constraint
                    // can no longer be satisfied
                    Assert.True(gap > TimeSpan.FromSeconds(1));

                    DateTime noMatchTime = earliestNotAfter + TimeSpan.FromSeconds(1);

                    X509Certificate2Collection col2 = col1.Find(
                        X509FindType.FindByTimeValid,
                        noMatchTime,
                        validOnly: false);

                    using (new ImportedCollection(col2))
                    {
                        Assert.Equal(0, col2.Count);
                    }
                });
        }

        [Fact]
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
        public static void TestFindByTimeNotYetValid_Match()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    DateTime earliestNotAfter = new[] { msCer.NotAfter, pfxCer.NotAfter }.Min();
                    DateTime latestNotBefore = new[] { msCer.NotBefore, pfxCer.NotBefore }.Max();

                    TimeSpan gap = latestNotBefore - earliestNotAfter;

                    // If this assert fails it means our test data was rebuilt and the constraint
                    // can no longer be satisfied
                    Assert.True(gap > TimeSpan.FromSeconds(1));

                    // One second before the latest NotBefore, so one is valid, the other is not yet valid.
                    DateTime matchTime = latestNotBefore - TimeSpan.FromSeconds(1);

                    X509Certificate2Collection col2 = col1.Find(
                        X509FindType.FindByTimeNotYetValid,
                        matchTime,
                        validOnly: false);

                    using (new ImportedCollection(col2))
                    {
                        Assert.Equal(1, col2.Count);
                    }
                });
        }

        [Fact]
        public static void TestFindByTimeNotYetValid_NoMatch()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    DateTime earliestNotAfter = new[] { msCer.NotAfter, pfxCer.NotAfter }.Min();
                    DateTime latestNotBefore = new[] { msCer.NotBefore, pfxCer.NotBefore }.Max();

                    TimeSpan gap = latestNotBefore - earliestNotAfter;

                    // If this assert fails it means our test data was rebuilt and the constraint
                    // can no longer be satisfied
                    Assert.True(gap > TimeSpan.FromSeconds(1));

                    // One second after the latest NotBefore, both certificates are time-valid
                    DateTime noMatchTime = latestNotBefore + TimeSpan.FromSeconds(1);

                    X509Certificate2Collection col2 = col1.Find(
                        X509FindType.FindByTimeNotYetValid,
                        noMatchTime,
                        validOnly: false);

                    using (new ImportedCollection(col2))
                    {
                        Assert.Equal(0, col2.Count);
                    }
                });
        }

        [Fact]
        public static void TestFindByTimeExpired_Match()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    DateTime earliestNotAfter = new[] { msCer.NotAfter, pfxCer.NotAfter }.Min();
                    DateTime latestNotBefore = new[] { msCer.NotBefore, pfxCer.NotBefore }.Max();

                    TimeSpan gap = latestNotBefore - earliestNotAfter;

                    // If this assert fails it means our test data was rebuilt and the constraint
                    // can no longer be satisfied
                    Assert.True(gap > TimeSpan.FromSeconds(1));

                    // One second after the earliest NotAfter, so one is valid, the other is no longer valid.
                    DateTime matchTime = earliestNotAfter + TimeSpan.FromSeconds(1);

                    X509Certificate2Collection col2 = col1.Find(
                        X509FindType.FindByTimeExpired,
                        matchTime,
                        validOnly: false);

                    using (new ImportedCollection(col2))
                    {
                        Assert.Equal(1, col2.Count);
                    }
                });
        }

        [Fact]
        public static void TestFindByTimeExpired_NoMatch()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    DateTime earliestNotAfter = new[] { msCer.NotAfter, pfxCer.NotAfter }.Min();
                    DateTime latestNotBefore = new[] { msCer.NotBefore, pfxCer.NotBefore }.Max();

                    TimeSpan gap = latestNotBefore - earliestNotAfter;

                    // If this assert fails it means our test data was rebuilt and the constraint
                    // can no longer be satisfied
                    Assert.True(gap > TimeSpan.FromSeconds(1));

                    // One second before the earliest NotAfter, so both certificates are valid
                    DateTime noMatchTime = earliestNotAfter - TimeSpan.FromSeconds(1);

                    X509Certificate2Collection col2 = col1.Find(
                        X509FindType.FindByTimeExpired,
                        noMatchTime,
                        validOnly: false);

                    using (new ImportedCollection(col2))
                    {
                        Assert.Equal(0, col2.Count);
                    }
                });
        }

        [Fact]
        public static void TestBySerialNumber_Decimal()
        {
            // Decimal string is an allowed input format.
            RunSingleMatchTest_PfxCer(
                X509FindType.FindBySerialNumber,
                "284069184166497622998429950103047369500");
        }

        [Fact]
        public static void TestBySerialNumber_DecimalLeadingZeros()
        {
            // Checking that leading zeros are ignored.
            RunSingleMatchTest_PfxCer(
               X509FindType.FindBySerialNumber,
               "000" + "284069184166497622998429950103047369500");
        }

        [Theory]
        [InlineData("1137338006039264696476027508428304567989436592")]
        // Leading zeroes are fine/ignored
        [InlineData("0001137338006039264696476027508428304567989436592")]
        // Compat: Minus signs are ignored
        [InlineData("-1137338006039264696476027508428304567989436592")]
        public static void TestBySerialNumber_Decimal_CertB(string serialNumber)
        {
            RunSingleMatchTest_MsCer(X509FindType.FindBySerialNumber, serialNumber);
        }

        [Fact]
        public static void TestBySerialNumber_Hex()
        {
            // Hex string is also an allowed input format.
            RunSingleMatchTest_PfxCer(
                X509FindType.FindBySerialNumber,
                "D5B5BC1C458A558845BFF51CB4DFF31C");
        }

        [Fact]
        public static void TestBySerialNumber_HexIgnoreCase()
        {
            // Hex string is also an allowed input format and case-blind
            RunSingleMatchTest_PfxCer(
                X509FindType.FindBySerialNumber,
                "d5b5bc1c458a558845bff51cb4dff31c");
        }

        [Fact]
        public static void TestBySerialNumber_HexLeadingZeros()
        {
            // Checking that leading zeros are ignored.
            RunSingleMatchTest_PfxCer(
                X509FindType.FindBySerialNumber,
                "0000" + "D5B5BC1C458A558845BFF51CB4DFF31C");
        }

        [Fact]
        public static void TestBySerialNumber_WithSpaces()
        {
            // Hex string is also an allowed input format and case-blind
            RunSingleMatchTest_PfxCer(
                X509FindType.FindBySerialNumber,
                "d5 b5 bc 1c 45 8a 55 88 45 bf f5 1c b4 df f3 1c");
        }

        [Fact]
        public static void TestBySerialNumber_WithLRM()
        {
            // Hex string is also an allowed input format and case-blind
            RunSingleMatchTest_PfxCer(
                X509FindType.FindBySerialNumber,
                LeftToRightMark + "d5 b5 bc 1c 45 8a 55 88 45 bf f5 1c b4 df f3 1c");
        }

        [Fact]
        public static void TestBySerialNumber_NoMatch()
        {
            RunZeroMatchTest(
                X509FindType.FindBySerialNumber,
                "23000000B011AF0A8BD03B9FDD0001000000B0");
        }

        [Theory]
        [MemberData(nameof(GenerateWorkingFauxSerialNumbers))]
        public static void TestBySerialNumber_Match_NonDecimalInput(string input)
        {
            RunSingleMatchTest_MsCer(X509FindType.FindBySerialNumber, input);
        }

        [Fact]
        public static void TestByExtension_FriendlyName()
        {
            // Cannot just say "Enhanced Key Usage" here because the extension name is localized.
            // Instead, look it up via the OID.
            RunSingleMatchTest_MsCer(X509FindType.FindByExtension, new Oid("2.5.29.37").FriendlyName);
        }

        [Fact]
        public static void TestByExtension_OidValue()
        {
            RunSingleMatchTest_MsCer(X509FindType.FindByExtension, "2.5.29.37");
        }

        [Fact]
        // Compat: Non-ASCII digits don't throw, but don't match.
        public static void TestByExtension_OidValue_ArabicNumericChar()
        {
            // This uses the same OID as TestByExtension_OidValue, but uses "Arabic-Indic Digit Two" instead
            // of "Digit Two" in the third segment.  This value can't possibly ever match, but it doesn't throw
            // as an illegal OID value.
            RunZeroMatchTest(X509FindType.FindByExtension, "2.5.\u06629.37");
        }

        [Fact]
        public static void TestByExtension_UnknownFriendlyName()
        {
            RunExceptionTest<ArgumentException>(X509FindType.FindByExtension, "BOGUS");
        }

        [Fact]
        public static void TestByExtension_NoMatch()
        {
            RunZeroMatchTest(X509FindType.FindByExtension, "2.9");
        }

        [Fact]
        public static void TestBySubjectKeyIdentifier_UsingFallback()
        {
            RunSingleMatchTest_PfxCer(
                X509FindType.FindBySubjectKeyIdentifier,
                "B4D738B2D4978AFF290A0B02987BABD114FEE9C7");
        }

        [Theory]
        [InlineData("5971A65A334DDA980780FF841EBE87F9723241F2")]
        // Whitespace is allowed
        [InlineData("59 71\tA6 5A 33 4D DA 98 07 80 FF 84 1E BE 87 F9 72 32 41 F2")]
        // Lots of kinds of whitespace (does not include \u000b or \u000c, because those
        // produce a build warning (which becomes an error):
        //     EXEC : warning : '(not included here)', hexadecimal value 0x0C, is an invalid character.
        [InlineData(
            "59\u000971\u000aA6\u30005A\u205f33\u000d4D\u0020DA\u008598\u00a007\u1680" +
            "80\u2000FF\u200184\u20021E\u2003BE\u200487\u2005F9\u200672\u200732\u2008" +
            "4\u20091\u200aF\u20282\u2029\u202f")]
        // Non-byte-aligned whitespace is allowed
        [InlineData("597 1A6 5A3 34D DA9 807 80F F84 1EB E87 F97 232 41F 2")]
        // Non-symmetric whitespace is allowed
        [InlineData("    5971A65   A334DDA980780FF84  1EBE87F97           23241F   2")]
        public static void TestBySubjectKeyIdentifier_ExtensionPresent(string subjectKeyIdentifier)
        {
            RunSingleMatchTest_MsCer(X509FindType.FindBySubjectKeyIdentifier, subjectKeyIdentifier);
        }

        // Should ignore Left-to-right mark \u200E
        [Theory]
        [InlineData(LeftToRightMark + "59 71 A6 5A 33 4D DA 98 07 80 FF 84 1E BE 87 F9 72 32 41 F2")]
        // Compat: Lone trailing nybbles are ignored
        [InlineData(LeftToRightMark + "59 71 A6 5A 33 4D DA 98 07 80 FF 84 1E BE 87 F9 72 32 41 F2 3")]
        // Compat: Lone trailing nybbles are ignored, even if not hex
        [InlineData(LeftToRightMark + "59 71 A6 5A 33 4D DA 98 07 80 FF 84 1E BE 87 F9 72 32 41 F2 p")]
        public static void TestBySubjectKeyIdentifier_ExtensionPresentWithLTM(string subjectKeyIdentifier)
        {
            RunSingleMatchTest_MsCer(X509FindType.FindBySubjectKeyIdentifier, subjectKeyIdentifier);
        }

        [Theory]
        // Compat: Lone trailing nybbles are ignored
        [InlineData("59 71 A6 5A 33 4D DA 98 07 80 FF 84 1E BE 87 F9 72 32 41 F2 3")]
        // Compat: Lone trailing nybbles are ignored, even if not hex
        [InlineData("59 71 A6 5A 33 4D DA 98 07 80 FF 84 1E BE 87 F9 72 32 41 F2 p")]
        // Compat: A non-hex character as the high nybble makes that nybble be F
        [InlineData("59 71 A6 5A 33 4D DA 98 07 80 FF 84 1E BE 87 p9 72 32 41 F2")]
        // Compat: A non-hex character as the low nybble makes the whole byte FF.
        [InlineData("59 71 A6 5A 33 4D DA 98 07 80 0p 84 1E BE 87 F9 72 32 41 F2")]
        public static void TestBySubjectKeyIdentifier_Compat(string subjectKeyIdentifier)
        {
            RunSingleMatchTest_MsCer(X509FindType.FindBySubjectKeyIdentifier, subjectKeyIdentifier);
        }

        [Fact]
        public static void TestBySubjectKeyIdentifier_NoMatch()
        {
            RunZeroMatchTest(X509FindType.FindBySubjectKeyIdentifier, "");
        }

        [Fact]
        public static void TestBySubjectKeyIdentifier_NoMatch_RightLength()
        {
            RunZeroMatchTest(
                X509FindType.FindBySubjectKeyIdentifier,
                "5971A65A334DDA980780FF841EBE87F9723241F0");
        }

        [Fact]
        public static void TestByApplicationPolicy_MatchAll()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    X509Certificate2Collection results =
                        col1.Find(X509FindType.FindByApplicationPolicy, "1.3.6.1.5.5.7.3.3", false);

                    using (new ImportedCollection(results))
                    {
                        Assert.Equal(2, results.Count);

                        Assert.True(results.Contains(msCer));
                        Assert.True(results.Contains(pfxCer));
                    }
                });
        }

        [Fact]
        public static void TestByApplicationPolicy_NoPolicyAlwaysMatches()
        {
            // PfxCer doesn't have any application policies which means it's good for all usages (even nonsensical ones.)
            RunSingleMatchTest_PfxCer(X509FindType.FindByApplicationPolicy, "2.2");
        }

        [Fact]
        public static void TestByApplicationPolicy_NoMatch()
        {
            RunTest(
                (msCer, pfxCer, col1) =>
                {
                    // Per TestByApplicationPolicy_NoPolicyAlwaysMatches we know that pfxCer will match, so remove it.
                    col1.Remove(pfxCer);

                    X509Certificate2Collection results =
                        col1.Find(X509FindType.FindByApplicationPolicy, "2.2", false);

                    using (new ImportedCollection(results))
                    {
                        Assert.Equal(0, results.Count);
                    }
                });
        }

        [Fact]
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
        public static void TestByCertificatePolicies_NoMatch()
        {
            using (var policyCert = new X509Certificate2(TestData.CertWithPolicies))
            {
                X509Certificate2Collection col1 = new X509Certificate2Collection(policyCert);

                X509Certificate2Collection results = col1.Find(X509FindType.FindByCertificatePolicy, "2.999", false);

                using (new ImportedCollection(results))
                {
                    Assert.Equal(0, results.Count);
                }
            }
        }

        [Fact]
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
        public static void TestByTemplate_NoMatch()
        {
            using (var templatedCert = new X509Certificate2(TestData.CertWithTemplateData))
            {
                X509Certificate2Collection col1 = new X509Certificate2Collection(templatedCert);

                X509Certificate2Collection results = col1.Find(X509FindType.FindByTemplateName, "2.999", false);

                using (new ImportedCollection(results))
                {
                    Assert.Equal(0, results.Count);
                }
            }
        }

        [Theory]
        [InlineData((int)0x80)]
        [InlineData((uint)0x80)]
        [InlineData(X509KeyUsageFlags.DigitalSignature)]
        [InlineData("DigitalSignature")]
        [InlineData("digitalSignature")]
        public static void TestFindByKeyUsage_Match(object matchCriteria)
        {
            TestFindByKeyUsage(true, matchCriteria);
        }

        [Theory]
        [InlineData((int)0x20)]
        [InlineData((uint)0x20)]
        [InlineData(X509KeyUsageFlags.KeyEncipherment)]
        [InlineData("KeyEncipherment")]
        [InlineData("KEYEncipherment")]
        public static void TestFindByKeyUsage_NoMatch(object matchCriteria)
        {
            TestFindByKeyUsage(false, matchCriteria);
        }

        private static void TestFindByKeyUsage(bool shouldMatch, object matchCriteria)
        {
            using (var noKeyUsages = new X509Certificate2(TestData.MsCertificate))
            using (var noKeyUsages2 = new X509Certificate2(Path.Combine("TestData", "test.cer")))
            using (var keyUsages = new X509Certificate2(Path.Combine("TestData", "microsoft.cer")))
            {
                var coll = new X509Certificate2Collection { noKeyUsages, noKeyUsages2, keyUsages, };
                X509Certificate2Collection results = coll.Find(X509FindType.FindByKeyUsage, matchCriteria, false);

                using (new ImportedCollection(results))
                {
                    // The two certificates with no KeyUsages extension will always match,
                    // the real question is about the third.
                    int matchCount = shouldMatch ? 3 : 2;
                    Assert.Equal(matchCount, results.Count);

                    if (shouldMatch)
                    {
                        bool found = false;

                        foreach (X509Certificate2 cert in results)
                        {
                            if (keyUsages.Equals(cert))
                            {
                                Assert.NotSame(cert, keyUsages);
                                found = true;
                                break;
                            }
                        }

                        Assert.True(found, "Certificate with key usages was found in the collection");
                    }
                    else
                    {
                        Assert.False(
                            results.Contains(keyUsages),
                            "KeyUsages certificate is not present in the collection");
                    }
                }
            }
        }

        public static IEnumerable<object[]> GenerateWorkingFauxSerialNumbers
        {
            get
            {
                const string seedDec = "1137338006039264696476027508428304567989436592";

                string[] nonHexWords = { "wow", "its", "tough", "using", "only", "high", "lttr", "vlus" };
                string gluedTogether = string.Join("", nonHexWords);
                string withSpaces = string.Join(" ", nonHexWords);

                yield return new object[] { gluedTogether + seedDec };
                yield return new object[] { seedDec + gluedTogether };
                yield return new object[] { withSpaces + seedDec };
                yield return new object[] { seedDec + withSpaces };

                StringBuilder builderDec = new StringBuilder(512);
                int offsetDec = 0;

                for (int i = 0; i < nonHexWords.Length; i++)
                {
                    if (offsetDec < seedDec.Length)
                    {
                        int appendLen = Math.Max(1, seedDec.Length / nonHexWords.Length);
                        appendLen = Math.Min(appendLen, seedDec.Length - offsetDec);

                        builderDec.Append(seedDec, offsetDec, appendLen);
                        offsetDec += appendLen;
                    }

                    builderDec.Append(nonHexWords[i]);
                }

                builderDec.Append(seedDec, offsetDec, seedDec.Length - offsetDec);

                yield return new object[] { builderDec.ToString() };

                builderDec.Length = 0;
                offsetDec = 0;

                for (int i = 0; i < nonHexWords.Length; i++)
                {
                    if (offsetDec < seedDec.Length)
                    {
                        int appendLen = Math.Max(1, seedDec.Length / nonHexWords.Length);
                        appendLen = Math.Min(appendLen, seedDec.Length - offsetDec);

                        builderDec.Append(seedDec, offsetDec, appendLen);
                        offsetDec += appendLen;

                        builderDec.Append(' ');
                    }

                    builderDec.Append(nonHexWords[i]);
                    builderDec.Append(' ');
                }

                builderDec.Append(seedDec, offsetDec, seedDec.Length - offsetDec);

                yield return new object[] { builderDec.ToString() };
            }
        }

        public static IEnumerable<object[]> GenerateInvalidOidInputs
        {
            get
            {
                X509FindType[] oidTypes =
                {
                    X509FindType.FindByApplicationPolicy,
                };

                string[] invalidOids =
                {
                    "",
                    "This Is Not An Oid",
                    "1",
                    "95.22",
                    ".1",
                    "1..1",
                    "1.",
                    "1.2.",
                };

                List<object[]> combinations = new List<object[]>(oidTypes.Length * invalidOids.Length);

                for (int findTypeIndex = 0; findTypeIndex < oidTypes.Length; findTypeIndex++)
                {
                    for (int oidIndex = 0; oidIndex < invalidOids.Length; oidIndex++)
                    {
                        combinations.Add(new object[] { oidTypes[findTypeIndex], invalidOids[oidIndex] });
                    }
                }

                return combinations;
            }
        }

        public static IEnumerable<object[]> GenerateInvalidInputs
        {
            get
            {
                Type[] allTypes =
                {
                    typeof(object),
                    typeof(DateTime),
                    typeof(byte[]),
                    typeof(string),
                };

                Tuple<X509FindType, Type>[] legalInputs =
                {
                    Tuple.Create(X509FindType.FindByThumbprint, typeof(string)),
                    Tuple.Create(X509FindType.FindBySubjectName, typeof(string)),
                    Tuple.Create(X509FindType.FindBySubjectDistinguishedName, typeof(string)),
                    Tuple.Create(X509FindType.FindByIssuerName, typeof(string)),
                    Tuple.Create(X509FindType.FindByIssuerDistinguishedName, typeof(string)),
                    Tuple.Create(X509FindType.FindBySerialNumber, typeof(string)),
                    Tuple.Create(X509FindType.FindByTimeValid, typeof(DateTime)),
                    Tuple.Create(X509FindType.FindByTimeNotYetValid, typeof(DateTime)),
                    Tuple.Create(X509FindType.FindByTimeExpired, typeof(DateTime)),
                    Tuple.Create(X509FindType.FindByTemplateName, typeof(string)),
                    Tuple.Create(X509FindType.FindByApplicationPolicy, typeof(string)),
                    Tuple.Create(X509FindType.FindByCertificatePolicy, typeof(string)),
                    Tuple.Create(X509FindType.FindByExtension, typeof(string)),
                    // KeyUsage supports int/uint/KeyUsage/string, but only one of those is in allTypes.
                    Tuple.Create(X509FindType.FindByKeyUsage, typeof(string)),
                    Tuple.Create(X509FindType.FindBySubjectKeyIdentifier, typeof(string)),
                };

                List<object[]> invalidCombinations = new List<object[]>();

                for (int findTypesIndex = 0; findTypesIndex < legalInputs.Length; findTypesIndex++)
                {
                    Tuple<X509FindType, Type> tuple = legalInputs[findTypesIndex];

                    for (int typeIndex = 0; typeIndex < allTypes.Length; typeIndex++)
                    {
                        Type t = allTypes[typeIndex];

                        if (t != tuple.Item2)
                        {
                            invalidCombinations.Add(new object[] { tuple.Item1, t });
                        }
                    }
                }

                return invalidCombinations;
            }
        }
    }
}
