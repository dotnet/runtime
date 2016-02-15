// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Xunit;

namespace Microsoft.Framework.WebEncoders
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
            UrlEncoder encoder = new UrlEncoder(filter);

            // Act & assert
            Assert.Equal("a", encoder.UrlEncode("a"));
            Assert.Equal("b", encoder.UrlEncode("b"));
            Assert.Equal("%63", encoder.UrlEncode("c"));
            Assert.Equal("d", encoder.UrlEncode("d"));
            Assert.Equal("%00", encoder.UrlEncode("\0")); // we still always encode control chars
            Assert.Equal("%26", encoder.UrlEncode("&")); // we still always encode HTML-special chars
            Assert.Equal("%EF%BF%BF", encoder.UrlEncode("\uFFFF")); // we still always encode non-chars and other forbidden chars
        }

        [Fact]
        public void Ctor_WithUnicodeRanges()
        {
            // Arrange
            UrlEncoder encoder = new UrlEncoder(UnicodeRanges.Latin1Supplement, UnicodeRanges.MiscellaneousSymbols);

            // Act & assert
            Assert.Equal("%61", encoder.UrlEncode("a"));
            Assert.Equal("\u00E9", encoder.UrlEncode("\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */));
            Assert.Equal("\u2601", encoder.UrlEncode("\u2601" /* CLOUD */));
        }

        [Fact]
        public void Ctor_WithNoParameters_DefaultsToBasicLatin()
        {
            // Arrange
            UrlEncoder encoder = new UrlEncoder();

            // Act & assert
            Assert.Equal("a", encoder.UrlEncode("a"));
            Assert.Equal("%C3%A9", encoder.UrlEncode("\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */));
            Assert.Equal("%E2%98%81", encoder.UrlEncode("\u2601" /* CLOUD */));
        }

        [Fact]
        public void Default_EquivalentToBasicLatin()
        {
            // Arrange
            UrlEncoder controlEncoder = new UrlEncoder(UnicodeRanges.BasicLatin);
            UrlEncoder testEncoder = UrlEncoder.Default;

            // Act & assert
            for (int i = 0; i <= Char.MaxValue; i++)
            {
                if (!IsSurrogateCodePoint(i))
                {
                    string input = new String((char)i, 1);
                    Assert.Equal(controlEncoder.UrlEncode(input), testEncoder.UrlEncode(input));
                }
            }
        }

        [Fact]
        public void UrlEncode_AllRangesAllowed_StillEncodesForbiddenChars()
        {
            // Arrange
            UrlEncoder encoder = new UrlEncoder(UnicodeRanges.All);

            // Act & assert - BMP chars
            for (int i = 0; i <= 0xFFFF; i++)
            {
                string input = new String((char)i, 1);
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
                        mustEncode = !UnicodeHelpers.IsCharacterDefined((char)i); // 'ucschar'
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

                string retVal = encoder.UrlEncode(input);
                Assert.Equal(expected, retVal);
            }

            // Act & assert - astral chars
            for (int i = 0x10000; i <= 0x10FFFF; i++)
            {
                string input = Char.ConvertFromUtf32(i);
                string expected = GetKnownGoodPercentEncodedValue(i);
                string retVal = encoder.UrlEncode(input);
                Assert.Equal(expected, retVal);
            }
        }

        [Fact]
        public void UrlEncode_BadSurrogates_ReturnsUnicodeReplacementChar()
        {
            // Arrange
            UrlEncoder encoder = new UrlEncoder(UnicodeRanges.All); // allow all codepoints

            // "a<unpaired leading>b<unpaired trailing>c<trailing before leading>d<unpaired trailing><valid>e<high at end of string>"
            const string input = "a\uD800b\uDFFFc\uDFFF\uD800d\uDFFF\uD800\uDFFFe\uD800";
            const string expected = "a%EF%BF%BDb%EF%BF%BDc%EF%BF%BD%EF%BF%BDd%EF%BF%BD%F0%90%8F%BFe%EF%BF%BD"; // 'D800' 'DFFF' was preserved since it's valid

            // Act
            string retVal = encoder.UrlEncode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void UrlEncode_EmptyStringInput_ReturnsEmptyString()
        {
            // Arrange
            UrlEncoder encoder = new UrlEncoder();

            // Act & assert
            Assert.Equal("", encoder.UrlEncode(""));
        }

        [Fact]
        public void UrlEncode_InputDoesNotRequireEncoding_ReturnsOriginalStringInstance()
        {
            // Arrange
            UrlEncoder encoder = new UrlEncoder();
            string input = "Hello,there!";

            // Act & assert
            Assert.Same(input, encoder.UrlEncode(input));
        }

        [Fact]
        public void UrlEncode_NullInput_ReturnsNull()
        {
            // Arrange
            UrlEncoder encoder = new UrlEncoder();

            Assert.Throws<ArgumentNullException>(() => { encoder.UrlEncode(null); });
        }

        [Fact]
        public void UrlEncode_WithCharsRequiringEncodingAtBeginning()
        {
            Assert.Equal(@"%26Hello,there!", new UrlEncoder().UrlEncode("&Hello,there!"));
        }

        [Fact]
        public void UrlEncode_WithCharsRequiringEncodingAtEnd()
        {
            Assert.Equal(@"Hello,there!%26", new UrlEncoder().UrlEncode("Hello,there!&"));
        }

        [Fact]
        public void UrlEncode_WithCharsRequiringEncodingInMiddle()
        {
            Assert.Equal(@"Hello,%20%26there!", new UrlEncoder().UrlEncode("Hello, &there!"));
        }

        [Fact]
        public void UrlEncode_WithCharsRequiringEncodingInterspersed()
        {
            Assert.Equal(@"Hello,%20%3Cthere%3E!", new UrlEncoder().UrlEncode("Hello, <there>!"));
        }

        [Fact]
        public void UrlEncode_CharArray()
        {
            // Arrange
            UrlEncoder encoder = new UrlEncoder();
            var output = new StringWriter();

            // Act
            encoder.UrlEncode("Hello+world!".ToCharArray(), 3, 5, output);

            // Assert
            Assert.Equal("lo%2Bwo", output.ToString());
        }

        [Fact]
        public void UrlEncode_StringSubstring()
        {
            // Arrange
            UrlEncoder encoder = new UrlEncoder();
            var output = new StringWriter();

            // Act
            encoder.UrlEncode("Hello+world!", 3, 5, output);

            // Assert
            Assert.Equal("lo%2Bwo", output.ToString());
        }

        [Fact]
        public void UrlEncode_DoesNotOutputHtmlSensitiveCharacters()
        {
            // Per the design document, we provide additional defense-in-depth
            // by never emitting HTML-sensitive characters unescaped.

            // Arrange
            UrlEncoder urlEncoder = new UrlEncoder(UnicodeRanges.All);
            HtmlEncoder htmlEncoder = new HtmlEncoder(UnicodeRanges.All);

            // Act & assert
            for (int i = 0; i <= 0x10FFFF; i++)
            {
                if (IsSurrogateCodePoint(i))
                {
                    continue; // surrogates don't matter here
                }

                string urlEncoded = urlEncoder.UrlEncode(Char.ConvertFromUtf32(i));
                string thenHtmlEncoded = htmlEncoder.HtmlEncode(urlEncoded);
                Assert.Equal(urlEncoded, thenHtmlEncoded); // should have contained no HTML-sensitive characters
            }
        }

        private static string GetKnownGoodPercentEncodedValue(int codePoint)
        {
            // Convert the code point to UTF16, then call Encoding.UTF8.GetBytes, then hex-encode everything
            return String.Concat(_utf8EncodingThrowOnInvalidBytes.GetBytes(Char.ConvertFromUtf32(codePoint)).Select(b => String.Format(CultureInfo.InvariantCulture, "%{0:X2}", b)));
        }

        private static bool IsSurrogateCodePoint(int codePoint)
        {
            return (0xD800 <= codePoint && codePoint <= 0xDFFF);
        }
    }
}
