// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
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

        [Fact]
        public static void PrintMultiComponentRdn()
        {
            byte[] encoded = (
                "30223120300C060355040313054A616D65733010060355040A13094D6963726F" +
                "736F6674").HexToByteArray();

            const string expected = "CN=James + O=Microsoft";
            X500DistinguishedName dn = new X500DistinguishedName(encoded);

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
        public static void QuotedWithQuotes(string quoted, string notQuoted, string hexEncoded)
        {
            byte[] encoded = hexEncoded.HexToByteArray();
            X500DistinguishedName dn = new X500DistinguishedName(encoded);

            Assert.Equal(quoted, dn.Decode(X500DistinguishedNameFlags.None));
        }

        [Theory]
        [MemberData(nameof(InternallyQuotedRDNs))]
        public static void NotQuotedWithQuotes(string quoted, string notQuoted, string hexEncoded)
        {
            byte[] encoded = hexEncoded.HexToByteArray();
            X500DistinguishedName dn = new X500DistinguishedName(encoded);

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
            // https://github.com/dotnet/corefx/issues/27466
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
