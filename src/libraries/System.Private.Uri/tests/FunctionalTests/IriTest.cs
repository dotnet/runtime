// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Common.Tests;
using System.Linq;
using System.Text;

using Xunit;

namespace System.PrivateUri.Tests
{
    /// <summary>
    /// Testing IRI (RFC 3987) related parsing code.
    /// </summary>
    public class IriTest
    {
        // List built based on https://www.microsoft.com/en-us/download/details.aspx?id=55979
        private string[] _testedLocales =
        {
            "en-us",
            "zh-cn",
            "de-ch",
            "de-lu",
            "ja-jp"
        };

        private const int maxUriLength = 0xFFF0 - 1;

        [Fact]
        public void Iri_Validate_LongUriWithQuery()
        {
            string uriString = "http://www.contos.com/abcde?fghijklm=noprstuvx&ab=CDEFG#hi=jk&lmn=o&p="
                              + "%F4%80%80%9C%F4%80%80%99&cp=18&pf=p&sclient=psy&site=webhp&source=hp&rlz=1R2ADRA_enUS"
                              + "435&aq=f&aqi=&aql=&oq=%F4%80%80%BA%F4%80%80%94%F4%80%80%93%F4%80%80%94%F4%80%80%95%F4"
                              + "%80%80%97%F4%80%80%93%F4%80%80%9C%F4%80%80%99&pbx=1&bav=on.2,or.r_gc.r_pw.&fp=fb838c8"
                              + "df90b57b2&biw=1600&bih=718";
            try
            {
                Uri u = new Uri(uriString);
            }
            catch
            { }

            GC.Collect(2);
        }

        [Fact]
        public void Iri_Uri_ShouldNotThrowArgumentOutOfRange()
        {
            string u1string = "http://ab.contos.com/search?query=" + "%F0%B3%BF%BF";
            Uri u1 = new Uri(u1string);
            Assert.Equal(u1string, u1.AbsoluteUri);

            string u2string = "http://www.contos.com/abcdefghijklmn-" + "%B4%F3%BF%AA%B4%D4.html";
            Uri u2 = new Uri(u2string);
            Assert.Equal(u2string, u2.AbsoluteUri);

            string u4string = "http://www.contoso-abcdefg.de/abcdefg" + "%F3%BE%8C%B5.html";
            Uri u3;
            Assert.True(Uri.TryCreate(u4string, UriKind.Absolute, out u3));
        }

        [Fact]
        public void Iri_Uri_SchemaParsing_ShouldNotThrowArgumentOutOfRange()
        {
            string root = "viewcode://./codeschema_class?";
            string uriDataFra = root + "Type=" + Uri.EscapeDataString("\u00E9");

            Uri u1 = new Uri(uriDataFra);

            Assert.Equal(root + "Type=%C3%A9", u1.AbsoluteUri);
        }

        [Fact]
        public void Iri_ReservedCharacters_NotNormalized()
        {
            Uri u1 = new Uri(@"http://test.com/%2c");
            Uri u2 = new Uri(@"http://test.com/,");

            Assert.NotEqual(
                u1.ToString(),
                u2.ToString());
        }

        [Fact]
        public void Iri_UnknownSchemeWithoutAuthority_DoesNormalize()
        {
            string[] paths = { "\u00E8", "%C3%A8" };
            foreach (string path in paths)
            {
                Uri noAuthority = new Uri("scheme:" + path);
                Assert.Equal("scheme:\u00E8", noAuthority.ToString());
            }
        }

