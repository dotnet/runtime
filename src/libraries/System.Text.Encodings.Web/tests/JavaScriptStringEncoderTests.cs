// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Xunit;

namespace Microsoft.Framework.WebEncoders
{
    public partial class JavaScriptStringEncoderTests
    {
        [Fact]
        public void TestSurrogate()
        {
            // Encode(string)
            Assert.Equal("\\uD83D\\uDCA9", System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode("\U0001f4a9"));

            // Encode(writer, string)
            using (var writer = new StringWriter())
            {
                System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(writer, "\U0001f4a9");
                Assert.Equal("\\uD83D\\uDCA9", writer.GetStringBuilder().ToString());
            }

            // Encode(Span, ...)
            Span<char> destination = new char[12];
            OperationStatus status = System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(
                "\U0001f4a9".AsSpan(), destination, out int charsConsumed, out int charsWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(2, charsConsumed);
            Assert.Equal(12, charsWritten);
            Assert.Equal("\\uD83D\\uDCA9", new string(destination.Slice(0, charsWritten).ToArray()));
        }

        [Fact]
        public void TestSurrogateBufferDoesNotUnderOrOverWrite()
        {
            Span<char> destination = new char[212];
            destination[99] = 'x';
            destination[112] = 'x';

            // Pass in destination + 100 to check for underwrite.
            OperationStatus status = System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(
                "\U0001f4a9".AsSpan(), destination.Slice(100, 12), out int charsConsumed, out int charsWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(2, charsConsumed);
            Assert.Equal(12, charsWritten);
            Assert.Equal('x', destination[99]);
            Assert.Equal('x', destination[112]);
        }

        [Fact]
        public void TestSurrogateBufferOverlaps()
        {
            Span<char> destination = new char[100];
            "\U0001f4a9".AsSpan().CopyTo(destination);

            // Overlap behavior is undefined but documented that it is not valid. Here we don't expect any issues.
            OperationStatus status = System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(
                destination.Slice(0, 2), destination, out int charsConsumed, out int charsWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(2, charsConsumed);
            Assert.Equal(12, charsWritten);
        }

        [Fact]
        public void TestSurrogateBufferTooSmall()
        {
            Span<char> destination = new char[11];
            OperationStatus status = System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(
                "\U0001f4a9".AsSpan(), destination, out int charsConsumed, out int charsWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.DestinationTooSmall, status);
            Assert.Equal(0, charsConsumed);
            Assert.Equal(0, charsWritten);
        }

        [Fact]
        public void JavaScriptStringEncoder_NonEmptySource_EmptyDest_Throws()
        {
            OperationStatus status = System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(
                "\U0001f4a9".AsSpan(), destination: null, out int _, out int _, isFinalBlock: true);

            Assert.Equal(OperationStatus.DestinationTooSmall, status);
        }

        [Fact]
        public void JavaScriptStringEncoder_EmptySource_EmptyDest()
        {
            OperationStatus status = System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(
                "".AsSpan(), destination: null, out int _, out int _, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
        }

        [Fact]
        public void TestEmptySourceEncode()
        {
            // Encode(string)
            Assert.Equal("", System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(""));

            // Encode(writer, string)
            using (var writer = new StringWriter())
            {
                System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(writer, "");
                Assert.Equal("", writer.GetStringBuilder().ToString());
            }

            // Encode(Span, ...)
            Span<char> destination = new char[12];
            OperationStatus status = System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(
                "".AsSpan(), destination, out int charsConsumed, out int charsWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(0, charsConsumed);
            Assert.Equal(0, charsWritten);
            Assert.Equal("", new string(destination.Slice(0, charsWritten).ToArray()));

            destination = null; // null doesn't throw is no characters to encode
            status = System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(
                "".AsSpan(), destination, out charsConsumed, out charsWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(0, charsConsumed);
            Assert.Equal(0, charsWritten);
            Assert.Equal("", new string(destination.Slice(0, charsWritten).ToArray()));
        }

        [Fact]
        public void Ctor_WithTextEncoderSettings()
        {
            // Arrange
            var filter = new TextEncoderSettings();
            filter.AllowCharacters('a', 'b');
            filter.AllowCharacters('\0', '&', '\uFFFF', 'd');

            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder(filter);

            // Act & assert
            Assert.Equal("a", encoder.JavaScriptStringEncode("a"));
            Assert.Equal("b", encoder.JavaScriptStringEncode("b"));
            Assert.Equal(@"\u0063", encoder.JavaScriptStringEncode("c"));
            Assert.Equal("d", encoder.JavaScriptStringEncode("d"));
            Assert.Equal(@"\u0000", encoder.JavaScriptStringEncode("\0")); // we still always encode control chars
            Assert.Equal(@"\u0026", encoder.JavaScriptStringEncode("&")); // we still always encode HTML-special chars
            Assert.Equal(@"\uFFFF", encoder.JavaScriptStringEncode("\uFFFF")); // we still always encode non-chars and other forbidden chars
        }

        [Fact]
        public void Ctor_WithUnicodeRanges()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder(UnicodeRanges.Latin1Supplement, UnicodeRanges.MiscellaneousSymbols);

            // Act & assert
            Assert.Equal(@"\u0061", encoder.JavaScriptStringEncode("a"));
            Assert.Equal("\u00E9", encoder.JavaScriptStringEncode("\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */));
            Assert.Equal("\u2601", encoder.JavaScriptStringEncode("\u2601" /* CLOUD */));
        }

        [Fact]
        public void Ctor_WithNoParameters_DefaultsToBasicLatin()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder();

            // Act & assert
            Assert.Equal("a", encoder.JavaScriptStringEncode("a"));
            Assert.Equal(@"\u00E9", encoder.JavaScriptStringEncode("\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */));
            Assert.Equal(@"\u2601", encoder.JavaScriptStringEncode("\u2601" /* CLOUD */));
        }

        [Fact]
        public void Default_EquivalentToBasicLatin()
        {
            // Arrange
            JavaScriptStringEncoder controlEncoder = new JavaScriptStringEncoder(UnicodeRanges.BasicLatin);
            JavaScriptStringEncoder testEncoder = JavaScriptStringEncoder.Default;

            // Act & assert
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (!IsSurrogateCodePoint(i))
                {
                    string input = new string((char)i, 1);
                    Assert.Equal(controlEncoder.JavaScriptStringEncode(input), testEncoder.JavaScriptStringEncode(input));
                }
            }
        }

        [Fact]
        public void JavaScriptStringEncode_AllRangesAllowed_StillEncodesForbiddenChars_Simple_Escaping() {
            // The following two calls could be simply InlineData to the Theory below
            // Unfortunately, the xUnit logger fails to escape the inputs when logging the test results,
            // and so the suite fails despite all tests passing. 
            // TODO: I will try to fix it in xUnit, but for now this is a workaround to enable these tests.
            JavaScriptStringEncode_AllRangesAllowed_StillEncodesForbiddenChars_Simple("\b", @"\b");
            JavaScriptStringEncode_AllRangesAllowed_StillEncodesForbiddenChars_Simple("\f", @"\f");
        }

        [Theory]
        [InlineData("<", @"\u003C")]
        [InlineData(">", @"\u003E")]
        [InlineData("&", @"\u0026")]
        [InlineData("'", @"\u0027")]
        [InlineData("\"", @"\u0022")]
        [InlineData("+", @"\u002B")]
        [InlineData("\\", @"\\")]
        [InlineData("\n", @"\n")]
        [InlineData("\t", @"\t")]
        [InlineData("\r", @"\r")]
        public void JavaScriptStringEncode_AllRangesAllowed_StillEncodesForbiddenChars_Simple(string input, string expected)
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder(UnicodeRanges.All);

            // Act
            string retVal = encoder.JavaScriptStringEncode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptStringEncode_AllRangesAllowed_StillEncodesForbiddenChars_Extended()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder(UnicodeRanges.All);

            // Act & assert - BMP chars
            for (int i = 0; i <= 0xFFFF; i++)
            {
                string input = new string((char)i, 1);
                string expected;
                if (IsSurrogateCodePoint(i))
                {
                    expected = "\uFFFD"; // unpaired surrogate -> Unicode replacement char
                }
                else
                {
                    if (input == "\b") { expected = @"\b"; }
                    else if (input == "\t") { expected = @"\t"; }
                    else if (input == "\n") { expected = @"\n"; }
                    else if (input == "\f") { expected = @"\f"; }
                    else if (input == "\r") { expected = @"\r"; }
                    else if (input == "\\") { expected = @"\\"; }
                    else if (input == "`") { expected = @"\u0060"; }
                    else
                    {
                        bool mustEncode = false;
                        switch (i)
                        {
                            case '<':
                            case '>':
                            case '&':
                            case '\"':
                            case '\'':
                            case '+':
                                mustEncode = true;
                                break;
                        }

                        if (i <= 0x001F || (0x007F <= i && i <= 0x9F))
                        {
                            mustEncode = true; // control char
                        }
                        else if (!UnicodeHelpers.IsCharacterDefined((char)i))
                        {
                            mustEncode = true; // undefined (or otherwise disallowed) char
                        }

                        if (mustEncode)
                        {
                            expected = string.Format(CultureInfo.InvariantCulture, @"\u{0:X4}", i);
                        }
                        else
                        {
                            expected = input; // no encoding
                        }
                    }
                }

                string retVal = encoder.JavaScriptStringEncode(input);
                Assert.Equal(expected, retVal);
            }

            // Act & assert - astral chars
            for (int i = 0x10000; i <= 0x10FFFF; i++)
            {
                string input = char.ConvertFromUtf32(i);
                string expected = string.Format(CultureInfo.InvariantCulture, @"\u{0:X4}\u{1:X4}", (uint)input[0], (uint)input[1]);
                string retVal = encoder.JavaScriptStringEncode(input);
                Assert.Equal(expected, retVal);
            }
        }

        [Fact]
        public void JavaScriptStringEncode_NoRangesAllowed_EmitsShortFormForCertainCodePoints()
        {
            // This test ensures that when we're encoding, we always emit the "\uXXXX" form of the
            // code point except for very specific code points where we allow a shorter representation.

            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder(UnicodeRanges.None); // allow no codepoints

            // "[U+0000][U+0001]...[U+007F]"
            string input = new string(Enumerable.Range(0, 128).Select(i => (char)i).ToArray());

            // @"\u0000\u0001..\u007F", then replace certain specific code points
            string expected = string.Concat(Enumerable.Range(0, 128).Select(i => FormattableString.Invariant($@"\u{i:X4}")));

            expected = expected.Replace(@"\u0008", @"\b"); // U+0008 BACKSPACE -> "\b"
            expected = expected.Replace(@"\u0009", @"\t"); // U+0009 CHARACTER TABULATION -> "\t"
            expected = expected.Replace(@"\u000A", @"\n"); // U+000A LINE FEED -> "\n"
            expected = expected.Replace(@"\u000C", @"\f"); // U+000C FORM FEED -> "\f"
            expected = expected.Replace(@"\u000D", @"\r"); // U+000D CARRIAGE RETURN -> "\n"
            expected = expected.Replace(@"\u005C", @"\\"); // U+005C REVERSE SOLIDUS -> "\\"

            // Act
            string retVal = encoder.JavaScriptStringEncode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptStringEncode_BadSurrogates_ReturnsUnicodeReplacementChar()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder(UnicodeRanges.All); // allow all codepoints

            // "a<unpaired leading>b<unpaired trailing>c<trailing before leading>d<unpaired trailing><valid>e<high at end of string>"
            const string Input = "a\uD800b\uDFFFc\uDFFF\uD800d\uDFFF\uD800\uDFFFe\uD800";
            const string Expected = "a\uFFFDb\uFFFDc\uFFFD\uFFFDd\uFFFD\\uD800\\uDFFFe\uFFFD"; // 'D800' 'DFFF' was preserved since it's valid

            // Act
            string retVal = encoder.JavaScriptStringEncode(Input);

            // Assert
            Assert.Equal(Expected, retVal);
        }

        [Fact]
        public void JavaScriptEncoder_BadSurrogates_ReturnsUnicodeReplacementChar()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.All); // allow all codepoints

            // "a<unpaired leading>b<unpaired trailing>c<trailing before leading>d<unpaired trailing><valid>e<high at end of string>"
            const string Input = "a\uD800b\uDFFFc\uDFFF\uD800d\uDFFF\uD800\uDFFFe\uD800";
            const string Expected = "a\uFFFDb\uFFFDc\uFFFD\uFFFDd\uFFFD\\uD800\\uDFFFe\uFFFD"; // 'D800' 'DFFF' was preserved since it's valid

            // String-based Encode()
            string retVal = encoder.Encode(Input);
            Assert.Equal(Expected, retVal);

            // OperationStatus-based Encode()
            Span<char> destination = new char[23];
            OperationStatus status = encoder.Encode(Input.AsSpan(), destination, out int charsConsumed, out int charsWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(13, charsConsumed);
            Assert.Equal(13, Input.Length);
            Assert.Equal(23, charsWritten);
            Assert.Equal(Expected, new string(destination.Slice(0, charsWritten).ToArray()));
        }

        [Fact]
        public void JavaScriptEncoder_UnpairedSurrogatesReplaced()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.All); // allow all codepoints

            // "a<unpaired leading low><unpaired leading high><unpaired leading high>"
            const string Input = "a\uDFFF\uD800\uD800";
            const string Expected = "a\uFFFD\uFFFD\uFFFD";

            Assert.Equal(4, Input.Length);

            // String-based Encode()
            string retVal = encoder.Encode(Input);
            Assert.Equal(Expected, retVal);

            // OperationStatus-based Encode()
            OperationStatus status;
            Span<char> destination = new char[100];
            status = encoder.Encode(Input.AsSpan(), destination, out int charsConsumed, out int charsWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(4, charsConsumed);
            Assert.Equal(4, charsWritten);
            Assert.Equal(Expected, new string(destination.Slice(0, charsWritten).ToArray()));
        }

        [Fact]
        public void JavaScriptEncoder_NeedsMoreData()
        {
            // "a<paired leading><paired trailing>"
            const string Input = "a\uD800\uDFFF";
            const string Expected = "a\\uD800\\uDFFF";

            Assert.Equal(3, Input.Length);

            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.All); // allow all codepoints
            Span<char> destination = new char[100];

            OperationStatus status;

            // Just pass in the first two characters, making uD800 an unpaired high surrogate. Set isFinalBlock=false so we get NeedMoreData.
            status = encoder.Encode(Input.AsSpan(0, 2), destination, out int charsConsumed1, out int charsWritten1, isFinalBlock: false);
            Assert.Equal(OperationStatus.NeedMoreData, status);
            Assert.Equal(1, charsConsumed1);
            Assert.Equal(1, charsWritten1);
            Assert.Equal("a", new string(destination.Slice(0, charsWritten1).ToArray()));

            // Append additional data; keep IsFinalBlock=false
            status = encoder.Encode(Input.AsSpan(charsConsumed1, 2), destination.Slice(charsWritten1), out int charsConsumed2, out int charsWritten2, isFinalBlock: false);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(2, charsConsumed2);
            Assert.Equal(12, charsWritten2);
            Assert.Equal(Expected, new string(destination.Slice(0, charsWritten1 + charsWritten2).ToArray()));

            // Ensure isFinalBlock=true has the same result since there is no longer a trailing unpaired high surrogate.
            status = encoder.Encode(Input.AsSpan(charsConsumed1, 2), destination.Slice(charsWritten1), out charsConsumed2, out charsWritten2, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(2, charsConsumed2);
            Assert.Equal(12, charsWritten2);
            Assert.Equal(Expected, new string(destination.Slice(0, charsWritten1 + charsWritten2).ToArray()));
        }

        [Fact]
        public void JavaScriptStringEncode_EmptyStringInput_ReturnsEmptyString()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder();

            // Act & assert
            Assert.Equal("", encoder.JavaScriptStringEncode(""));
        }

        [Fact]
        public void JavaScriptStringEncode_InputDoesNotRequireEncoding_ReturnsOriginalStringInstance()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder();
            string input = "Hello, there!";

            // Act & assert
            Assert.Same(input, encoder.JavaScriptStringEncode(input));
        }

        [Fact]
        public void JavaScriptStringEncode_NullInput_Throws()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder();

            Assert.Throws<ArgumentNullException>(() => { encoder.JavaScriptStringEncode(null); });
        }

        [Fact]
        public void JavaScriptStringEncode_WithCharsRequiringEncodingAtBeginning()
        {
            Assert.Equal(@"\u0026Hello, there!", new JavaScriptStringEncoder().JavaScriptStringEncode("&Hello, there!"));
        }

        [Fact]
        public void JavaScriptStringEncode_WithCharsRequiringEncodingAtEnd()
        {
            Assert.Equal(@"Hello, there!\u0026", new JavaScriptStringEncoder().JavaScriptStringEncode("Hello, there!&"));
        }

        [Fact]
        public void JavaScriptStringEncode_WithCharsRequiringEncodingInMiddle()
        {
            Assert.Equal(@"Hello, \u0026there!", new JavaScriptStringEncoder().JavaScriptStringEncode("Hello, &there!"));
        }

        [Fact]
        public void JavaScriptStringEncode_WithCharsRequiringEncodingInterspersed()
        {
            Assert.Equal(@"Hello, \u003Cthere\u003E!", new JavaScriptStringEncoder().JavaScriptStringEncode("Hello, <there>!"));
        }

        [Fact]
        public void JavaScriptStringEncode_CharArray()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder();
            var output = new StringWriter();

            // Act
            encoder.JavaScriptStringEncode("Hello+world!".ToCharArray(), 3, 5, output);

            // Assert
            Assert.Equal(@"lo\u002Bwo", output.ToString());
        }

