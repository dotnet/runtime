// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Text.Unicode;
using Xunit;

namespace System.Text.Encodings.Web.Tests
{
    public partial class JavaScriptEncoderTests
    {
        [Fact]
        public void TestSurrogate_Relaxed()
        {
            Assert.Equal("\\uD83D\\uDCA9", JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode("\U0001f4a9"));

            using var writer = new StringWriter();

            JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(writer, "\U0001f4a9");
            Assert.Equal("\\uD83D\\uDCA9", writer.GetStringBuilder().ToString());
        }

        [Fact]
        public void Relaxed_EquivalentToAll_WithExceptions()
        {
            // Arrange
            JavaScriptEncoder controlEncoder = JavaScriptEncoder.Create(UnicodeRanges.All);
            JavaScriptEncoder testEncoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            // Act & assert
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (i == '"' || i == '&' || i == '<' || i == '>' || i == '+' || i == '\'' || i == '`')
                {
                    string input = new string((char)i, 1);
                    Assert.NotEqual(controlEncoder.Encode(input), testEncoder.Encode(input));
                    continue;
                }

                if (!IsSurrogateCodePoint(i))
                {
                    string input = new string((char)i, 1);
                    Assert.Equal(controlEncoder.Encode(input), testEncoder.Encode(input));
                }
            }
        }

        [Fact]
        public void JavaScriptEncode_Relaxed_StillEncodesForbiddenChars_Simple_Escaping()
        {
            // The following two calls could be simply InlineData to the Theory below
            // Unfortunately, the xUnit logger fails to escape the inputs when logging the test results,
            // and so the suite fails despite all tests passing.
            // TODO: I will try to fix it in xUnit, but for now this is a workaround to enable these tests.
            JavaScriptEncode_Relaxed_StillEncodesForbiddenChars_Simple("\b", @"\b");
            JavaScriptEncode_Relaxed_StillEncodesForbiddenChars_Simple("\f", @"\f");
        }

        [Theory]
        [InlineData("\"", "\\\"")]
        [InlineData("\\", @"\\")]
        [InlineData("\n", @"\n")]
        [InlineData("\t", @"\t")]
        [InlineData("\r", @"\r")]
        public void JavaScriptEncode_Relaxed_StillEncodesForbiddenChars_Simple(string input, string expected)
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            // Act
            string retVal = encoder.Encode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptEncode_Relaxed_StillEncodesForbiddenChars_Extended()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            // Act & assert - BMP chars
            for (int i = 0; i <= 0xFFFF; i++)
            {
                string input = new string((char)i, 1);
                string expected;
                if (IsSurrogateCodePoint(i))
                {
                    expected = "\\uFFFD"; // unpaired surrogate -> Unicode replacement char
                }
                else
                {
                    if (input == "\b")
                    {
                        expected = @"\b";
                    }
                    else if (input == "\t")
                    {
                        expected = @"\t";
                    }
                    else if (input == "\n")
                    {
                        expected = @"\n";
                    }
                    else if (input == "\f")
                    {
                        expected = @"\f";
                    }
                    else if (input == "\r")
                    {
                        expected = @"\r";
                    }
                    else if (input == "\\")
                    {
                        expected = @"\\";
                    }
                    else if (input == "\"")
                    {
                        expected = "\\\"";
                    }
                    else
                    {
                        bool mustEncode = false;

                        if (i <= 0x001F || (0x007F <= i && i <= 0x9F))
                        {
                            mustEncode = true; // control char
                        }
                        else if (!UnicodeTestHelpers.IsCharacterDefined((char)i))
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

                string retVal = encoder.Encode(input);
                Assert.Equal(expected, retVal);
            }

            // Act & assert - astral chars
            for (int i = 0x10000; i <= 0x10FFFF; i++)
            {
                string input = char.ConvertFromUtf32(i);
                string expected = string.Format(CultureInfo.InvariantCulture, @"\u{0:X4}\u{1:X4}", (uint)input[0], (uint)input[1]);
                string retVal = encoder.Encode(input);
                Assert.Equal(expected, retVal);
            }
        }

        [Fact]
        public void JavaScriptEncode_BadSurrogates_ReturnsUnicodeReplacementChar_Relaxed()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping; // allow all codepoints

            // "a<unpaired leading>b<unpaired trailing>c<trailing before leading>d<unpaired trailing><valid>e<high at end of string>"
            const string input = "a\uD800b\uDFFFc\uDFFF\uD800d\uDFFF\uD800\uDFFFe\uD800";
            const string expected = "a\\uFFFDb\\uFFFDc\\uFFFD\\uFFFDd\\uFFFD\\uD800\\uDFFFe\\uFFFD"; // 'D800' 'DFFF' was preserved since it's valid

            // Act
            string retVal = encoder.Encode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptEncode_EmptyStringInput_ReturnsEmptyString_Relaxed()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            // Act & assert
            Assert.Equal("", encoder.Encode(""));
        }