        [Fact]
        public void Iri_804110_TryCreateUri_ShouldNotThrowIndexOutOfRange()
        {
            string u1 = "http://b.contos.oc.om/entry/d.contos.oc.om/AbcDefg/21234567/1234567890";
            string u2 = "http://d.contos.oc.om/keyword/" + "%C6%F3%BC%A1%B8%B5";
            Uri baseUri = new Uri(u1);
            Uri resultUri;
            Uri.TryCreate(baseUri, u2, out resultUri);
            Assert.Equal(u2, resultUri.AbsoluteUri);

            string u3 = "http://con.tosoco.ntosoc.com/abcdefghi/jk" + "%c8%f3%b7%a2%b7%bf%b2%fa";
            Uri.TryCreate(u3, UriKind.Absolute, out resultUri);

            Assert.Equal(
                "http://con.tosoco.ntosoc.com/abcdefghi/jk" + "%C8%F3%B7%A2%B7%BF%B2%FA",
                resultUri.ToString());

            string u4 = "http://co.ntsosocon.com/abcd/" + "%A2%F3%BD%CB%FC%FB%D5%E9%F3%B7%AA%B5";
            Uri uri4 = new Uri(u4);
            Uri.TryCreate(
                "http://co.ntsosocon.com/abcd/" + "%A2%F3%BD%CB%FC%FB%D5%E9%F3%B7%AA%B5",
                UriKind.Absolute,
                out resultUri);
            Assert.Equal(u4, resultUri.ToString());

            string[] sctiTeamReproUris = new string[] {
                "http://con.toso.com/abc/defg.html?n=12345678&ab="
                    + "%BF%E4%C1%F2%B3%AF%BE%BE%B4%F5%BF%EE%C0%CC%C0%AF",
                "http://contos.com/abcdef_ghijklm/" + "%8E%E8%90%F4%82%A2%8A%ED/?abcdefgh=0123",
                "http://www.conto.com/abcd/" + "%D7%F3%B1%A6%B9%F3",
                "http://c.ontoso.co.mc/abcdefg/" + "%C6%F3%B8%AB%B1%CD%CD%FD%BB%D2",
                "http://www.conto.com/abcd/" + "%D6%E2%BA%F3%B1%B8%BC%B1%B7%BD",
                "http://www.conto.com/abcd/" + "%A1%B6%B4%F3%BA%BD%BA%A3%A1%B7"
            };

            foreach (string uristring in sctiTeamReproUris)
            {
                Uri u = new Uri(uristring);
                Assert.Equal(uristring, u.AbsoluteUri);
            }

            Uri uri5 = new Uri("http://contos.com/abcdef_ghijklm/"
                    + "%90H%8A%ED%90%F4%82%A2%8B%40+%90%98%82%A6%92u%82%AB/?abcdefgh=0002%5F0042");
            Assert.Equal(
                "http://contos.com/abcdef_ghijklm/"
                + "%90H%8A%ED%90%F4%82%A2%8B%40+%90%98%82%A6%92u%82%AB/?abcdefgh=0002_0042",
                uri5.ToString());
        }

        [Fact]
        public void Iri_FragmentInvalidCharacters()
        {
            string input = "%F4%80%80%BA";
            VerifyUriNormalizationForEscapedCharacters(input);
        }

        [Fact]
        public void Iri_EscapedAscii()
        {
            string input = "%%%01%35%36";
            VerifyUriNormalizationForEscapedCharacters(input);
        }

        [Fact]
        public void Iri_EscapedAsciiFollowedByUnescaped()
        {
            string input = "%ABabc";
            VerifyUriNormalizationForEscapedCharacters(input);
        }

        [Fact]
        public void Iri_InvalidHexSequence()
        {
            string input = "%AB%FG%GF";
            VerifyUriNormalizationForEscapedCharacters(input);
        }

        [Fact]
        public void Iri_InvalidUTF8Sequence()
        {
            string input = "%F4%80%80%7F";
            VerifyUriNormalizationForEscapedCharacters(input);
        }

        [Fact]
        public void Iri_IncompleteEscapedCharacter()
        {
            string input = "%F4%80%80%B";
            VerifyUriNormalizationForEscapedCharacters(input);
        }

