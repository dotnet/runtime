// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Unicode;
using Xunit;

namespace System.Text.Encodings.Web.Tests
{
    public class UrlEncoderTests
    {
        private static UTF8Encoding _utf8EncodingThrowOnInvalidBytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        [Fact]
        public void TestSurrogate()
        {
            Assert.Equal("%F0%9F%92%A9", System.Text.Encodings.Web.UrlEncoder.Default.Encode("\U0001f4a9"));
            using (var writer = new StringWriter())
            {
                System.Text.Encodings.Web.UrlEncoder.Default.Encode(writer, "\U0001f4a9");
                Assert.Equal("%F0%9F%92%A9", writer.GetStringBuilder().ToString());
            }
        }

        [Fact]
        public void Ctor_WithTextEncoderSettings()
        {
            // Arrange
            var filter = new TextEncoderSettings();
            filter.AllowCharacters('a', 'b');
            filter.AllowCharacters('\0', '&', '\uFFFF', 'd');
            UrlEncoder encoder = UrlEncoder.Create(filter);

            // Act & assert
            Assert.Equal("a", encoder.Encode("a"));
            Assert.Equal("b", encoder.Encode("b"));
            Assert.Equal("%63", encoder.Encode("c"));
            Assert.Equal("d", encoder.Encode("d"));
            Assert.Equal("%00", encoder.Encode("\0")); // we still always encode control chars
            Assert.Equal("%26", encoder.Encode("&")); // we still always encode HTML-special chars
            Assert.Equal("%EF%BF%BF", encoder.Encode("\uFFFF")); // we still always encode non-chars and other forbidden chars
        }

