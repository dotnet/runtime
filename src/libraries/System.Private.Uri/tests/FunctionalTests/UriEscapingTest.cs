// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Tests;
using System.Text;

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

        #region EscapeUnescapeDataString

        [Fact]
        public void EscapeUnescapeDataString_NullArgument()
        {
            AssertExtensions.Throws<ArgumentNullException>("stringToEscape", () => Uri.EscapeDataString(null));
            AssertExtensions.Throws<ArgumentNullException>("stringToUnescape", () => Uri.UnescapeDataString(null));
        }

        private static IEnumerable<(string Unescaped, string Escaped)> CombinationsWithDifferentSections(string unescaped, string escaped)
        {
            yield return (unescaped, escaped);
            yield return (unescaped + unescaped, escaped + escaped);

            foreach ((string padding, string escapedPadding) in new[]
            {
                (" ", "%20"), ("abc", "abc"), ("a b%", "a%20b%25"), ("\u00FC", "%C3%BC"), ("\uD83C\uDF49", "%F0%9F%8D%89")
            })
            {
                yield return ($"{padding}{unescaped}", $"{escapedPadding}{escaped}");
                yield return ($"{unescaped}{padding}", $"{escaped}{escapedPadding}");
                yield return ($"{padding}{unescaped}{padding}", $"{escapedPadding}{escaped}{escapedPadding}");
                yield return ($"{unescaped}{padding}{unescaped}", $"{escaped}{escapedPadding}{escaped}");
                yield return ($"{padding}{unescaped}{padding}{unescaped}{padding}", $"{escapedPadding}{escaped}{escapedPadding}{escaped}{escapedPadding}");
            }
        }

        private static IEnumerable<(string Unescaped, string Escaped)> UriEscapeUnescapeDataStringTestInputs()
        {
            yield return ("", "");
            yield return ("He\\l/lo", "He%5Cl%2Flo");

            yield return (AlphaNumeric, AlphaNumeric);
            yield return (RFC3986Unreserved, RFC3986Unreserved);

            yield return (RFC2396Reserved, EscapeAscii(RFC2396Reserved));
            yield return (RFC3986Reserved, EscapeAscii(RFC3986Reserved));

            // Note that \ and % are not officially reserved, but we treat it as reserved.
            yield return (RFC3986Reserved + "\\%", EscapeAscii(RFC3986Reserved + "\\%"));

            yield return ("\u30AF", "%E3%82%AF");
            yield return (GB18030CertificationString1, "%E6%95%B0%E6%8D%AE%20eq%20%27%F0%A0%80%80%F0%A0%80%81%F0%A0%80%82%F0%A0%80%83%F0%AA%9B%91%F0%AA%9B%92%F0%AA%9B%93%F0%AA%9B%94%F0%AA%9B%95%F0%AA%9B%96%27");

            // Test all ASCII that should be escaped
            for (int i = 0; i < 128; i++)
            {
                if (!RFC3986Unreserved.Contains((char)i))
                {
                    string s = new string((char)i, 42);
                    yield return (s, EscapeAscii(s));
                }
            }

            // Valid surrogate pairs
            yield return ("\uD800\uDC00", "%F0%90%80%80");
            yield return ("\uD83C\uDF49", "%F0%9F%8D%89");
        }

        public static IEnumerable<object[]> UriEscapeDataString_MemberData()
        {
            (string Unescaped, string Escaped)[] pairs =
            [
                .. UriEscapeUnescapeDataStringTestInputs(),

                // Invalid surrogate pairs
                ("\uD800", "%EF%BF%BD"),
                ("abc\uD800", "abc%EF%BF%BD"),
                ("abc\uD800\uD800abc", "abc%EF%BF%BD%EF%BF%BDabc"),
                ("\xD800\xD800\xDFFF", "%EF%BF%BD%F0%90%8F%BF"),
            ];

            return pairs
                .SelectMany(p => CombinationsWithDifferentSections(p.Unescaped, p.Escaped))
                .Select(p => new[] { p.Unescaped, p.Escaped });
        }

        public static IEnumerable<object[]> UriUnescapeDataString_MemberData()
        {
            const string OneByteUtf8 = "%41";           // A
            const string TwoByteUtf8 = "%C3%BC";        // \u00FC
            const string ThreeByteUtf8 = "%E8%AF%B6";   // \u8BF6
            const string FourByteUtf8 = "%F0%9F%98%80"; // \uD83D\uDE00

            const string InvalidOneByteUtf8 = "%FF";
            const string OverlongTwoByteUtf8 = "%C1%81";        // A
            const string OverlongThreeByteUtf8 = "%E0%83%BC";   // \u00FC
            const string OverlongFourByteUtf8 = "%F0%88%AF%B6"; // \u8BF6;

            (string Unescaped, string Escaped)[] pairs =
            [
                .. UriEscapeUnescapeDataStringTestInputs(),

                // Many combinations that include non-ASCII to test the PercentEncodingHelper
                ("A", OneByteUtf8),
                ("\u00FC", TwoByteUtf8),
                ("\u8BF6", ThreeByteUtf8),
                ("\uD83D\uDE00", FourByteUtf8),

                ("AA", OneByteUtf8 + OneByteUtf8),
                ("\u00FC\u00FC", TwoByteUtf8 + TwoByteUtf8),
                ("\u8BF6\u8BF6", ThreeByteUtf8 + ThreeByteUtf8),
                ("\uD83D\uDE00\uD83D\uDE00", FourByteUtf8 + FourByteUtf8),

                ("A\u00FCA", OneByteUtf8 + TwoByteUtf8 + OneByteUtf8),
                ("\u00FC\u8BF6\u00FC", TwoByteUtf8 + ThreeByteUtf8 + TwoByteUtf8),

                (InvalidOneByteUtf8 + "A", InvalidOneByteUtf8 + OneByteUtf8),
                (OverlongTwoByteUtf8 + "\u00FC", OverlongTwoByteUtf8 + TwoByteUtf8),
                (OverlongThreeByteUtf8 + "\u8BF6", OverlongThreeByteUtf8 + ThreeByteUtf8),
                (OverlongFourByteUtf8 + "\uD83D\uDE00", OverlongFourByteUtf8 + FourByteUtf8),

                (InvalidOneByteUtf8, InvalidOneByteUtf8),
                (InvalidOneByteUtf8 + InvalidOneByteUtf8, InvalidOneByteUtf8 + InvalidOneByteUtf8),
                (InvalidOneByteUtf8 + InvalidOneByteUtf8 + InvalidOneByteUtf8, InvalidOneByteUtf8 + InvalidOneByteUtf8 + InvalidOneByteUtf8),

                // 11001010 11100100 10001000 10110010 - 2-byte marker followed by 3-byte sequence
                ("%CA" + '\u4232', "%CA" + "%E4%88%B2"),

                // 4 valid UTF8 bytes followed by 5 invalid UTF8 bytes
                ("\U0010003A" + "%FD%80%80%BA%CD", "%F4%80%80%BA" + "%FD%80%80%BA%CD"),

                // BIDI char
                ("\u200E", "%E2%80%8E"),

                // Char Block: 3400..4DBF-CJK Unified Ideographs Extension A
                ("\u4232", "%E4%88%B2"),

                // BIDI char followed by a valid 3-byte UTF8 sequence (\u30AF)
                ("\u200E" + "\u30AF", "%E2%80%8E" + "%E3%82%AF"),

                // BIDI char followed by invalid UTF8 bytes
                ("\u200E" + "%F0%90%90", "%E2%80%8E" + "%F0%90%90"),

                // Input string:                %98%C8%D4%F3 %D4%A8 %7A %CF%DE %41 %16
                // Valid Unicode sequences:                  %D4%A8 %7A        %41 %16
                ("%98%C8%D4%F3" + '\u0528' + 'z' + "%CF%DE" + 'A' + '\x16', "%98%C8%D4%F3" + "%D4%A8" + "%7A" + "%CF%DE" + "%41" + "%16"),

                // 2-byte marker, valid 4-byte sequence, continuation byte
                ("%C6" + "\U000FC878" + "%B5", "%C6" + "%F3%BC%A1%B8" + "%B5"),
            ];

            return pairs
                .SelectMany(p => CombinationsWithDifferentSections(p.Unescaped, p.Escaped))
                .Select(p => new[] { p.Unescaped, p.Escaped });
        }

        [Theory]
        [MemberData(nameof(UriEscapeDataString_MemberData))]
        public void UriEscapeDataString(string unescaped, string escaped)
        {
            ValidateEscape(unescaped, escaped);

            using (new ThreadCultureChange("zh-cn"))
            {
                // Same result expected in different locales.
                ValidateEscape(unescaped, escaped);
            }

            static void ValidateEscape(string input, string expectedOutput)
            {
                Assert.True(input.Length <= expectedOutput.Length);

                // String overload
                string output = Uri.EscapeDataString(input);
                Assert.Equal(expectedOutput, output);

                if (input == expectedOutput)
                {
                    Assert.Same(input, output);
                }

                // Span overload
                output = Uri.EscapeDataString(input.AsSpan());
                Assert.Equal(expectedOutput, output);

                char[] destination = new char[expectedOutput.Length + 2];

                // Exact destination size
                Assert.True(Uri.TryEscapeDataString(input, destination.AsSpan(0, expectedOutput.Length), out int charsWritten));
                Assert.Equal(expectedOutput.Length, charsWritten);
                Assert.Equal(expectedOutput, destination.AsSpan(0, charsWritten));

                // Larger destination
                Assert.True(Uri.TryEscapeDataString(input, destination.AsSpan(1), out charsWritten));
                Assert.Equal(expectedOutput.Length, charsWritten);
                Assert.Equal(expectedOutput, destination.AsSpan(1, charsWritten));

                // Destination too small
                if (expectedOutput.Length > 0)
                {
                    Assert.False(Uri.TryEscapeDataString(input, destination.AsSpan(0, expectedOutput.Length - 1), out charsWritten));
                    Assert.Equal(0, charsWritten);
                }

                // Overlapped source/destination
                input.CopyTo(destination);
                Assert.True(Uri.TryEscapeDataString(destination.AsSpan(0, input.Length), destination, out charsWritten));
                Assert.Equal(expectedOutput.Length, charsWritten);
                Assert.Equal(expectedOutput, destination.AsSpan(0, charsWritten));

                // Overlapped source/destination with different starts
                input.CopyTo(destination.AsSpan(1));
                Assert.True(Uri.TryEscapeDataString(destination.AsSpan(1, input.Length), destination, out charsWritten));
                Assert.Equal(expectedOutput.Length, charsWritten);
                Assert.Equal(expectedOutput, destination.AsSpan(0, charsWritten));

                input.CopyTo(destination);
                Assert.True(Uri.TryEscapeDataString(destination.AsSpan(0, input.Length), destination.AsSpan(1), out charsWritten));
                Assert.Equal(expectedOutput.Length, charsWritten);
                Assert.Equal(expectedOutput, destination.AsSpan(1, charsWritten));
            }
        }

        [Theory]
        [MemberData(nameof(UriUnescapeDataString_MemberData))]
        public void UriUnescapeDataString(string unescaped, string escaped)
        {
            ValidateUnescape(escaped, unescaped);

            using (new ThreadCultureChange("zh-cn"))
            {
                // Same result expected in different locales.
                ValidateUnescape(escaped, unescaped);
            }

            static void ValidateUnescape(string input, string expectedOutput)
            {
                Assert.True(input.Length >= expectedOutput.Length);

                // String overload
                string output = Uri.UnescapeDataString(input);
                Assert.Equal(expectedOutput, output);

                // Span overload
                output = Uri.UnescapeDataString(input.AsSpan());
                Assert.Equal(expectedOutput, output);

                char[] destination = new char[input.Length + 2];

                // Exact destination size
                Assert.True(Uri.TryUnescapeDataString(input, destination.AsSpan(0, expectedOutput.Length), out int charsWritten));
                Assert.Equal(expectedOutput.Length, charsWritten);
                Assert.Equal(expectedOutput, destination.AsSpan(0, charsWritten));

                // Larger destination
                Assert.True(Uri.TryUnescapeDataString(input, destination.AsSpan(1), out charsWritten));
                Assert.Equal(expectedOutput.Length, charsWritten);
                Assert.Equal(expectedOutput, destination.AsSpan(1, charsWritten));

                // Destination too small
                if (expectedOutput.Length > 0)
                {
                    Assert.False(Uri.TryUnescapeDataString(input, destination.AsSpan(0, expectedOutput.Length - 1), out charsWritten));
                    Assert.Equal(0, charsWritten);
                }

                // Overlapped source/destination
                input.CopyTo(destination);
                Assert.True(Uri.TryUnescapeDataString(destination.AsSpan(0, input.Length), destination, out charsWritten));
                Assert.Equal(expectedOutput.Length, charsWritten);
                Assert.Equal(expectedOutput, destination.AsSpan(0, charsWritten));

                // Overlapped source/destination with different starts
                input.CopyTo(destination.AsSpan(1));
                Assert.True(Uri.TryUnescapeDataString(destination.AsSpan(1, input.Length), destination, out charsWritten));
                Assert.Equal(expectedOutput.Length, charsWritten);
                Assert.Equal(expectedOutput, destination.AsSpan(0, charsWritten));

                input.CopyTo(destination);
                Assert.True(Uri.TryUnescapeDataString(destination.AsSpan(0, input.Length), destination.AsSpan(1), out charsWritten));
                Assert.Equal(expectedOutput.Length, charsWritten);
                Assert.Equal(expectedOutput, destination.AsSpan(1, charsWritten));
            }
        }

        [Fact]
        public void UriEscapeUnescapeDataString_LongInputs()
        {
            // Test the no-longer-existing "c_MaxUriBufferSize" limit of 0xFFF0,
            // as well as lengths longer than the max Uri length of ushort.MaxValue.
            foreach (int length in new[] { 1, 0xFFF0, 0xFFF1, ushort.MaxValue + 10 })
            {
                string unescaped = new string('s', length);
                string escaped = unescaped;

                Assert.Equal(Uri.EscapeDataString(unescaped), escaped);
                Assert.Equal(Uri.UnescapeDataString(escaped), unescaped);

                unescaped = new string('/', length);
                escaped = EscapeAscii(unescaped);

                Assert.Equal(Uri.EscapeDataString(unescaped), escaped);
                Assert.Equal(Uri.UnescapeDataString(escaped), unescaped);
            }
        }

        #endregion EscapeUnescapeDataString

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
            string input = "\u30AF";
            string output = Uri.EscapeUriString(input);
            Assert.Equal("%E3%82%AF", output);

            using (new ThreadCultureChange("zh-cn"))
            {
                Assert.Equal(output, Uri.EscapeUriString(input)); // Same normalized result expected in different locales.
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

        public static IEnumerable<object[]> UriEscapingUriString_Long_MemberData()
        {
            // Test the no-longer-existing "c_MaxUriBufferSize" limit of 0xFFF0,
            // as well as lengths longer than the max Uri length of ushort.MaxValue.
            foreach (int length in new[] { 1, 0xFFF0, 0xFFF1, ushort.MaxValue + 10 })
            {
                yield return new object[] { new string('s', length), string.Concat(Enumerable.Repeat("s", length)) };
                yield return new object[] { new string('<', length), string.Concat(Enumerable.Repeat("%3C", length)) };
            }
        }

        [Theory]
        [MemberData(nameof(UriEscapingUriString_Long_MemberData))]
        public void UriEscapingUriString_Long_Escaped(string input, string expectedEscaped)
        {
            Assert.Equal(expectedEscaped, Uri.EscapeUriString(input));
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
            string escapedAlphaNum = EscapeAscii(AlphaNumeric);
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
            string escaped = EscapeAscii(RFC3986Unreserved);
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
            string escaped = EscapeAscii(RFC2396Reserved);
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
            string escaped = EscapeAscii(RFC3986Reserved);
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

            Uri uri = new Uri(uriString + Uri.EscapeDataString(GB18030CertificationString1));
            Assert.Equal(expectedString, uri.AbsoluteUri);

            using (new ThreadCultureChange("zh-cn"))
            {
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
            string input = "http://host/%5A%E6%9C%88%5A";

            string output = Uri.UnescapeDataString(input);
            Assert.Equal("http://host/Z\u6708Z", output);

            Uri uri = new Uri(input);
            Assert.Equal("http://host/Z%E6%9C%88Z", uri.AbsoluteUri);

            using (new ThreadCultureChange("zh-cn"))
            {
                Assert.Equal(output, Uri.UnescapeDataString(input));
                Assert.Equal(uri.AbsoluteUri, new Uri(input).AbsoluteUri);
            }
        }

        [Fact]
        public void UriUnescapeInvalid_AsciiIncompleteUtf8AsciiIriOn_InvalidUtf8LeftAlone()
        {
            string input = "http://host/%5A%E5%9B%5A";

            string output = Uri.UnescapeDataString(input);
            Assert.Equal("http://host/Z%E5%9BZ", output);

            Uri uri = new Uri(input);
            Assert.Equal("http://host/Z%E5%9BZ", uri.ToString());
            Assert.Equal("http://host/Z%E5%9BZ", uri.AbsoluteUri);

            using (new ThreadCultureChange("zh-cn"))
            {
                Assert.Equal(output, Uri.UnescapeDataString(input));

                Uri uriZhCn = new Uri(input);
                Assert.Equal(uri.AbsoluteUri, uriZhCn.AbsoluteUri);
                Assert.Equal(uri.ToString(), uriZhCn.ToString());
            }
        }

        [Fact]
        public void UriUnescapeInvalid_IncompleteUtf8BetweenValidUtf8IriOn_InvalidUtf8LeftAlone()
        {
            string input = "http://host/%E6%9C%88%E5%9B%E6%9C%88";

            string output = Uri.UnescapeDataString(input);
            Assert.Equal("http://host/\u6708%E5%9B\u6708", output);

            Uri uri = new Uri(input);
            Assert.Equal(input, uri.AbsoluteUri);

            using (new ThreadCultureChange("zh-cn"))
            {
                Assert.Equal(output, Uri.UnescapeDataString(input));
                Assert.Equal(uri.AbsoluteUri, new Uri(input).AbsoluteUri);
            }
        }

        [Fact]
        public void UriUnescapeInvalid_IncompleteUtf8AfterValidUtf8IriOn_InvalidUtf8LeftAlone()
        {
            string input = "http://host/%59%E6%9C%88%E5%9B";

            Uri uri = new Uri(input);
            Assert.Equal("http://host/Y%E6%9C%88%E5%9B", uri.AbsoluteUri);
            Assert.Equal("http://host/Y\u6708%E5%9B", uri.ToString());

            string output = Uri.UnescapeDataString(input);
            Assert.Equal("http://host/Y\u6708%E5%9B", output);

            using (new ThreadCultureChange("zh-cn"))
            {
                Uri uriZhCn = new Uri(input);
                Assert.Equal(uri.ToString(), uriZhCn.ToString());
                Assert.Equal(uri.AbsoluteUri, uriZhCn.AbsoluteUri);

                Assert.Equal(output, Uri.UnescapeDataString(input));
            }
        }

        [Fact]
        public void UriUnescapeInvalid_ValidUtf8IncompleteUtf8AsciiIriOn_InvalidUtf8LeftAlone()
        {
            string input = "http://host/%E6%9C%88%E6%9C%59";

            Uri uri = new Uri(input);
            Assert.Equal("http://host/%E6%9C%88%E6%9CY", uri.AbsoluteUri);
            Assert.Equal("http://host/\u6708%E6%9CY", uri.ToString());

            string output = Uri.UnescapeDataString(input);
            Assert.Equal("http://host/\u6708%E6%9CY", output);

            using (new ThreadCultureChange("zh-cn"))
            {
                Uri uriZhCn = new Uri(input);
                Assert.Equal(uri.ToString(), uriZhCn.ToString());
                Assert.Equal(uri.AbsoluteUri, uriZhCn.AbsoluteUri);

                Assert.Equal(output, Uri.UnescapeDataString(input));
            }
        }

        #endregion Invalid escape sequences

        #region Helpers

        // Percent encode every character
        private static string EscapeAscii(string input)
        {
            Assert.True(Ascii.IsValid(input));

            return string.Concat(input.Select(c => $"%{(int)c:X2}"));
        }

        #endregion Helpers
    }
}