        [Fact]
        public void Iri_ReservedCharacters()
        {
            string input = "/?#??#%[]";
            VerifyUriNormalizationForEscapedCharacters(input);
        }

        [Fact]
        public void Iri_EscapedReservedCharacters()
        {
            string input = "%2F%3F%23%3F%3F%23%25%5B%5D";
            VerifyUriNormalizationForEscapedCharacters(input);
        }

        [Fact]
        public void Iri_BidiCharacters()
        {
            string input = "\u200E";
            VerifyUriNormalizationForEscapedCharacters(input);
        }

        [Fact]
        public void Iri_IncompleteSurrogate()
        {
            string input = "\uDBC0\uDC3A\uDBC0";
            VerifyUriNormalizationForEscapedCharacters(input);
        }

        [Fact]
        public void Iri_InIriRange_AfterEscapingBufferInitialized()
        {
            string input = "\uDBC0\uDC3A\u00A1";
            VerifyUriNormalizationForEscapedCharacters(input);
        }

        [Fact]
        public void Iri_BidiCharacter_AfterEscapingBufferInitialized()
        {
            string input = "\uDBC0\uDC3A\u200E";
            VerifyUriNormalizationForEscapedCharacters(input);
        }

        [Fact]
        public void Iri_UnicodePlane0()
        {
            EscapeUnescapeTestUnicodePlane(0x0, 0xFFFF, 1);
        }

        [Fact]
        public void Iri_UnicodePlane1()
        {
            EscapeUnescapeTestUnicodePlane(0x10000, 0x1FFFF, 1);
        }

        [Fact]
        public void Iri_UnicodePlane2()
        {
            EscapeUnescapeTestUnicodePlane(0x20000, 0x2FFFF, 1);
        }

        [Fact]
        public void Iri_UnicodePlane3_13()
        {
            EscapeUnescapeTestUnicodePlane(0x30000, 0xDFFFF, 1);
        }

        [Fact]
        public void Iri_UnicodePlane14()
        {
            EscapeUnescapeTestUnicodePlane(0xE0000, 0xEFFFF, 1);
        }

        [Fact]
        public void Iri_UnicodePlane15_16()
        {
            EscapeUnescapeTestUnicodePlane(0xF0000, 0x10FFFF, 1);
        }

        private void EscapeUnescapeTestUnicodePlane(int start, int end, int step)
        {
            string input = GetUnicodeString(start, end, step);

            VerifyUriNormalizationForEscapedCharacters(input);
        }

        private static string GetUnicodeString(int start, int end, int step, int minLength = 1)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = start; i < end; i += step)
            {
                if (i < 0xFFFF)
                {
                    // ConvertFromUtf32 doesn't allow surrogate codepoint values.
                    // This may generate invalid Unicode sequences when i is between 0xd800 and 0xdfff.
                    sb.Append((char)i);
                }
                else
                {
                    sb.Append(char.ConvertFromUtf32(i));
                }
            }

            string input = sb.ToString();

            while (sb.Length < minLength)
            {
                sb.Append(input);
            }

