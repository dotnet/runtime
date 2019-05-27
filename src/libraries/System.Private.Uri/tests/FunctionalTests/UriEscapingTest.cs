// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Common.Tests;

using Xunit;

namespace System.PrivateUri.Tests
{
    /// <summary>
    /// Summary description for UriEscaping
    /// </summary>
    public class UriEscapingTest
    {
        private const string AlphaNumeric = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string RFC2396Unreserved = AlphaNumeric + "-_.!~*'()";
        private const string RFC2396Reserved = @";/:@&=+$,?";
        private const string RFC3986Unreserved = AlphaNumeric + "-._~";
        private const string RFC3986Reserved = @":/[]@!$&'()*+,;=?#";
        private const string GB18030CertificationString1 =
            "\u6570\u636E eq '\uD840\uDC00\uD840\uDC01\uD840\uDC02\uD840\uDC03\uD869\uDED1\uD869\uDED2\uD869\uDED3"
            + "\uD869\uDED4\uD869\uDED5\uD869\uDED6'";

        #region EscapeDataString

        [Fact]
        public void UriEscapingDataString_JustAlphaNumeric_NothingEscaped()
        {
            string output = Uri.EscapeDataString(AlphaNumeric);
            Assert.Equal(AlphaNumeric, output);
        }

        [Fact]
        public void UriEscapingDataString_RFC2396Reserved_Escaped()
        {
            string input = RFC2396Reserved;
            string output = Uri.EscapeDataString(input);
            Assert.Equal(Escape(RFC2396Reserved), output);
        }

        [Fact]
        public void UriEscapingDataString_RFC3986Unreserved_NothingEscaped()
        {
            string input = RFC3986Unreserved;
            string output = Uri.EscapeDataString(input);
            Assert.Equal(input, output);
        }

        [Fact]
        public void UriEscapingDataString_RFC3986Reserved_Escaped()
        {
            string input = RFC3986Reserved;
            string output = Uri.EscapeDataString(input);
            Assert.Equal(Escape(RFC3986Reserved), output);
        }

        [Fact]
        public void UriEscapingDataString_RFC3986ReservedWithIRI_Escaped()
        {
            // Note that \ and % are not officaly reserved, but we treat it as reserved.
            string input = RFC3986Reserved;
            string output = Uri.EscapeDataString(input);
            Assert.Equal(Escape(RFC3986Reserved), output);
        }

        [Fact]
        public void UriEscapingDataString_Unicode_Escaped()
        {
            string input = "\u30AF";
            string output = Uri.EscapeDataString(input);
            Assert.Equal("%E3%82%AF", output);
        }

        [Fact]
        public void UriEscapingDataString_UnicodeWithIRI_Escaped()
        {
            using (var iriHelper = new ThreadCultureChange())
            {
                string input = "\u30AF";
                string output = Uri.EscapeDataString(input);
                Assert.Equal("%E3%82%AF", output);

                iriHelper.ChangeCultureInfo("zh-cn");
                string outputZhCn = Uri.EscapeDataString(input);
                Assert.Equal(output, outputZhCn); //, "Same normalized result expected in different locales."
            }
        }

        [Fact]
        public void UriEscapingDataString_Unicode_SurrogatePair()
        {
            using (ThreadCultureChange iriHelper = new ThreadCultureChange())
            {
                string output = Uri.EscapeDataString(GB18030CertificationString1);
                Assert.Equal(
                 @"%E6%95%B0%E6%8D%AE%20eq" +
                "%20%27%F0%A0%80%80%F0%A0%80%81%F0%A0%80%82%F0%A0%80%83%F0%AA%9B%91" +
                "%F0%AA%9B%92%F0%AA%9B%93%F0%AA%9B%94%F0%AA%9B%95%F0%AA%9B%96%27",
                output);

                iriHelper.ChangeCultureInfo("zh-cn");
                string outputZhCn = Uri.EscapeDataString(GB18030CertificationString1);
                Assert.Equal(output, outputZhCn); //"Same normalized result expected in different locales."
            }
        }

        #endregion EscapeDataString

        #region UnescapeDataString