        [Fact]
        public void Ctor_WithUnicodeRanges()
        {
            // Arrange
            UrlEncoder encoder = UrlEncoder.Create(UnicodeRanges.Latin1Supplement, UnicodeRanges.MiscellaneousSymbols);

            // Act & assert
            Assert.Equal("%61", encoder.Encode("a"));
            Assert.Equal("\u00E9", encoder.Encode("\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */));
            Assert.Equal("\u2601", encoder.Encode("\u2601" /* CLOUD */));
        }

        [Fact]
        public void DefaultFactory_IsBasicLatin()
        {
            // Arrange
            UrlEncoder encoder = UrlEncoder.Default;

            // Act & assert
            Assert.Equal("a", encoder.Encode("a"));
            Assert.Equal("%C3%A9", encoder.Encode("\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */));
            Assert.Equal("%E2%98%81", encoder.Encode("\u2601" /* CLOUD */));
        }

        [Fact]
        public void Default_EquivalentToBasicLatin()
        {
            // Arrange
            UrlEncoder controlEncoder = UrlEncoder.Create(UnicodeRanges.BasicLatin);
            UrlEncoder testEncoder = UrlEncoder.Default;

            // Act & assert
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (!IsSurrogateCodePoint(i))
                {
                    string input = new string((char)i, 1);
                    Assert.Equal(controlEncoder.Encode(input), testEncoder.Encode(input));
                }
            }
        }

        [Fact]
        public void UrlEncode_AllRangesAllowed_StillEncodesForbiddenChars()
        {
            // Arrange
            UrlEncoder encoder = UrlEncoder.Create(UnicodeRanges.All);

            // Act & assert - BMP chars
            for (int i = 0; i <= 0xFFFF; i++)
            {
                string input = new string((char)i, 1);
                string expected;
                if (IsSurrogateCodePoint(i))
                {
                    expected = "%EF%BF%BD"; // unpaired surrogate -> Unicode replacement char
                }
                else
                {
                    bool mustEncode = true;

                    // RFC 3987, Sec. 2.2 gives the list of allowed chars
                    // (We allow 'ipchar' except for "'", "&", "+", "%", and "="
                    if (('a' <= i && i <= 'z') || ('A' <= i && i <= 'Z') || ('0' <= i && i <= '9'))
                    {
                        mustEncode = false; // ALPHA / DIGIT
                    }
                    else if ((0x00A0 <= i && i <= 0xD7FF) | (0xF900 <= i && i <= 0xFDCF) | (0xFDF0 <= i && i <= 0xFFEF))
                    {
                        mustEncode = !UnicodeTestHelpers.IsCharacterDefined((char)i); // 'ucschar'
                    }
                    else
                    {
                        switch (i)
                        {
                            // iunreserved
                            case '-':
                            case '.':
                            case '_':
                            case '~':

                            // isegment-nz-nc
                            case '@':

                            // sub-delims
                            case '!':
                            case '$':
                            case '(':
                            case ')':
                            case '*':
                            case ',':
                            case ';':
                                mustEncode = false;
                                break;
                        }
                    }

                    if (mustEncode)
                    {
                        expected = GetKnownGoodPercentEncodedValue(i);
                    }
                    else
                    {
                        expected = input; // no encoding
                    }
                }

                string retVal = encoder.Encode(input);
                Assert.Equal(expected, retVal);
            }

            // Act & assert - astral chars
            for (int i = 0x10000; i <= 0x10FFFF; i++)
            {
                string input = char.ConvertFromUtf32(i);
                string expected = GetKnownGoodPercentEncodedValue(i);
                string retVal = encoder.Encode(input);
                Assert.Equal(expected, retVal);
            }
        }

        [Fact]
        public void UrlEncode_BadSurrogates_ReturnsUnicodeReplacementChar()
        {
            // Arrange
            UrlEncoder encoder = UrlEncoder.Create(UnicodeRanges.All); // allow all codepoints

            // "a<unpaired leading>b<unpaired trailing>c<trailing before leading>d<unpaired trailing><valid>e<high at end of string>"
            const string input = "a\uD800b\uDFFFc\uDFFF\uD800d\uDFFF\uD800\uDFFFe\uD800";
            const string expected = "a%EF%BF%BDb%EF%BF%BDc%EF%BF%BD%EF%BF%BDd%EF%BF%BD%F0%90%8F%BFe%EF%BF%BD"; // 'D800' 'DFFF' was preserved since it's valid

            // Act
            string retVal = encoder.Encode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void UrlEncode_EmptyStringInput_ReturnsEmptyString()
        {
            // Arrange
            UrlEncoder encoder = UrlEncoder.Default;

            // Act & assert
            Assert.Equal("", encoder.Encode(""));
        }

        [Fact]
        public void UrlEncode_InputDoesNotRequireEncoding_ReturnsOriginalStringInstance()
        {
            // Arrange
            UrlEncoder encoder = UrlEncoder.Default;
            string input = "Hello,there!";

            // Act & assert
            Assert.Same(input, encoder.Encode(input));
        }

        [Fact]
        public void UrlEncode_NullInput_ReturnsNull()
        {
            // Arrange
            UrlEncoder encoder = UrlEncoder.Default;

            Assert.Throws<ArgumentNullException>(() => { encoder.Encode(null); });
        }

        [Fact]
        public void UrlEncode_WithCharsRequiringEncodingAtBeginning()
        {
            Assert.Equal(@"%26Hello,there!", UrlEncoder.Default.Encode("&Hello,there!"));
        }

        [Fact]
        public void UrlEncode_WithCharsRequiringEncodingAtEnd()
        {
            Assert.Equal(@"Hello,there!%26", UrlEncoder.Default.Encode("Hello,there!&"));
        }

        [Fact]
        public void UrlEncode_WithCharsRequiringEncodingInMiddle()
        {
            Assert.Equal(@"Hello,%20%26there!", UrlEncoder.Default.Encode("Hello, &there!"));
        }

        [Fact]
        public void UrlEncode_WithCharsRequiringEncodingInterspersed()
        {
            Assert.Equal(@"Hello,%20%3Cthere%3E!", UrlEncoder.Default.Encode("Hello, <there>!"));
        }

        [Fact]
        public void UrlEncode_CharArray()
        {
            // Arrange
            UrlEncoder encoder = UrlEncoder.Default;
            var output = new StringWriter();

            // Act
            encoder.Encode(output, "Hello+world!".ToCharArray(), 3, 5);

            // Assert
            Assert.Equal("lo%2Bwo", output.ToString());
        }

        [Fact]
        public void UrlEncode_StringSubstring()
        {
            // Arrange
            UrlEncoder encoder = UrlEncoder.Default;
            var output = new StringWriter();

            // Act
            encoder.Encode(output, "Hello+world!", 3, 5);

            // Assert
            Assert.Equal("lo%2Bwo", output.ToString());
        }

        [Fact]
        public void UrlEncode_DoesNotOutputHtmlSensitiveCharacters()
        {
            // Per the design document, we provide additional defense-in-depth
            // by never emitting HTML-sensitive characters unescaped.

            // Arrange
            UrlEncoder urlEncoder = UrlEncoder.Create(UnicodeRanges.All);
            HtmlEncoder htmlEncoder = HtmlEncoder.Create(UnicodeRanges.All);

            // Act & assert
            for (int i = 0; i <= 0x10FFFF; i++)
            {
                if (IsSurrogateCodePoint(i))
                {
                    continue; // surrogates don't matter here
                }

                string urlEncoded = urlEncoder.Encode(char.ConvertFromUtf32(i));
                string thenHtmlEncoded = htmlEncoder.Encode(urlEncoded);
                Assert.Equal(urlEncoded, thenHtmlEncoded); // should have contained no HTML-sensitive characters
            }
        }

        private static string GetKnownGoodPercentEncodedValue(int codePoint)
        {
            // Convert the code point to UTF16, then call Encoding.UTF8.GetBytes, then hex-encode everything
            return string.Concat(_utf8EncodingThrowOnInvalidBytes.GetBytes(char.ConvertFromUtf32(codePoint)).Select(b => string.Format(CultureInfo.InvariantCulture, "%{0:X2}", b)));
        }

        private static bool IsSurrogateCodePoint(int codePoint)
        {
            return (0xD800 <= codePoint && codePoint <= 0xDFFF);
        }
    }
}
