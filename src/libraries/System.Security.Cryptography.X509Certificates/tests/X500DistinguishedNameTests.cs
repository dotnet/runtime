// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class X500DistinguishedNameTests
    {
        [Fact]
        public static void PrintInvalidEncoding()
        {
            // One byte has been removed from the payload here.  Since DER is length-prepended
            // this will run out of data too soon, and report as invalid.
            byte[] encoded = "3017311530130603550403130C436F6D6D6F6E204E616D65".HexToByteArray();

            X500DistinguishedName dn = new X500DistinguishedName(encoded);
            Assert.Equal("", dn.Decode(X500DistinguishedNameFlags.None));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void PrintMultiComponentRdn(bool fromSpan)
        {
            byte[] encoded = (
                "30223120300C060355040313054A616D65733010060355040A13094D6963726F" +
                "736F6674").HexToByteArray();

            const string expected = "CN=James + O=Microsoft";
            X500DistinguishedName dn;

            if (fromSpan)
            {
                dn = new X500DistinguishedName(new ReadOnlySpan<byte>(encoded));
            }
            else
            {
                dn = new X500DistinguishedName(encoded);
                byte[] readBack = dn.RawData;
                Assert.NotSame(readBack, encoded);
                Assert.Equal(readBack, encoded);
            }

            Assert.Equal(expected, dn.Decode(X500DistinguishedNameFlags.None));

            // It should not change ordering when reversed, since the two are one unit.
            Assert.Equal(expected, dn.Decode(X500DistinguishedNameFlags.Reversed));
        }

        [Fact]
        public static void PrintUnknownOidRdn()
        {
            byte[] encoded = (
                "30183116301406052901020203130B496E76616C6964204F6964").HexToByteArray();

            X500DistinguishedName dn = new X500DistinguishedName(encoded);
            Assert.Equal("OID.1.1.1.2.2.3=Invalid Oid", dn.Decode(X500DistinguishedNameFlags.None));
        }

        [Theory]
        [MemberData(nameof(WhitespaceBeforeCases))]
        public static void QuoteWhitespaceBefore(string expected, string hexEncoded)
        {
            byte[] encoded = hexEncoded.HexToByteArray();
            X500DistinguishedName dn = new X500DistinguishedName(encoded);
            Assert.Equal(expected, dn.Decode(X500DistinguishedNameFlags.None));
        }

        [Theory]
        [MemberData(nameof(WhitespaceBeforeCases))]
        public static void NoQuoteWhitespaceBefore(string expectedQuoted, string hexEncoded)
        {
            string expected = expectedQuoted.Replace("\"", "");
            byte[] encoded = hexEncoded.HexToByteArray();

            X500DistinguishedName dn = new X500DistinguishedName(encoded);
            Assert.Equal(expected, dn.Decode(X500DistinguishedNameFlags.DoNotUseQuotes));
        }

        [Theory]
        [MemberData(nameof(WhitespaceAfterCases))]
        public static void QuoteWhitespaceAfter(string expected, string hexEncoded)
        {
            byte[] encoded = hexEncoded.HexToByteArray();
            X500DistinguishedName dn = new X500DistinguishedName(encoded);
            Assert.Equal(expected, dn.Decode(X500DistinguishedNameFlags.None));
        }

        [Theory]
        [MemberData(nameof(WhitespaceAfterCases))]
        public static void NoQuoteWhitespaceAfter(string expectedQuoted, string hexEncoded)
        {
            string expected = expectedQuoted.Replace("\"", "");
            byte[] encoded = hexEncoded.HexToByteArray();

            X500DistinguishedName dn = new X500DistinguishedName(encoded);
            Assert.Equal(expected, dn.Decode(X500DistinguishedNameFlags.DoNotUseQuotes));
        }

        [Theory]
        [MemberData(nameof(QuotedContentsCases))]
        public static void QuoteByContents(string expected, string hexEncoded)
        {
            byte[] encoded = hexEncoded.HexToByteArray();
            X500DistinguishedName dn = new X500DistinguishedName(encoded);
            Assert.Equal(expected, dn.Decode(X500DistinguishedNameFlags.None));
        }

        [Theory]
        [MemberData(nameof(QuotedContentsCases))]
        public static void NoQuoteByContents(string expectedQuoted, string hexEncoded)
        {
            string expected = expectedQuoted.Replace("\"", "");
            byte[] encoded = hexEncoded.HexToByteArray();

            X500DistinguishedName dn = new X500DistinguishedName(encoded);
            Assert.Equal(expected, dn.Decode(X500DistinguishedNameFlags.DoNotUseQuotes));
        }

        [Theory]
        [MemberData(nameof(InternallyQuotedRDNs))]
        public static void QuotedWithQuotesAsAppropriate(string quoted, string notQuoted, string hexEncoded)
        {
            byte[] encoded = hexEncoded.HexToByteArray();
            X500DistinguishedName dn = new X500DistinguishedName(encoded);

            Assert.Equal(quoted, dn.Decode(X500DistinguishedNameFlags.None));
            Assert.Equal(notQuoted, dn.Decode(X500DistinguishedNameFlags.DoNotUseQuotes));
        }

        [Theory]
        [MemberData(nameof(T61Cases))]
        public static void T61Strings(string expected, string hexEncoded)
        {
            byte[] encoded = hexEncoded.HexToByteArray();
            X500DistinguishedName dn = new X500DistinguishedName(encoded);

            Assert.Equal(expected, dn.Name);
        }

        [Fact]
        public static void PrintComplexReversed()
        {
            byte[] encoded = MicrosoftDotComSubject.HexToByteArray();
            X500DistinguishedName dn = new X500DistinguishedName(encoded);

            const string expected =
                "CN=www.microsoft.com, OU=MSCOM, O=Microsoft Corporation, STREET=1 Microsoft Way, " +
                "L=Redmond, S=Washington, PostalCode=98052, C=US, SERIALNUMBER=600413485, ";

            // Windows 8.1 would continue the string with some unknown OIDs, but OpenSSL 1.0.1 can decode
            // at least businessCategory (2.5.4.15), and other Windows versions may do so in the future.
            //    "OID.2.5.4.15=Private Organization, OID.1.3.6.1.4.1.311.60.2.1.2=Washington, " +
            //    "OID.1.3.6.1.4.1.311.60.2.1.3=US";

            Assert.StartsWith(expected, dn.Decode(X500DistinguishedNameFlags.Reversed), StringComparison.Ordinal);
        }

        [Fact]
        public static void PrintComplexForwards()
        {
            byte[] encoded = MicrosoftDotComSubject.HexToByteArray();
            X500DistinguishedName dn = new X500DistinguishedName(encoded);

            const string expected =
                ", SERIALNUMBER=600413485, C=US, PostalCode=98052, S=Washington, L=Redmond, " +
                "STREET=1 Microsoft Way, O=Microsoft Corporation, OU=MSCOM, CN=www.microsoft.com";

            Assert.EndsWith(expected, dn.Decode(X500DistinguishedNameFlags.None), StringComparison.Ordinal);
        }

        [Fact]
        public static void EdgeCaseEmptyFormat()
        {
            X500DistinguishedName dn = new X500DistinguishedName("");
            Assert.Equal(string.Empty, dn.Format(true));
            Assert.Equal(string.Empty, dn.Format(false));
        }

        [Fact]
        public static void EdgeCaseUseCommaAndNewLines()
        {
            const string rname = "C=US, O=\"RSA Data Security, Inc.\", OU=Secure Server Certification Authority";
            X500DistinguishedName dn = new X500DistinguishedName(rname, X500DistinguishedNameFlags.None);
            Assert.Equal(rname, dn.Decode(X500DistinguishedNameFlags.UseCommas | X500DistinguishedNameFlags.UseNewLines));
        }

        [Fact]
        public static void TpmIdentifiers()
        {
            // On Windows the X.500 name pretty printer is in crypt32, so it doesn't use our OidLookup.
            // Windows 7 doesn't have the TPM OIDs mapped, so they come back as (e.g.) OID.2.23.133.2.3 still.
            //
            // Just skip this test there.
            if (PlatformDetection.IsWindows7)
            {
                return;
            }

            X500DistinguishedName dn = new X500DistinguishedName("OID.2.23.133.2.3=id:0020065,OID.2.23.133.2.2=,OID.2.23.133.2.1=id:564D5700");
            X500DistinguishedName dn2 = new X500DistinguishedName(dn.RawData);
            Assert.Equal("TPMManufacturer=id:564D5700, TPMModel=\"\", TPMVersion=id:0020065", dn2.Decode(X500DistinguishedNameFlags.None));
        }

        [Fact]
        public static void NameWithNumericString()
        {
            X500DistinguishedName dn = new X500DistinguishedName(
                "30283117301506052901020203120C313233203635342037383930310D300B0603550403130454657374".HexToByteArray());

            Assert.Equal("OID.1.1.1.2.2.3=123 654 7890, CN=Test", dn.Decode(X500DistinguishedNameFlags.None));
        }

        [Fact]
        public static void OrganizationUnitMultiValueWithIncorrectlySortedDerSet()
        {
            X500DistinguishedName dn = new X500DistinguishedName(
                "301C311A300B060355040B13047A7A7A7A300B060355040B130461616161".HexToByteArray());

            Assert.Equal("OU=zzzz + OU=aaaa", dn.Decode(X500DistinguishedNameFlags.None));
        }

        [Fact]
        public static void NameWithSTIdentifierForState()
        {
            X500DistinguishedName dn = new X500DistinguishedName("ST=VA, C=US");
            Assert.Equal("C=US, S=VA", dn.Decode(X500DistinguishedNameFlags.None));
        }

        [Fact]
        public static void EnumeratorWithNonTextualData()
        {
            // OID.2.5.4.106=#06032A0304, CN=localhost, OU=.NET Framework (CoreFX), O=Microsoft Corporation,
            // L=Redmond, S=Washington, C=US
            byte[] encoded =
            {
                0x30, 0x81, 0x98, 0x31, 0x0B, 0x30, 0x09, 0x06, 0x03, 0x55, 0x04, 0x06, 0x13, 0x02, 0x55, 0x53,
                0x31, 0x13, 0x30, 0x11, 0x06, 0x03, 0x55, 0x04, 0x08, 0x13, 0x0A, 0x57, 0x61, 0x73, 0x68, 0x69,
                0x6E, 0x67, 0x74, 0x6F, 0x6E, 0x31, 0x10, 0x30, 0x0E, 0x06, 0x03, 0x55, 0x04, 0x07, 0x13, 0x07,
                0x52, 0x65, 0x64, 0x6D, 0x6F, 0x6E, 0x64, 0x31, 0x1E, 0x30, 0x1C, 0x06, 0x03, 0x55, 0x04, 0x0A,
                0x13, 0x15, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x20, 0x43, 0x6F, 0x72, 0x70,
                0x6F, 0x72, 0x61, 0x74, 0x69, 0x6F, 0x6E, 0x31, 0x20, 0x30, 0x1E, 0x06, 0x03, 0x55, 0x04, 0x0B,
                0x13, 0x17, 0x2E, 0x4E, 0x45, 0x54, 0x20, 0x46, 0x72, 0x61, 0x6D, 0x65, 0x77, 0x6F, 0x72, 0x6B,
                0x20, 0x28, 0x43, 0x6F, 0x72, 0x65, 0x46, 0x58, 0x29, 0x31, 0x12, 0x30, 0x10, 0x06, 0x03, 0x55,
                0x04, 0x03, 0x13, 0x09, 0x6C, 0x6F, 0x63, 0x61, 0x6C, 0x68, 0x6F, 0x73, 0x74, 0x31, 0x0C, 0x30,
                0x0A, 0x06, 0x03, 0x55, 0x04, 0x6A, 0x06, 0x03, 0x2A, 0x03, 0x04,
            };

            X500DistinguishedName dn = new X500DistinguishedName(encoded);
            IEnumerable<X500RelativeDistinguishedName> able = dn.EnumerateRelativeDistinguishedNames(false);
            IEnumerator<X500RelativeDistinguishedName> ator = able.GetEnumerator();

            int index = 0;
            AssertRDN(ator, "2.5.4.6", "US", ++index);
            AssertRDN(ator, "2.5.4.8", "Washington", ++index);
            AssertRDN(ator, "2.5.4.7", "Redmond", ++index);
            AssertRDN(ator, "2.5.4.10", "Microsoft Corporation", ++index);
            AssertRDN(ator, "2.5.4.11", ".NET Framework (CoreFX)", ++index);
            AssertRDN(ator, "2.5.4.3", "localhost", ++index);
            AssertRDN(ator, "2.5.4.106", null, ++index);

            Assert.False(ator.MoveNext(), $"ator.MoveNext() {++index}");

            static void AssertRDN(
                IEnumerator<X500RelativeDistinguishedName> ator,
                string typeOid,
                string? value,
                int index)
            {
                Assert.True(ator.MoveNext(), $"ator.MoveNext() {index}");
                Assert.False(ator.Current.HasMultipleElements, $"ator.Current.HasMultipleElements {index}");
                Assert.Equal(typeOid, ator.Current.GetSingleElementType().Value);
                Assert.Equal(value, ator.Current.GetSingleElementValue());
            }
        }

        [Fact]
        public static void EnumeratorWithInvalidData()
        {
            // A variety of encodings of a country name, except it has two versions of
            // the CountryCode3n attribute, both say they are NumericString (as matching the spec),
            // but the latter one has the correct value (840) and the earlier one has the 3c text (USA).
            //
            // C=US, n3=840, n3=USA, c3=USA, jurisdictionOfIncorporationCountryName=US
            X500DistinguishedName dn = new X500DistinguishedName((
                "304C31133011060B2B0601040182373C02010313025553310C300A0603550462" +
                "1303555341310C300A06035504631203555341310C300A060355046312033834" +
                "30310B3009060355040613025553").HexToByteArray());

            int index = 0;

            foreach (X500RelativeDistinguishedName rdn in dn.EnumerateRelativeDistinguishedNames(reversed: false))
            {
                switch (index)
                {
                    case 0:
                        AssertSimpleRDN(rdn, "1.3.6.1.4.1.311.60.2.1.3", "US", index);
                        break;
                    case 1:
                        AssertSimpleRDN(rdn, "2.5.4.98", "USA", index);
                        break;
                    case 2:
                        Assert.False(rdn.HasMultipleElements, $"rdn.HasMultipleElements {index}");
                        Assert.Equal("2.5.4.99", rdn.GetSingleElementType().Value);

                        CryptographicException ex = Assert.Throws<CryptographicException>(
                            () => rdn.GetSingleElementValue());

                        Assert.IsType<AsnContentException>(ex.InnerException);
                        Assert.IsType<DecoderFallbackException>(ex.InnerException.InnerException);
                        break;
                    case 3:
                        AssertSimpleRDN(rdn, "2.5.4.99", "840", index);
                        break;
                    case 4:
                        AssertSimpleRDN(rdn, "2.5.4.6", "US", index);
                        break;
                    default:
                        Assert.Fail($"Enumeration produced an unexpected {index}th result");
                        break;
                }

                index++;
            }

            Assert.Equal(5, index);

            static void AssertSimpleRDN(
                X500RelativeDistinguishedName rdn,
                string typeOid,
                string? value,
                int index)
            {
                Assert.False(rdn.HasMultipleElements, $"rdn.HasMultipleElements {index}");
                Assert.Equal(typeOid, rdn.GetSingleElementType().Value);
                Assert.Equal(value, rdn.GetSingleElementValue());
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void EnumerateWithMultiValueRdn(bool fromSpan)
        {
            // n3=840, CN=James + O=Microsoft, C=US
            ReadOnlySpan<byte> encoded = new byte[]
            {
                0x30, 0x3D, 0x31, 0x0B, 0x30, 0x09, 0x06, 0x03, 0x55, 0x04, 0x06, 0x13, 0x02, 0x55, 0x53, 0x31,
                0x20, 0x30, 0x0C, 0x06, 0x03, 0x55, 0x04, 0x03, 0x13, 0x05, 0x4A, 0x61, 0x6D, 0x65, 0x73, 0x30,
                0x10, 0x06, 0x03, 0x55, 0x04, 0x0A, 0x13, 0x09, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66,
                0x74, 0x31, 0x0C, 0x30, 0x0A, 0x06, 0x03, 0x55, 0x04, 0x63, 0x12, 0x03, 0x38, 0x34, 0x30,
            };

            X500DistinguishedName dn;

            if (fromSpan)
            {
                dn = new X500DistinguishedName(encoded);
            }
            else
            {
                byte[] tmp = encoded.ToArray();
                dn = new X500DistinguishedName(tmp);

                // Updating encoded here is important for the !Overlaps test.
                encoded = tmp;
            }

            int index = 0;

            foreach (X500RelativeDistinguishedName rdn in dn.EnumerateRelativeDistinguishedNames(reversed: false))
            {
                switch (index)
                {
                    case 0:
                        Assert.False(rdn.HasMultipleElements, $"rdn.HasMultipleElements {index}");
                        Assert.Equal("2.5.4.6", rdn.GetSingleElementType().Value);
                        Assert.Equal("US", rdn.GetSingleElementValue());
                        break;
                    case 1:
                        Assert.True(rdn.HasMultipleElements, $"rdn.HasMultipleElements {index}");
                        Assert.Throws<InvalidOperationException>(() => rdn.GetSingleElementType());
                        Assert.Throws<InvalidOperationException>(() => rdn.GetSingleElementValue());

                        ReadOnlySpan<byte> expected = encoded.Slice(15, 34);
                        AssertExtensions.SequenceEqual(expected, rdn.RawData.Span);
                        Assert.False(expected.Overlaps(rdn.RawData.Span), "expected.Overlaps(rdn.RawData.Span)");

                        break;
                    case 2:
                        Assert.False(rdn.HasMultipleElements, $"rdn.HasMultipleElements {index}");
                        Assert.Equal("2.5.4.99", rdn.GetSingleElementType().Value);
                        Assert.Equal("840", rdn.GetSingleElementValue());
                        break;
                    default:
                        Assert.Fail($"Enumeration produced an unexpected {index}th result");
                        break;
                }

                index++;
            }

            Assert.Equal(3, index);
        }

        [Fact]
        public static void CheckCachedOids()
        {
            // This test uses a couple of OIDs that should be cached, and a couple that are rare
            // and thus are not cached.  It's not intended to test the caching 100%, just that the
            // caching is matching some basic sanity.

            // n3=840, C=CA, E=totes not an email, n3=840 C=US, E=totes not an email
            X500DistinguishedName dn = new X500DistinguishedName((
                "307C3121301F06092A864886F70D0109011612746F746573206E6F7420616E20" +
                "656D61696C310B3009060355040613025553310C300A06035504631203383430" +
                "3121301F06092A864886F70D0109011612746F746573206E6F7420616E20656D" +
                "61696C310B3009060355040613024341310C300A06035504631203383430").HexToByteArray());

            List<X500RelativeDistinguishedName> rdns = dn.EnumerateRelativeDistinguishedNames().ToList();

            // n3 isn't cached, so the two Oid instances are not the same.
            Assert.NotSame(rdns[0].GetSingleElementType(), rdns[3].GetSingleElementType());
            Assert.Equal(rdns[0].GetSingleElementType().Value, rdns[3].GetSingleElementType().Value);
            Assert.Equal("2.5.4.99", rdns[0].GetSingleElementType().Value);

            // C is cached
            Assert.Same(rdns[1].GetSingleElementType(), rdns[4].GetSingleElementType());
            Assert.Equal("2.5.4.6", rdns[1].GetSingleElementType().Value);

            // E is cached
            Assert.Same(rdns[2].GetSingleElementType(), rdns[5].GetSingleElementType());
            Assert.Equal("1.2.840.113549.1.9.1", rdns[2].GetSingleElementType().Value);
        }

        public static readonly object[][] WhitespaceBeforeCases =
        {
            // Regular space.
            new object[]
            {
                "CN=\" Common Name\"",
                "3017311530130603550403130C20436F6D6D6F6E204E616D65"
            },

            // Tab
            new object[]
            {
                "CN=\"\tCommon Name\"",
                "30233121301F06035504031E1800090043006F006D006D006F006E0020004E00" +
                "61006D0065"
            },

            // Newline
            new object[]
            {
                "CN=\"\nCommon Name\"",
                "30233121301F06035504031E18000A0043006F006D006D006F006E0020004E00" +
                "61006D0065"

            },

            // xUnit doesn't like \v in Assert.Equals, reports it as an invalid character.
            //new object[]
            //{
            //    "CN=\"\vCommon Name\"",
            //    "30233121301F06035504031E18000B0043006F006D006D006F006E0020004E00" +
            //    "61006D0065"
            //},

            // xUnit doesn't like FormFeed in Assert.Equals, reports it as an invalid character.
            //new object[]
            //{
            //    "CN=\"\u000cCommon Name\"",
            //    "30233121301F06035504031E18000C0043006F006D006D006F006E0020004E00" +
            //    "61006D0065"
            //},

            // Carriage return
            new object[]
            {
                "CN=\"\rCommon Name\"",
                "30233121301F06035504031E18000D0043006F006D006D006F006E0020004E00" +
                "61006D0065"
            },

            // em quad.  This is char.IsWhitespace, but is not quoted.
            new object[]
            {
                "CN=\u2002Common Name",
                "30233121301F06035504031E1820020043006F006D006D006F006E0020004E00" +
                "61006D0065"
            },
        };

        public static readonly object[][] WhitespaceAfterCases =
        {
            // Regular space.
            new object[]
            {
                "CN=\"Common Name \"",
                "3017311530130603550403130C436F6D6D6F6E204E616D6520"
            },

            // Newline
            new object[]
            {
                "CN=\"Common Name\t\"",
                "30233121301F06035504031E180043006F006D006D006F006E0020004E006100" +
                "6D00650009"
            },

            // Newline
            new object[]
            {
                "CN=\"Common Name\n\"",
                "30233121301F06035504031E180043006F006D006D006F006E0020004E006100" +
                "6D0065000A"
            },

            // xUnit doesn't like \v in Assert.Equals, reports it as an invalid character.
            //new object[]
            //{
            //    "CN=\"Common Name\v\"",
            //    "30233121301F06035504031E180043006F006D006D006F006E0020004E006100" +
            //    "6D0065000B"
            //},

            // xUnit doesn't like FormFeed in Assert.Equals, reports it as an invalid character.
            //new object[]
            //{
            //    "CN=\"Common Name\u000c\"",
            //    "30233121301F06035504031E180043006F006D006D006F006E0020004E006100" +
            //    "6D0065000C"
            //},

             // Carriage return
            new object[]
            {
                "CN=\"Common Name\r\"",
                "30233121301F06035504031E180043006F006D006D006F006E0020004E006100" +
                "6D0065000D"
            },

            // em quad.  This is char.IsWhitespace, but is not quoted.
            new object[]
            {
                "CN=Common Name\u2002",
                "30233121301F06035504031E180043006F006D006D006F006E0020004E006100" +
                "6D00652002"
            },
        };

        public static readonly object[][] QuotedContentsCases =
        {
            // Empty value
            new object[]
            {
                "CN=\"\"",
                "300B3109300706035504031300"
            },

            // Comma (RDN separator)
            new object[]
            {
                "CN=\"Common,Name\"",
                "3016311430120603550403130B436F6D6D6F6E2C4E616D65"
            },

            // Plus (RDN component separator)
            new object[]
            {
                "CN=\"Common+Name\"",
                "3016311430120603550403130B436F6D6D6F6E2B4E616D65"
            },

            // Equal (Key/Value separator)
            new object[]
            {
                "CN=\"Common=Name\"",
                "3016311430120603550403130B436F6D6D6F6E3D4E616D65"
            },

            // Note: Double Quote has been removed from this set, it's a dedicated test suite.

            // Newline
            new object[]
            {
                "CN=\"Common\nName\"",
                "3021311F301D06035504031E160043006F006D006D006F006E000A004E006100" +
                "6D0065"
            },

            // Carriage return is NOT quoted.
            new object[]
            {
                "CN=Common\rName",
                "3021311F301D06035504031E160043006F006D006D006F006E000D004E006100" +
                "6D0065"
            },

            // Less-than
            new object[]
            {
                "CN=\"Common<Name\"",
                "3021311F301D06035504031E160043006F006D006D006F006E003C004E006100" +
                "6D0065"
            },

            // Greater-than
            new object[]
            {
                "CN=\"Common>Name\"",
                "3021311F301D06035504031E160043006F006D006D006F006E003E004E006100" +
                "6D0065"
            },

            // Octothorpe (Number Sign, Pound, Hash, whatever)
            new object[]
            {
                "CN=\"Common#Name\"",
                "3021311F301D06035504031E160043006F006D006D006F006E0023004E006100" +
                "6D0065"
            },

            // Semi-colon
            new object[]
            {
                "CN=\"Common;Name\"",
                "3021311F301D06035504031E160043006F006D006D006F006E003B004E006100" +
                "6D0065"
            },
        };

        public static readonly object[][] InternallyQuotedRDNs =
        {
            // Interior Double Quote
            new object[]
            {
                "CN=\"Common\"\"Name\"", // Quoted
                "CN=Common\"Name", // Not-Quoted
                "3021311F301D06035504031E160043006F006D006D006F006E0022004E006100" +
                "6D0065"
            },

            // Starts with a double quote
            new object[]
            {
                "CN=\"\"\"Common Name\"", // Quoted
                "CN=\"Common Name", // Not-Quoted
                "30233121301F06035504031E1800220043006F006D006D006F006E0020004E00" +
                "61006D0065"
            },

            // Ends with a double quote
            new object[]
            {
                "CN=\"Common Name\"\"\"", // Quoted
                "CN=Common Name\"", // Not-Quoted
                "30233121301F06035504031E180043006F006D006D006F006E0020004E006100" +
                "6D00650022"
            },
        };

        public static readonly object[][] T61Cases =
        {
            // https://github.com/dotnet/runtime/issues/25195
            new object[]
            {
                "CN=GrapeCity inc., OU=Tools Development, O=GrapeCity inc., " +
                "L=Sendai Izumi-ku, S=Miyagi, C=JP",
                "308186310b3009060355040613024a50310f300d060355040813064d69796167" +
                "69311830160603550407130f53656e64616920497a756d692d6b753117301506" +
                "0355040a140e47726170654369747920696e632e311a3018060355040b141154" +
                "6f6f6c7320446576656c6f706d656e74311730150603550403140e4772617065" +
                "4369747920696e632e"
            },

            // Mono test case taken from old bug report
            new object[]
            {
                "SERIALNUMBER=CVR:13471967-UID:121212121212, E=vhm@use.test.dk, " +
                "CN=Hedeby's M\u00f8belhandel - Salgsafdelingen, " +
                "O=Hedeby's M\u00f8belhandel // CVR:13471967, C=DK",
                "3081B5310B300906035504061302444B312D302B060355040A14244865646562" +
                "792773204DF862656C68616E64656C202F2F204356523A313334373139363731" +
                "2F302D060355040314264865646562792773204DF862656C68616E64656C202D" +
                "2053616C6773616664656C696E67656E311E301C06092A864886F70D01090116" +
                "0F76686D407573652E746573742E646B312630240603550405131D4356523A31" +
                "333437313936372D5549443A313231323132313231323132"
            },

            // Valid UTF-8 string is interpreted as UTF-8
            new object[]
            {
                "C=\u00a2",
                "300D310B300906035504061402C2A2"
            },

            // Invalid UTF-8 string with valid UTF-8 sequence is interpreted as ISO 8859-1
            new object[]
            {
                "L=\u00c2\u00a2\u00f8",
                "300E310C300A06035504071403C2A2F8"
            },
        };

        private const string MicrosoftDotComSubject =
            "3082010F31133011060B2B0601040182373C02010313025553311B3019060B2B" +
            "0601040182373C0201020C0A57617368696E67746F6E311D301B060355040F13" +
            "1450726976617465204F7267616E697A6174696F6E3112301006035504051309" +
            "363030343133343835310B3009060355040613025553310E300C06035504110C" +
            "0539383035323113301106035504080C0A57617368696E67746F6E3110300E06" +
            "035504070C075265646D6F6E643118301606035504090C0F31204D6963726F73" +
            "6F667420576179311E301C060355040A0C154D6963726F736F667420436F7270" +
            "6F726174696F6E310E300C060355040B0C054D53434F4D311A30180603550403" +
            "0C117777772E6D6963726F736F66742E636F6D";
    }
}