        [Fact]
        public void UriUnescapingDataString_JustAlphaNumeric_Unescaped()
        {
            string output = Uri.UnescapeDataString(Escape(AlphaNumeric));
            Assert.Equal(AlphaNumeric, output);
        }

        [Fact]
        public void UriUnescapingDataString_RFC2396Unreserved_Unescaped()
        {
            string input = RFC2396Unreserved;
            string output = Uri.UnescapeDataString(Escape(input));
            Assert.Equal(input, output);
        }

        [Fact]
        public void UriUnescapingDataString_RFC2396Reserved_Unescaped()
        {
            string input = RFC2396Reserved;
            string output = Uri.UnescapeDataString(Escape(input));
            Assert.Equal(input, output);
        }

        [Fact]
        public void UriUnescapingDataString_RFC3986Unreserved_Unescaped()
        {
            string input = RFC3986Unreserved;
            string output = Uri.UnescapeDataString(Escape(input));
            Assert.Equal(input, output);
        }

        [Fact]
        public void UriUnescapingDataString_RFC3986Reserved_Unescaped()
        {
            string input = RFC3986Reserved;
            string output = Uri.UnescapeDataString(Escape(input));
            Assert.Equal(input, output);
        }

        [Fact]
        public void UriUnescapingDataString_RFC3986ReservedWithIRI_Unescaped()
        {
            // Note that \ and % are not officaly reserved, but we treat it as reserved.
            string input = RFC3986Reserved;
            string output = Uri.UnescapeDataString(Escape(input));
            Assert.Equal(input, output);
        }

        [Fact]
        public void UriUnescapingDataString_Unicode_Unescaped()
        {
            string input = @"\u30AF";
            string output = Uri.UnescapeDataString(Escape(input));
            Assert.Equal(input, output);
        }

        [Fact]
        public void UriUnescapingDataString_UnicodeWithIRI_Unescaped()
        {
            using (ThreadCultureChange helper = new ThreadCultureChange())
            {
                string input = @"\u30AF";
                string output = Uri.UnescapeDataString(Escape(input));
                Assert.Equal(input, output);

                helper.ChangeCultureInfo("zh-cn");
                string outputZhCn = Uri.UnescapeDataString(Escape(input));
                Assert.Equal(output, outputZhCn); // Same normalized result expected in different locales.
            }
        }

        [Fact]
        public void UriUnescapingDataString_Unicode_SurrogatePair()
        {
            using (ThreadCultureChange iriHelper = new ThreadCultureChange())
            {
                string escapedInput = Uri.EscapeDataString(GB18030CertificationString1);
                string output = Uri.UnescapeDataString(escapedInput);
                Assert.Equal(GB18030CertificationString1, output);

                iriHelper.ChangeCultureInfo("zh-cn");
                string outputZhCn = Uri.UnescapeDataString(escapedInput);
                Assert.Equal(output, outputZhCn); //Same normalized result expected in different locales.
            }
        }

        #endregion UnescapeDataString

        #region EscapeUriString

        [Fact]
        public void UriEscapingUriString_JustAlphaNumeric_NothingEscaped()
        {
            string output = Uri.EscapeUriString(AlphaNumeric);
            Assert.Equal(AlphaNumeric, output);
        }

        [Fact]
        public void UriEscapingUriString_RFC2396Unreserved_NothingEscaped()
        {
            string input = RFC2396Unreserved;
            string output = Uri.EscapeUriString(input);
            Assert.Equal(input, output);
        }

        [Fact]
        public void UriEscapingUriString_RFC2396Reserved_NothingEscaped()
        {
            string input = RFC2396Reserved;
            string output = Uri.EscapeUriString(input);
            Assert.Equal(RFC2396Reserved, output);
        }

        [Fact]
        public void UriEscapingUriString_RFC3986Unreserved_NothingEscaped()
        {
            string input = RFC3986Unreserved;
            string output = Uri.EscapeUriString(input);
            Assert.Equal(input, output);
        }

        [Fact]
        public void UriEscapingUriString_RFC3986Reserved_NothingEscaped()
        {
            string input = RFC3986Reserved;
            string output = Uri.EscapeUriString(input);
            Assert.Equal(RFC3986Reserved, output);
        }