            input = sb.ToString();
            return input;
        }

        private void VerifyUriNormalizationForEscapedCharacters(string uriInput)
        {
            UriComponents[] components = new UriComponents[]
            {
                UriComponents.Fragment,
                UriComponents.Host,
                UriComponents.Path,
                UriComponents.Port,
                UriComponents.Query,
                UriComponents.Scheme,
                UriComponents.UserInfo,
            };

            using (ThreadCultureChange helper = new ThreadCultureChange())
            {
                string[] results1 = new string[components.Length];

                helper.ChangeCultureInfo(_testedLocales[0]);
                for (int i = 0; i < components.Length; i++)
                {
                    results1[i] = EscapeUnescapeTestComponent(uriInput, components[i]);
                }

                for (int j = 1; j < _testedLocales.Length; j++)
                {
                    helper.ChangeCultureInfo(_testedLocales[j]);

                    string[] results2 = new string[components.Length];
                    for (int i = 0; i < components.Length; i++)
                    {
                        results2[i] = EscapeUnescapeTestComponent(uriInput, components[i]);
                    }

                    for (int i = 0; i < components.Length; i++)
                    {
                        Assert.Equal(
                            0,
                            string.CompareOrdinal(results1[i], results2[i]));
                    }
                }
            }
        }

        private string EscapeUnescapeTestComponent(string uriInput, UriComponents component)
        {
            string ret = null;
            string uriString = null;

            switch (component)
            {
                case UriComponents.Fragment:
                    uriString = string.Format(
                        "http://userInfo@server:80/path/resource.ext?query=qvalue#{0}",
                        uriInput);
                    break;
                case UriComponents.Host:
                    uriString = string.Format(
                        "http://userInfo@{0}:80/path/resource.ext?query=qvalue#fragment",
                        uriInput);
                    break;
                case UriComponents.Path:
                    uriString = string.Format(
                        "http://userInfo@server:80/{0}/{0}/resource.ext?query=qvalue#fragment",
                        uriInput);
                    break;
                case UriComponents.Port:
                    uriString = string.Format(
                        "http://userInfo@server:{0}/path/resource.ext?query=qvalue#fragment",
                        uriInput);
                    break;
                case UriComponents.Query:
                    uriString = string.Format(
                        "http://userInfo@server:80/path/resource.ext?query{0}=qvalue{0}#fragment",
                        uriInput);
                    break;
                case UriComponents.Scheme:
                    uriString = string.Format(
                        "{0}://userInfo@server:80/path/resource.ext?query=qvalue#fragment",
                        uriInput);
                    break;
                case UriComponents.UserInfo:
                    uriString = string.Format(
                        "http://{0}@server:80/path/resource.ext?query=qvalue#fragment",
                        uriInput);
                    break;
                default:
                    Assert.False(true, "Unknown Uri component: " + component.ToString());
                    break;
            }

            try
            {
                Uri u = new Uri(uriString);
                ret = u.AbsoluteUri;
            }
            catch (ArgumentNullException)
            {
                ret = "";
            }
            catch (FormatException)
            {
                ret = "";
            }

            return ret;
        }

        /// <summary>
        /// First column contains input characters found to be potential issues with the current implementation.
        /// The second column contains the current (.NET Core 2.1/Framework 4.7.2) Uri behavior for Uri normalization.
        /// </summary>
        private static string[,] s_checkIsReservedEscapingStrings =
        {
            // : [ ] in host.
            {"http://user@ser%3Aver.srv:123/path/path/resource.ext?query=expression#fragment", null},
            {"http://user@server.srv%3A999/path/path/resource.ext?query=expression#fragment", null},
            {"http://user@server.%5Bsrv:123/path/path/resource.ext?query=expression#fragment", null},
            {"http://user@ser%5Dver.srv:123/path/path/resource.ext?query=expression#fragment", null},

            // [ ] in userinfo.
            {"http://us%5Ber@server.srv:123/path/path/resource.ext?query=expression#fragment",  
                "http://us%5Ber@server.srv:123/path/path/resource.ext?query=expression#fragment"},
            {"http://u%5Dser@server.srv:123/path/path/resource.ext?query=expression#fragment", 
                "http://u%5Dser@server.srv:123/path/path/resource.ext?query=expression#fragment"},
            {"http://us%5B%5Der@server.srv:123/path/path/resource.ext?query=expression#fragment", 
                "http://us%5B%5Der@server.srv:123/path/path/resource.ext?query=expression#fragment"},
            
            // [ ] : ' in path.
            {"http://user@server.srv:123/path/pa%5B%3A%27th/resource.ext?query=expression#fragment", 
                "http://user@server.srv:123/path/pa%5B%3A%27th/resource.ext?query=expression#fragment"},
            {"http://user@server.srv:123/pa%5D%3A%27th/path%5D%3A%27/resource.ext?query=expression#fragment", 
                "http://user@server.srv:123/pa%5D%3A%27th/path%5D%3A%27/resource.ext?query=expression#fragment"},
            {"http://user@server.srv:123/path/p%5B%3A%27a%5D%3A%27th/resource.ext?query=expression#fragment", 
                "http://user@server.srv:123/path/p%5B%3A%27a%5D%3A%27th/resource.ext?query=expression#fragment"},
            
            // [ ] : ' in query.
            {"http://user@server.srv:123/path/path/resource.ext?que%5B%3A%27ry=expression#fragment",
                "http://user@server.srv:123/path/path/resource.ext?que%5B%3A%27ry=expression#fragment"},
            {"http://user@server.srv:123/path/path/resource.ext?query=exp%5D%3A%27ression#fragment",
                "http://user@server.srv:123/path/path/resource.ext?query=exp%5D%3A%27ression#fragment"},
            {"http://user@server.srv:123/path/path/resource.ext?que%5B%3A%27ry=exp%5D%3A%27ression#fragment",
                "http://user@server.srv:123/path/path/resource.ext?que%5B%3A%27ry=exp%5D%3A%27ression#fragment"},
            
            // [ ] : ' in fragment.
            {"http://user@server.srv:123/path/path/resource.ext?query=expression#fr%5B%3A%27agment",
                "http://user@server.srv:123/path/path/resource.ext?query=expression#fr%5B%3A%27agment"},
            {"http://user@server.srv:123/path/path/resource.ext?query=expression#fragment%5D%3A%27",
                "http://user@server.srv:123/path/path/resource.ext?query=expression#fragment%5D%3A%27"},
            {"http://user@server.srv:123/path/path/resource.ext?query=expression#fr%5B%3A%27agment%5D%3A%27",
                "http://user@server.srv:123/path/path/resource.ext?query=expression#fr%5B%3A%27agment%5D%3A%27"}
        };

        /// <summary>
        /// This test validates behavior differences caused by the incorrect conditional expression found in function
        /// CheckIsReserved().
        /// </summary>
        [Fact]
        public void Iri_CheckIsReserved_EscapingBehavior()
        {
            for (int i = 0; i < s_checkIsReservedEscapingStrings.GetLength(0); i++)
            {
                try
                {
                    string uriInput = s_checkIsReservedEscapingStrings[i, 0];
                    string expectedToString = s_checkIsReservedEscapingStrings[i, 1];
                    Uri uri = new Uri(uriInput);
                    Assert.Equal(uriInput, uri.AbsoluteUri); //"Unexpected URI normalization behavior."
                    Assert.Equal(expectedToString, uri.ToString()); //"Unexpected URI normalization behavior."
                }
                catch (FormatException)
                { }
            }
        }

        [Fact]
        public void Iri_ValidateVeryLongInputString_Ctor()
        {
            string validUriStart = "http://host/q=";

            string bigString1 = GetUnicodeString(0x1000, 0x1001, 1, maxUriLength - validUriStart.Length);
            string test = validUriStart + bigString1;
            Assert.True(test.Length == maxUriLength);

            Uri u1 = new Uri(validUriStart + bigString1);
            Assert.True(u1.ToString().Length > bigString1.Length);

            try
            {
                string bigString2 = GetUnicodeString(0, maxUriLength + 1, 1);
                Uri u = new Uri(bigString2);
                Assert.False(true, "Expected UriFormatException: Uri too large");
            }
            catch (FormatException)
            { }
        }

        [Fact]
        public void Iri_ValidateVeryLongInputString_EscapeDataString()
        {
            string bigString1 = GetUnicodeString(0, maxUriLength, 1);
            Assert.True(Uri.EscapeDataString(bigString1).Length > bigString1.Length);

            try
            {
                string bigString2 = GetUnicodeString(0, maxUriLength + 1, 1);
                Uri.EscapeDataString(bigString2);
                Assert.False(true, "Expected UriFormatException: Uri too large");
            }
            catch (FormatException)
            { }
        }

        [Fact]
        public void Iri_ValidateVeryLongInputString_EscapeUriString()
        {
            string bigString1 = GetUnicodeString(0, maxUriLength, 1);
            Assert.True(Uri.EscapeUriString(bigString1).Length > bigString1.Length);

            try
            {
                string bigString2 = GetUnicodeString(0, maxUriLength + 1, 1);
                Uri.EscapeUriString(bigString2);
                Assert.False(true, "Expected UriFormatException: Uri too large");
            }
            catch (FormatException)
            { }
        }

        [Theory]
        [InlineData("\u00E8")]
        [InlineData("_\u00E8")]
        [InlineData("_")]
        public void Iri_FileUriUncFallback_DoesSupportUnicodeHost(string authority)
        {
            Uri fileTwoSlashes = new Uri("file://" + authority);
            Uri fileFourSlashes = new Uri("file:////" + authority);

            Assert.Equal(authority, fileTwoSlashes.Authority); // Two slashes must be followed by an authority
            Assert.Equal(authority, fileFourSlashes.Authority); // More than three slashes looks like a UNC share
        }

        [Theory]
        [InlineData(@"c:/path/with/unicode/ö/test.xml")]
        [InlineData(@"file://c:/path/with/unicode/ö/test.xml")]
        public void Iri_WindowsPathWithUnicode_DoesRemoveScheme(string uriString)
        {
            var uri = new Uri(uriString);
            Assert.False(uri.LocalPath.StartsWith("file:"));
        }

        [Theory]
        [InlineData("http:%C3%A8")]
        [InlineData("http:\u00E8")]
        [InlineData("%C3%A8")]
        [InlineData("\u00E8")]
        public void Iri_RelativeUriCreation_ShouldNotNormalize(string uriString)
        {
            Uri href;
            Uri hrefAbsolute;
            Uri baseIri = new Uri("http://www.contoso.com");

            Assert.True(Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out href));
            Assert.True(Uri.TryCreate(baseIri, href, out hrefAbsolute));
            Assert.Equal("http://www.contoso.com/%C3%A8", hrefAbsolute.AbsoluteUri);
        }

        public static IEnumerable<object[]> AllForbiddenDecompositions() =>
            from host in new[] { "canada.c\u2100.microsoft.com", // Unicode U+2100 'Account Of' decomposes to 'a/c'
                                 "canada.c\u2488.microsoft.com", // Unicode U+2488 'Digit One Full Stop" decomposes to '1.'
                                 "canada.c\u2048.microsoft.com", // Unicode U+2048 'Question Exclamation Mark" decomposes to '?!'
                                 "canada.c\uD83C\uDD00.microsoft.com" } // Unicode U+2488 'Digit Zero Full Stop" decomposes to '0.'
            from scheme in new[] { "http", // Known scheme.
                                   "test" } // Unknown scheme.
            select new object[] { scheme, host };

        [Theory]
        [MemberData(nameof(AllForbiddenDecompositions))]
        public void Iri_AllForbiddenDecompositions_IdnHostThrows(string scheme, string host)
        {
            Uri uri = new Uri(scheme + "://" + host);
            Assert.Throws<UriFormatException>(() => uri.IdnHost);
        }

        [Theory]
        [MemberData(nameof(AllForbiddenDecompositions))]
        public void Iri_AllForbiddenDecompositions_NonIdnPropertiesOk(string scheme, string host)
        {
            Uri uri = new Uri(scheme + "://" + host);
            Assert.Equal(host, uri.Host);
            Assert.Equal(host, uri.DnsSafeHost);
            Assert.Equal(host, uri.Authority);
            Assert.Equal(scheme + "://" + host + "/", uri.AbsoluteUri);
        }
    }
}