        [Fact]
        public void JavaScriptStringEncode_StringSubstring()
        {
            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder();
            var output = new StringWriter();

            // Act
            encoder.JavaScriptStringEncode("Hello+world!", 3, 5, output);

            // Assert
            Assert.Equal(@"lo\u002Bwo", output.ToString());
        }

        [Theory]
        [InlineData("\"", @"\u0022")]
        [InlineData("'", @"\u0027")]
        public void JavaScriptStringEncode_Quotes(string input, string expected)
        {
            // Per the design document, we provide additional defense-in-depth
            // against breaking out of HTML attributes by having the encoders
            // never emit the ' or " characters. This means that we want to
            // \u-escape these characters instead of using \' and \".

            // Arrange
            JavaScriptStringEncoder encoder = new JavaScriptStringEncoder(UnicodeRanges.All);

            // Act
            string retVal = encoder.JavaScriptStringEncode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptStringEncode_DoesNotOutputHtmlSensitiveCharacters()
        {
            // Per the design document, we provide additional defense-in-depth
            // by never emitting HTML-sensitive characters unescaped.

            // Arrange
            JavaScriptStringEncoder javaScriptStringEncoder = new JavaScriptStringEncoder(UnicodeRanges.All);
            HtmlEncoder htmlEncoder = new HtmlEncoder(UnicodeRanges.All);

            // Act & assert
            for (int i = 0; i <= 0x10FFFF; i++)
            {
                if (IsSurrogateCodePoint(i))
                {
                    continue; // surrogates don't matter here
                }

                string javaScriptStringEncoded = javaScriptStringEncoder.JavaScriptStringEncode(char.ConvertFromUtf32(i));
                string thenHtmlEncoded = htmlEncoder.HtmlEncode(javaScriptStringEncoded);
                Assert.Equal(javaScriptStringEncoded, thenHtmlEncoded); // should have contained no HTML-sensitive characters
            }
        }

        private static bool IsSurrogateCodePoint(int codePoint)
        {
            return (0xD800 <= codePoint && codePoint <= 0xDFFF);
        }
    }
}