        [Fact]
        public void UriEscapingUriString_RFC3986ReservedWithIRI_NothingEscaped()
        {
            string input = RFC3986Reserved;
            string output = Uri.EscapeUriString(input);
            Assert.Equal(RFC3986Reserved, output);
        }

        [Fact]
        public void UriEscapingUriString_Unicode_Escaped()
        {
            string input = "\u30AF";
            string output = Uri.EscapeUriString(input);
            Assert.Equal("%E3%82%AF", output);
        }

        [Fact]
        public void UriEscapingUriString_UnicodeWithIRI_Escaped()
        {
            using (ThreadCultureChange helper = new ThreadCultureChange())
            {
                string input = "\u30AF";
                string output = Uri.EscapeUriString(input);
                Assert.Equal("%E3%82%AF", output);

                helper.ChangeCultureInfo("zh-cn");
                string outputZhCn = Uri.EscapeUriString(input);
                Assert.Equal(output, outputZhCn); // Same normalized result expected in different locales.
            }
        }

        [Fact]
        public void UriEscapingUriString_FullUri_NothingEscaped()
        {
            string input = "http://host:90/path/path?query#fragment";
            string output = Uri.EscapeUriString(input);
            Assert.Equal(input, output);
        }

        [Fact]
        public void UriEscapingUriString_FullIPv6Uri_NothingEscaped()
        {
            string input = "http://[::1]:90/path/path?query[]#fragment[]#";
            string output = Uri.EscapeUriString(input);
            Assert.Equal(input, output);
        }

        #endregion EscapeUriString

        #region AbsoluteUri escaping

        [Fact]
        public void UriAbsoluteEscaping_AlphaNumeric_NoEscaping()
        {
            string input = "http://" + AlphaNumeric.ToLowerInvariant() + "/" + AlphaNumeric
                + "?" + AlphaNumeric + "#" + AlphaNumeric;
            Uri testUri = new Uri(input);
            Assert.Equal(input, testUri.AbsoluteUri);
        }

        [Fact]
        public void UriAbsoluteUnEscaping_AlphaNumericEscapedIriOn_UnEscaping()
        {
            string escapedAlphaNum = Escape(AlphaNumeric);
            string input = "http://" + AlphaNumeric.ToLowerInvariant() + "/" + escapedAlphaNum
                + "?" + escapedAlphaNum + "#" + escapedAlphaNum;
            string expectedOutput = "http://" + AlphaNumeric.ToLowerInvariant() + "/" + AlphaNumeric
                + "?" + AlphaNumeric + "#" + AlphaNumeric;
            Uri testUri = new Uri(input);
            Assert.Equal(expectedOutput, testUri.AbsoluteUri);
        }

        [Fact]
        public void UriAbsoluteEscaping_RFC2396Unreserved_NoEscaping()
        {
            string input = "http://" + AlphaNumeric.ToLowerInvariant() + "/" + RFC2396Unreserved
                + "?" + RFC2396Unreserved + "#" + RFC2396Unreserved;
            Uri testUri = new Uri(input);
            Assert.Equal(input, testUri.AbsoluteUri);
        }

        [Fact]
        public void UriAbsoluteUnEscaping_RFC3986UnreservedEscaped_AllUnescaped()
        {
            string escaped = Escape(RFC3986Unreserved);
            string input = "http://" + AlphaNumeric.ToLowerInvariant() + "/" + escaped
                + "?" + escaped + "#" + escaped;
            string expectedOutput = "http://" + AlphaNumeric.ToLowerInvariant() + "/" + RFC3986Unreserved
                + "?" + RFC3986Unreserved + "#" + RFC3986Unreserved;

            Uri testUri = new Uri(input);
            Assert.Equal(expectedOutput, testUri.AbsoluteUri);
        }

        [Fact]
        public void UriAbsoluteEscaping_RFC2396Reserved_NoEscaping()
        {
            string input = "http://host/" + RFC2396Reserved
                + "?" + RFC2396Reserved + "#" + RFC2396Reserved;
            Uri testUri = new Uri(input);
            Assert.Equal(input, testUri.AbsoluteUri);
        }