        [Fact]
        public void JavaScriptEncode_InputDoesNotRequireEncoding_ReturnsOriginalStringInstance_Relaxed()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            string input = "Hello, there!";

            // Act & assert
            Assert.Same(input, encoder.Encode(input));
        }

        [Fact]
        public void JavaScriptEncode_NullInput_Throws_Relaxed()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            Assert.Throws<ArgumentNullException>(() => { encoder.Encode(null); });
        }

        [Fact]
        public void JavaScriptEncode_WithCharsRequiringEncodingAtBeginning_Relaxed()
        {
            Assert.Equal(@"\\Hello, there!", JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode("\\Hello, there!"));
        }

        [Fact]
        public void JavaScriptEncode_WithCharsRequiringEncodingAtEnd_Relaxed()
        {
            Assert.Equal(@"Hello, there!\\", JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode("Hello, there!\\"));
        }

        [Fact]
        public void JavaScriptEncode_WithCharsRequiringEncodingInMiddle_Relaxed()
        {
            Assert.Equal(@"Hello, \\there!", JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode("Hello, \\there!"));
        }

        [Fact]
        public void JavaScriptEncode_WithCharsRequiringEncodingInterspersed_Relaxed()
        {
            Assert.Equal("Hello, \\\\there\\\"!", JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode("Hello, \\there\"!"));
        }

        [Fact]
        public void JavaScriptEncode_CharArray_Relaxed()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            using var output = new StringWriter();

            // Act
            encoder.Encode(output, "Hello\\world!".ToCharArray(), 3, 5);

            // Assert
            Assert.Equal(@"lo\\wo", output.ToString());
        }

        [Fact]
        public void JavaScriptEncode_StringSubstring_Relaxed()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            using var output = new StringWriter();

            // Act
            encoder.Encode(output, "Hello\\world!", 3, 5);

            // Assert
            Assert.Equal(@"lo\\wo", output.ToString());
        }

        [Theory]
        [InlineData("\"", "\\\"")]
        [InlineData("'", "'")]
        public void JavaScriptEncode_Quotes_Relaxed(string input, string expected)
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            // Act
            string retVal = encoder.Encode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Theory]
        [InlineData("hello+world", "hello+world")]
        [InlineData("hello<world>", "hello<world>")]
        [InlineData("hello&world", "hello&world")]
        public void JavaScriptEncode_DoesOutputHtmlSensitiveCharacters_Relaxed(string input, string expected)
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            // Act
            string retVal = encoder.Encode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptEncode_AboveAscii_Relaxed()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            // Act & assert
            for (int i = 0x128; i <= 0xFFFF; i++)
            {
                if (IsSurrogateCodePoint(i))
                {
                    continue; // surrogates don't matter here
                }

                UnicodeCategory category = char.GetUnicodeCategory((char)i);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    continue; // skip undefined characters like U+0378, or spacing characters like U+2028
                }

                string javaScriptEncoded = encoder.Encode(char.ConvertFromUtf32(i));
                Assert.True(char.ConvertFromUtf32(i) == javaScriptEncoded, i.ToString());
            }
        }

        [Fact]
        public void JavaScriptEncode_ControlCharacters_Relaxed()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            // Act & assert
            for (int i = 0; i <= 0x1F; i++)
            {
                // Skip characters that are escaped using '\\' since they are covered in other tests.
                if (i == '\b' || i == '\f' || i == '\n' || i == '\r' || i == '\t')
                {
                    continue;
                }
                string javaScriptEncoded = encoder.Encode(char.ConvertFromUtf32(i));
                string expected = string.Format("\\u00{0:X2}", i);
                Assert.Equal(expected, javaScriptEncoded);
            }
        }
    }
}
