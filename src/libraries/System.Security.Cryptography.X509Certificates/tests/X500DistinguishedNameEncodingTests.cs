// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class X500DistinguishedNameEncodingTests
    {
        private const string InvalidX500NameFragment = "invalid X500 name";
        private const string InvalidIA5StringFragment = "7 bit ASCII";

        [Fact]
        public static void EncodeEmptyValue()
        {
            X500DistinguishedName dn = new X500DistinguishedName("");

            ProcessTestCase(
                new SimpleEncoderTestCase(
                    "",
                    null,
                    "3000",
                    null),
                dn);
        }

        [Theory]
        [MemberData(nameof(SingleRdnTestCases))]
        public static void EncodeSingleRdn(SimpleEncoderTestCase testCase)
        {
            X500DistinguishedName dn = new X500DistinguishedName(testCase.Input, X500DistinguishedNameFlags.None);

            ProcessTestCase(testCase, dn);
        }

        [Theory]
        [MemberData(nameof(GetFlagBasedDnCases))]
        public static void EncodeWithFlags(FlagControlledEncoderTestCase testCase)
        {
            X500DistinguishedName dn = new X500DistinguishedName(testCase.Input, testCase.Flags);

            ProcessTestCase(testCase, dn);
        }

        [Theory]
        [MemberData(nameof(SeparatorFlagCombinations))]
        public static void VerifySeparatorProcessing(X500DistinguishedNameFlags flags)
        {
            const string input = "CN=a, O=b; OU=c\r L=d\n S=e";

            // No separator flags: , and ; => CN, O, OU
            const string withNoFlags = "CN=a, O=b, OU=\"c\r L=d\n S=e\"";
            // UseNewlines: \r and \n => CN, L, S
            const string withNewlines = "CN=\"a, O=b; OU=c\", L=d, S=e";
            // UseCommas: , => CN, O
            const string withCommas = "CN=a, O=\"b; OU=c\r L=d\n S=e\"";
            // UseSemicolons: ; => CN, OU
            const string withSemicolons = "CN=\"a, O=b\", OU=\"c\r L=d\n S=e\"";

            string expected;

            // Semicolons, if specified, always wins.
            // then commas, if specified, wins.
            // then newlines, if specified is valid.
            // Specifying nothing at all is (for some reason) both commas and semicolons.
            if ((flags & X500DistinguishedNameFlags.UseSemicolons) != 0)
            {
                expected = withSemicolons;
            }
            else if ((flags & X500DistinguishedNameFlags.UseCommas) != 0)
            {
                expected = withCommas;
            }
            else if ((flags & X500DistinguishedNameFlags.UseNewLines) != 0)
            {
                expected = withNewlines;
            }
            else
            {
                expected = withNoFlags;
            }

            X500DistinguishedName dn = new X500DistinguishedName(input, flags);

            Assert.Equal(expected, dn.Format(false));
        }

        [Theory]
        // Separator character with no tag
        [InlineData(",", InvalidX500NameFragment)]
        // Control character preceding tag
        [InlineData("\u0008CN=a", InvalidX500NameFragment)]
        // Control character after tag
        [InlineData("CN\u0008=a", InvalidX500NameFragment)]
        // Control character within tag
        [InlineData("C\u0008N=a", InvalidX500NameFragment)]
        // Control character after whitespace after tag
        [InlineData("CN \u0008=a", InvalidX500NameFragment)]
        // Unresolvable OID
        [InlineData("1.a.1.3=a", InvalidX500NameFragment)]
        // Open quote with no close quote. (Don't you hate it when people do that?
        [InlineData("CN=\"unterminated", InvalidX500NameFragment)]
        // Non-ASCII values in an E field
        [InlineData("E=\u65E5\u672C\u8A9E", InvalidIA5StringFragment)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50937", TestPlatforms.Android)]
        public static void InvalidInput(string input, string messageFragment)
        {
            CryptographicException exception =
                Assert.ThrowsAny<CryptographicException>(() => new X500DistinguishedName(input));

            if (CultureInfo.CurrentCulture.Name == "en-US")
            {
                Assert.Contains(messageFragment, exception.Message);
            }
        }

        [Theory]
        [MemberData(nameof(ParserBoundaryCases))]
        public static void CheckParserBoundaryCases(SimpleEncoderTestCase testCase)
        {
            X500DistinguishedName dn = new X500DistinguishedName(testCase.Input, X500DistinguishedNameFlags.None);

            ProcessTestCase(testCase, dn);
        }

        private static void ProcessTestCase(SimpleEncoderTestCase testCase, X500DistinguishedName dn)
        {
            // The simple encoding test is "does it output the expected text?", then
            // we'll move on to the exact bytes.
            Assert.Equal(testCase.GetNormalizedValue(), dn.Format(false));

            string expectedHex;

            if (OperatingSystem.IsWindows())
            {
                expectedHex = testCase.GetBmpEncoding() ?? testCase.GetPreferredEncoding();
            }
            else
            {
                expectedHex = testCase.GetPreferredEncoding();
            }

            string actualHex = dn.RawData.ByteArrayToHex();

            Assert.Equal(expectedHex, actualHex);
        }

        public static readonly object[][] SingleRdnTestCases =
        {
            new object[]
            {
                // No accent characters means this fits within a PrintableString.
                // There's only one correct encoding.
                new SimpleEncoderTestCase(
                    "CN=Common Name",
                    null,
                    "3016311430120603550403130B436F6D6D6F6E204E616D65",
                    null),
            },

            new object[]
            {
                // Let's get a bit spicy and accent an o. Now that it doesn't fit in
                // a PrintableString it should be a UTF8String (RFC 3280 - 2004 deadline,
                // RFC 5280 unconditional (5280 was written in 2008)).
                //
                // Windows continues to use BMPString (UCS-2), so we have to allow that, too.
                new SimpleEncoderTestCase(
                    "CN=Comm\u00f3n Name",
                    null,
                    "30173115301306035504030C0C436F6D6DC3B36E204E616D65",
                    "3021311F301D06035504031E160043006F006D006D00F3006E0020004E0061006D0065"),
            },

            new object[]
            {
                // E-mail addresses usually contain an @.
                // @ is not a PrintableString character.
                // The industry (and RFC-acknowledged) compromise on this is an IA5String.
                new SimpleEncoderTestCase(
                    "E=user@example.com",
                    null,
                    "3021311F301D06092A864886F70D010901161075736572406578616D706C652E636F6D",
                    null),
            },

            new object[]
            {
                // Technically this 'email address' could be a PrintableString, but E just
                // uses IA5String.
                new SimpleEncoderTestCase(
                    "E=Common Name",
                    null,
                    "301C311A301806092A864886F70D010901160B436F6D6D6F6E204E616D65",
                    null),
            },

            new object[]
            {
                // An unknown OID in numeric form will parse just fine.
                new SimpleEncoderTestCase(
                    "1.1.1.1=Common Name",
                    "OID.1.1.1.1=Common Name",
                    "3016311430120603290101130B436F6D6D6F6E204E616D65",
                    null),
            },

            new object[]
            {
                // An unknown OID with the OID.numericform form.
                new SimpleEncoderTestCase(
                    "OID.1.1.1.1=Common Name",
                    null,
                    "3016311430120603290101130B436F6D6D6F6E204E616D65",
                    null),
            },

            new object[]
            {
                // Common Name in the raw numeric form.
                new SimpleEncoderTestCase(
                    "2.5.4.3=Common Name",
                    "CN=Common Name",
                    "3016311430120603550403130B436F6D6D6F6E204E616D65",
                    null),
            },

            new object[]
            {
                // Common Name in the OID.-prefixed numeric form.
                new SimpleEncoderTestCase(
                    "OID.2.5.4.3=Common Name",
                    "CN=Common Name",
                    "3016311430120603550403130B436F6D6D6F6E204E616D65",
                    null),
            },

            new object[]
            {
                // Common Name with a dangling separator.
                new SimpleEncoderTestCase(
                    "CN=Common Name,",
                    "CN=Common Name",
                    "3016311430120603550403130B436F6D6D6F6E204E616D65",
                    null),
            },

            new object[]
            {
                // Unnecessarily quoted value
                new SimpleEncoderTestCase(
                    "CN=\"Common Name\"",
                    "CN=Common Name",
                    "3016311430120603550403130B436F6D6D6F6E204E616D65",
                    null),
            },

            new object[]
            {
                // Quoted because of an embedded comma.  The quotes are not
                // included in the encoded value, just the string form.
                new SimpleEncoderTestCase(
                    "CN=\"Name, Common\"",
                    null,
                    "3017311530130603550403130C4E616D652C20436F6D6D6F6E",
                    null),
            },

            new object[]
            {
                // Contains two escaped quotation marks.
                // Quotation mark is not a PrintableString character, so UTF8 (or BMP).
                new SimpleEncoderTestCase(
                    "CN=\"\"\"Common\"\" Name\"",
                    null,
                    "30183116301406035504030C0D22436F6D6D6F6E22204E616D65",
                    "30253123302106035504031E1A00220043006F006D006D006F006E00220020004E0061006D0065"),
            },

            new object[]
            {
                // Unnecessarily quoted email address
                new SimpleEncoderTestCase(
                    "E=\"user@example.com\"",
                    "E=user@example.com",
                    "3021311F301D06092A864886F70D010901161075736572406578616D706C652E636F6D",
                    null),
            },

            new object[]
            {
                // "email address" with an embedded quote.
                // While this isn't a legal email address it is a valid IA5String.
                new SimpleEncoderTestCase(
                    "E=\"\"\"user\"\"@example.com\"",
                    null,
                    "30233121301F06092A864886F70D0109011612227573657222406578616D706C652E636F6D",
                    null),
            },

            new object[]
            {
                // The RFCs have no statement like "when a BMPString would contain fewer octets,
                // it should be preferred over UTF8String".  So we'll continue to call UTF-8
                // preferred in this case (CN=nihongo)
                new SimpleEncoderTestCase(
                    "CN=\u65E5\u672C\u8A9E",
                    null,
                    "30143112301006035504030C09E697A5E69CACE8AA9E",
                    "3011310F300D06035504031E0665E5672C8A9E"),
            },

            new object[]
            {
                // Leading and trailing whitespace is removed
                new SimpleEncoderTestCase(
                    "    CN  = Common Name             ",
                    "CN=Common Name",
                    "3016311430120603550403130B436F6D6D6F6E204E616D65",
                    null),
            },

            new object[]
            {
                // This looks like it could be two RDNs, but it's only one.
                new SimpleEncoderTestCase(
                    "CN=\"Common Name, E=user@example.com\"",
                    null,
                    "302A3128302606035504030C1F436F6D6D6F6E204E616D652C20453D75736572406578616D706C652E636F6D",
                    "30493147304506035504031E3E0043006F006D006D006F006E0020004E006100" +
                        "6D0065002C00200045003D00750073006500720040006500780061006D007000" +
                        "6C0065002E0063006F006D"),
            },

            new object[]
            {
                // Control codes are valid in the value portion (as the first character)
                new SimpleEncoderTestCase(
                    "CN=\u0008",
                    null,
                    "300C310A300806035504030C0108",
                    "300D310B300906035504031E020008"),
            },

            new object[]
            {
                // Control codes are valid in the value portion (anywhere, really)
                new SimpleEncoderTestCase(
                    "CN=a     \u0008",
                    null,
                    "30123110300E06035504030C0761202020202008",
                    "30193117301506035504031E0E0061002000200020002000200008"),
            },

            new object[]
            {
                // Empty values are legal.
                new SimpleEncoderTestCase(
                    "CN=",
                    "CN=\"\"",
                    "300B3109300706035504031300",
                    null),
            },

            new object[]
            {
                // Contains two adjacent escaped quotes.
                new SimpleEncoderTestCase(
                    "CN=\"Adjacent Escaped\"\"\"\"Quotes\"",
                    null,
                    "30233121301F06035504030C1841646A6163656E742045736361706564222251756F746573",
                    "303B3139303706035504031E3000410064006A006100630065006E00740020004500730063" +
                        "00610070006500640022002200510075006F007400650073"),
            },
        };

        public static IEnumerable<object[]> GetFlagBasedDnCases()
        {
            foreach (FlagVariantEncoderTestCases generator in s_flagVariantTestCases)
            {
                foreach (FlagControlledEncoderTestCase testCase in generator.GenerateTestCases())
                {
                    yield return new object[] {testCase };
                }
            }
        }

        public static object[][] SeparatorFlagCombinations =
        {
            new object[]
            {
                X500DistinguishedNameFlags.None,
            },
            new object[]
            {
                X500DistinguishedNameFlags.UseNewLines
            },
            new object[]
            {
                X500DistinguishedNameFlags.UseCommas
            },
            new object[]
            {
                X500DistinguishedNameFlags.UseCommas |
                X500DistinguishedNameFlags.UseNewLines
            },
            new object[]
            {
                X500DistinguishedNameFlags.UseSemicolons
            },
            new object[]
            {
                X500DistinguishedNameFlags.UseSemicolons |
                X500DistinguishedNameFlags.UseNewLines
            },
            new object[]
            {
                X500DistinguishedNameFlags.UseSemicolons |
                X500DistinguishedNameFlags.UseCommas
            },
            new object[]
            {
                X500DistinguishedNameFlags.UseSemicolons |
                X500DistinguishedNameFlags.UseCommas |
                X500DistinguishedNameFlags.UseNewLines
            },
        };

        private static readonly FlagVariantEncoderTestCases[] s_flagVariantTestCases =
        {
            new FlagVariantEncoderTestCases("CN=Common Name, E=user@example.com")
            {
                {
                    X500DistinguishedNameFlags.None,
                    "CN=Common Name, E=user@example.com",
                    "3037311430120603550403130B436F6D6D6F6E204E616D65311F301D06092A86" +
                        "4886F70D010901161075736572406578616D706C652E636F6D",
                    null
                },

                // Since the delimiter used is a comma, and comma is in the special none group,
                // this has no visible effect.
                {
                    X500DistinguishedNameFlags.UseCommas,
                    "CN=Common Name, E=user@example.com",
                    "3037311430120603550403130B436F6D6D6F6E204E616D65311F301D06092A86" +
                        "4886F70D010901161075736572406578616D706C652E636F6D",
                    null
                },

                // Parse reversed
                {
                    X500DistinguishedNameFlags.Reversed,
                    "E=user@example.com, CN=Common Name",
                    "3037311F301D06092A864886F70D010901161075736572406578616D706C652E" +
                        "636F6D311430120603550403130B436F6D6D6F6E204E616D65",
                    null
                },

                // Reversed and UseCommas aren't conflicting.  Since we did use commas, this is just Reversed.
                {
                    X500DistinguishedNameFlags.Reversed | X500DistinguishedNameFlags.UseCommas,
                    "E=user@example.com, CN=Common Name",
                    "3037311F301D06092A864886F70D010901161075736572406578616D706C652E" +
                        "636F6D311430120603550403130B436F6D6D6F6E204E616D65",
                    null
                },

                // Only Semicolons are delimiters, so this is one RDN with a value which isn't a PrintableString
                {
                    X500DistinguishedNameFlags.UseSemicolons,
                    "CN=\"Common Name, E=user@example.com\"",
                    "302A3128302606035504030C1F436F6D6D6F6E204E616D652C20453D75736572406578616D706C652E636F6D",
                    "30493147304506035504031E3E0043006F006D006D006F006E0020004E006100" +
                        "6D0065002C00200045003D00750073006500720040006500780061006D007000" +
                        "6C0065002E0063006F006D"
                },

                // Reversed processing isn't quite the same as doing a scan from the right.
                // This is CN only, not an invalid value.
                {
                    X500DistinguishedNameFlags.UseSemicolons | X500DistinguishedNameFlags.Reversed,
                    "CN=\"Common Name, E=user@example.com\"",
                    "302A3128302606035504030C1F436F6D6D6F6E204E616D652C20453D75736572406578616D706C652E636F6D",
                    "30493147304506035504031E3E0043006F006D006D006F006E0020004E006100" +
                        "6D0065002C00200045003D00750073006500720040006500780061006D007000" +
                        "6C0065002E0063006F006D"
                },
            },

            new FlagVariantEncoderTestCases("CN=\"Common Name, O=Organization\"")
            {
                // The quotes match, so this is just what it looks like.
                {
                    X500DistinguishedNameFlags.None,
                    "CN=\"Common Name, O=Organization\"",
                    "3026312430220603550403131B436F6D6D6F6E204E616D652C204F3D4F726761" +
                        "6E697A6174696F6E",
                    null
                },

                // If we ignore the quotes, this is now a CN with an embedded quote and an O with an embedded quote.
                {
                    X500DistinguishedNameFlags.DoNotUseQuotes,
                    "CN=\"\"\"Common Name\", O=\"Organization\"\"\"",
                    "302F3115301306035504030C0C22436F6D6D6F6E204E616D6531163014060355" +
                        "040A0C0D4F7267616E697A6174696F6E22",
                    "30483121301F06035504031E1800220043006F006D006D006F006E0020004E00" +
                        "61006D006531233021060355040A1E1A004F007200670061006E0069007A0061" +
                        "00740069006F006E0022"
                },

                // Reversed doesn't matter, there's only one value.
                {
                    X500DistinguishedNameFlags.Reversed,
                    "CN=\"Common Name, O=Organization\"",
                    "3026312430220603550403131B436F6D6D6F6E204E616D652C204F3D4F726761" +
                        "6E697A6174696F6E",
                    null
                },

                // Reversed with no quotes is different.
                {
                    X500DistinguishedNameFlags.DoNotUseQuotes | X500DistinguishedNameFlags.Reversed,
                    "O=\"Organization\"\"\", CN=\"\"\"Common Name\"",
                    "302F31163014060355040A0C0D4F7267616E697A6174696F6E22311530130603" +
                        "5504030C0C22436F6D6D6F6E204E616D65",
                    "304831233021060355040A1E1A004F007200670061006E0069007A0061007400" +
                        "69006F006E00223121301F06035504031E1800220043006F006D006D006F006E" +
                        "0020004E0061006D0065"
                },
            },

            new FlagVariantEncoderTestCases("CN=@")
            {
                // A non-PrintableString value. Unix: UTF-8, Windows: BMP (UCS-2)
                {
                    X500DistinguishedNameFlags.None,
                    "CN=@",
                    "300C310A300806035504030C0140",
                    "300D310B300906035504031E020040"
                },

                // Windows can emit the UTF-8 form, but you have to ask it to.
                {
                    X500DistinguishedNameFlags.UseUTF8Encoding,
                    "CN=@",
                    "300C310A300806035504030C0140",
                    null
                },

                // Windows can also do the T61 (teletex) encoding.
                // Unix doesn't honor this flag, it sticks with UTF-8
                {
                    X500DistinguishedNameFlags.UseT61Encoding,
                    "CN=@",
                    "300C310A300806035504030C0140",
                    "300C310A30080603550403140140"
                },
            },

            new FlagVariantEncoderTestCases("CN=a\nO=b\rOU=c\r\nL=d\n\n \n")
            {
                // One whitespace-heavy value.  Note that the whitespace at the end
                // was removed, even though the default-printed form has quotes.
                {
                    X500DistinguishedNameFlags.None,
                    "CN=\"a\nO=b\rOU=c\r\nL=d\"",
                    "301A3118301606035504030C0F610A4F3D620D4F553D630D0A4C3D64",
                    "30293127302506035504031E1E0061000A004F003D0062000D004F0055003D00" +
                        "63000D000A004C003D0064"
                },

                // \r and \n are the only delimiters that are allowed to
                // appear consecutively.  That's because they're also whitespace.
                {
                    X500DistinguishedNameFlags.UseNewLines,
                    "CN=a, O=b, OU=c, L=d",
                    "3030310A30080603550403130161310A3008060355040A130162310A30080603" +
                        "55040B130163310A30080603550407130164",
                    null
                },
            },
        };

        public static readonly object[][] ParserBoundaryCases =
        {
            // The parser states referenced here are based on the managed implementation
            // used by the Unix builds.

            new object[]
            {
                // First RDN goes from = to , directly.
                // First-to-second transition has no whitespace
                // Second RDN goes from = to the end of the string.
                // Parser ends in SeekValueStart without having done the loop in that state.
                new SimpleEncoderTestCase(
                    "CN=,O=",
                    "CN=\"\", O=\"\"",
                    "3016310930070603550403130031093007060355040A1300",
                    null)
            },

            new object[]
            {
                // Same as above, but with abundant whitespace.
                // Parser ends in SeekValueStart after having processed some whitespace
                new SimpleEncoderTestCase(
                    "  CN=                  ,    O=  ",
                    "CN=\"\", O=\"\"",
                    "3016310930070603550403130031093007060355040A1300",
                    null)
            },

             new object[]
            {
                // Lets add some quotes for good measure.
                // Parser ends in MaybeEndQuote
                new SimpleEncoderTestCase(
                    "  CN=                \"\"  ,    O= \"\"",
                    "CN=\"\", O=\"\"",
                    "3016310930070603550403130031093007060355040A1300",
                    null)
            },

            new object[]
            {
                // Same as above, with trailing whitespace.
                // Parser ends in SeekComma
                new SimpleEncoderTestCase(
                    "  CN=                \"\"  ,    O= \"\" ",
                    "CN=\"\", O=\"\"",
                    "3016310930070603550403130031093007060355040A1300",
                    null)
            },

            new object[]
            {
                // Give the parser the comma it wanted, now it ends in SeekTag
                new SimpleEncoderTestCase(
                    "  CN=                \"\"  ,    O= \"\" ,",
                    "CN=\"\", O=\"\"",
                    "3016310930070603550403130031093007060355040A1300",
                    null)
            },

            new object[]
            {
                // Parser ends in SeekValueEnd with no whitespace
                new SimpleEncoderTestCase(
                    "  CN=                \"\"  ,    O= a",
                    "CN=\"\", O=a",
                    "30173109300706035504031300310A3008060355040A130161",
                    null)
            },

            new object[]
            {
                // Parser ends in SeekValueEnd after reading whitespace
                new SimpleEncoderTestCase(
                    "  CN=                \"\"  ,    O= a ",
                    "CN=\"\", O=a",
                    "30173109300706035504031300310A3008060355040A130161",
                    null)
            },
        };

        public class SimpleEncoderTestCase
        {
            private readonly string _normalized;
            private readonly string _preferredEncodingHex;
            private readonly string _bmpEncodingHex;

            public string Input { get; private set; }

            internal SimpleEncoderTestCase(
                string input,
                string normalized,
                string preferredEncodingHex,
                string bmpEncodingHex)
            {
                _normalized = normalized;
                _preferredEncodingHex = preferredEncodingHex;
                _bmpEncodingHex = bmpEncodingHex;
                Input = input;
            }

            internal string GetNormalizedValue()
            {
                return _normalized ?? Input;
            }

            internal string GetPreferredEncoding()
            {
                return _preferredEncodingHex;
            }

            internal string GetBmpEncoding()
            {
                return _bmpEncodingHex;
            }
        }

        public class FlagControlledEncoderTestCase : SimpleEncoderTestCase
        {
            public X500DistinguishedNameFlags Flags { get; private set; }

            internal FlagControlledEncoderTestCase(
                string input,
                X500DistinguishedNameFlags flags,
                string normalized,
                string preferredEncodingHex,
                string bmpEncodingHex)
                : base(input, normalized, preferredEncodingHex, bmpEncodingHex)
            {
                Flags = flags;
            }
        }

        public class FlagVariantEncoderTestCases : IEnumerable<FlagControlledEncoderTestCase>
        {
            private readonly string _template;
            private readonly List<Variation> _variations = new List<Variation>();

            internal FlagVariantEncoderTestCases(string template)
            {
                _template = template;
            }

            internal void Add(
                X500DistinguishedNameFlags flags,
                string output,
                string preferredEncoding,
                string bmpEncoding)
            {
                _variations.Add(new Variation(flags, ',', output, preferredEncoding, bmpEncoding));
            }

            internal IEnumerable<FlagControlledEncoderTestCase> GenerateTestCases()
            {
                if (_variations.Count == 0)
                {
                    throw new InvalidOperationException("No variations were added");
                }

                foreach (Variation variation in _variations)
                {
                    yield return new FlagControlledEncoderTestCase(
                        _template,
                        variation.Flags,
                        variation.Output,
                        variation.PreferredEncoding,
                        variation.BmpEncoding);
                }
            }

            private class Variation
            {
                public X500DistinguishedNameFlags Flags { get; private set; }
                public char Separator { get; private set; }
                public string Output { get; private set; }
                public string PreferredEncoding { get; private set; }
                public string BmpEncoding { get; private set; }

                public Variation(
                    X500DistinguishedNameFlags flags,
                    char separator,
                    string output,
                    string preferredEncoding,
                    string bmpEncoding)
                {
                    Flags = flags;
                    Separator = separator;
                    Output = output;
                    PreferredEncoding = preferredEncoding;
                    BmpEncoding = bmpEncoding;
                }
            }

            IEnumerator<FlagControlledEncoderTestCase> IEnumerable<FlagControlledEncoderTestCase>.GetEnumerator()
            {
                return GenerateTestCases().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GenerateTestCases().GetEnumerator();
            }
        }
    }
}