        [Fact]
        public void UriAbsoluteUnEscaping_RFC2396ReservedEscaped_NoUnEscaping()
        {
            string escaped = Escape(RFC2396Reserved);
            string input = "http://host/" + escaped + "?" + escaped + "#" + escaped;

            Uri testUri = new Uri(input);
            Assert.Equal(input, testUri.AbsoluteUri);
        }

        [Fact]
        public void UriAbsoluteEscaping_RFC3986Unreserved_NoEscaping()
        {
            string input = "http://" + AlphaNumeric.ToLowerInvariant() + "/" + RFC3986Unreserved
                + "?" + RFC3986Unreserved + "#" + RFC3986Unreserved;
            Uri testUri = new Uri(input);
            Assert.Equal(input, testUri.AbsoluteUri);
        }

        [Fact]
        public void UriAbsoluteEscaping_RFC3986Reserved_NothingEscaped()
        {
            string input = "http://host/" + RFC3986Reserved
                + "?" + RFC3986Reserved + "#" + RFC3986Reserved;

            Uri testUri = new Uri(input);
            Assert.Equal(input, testUri.AbsoluteUri);
        }

        [Fact]
        public void UriAbsoluteUnEscaping_RFC3986ReservedEscaped_NothingUnescaped()
        {
            string escaped = Escape(RFC3986Reserved);
            string input = "http://host/" + escaped + "?" + escaped + "#" + escaped;

            Uri testUri = new Uri(input);
            Assert.Equal(input, testUri.AbsoluteUri);
        }

        [Fact]
        public void UriAbsoluteEscaping_FullUri_NothingEscaped()
        {
            string input = "http://host:90/path/path?query#fragment";
            Uri output = new Uri(input);
            Assert.Equal(input, output.AbsoluteUri);
        }

        [Fact]
        public void UriAbsoluteEscaping_FullIPv6Uri_NothingEscaped()
        {
            string input = "http://[0000:0000:0000:0000:0000:0000:0000:0001]:90/path/path[]?query[]#fragment[]#";
            Uri output = new Uri(input);
            Assert.Equal("http://[::1]:90/path/path[]?query[]#fragment[]#", output.AbsoluteUri);
        }

        [Fact]
        public void UriAbsoluteEscaping_SurrogatePair_LocaleIndependent()
        {
            string uriString = "http://contosotest.conto.soco.ntosoco.com/surrgtest()?$filter=";
            string expectedString = uriString + "%E6%95%B0%E6%8D%AE%20eq%20%27%F0%A0%80%80%F0%A0%80%81%F0%A0%80%82%F0%A0%80%83%F0" + 
                                                "%AA%9B%91%F0%AA%9B%92%F0%AA%9B%93%F0%AA%9B%94%F0%AA%9B%95%F0%AA%9B%96%27";

            using (ThreadCultureChange iriHelper = new ThreadCultureChange())
            {
                Uri uri = new Uri(uriString + Uri.EscapeDataString(GB18030CertificationString1));
                Assert.Equal(expectedString, uri.AbsoluteUri);

                iriHelper.ChangeCultureInfo("zh-cn");
                Uri uriZhCn = new Uri(uriString + Uri.EscapeDataString(GB18030CertificationString1));
                Assert.Equal(uri.AbsoluteUri, uriZhCn.AbsoluteUri); // Same normalized result expected in different locales.
            }
        }

        [Fact]
        public void UriAbsoluteEscaping_EscapeBufferRealloc()
        {
            string strUriRoot = "http://host";
            string strUriQuery = @"/x?=srch_type=\uFFFD\uFFFD\uFFFD&sop=and&stx=\uFFFD\uFFFD\uFFFD\uDB8F\uDCB5\uFFFD\u20\uFFFD\uFFFD";

            Uri uriRoot = new Uri(strUriRoot);

            Uri uriCtor1 = new Uri(strUriRoot + strUriQuery);
            Uri uriCtor2 = new Uri(uriRoot, strUriQuery);
            Uri uriRelative = new Uri(strUriQuery, UriKind.Relative);
            Uri uriCtor3 = new Uri(uriRoot, uriRelative);

            Assert.Equal(
                uriCtor1.AbsoluteUri,
                uriCtor2.AbsoluteUri); // Uri(string) is not producing the same AbsoluteUri result as Uri(Uri, string).

            Assert.Equal(
                uriCtor1.AbsoluteUri,
                uriCtor3.AbsoluteUri); // Uri(string) is not producing the same result as Uri(Uri, Uri).
        }

        #endregion AbsoluteUri escaping

        #region FileUri escaping

        [Fact]
        public void UriFile_ExplicitFile_QueryAllowed()
        {
            string input = "file://host/path/path?query?#fragment?";
            string expectedOutput = "file://host/path/path?query?#fragment?";
            Uri testUri = new Uri(input);
            Assert.Equal(expectedOutput, testUri.AbsoluteUri);
            Assert.Equal("/path/path", testUri.AbsolutePath);
            Assert.Equal("?query?", testUri.Query);
            Assert.Equal("#fragment?", testUri.Fragment);
        }

        [Fact]
        public void UriFile_ExplicitDosFile_QueryAllowed()
        {
            string input = "file:///c:/path/path?query?#fragment?";
            Uri testUri = new Uri(input);
            Assert.Equal("file:///c:/path/path?query?#fragment?", testUri.AbsoluteUri);
            Assert.Equal("c:/path/path", testUri.AbsolutePath);
            Assert.Equal(@"c:\path\path", testUri.LocalPath);
            Assert.Equal("?query?", testUri.Query);
            Assert.Equal("#fragment?", testUri.Fragment);
        }

        [Fact]
        public void UriFile_ImplicitDosFile_QueryNotAllowed()
        {
            string input = "c:/path/path?query";
            Uri testUri = new Uri(input);
            Assert.Equal("file:///c:/path/path%3Fquery", testUri.AbsoluteUri);
            Assert.Equal("c:/path/path%3Fquery", testUri.AbsolutePath);
            Assert.Equal(@"c:\path\path?query", testUri.LocalPath);
            Assert.Equal(string.Empty, testUri.Query);
            Assert.Equal(string.Empty, testUri.Fragment);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Unix path
        public void UriFile_ImplicitUnixFile_QueryNotAllowed()
        {
            string input = "/path/path?query";
            Uri testUri = new Uri(input);
            Assert.Equal("file:///path/path%3Fquery", testUri.AbsoluteUri);
            Assert.Equal("/path/path%3Fquery", testUri.AbsolutePath);
            Assert.Equal("/path/path?query", testUri.LocalPath);
            Assert.Equal(string.Empty, testUri.Query);
            Assert.Equal(string.Empty, testUri.Fragment);
        }

        [Fact]
        public void UriFile_ImplicitUncFile_QueryNotAllowed()
        {
            string input = @"\\Server\share\path?query";
            Uri testUri = new Uri(input);
            Assert.Equal("file://server/share/path%3Fquery", testUri.AbsoluteUri);
            Assert.Equal("/share/path%3Fquery", testUri.AbsolutePath);
            Assert.Equal(@"\\server\share\path?query", testUri.LocalPath);
            Assert.Equal(string.Empty, testUri.Query);
            Assert.Equal(string.Empty, testUri.Fragment);
        }

        [Fact]
        public void UriFile_ImplicitDosFile_FragmentNotAllowed()
        {
            string input = "c:/path/path#fragment#";
            Uri testUri = new Uri(input);
            Assert.Equal("file:///c:/path/path%23fragment%23", testUri.AbsoluteUri);
            Assert.Equal("c:/path/path%23fragment%23", testUri.AbsolutePath);
            Assert.Equal(@"c:\path\path#fragment#", testUri.LocalPath);
            Assert.Equal(string.Empty, testUri.Query);
            Assert.Equal(string.Empty, testUri.Fragment);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Unix path
        public void UriFile_ImplicitUnixFile_FragmentNotAllowed()
        {
            string input = "/path/path#fragment#";
            Uri testUri = new Uri(input);
            Assert.Equal("file:///path/path%23fragment%23", testUri.AbsoluteUri);
            Assert.Equal("/path/path%23fragment%23", testUri.AbsolutePath);
            Assert.Equal("/path/path#fragment#", testUri.LocalPath);
            Assert.Equal(string.Empty, testUri.Query);
            Assert.Equal(string.Empty, testUri.Fragment);
        }

        [Fact]
        public void UriFile_ImplicitUncFile_FragmentNotAllowed()
        {
            string input = @"\\Server\share\path#fragment#";
            Uri testUri = new Uri(input);
            Assert.Equal("file://server/share/path%23fragment%23", testUri.AbsoluteUri);
            Assert.Equal("/share/path%23fragment%23", testUri.AbsolutePath);
            Assert.Equal(@"\\server\share\path#fragment#", testUri.LocalPath);
            Assert.Equal(string.Empty, testUri.Query);
            Assert.Equal(string.Empty, testUri.Fragment);
        }

        #endregion FileUri escaping

        #region Invalid escape sequences

        [Fact]
        public void UriUnescapeInvalid__Percent_LeftAlone()
        {
            string input = "http://host/%";

            string output = Uri.UnescapeDataString(input);
            Assert.Equal(input, output);

            Uri uri = new Uri(input);
            Assert.Equal("http://host/%25", uri.AbsoluteUri);
        }

        [Fact]
        public void UriUnescapeInvalid_Regex_LeftAlone()
        {
            string input = @"(https?://)?(([\w!~*'().&;;=+$%-]+: )?[\w!~*'().&;;=+$%-]+@)?(([0-9]{1,3}\.){3}[0-9]"
                + @"{1,3}|([\w!~*'()-]+\.)*([\w^-][\w-]{0,61})?[\w]\.[a-z]{2,6})(:[0-9]{1,4})?((/*)|(/+[\w!~*'()."
                + @";?:@&;;=+$,%#-]+)+/*)";
            string output = Uri.UnescapeDataString(input);
            Assert.Equal(input, output);
        }

        [Fact]
        public void UriUnescape_EscapedAsciiIriOn_Unescaped()
        {
            string input = "http://host/%7A";

            string output = Uri.UnescapeDataString(input);
            Assert.Equal("http://host/z", output);

            Uri uri = new Uri(input);
            Assert.Equal("http://host/z", uri.ToString());
            Assert.Equal("http://host/z", uri.AbsoluteUri);
        }

        [Fact]
        public void UriUnescapeInvalid_IncompleteUtf8IriOn_LeftAlone()
        {
            string input = "http://host/%E5%9B";
            Uri uri = new Uri(input);
            Assert.Equal(input, uri.AbsoluteUri);

            string output = Uri.UnescapeDataString(input);
            Assert.Equal(input, output);
        }

        [Fact]
        public void UriUnescape_AsciiUtf8AsciiIriOn_ValidUnescaped()
        {
            using (ThreadCultureChange irihelper = new ThreadCultureChange())
            {
                string input = "http://host/%5A%E6%9C%88%5A";

                string output = Uri.UnescapeDataString(input);
                Assert.Equal("http://host/Z\u6708Z", output);

                Uri uri = new Uri(input);
                Assert.Equal("http://host/Z%E6%9C%88Z", uri.AbsoluteUri);

                irihelper.ChangeCultureInfo("zh-cn");
                string outputZhCn = Uri.UnescapeDataString(input);
                Assert.Equal(output, outputZhCn);

                Uri uriZhCn = new Uri(input);
                Assert.Equal(uri.AbsoluteUri, uriZhCn.AbsoluteUri);
            }
        }

        [Fact]
        public void UriUnescapeInvalid_AsciiIncompleteUtf8AsciiIriOn_InvalidUtf8LeftAlone()
        {
            using (ThreadCultureChange irihelper = new ThreadCultureChange())
            {
                string input = "http://host/%5A%E5%9B%5A";

                string output = Uri.UnescapeDataString(input);
                Assert.Equal("http://host/Z%E5%9BZ", output);

                Uri uri = new Uri(input);
                Assert.Equal("http://host/Z%E5%9BZ", uri.ToString());
                Assert.Equal("http://host/Z%E5%9BZ", uri.AbsoluteUri);

                irihelper.ChangeCultureInfo("zh-cn");
                string outputZhCn = Uri.UnescapeDataString(input);
                Assert.Equal(output, outputZhCn);

                Uri uriZhCn = new Uri(input);
                Assert.Equal(uri.AbsoluteUri, uriZhCn.AbsoluteUri);
                Assert.Equal(uri.ToString(), uriZhCn.ToString());
            }
        }

        [Fact]
        public void UriUnescapeInvalid_IncompleteUtf8BetweenValidUtf8IriOn_InvalidUtf8LeftAlone()
        {
            using (ThreadCultureChange irihelper = new ThreadCultureChange())
            {
                string input = "http://host/%E6%9C%88%E5%9B%E6%9C%88";

                string output = Uri.UnescapeDataString(input);
                Assert.Equal("http://host/\u6708%E5%9B\u6708", output);

                Uri uri = new Uri(input);
                Assert.Equal(input, uri.AbsoluteUri);

                irihelper.ChangeCultureInfo("zh-cn");
                string outputZhCn = Uri.UnescapeDataString(input);
                Assert.Equal(output, outputZhCn);

                Uri uriZhCn = new Uri(input);
                Assert.Equal(uri.AbsoluteUri, uriZhCn.AbsoluteUri);
            }
        }

        [Fact]
        public void UriUnescapeInvalid_IncompleteUtf8AfterValidUtf8IriOn_InvalidUtf8LeftAlone()
        {
            using (ThreadCultureChange irihelper = new ThreadCultureChange())
            {
                string input = "http://host/%59%E6%9C%88%E5%9B";

                Uri uri = new Uri(input);
                Assert.Equal("http://host/Y%E6%9C%88%E5%9B", uri.AbsoluteUri);
                Assert.Equal("http://host/Y\u6708%E5%9B", uri.ToString());

                string output = Uri.UnescapeDataString(input);
                Assert.Equal("http://host/Y\u6708%E5%9B", output);

                irihelper.ChangeCultureInfo("zh-cn");
                Uri uriZhCn = new Uri(input);
                Assert.Equal(uri.ToString(), uriZhCn.ToString());
                Assert.Equal(uri.AbsoluteUri, uriZhCn.AbsoluteUri);

                string outputZhCn = Uri.UnescapeDataString(input);
                Assert.Equal(output, outputZhCn);
            }
        }

        [Fact]
        public void UriUnescapeInvalid_ValidUtf8IncompleteUtf8AsciiIriOn_InvalidUtf8LeftAlone()
        {
            using (ThreadCultureChange irihelper = new ThreadCultureChange())
            {
                string input = "http://host/%E6%9C%88%E6%9C%59";

                Uri uri = new Uri(input);
                Assert.Equal("http://host/%E6%9C%88%E6%9CY", uri.AbsoluteUri);
                Assert.Equal("http://host/\u6708%E6%9CY", uri.ToString());

                string output = Uri.UnescapeDataString(input);
                Assert.Equal("http://host/\u6708%E6%9CY", output);

                irihelper.ChangeCultureInfo("zh-cn");
                Uri uriZhCn = new Uri(input);
                Assert.Equal(uri.ToString(), uriZhCn.ToString());
                Assert.Equal(uri.AbsoluteUri, uriZhCn.AbsoluteUri);

                string outputZhCn = Uri.UnescapeDataString(input);
                Assert.Equal(output, outputZhCn);
            }
        }

        #endregion Invalid escape sequences

        #region Helpers

        private static readonly char[] s_hexUpperChars = {
                                   '0', '1', '2', '3', '4', '5', '6', '7',
                                   '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
                                   };

        // Percent encode every character
        private static string Escape(string input)
        {
            byte[] bytes = new byte[4];
            StringBuilder output = new StringBuilder();
            for (int index = 0; index < input.Length; index++)
            {
                // Non-Ascii is escaped as UTF-8
                int byteCount = Encoding.UTF8.GetBytes(input, index, 1, bytes, 0);
                for (int byteIndex = 0; byteIndex < byteCount; byteIndex++)
                {
                    output.Append("%");
                    output.Append(s_hexUpperChars[(bytes[byteIndex] & 0xf0) >> 4]);
                    output.Append(s_hexUpperChars[bytes[byteIndex] & 0xf]);
                }
            }
            return output.ToString();
        }

        #endregion Helpers
    }
}
