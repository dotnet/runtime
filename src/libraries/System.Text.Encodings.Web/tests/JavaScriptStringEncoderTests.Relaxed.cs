// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Xunit;

namespace Microsoft.Framework.WebEncoders
{
    public partial class JavaScriptStringEncoderTests
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
            JavaScriptStringEncoder controlEncoder = new JavaScriptStringEncoder(UnicodeRanges.All);
            JavaScriptStringEncoder testEncoder = JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping;

            // Act & assert
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (i == '"' || i == '&' || i == '<' || i == '>' || i == '+' || i == '\'' || i == '`')
                {
                    string input = new string((char)i, 1);
                    Assert.NotEqual(controlEncoder.JavaScriptStringEncode(input), testEncoder.JavaScriptStringEncode(input));
                    continue;
                }

                if (!IsSurrogateCodePoint(i))
                {
                    string input = new string((char)i, 1);
                    Assert.Equal(controlEncoder.JavaScriptStringEncode(input), testEncoder.JavaScriptStringEncode(input));
                }
            }
        }

        [Fact]
        public void JavaScriptStringEncode_Relaxed_StillEncodesForbiddenChars_Simple_Escaping()
        {
            // The following two calls could be simply InlineData to the Theory below
            // Unfortunately, the xUnit logger fails to escape the inputs when logging the test results,
            // and so the suite fails despite all tests passing. 
            // TODO: I will try to fix it in xUnit, but for now this is a workaround to enable these tests.
            JavaScriptStringEncode_Relaxed_StillEncodesForbiddenChars_Simple("\b", @"\b");
            JavaScriptStringEncode_Relaxed_StillEncodesForbiddenChars_Simple("\f", @"\f");
        }

        [Theory]
        [InlineData("\"", "\\\"")]
        [InlineData("\\", @"\\")]
        [InlineData("\n", @"\n")]
        [InlineData("\t", @"\t")]
        [InlineData("\r", @"\r")]
        public void JavaScriptStringEncode_Relaxed_StillEncodesForbiddenChars_Simple(string input, string expected)
        {
            // Arrange
            JavaScriptStringEncoder encoder = JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping;

            // Act
            string retVal = encoder.JavaScriptStringEncode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptStringEncode_Relaxed_StillEncodesForbiddenChars_Extended()
        {
            // Arrange
            JavaScriptStringEncoder encoder = JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping;

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
        public void JavaScriptStringEncode_BadSurrogates_ReturnsUnicodeReplacementChar_Relaxed()
        {
            // Arrange
            JavaScriptStringEncoder encoder = JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping; // allow all codepoints

            // "a<unpaired leading>b<unpaired trailing>c<trailing before leading>d<unpaired trailing><valid>e<high at end of string>"
            const string input = "a\uD800b\uDFFFc\uDFFF\uD800d\uDFFF\uD800\uDFFFe\uD800";
            const string expected = "a\uFFFDb\uFFFDc\uFFFD\uFFFDd\uFFFD\\uD800\\uDFFFe\uFFFD"; // 'D800' 'DFFF' was preserved since it's valid

            // Act
            string retVal = encoder.JavaScriptStringEncode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptStringEncode_EmptyStringInput_ReturnsEmptyString_Relaxed()
        {
            // Arrange
            JavaScriptStringEncoder encoder = JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping;

            // Act & assert
            Assert.Equal("", encoder.JavaScriptStringEncode(""));
        }

        [Fact]
        public void JavaScriptStringEncode_InputDoesNotRequireEncoding_ReturnsOriginalStringInstance_Relaxed()
        {
            // Arrange
            JavaScriptStringEncoder encoder = JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping;
            string input = "Hello, there!";

            // Act & assert
            Assert.Same(input, encoder.JavaScriptStringEncode(input));
        }

        [Fact]
        public void JavaScriptStringEncode_NullInput_Throws_Relaxed()
        {
            // Arrange
            JavaScriptStringEncoder encoder = JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping;

            Assert.Throws<ArgumentNullException>(() => { encoder.JavaScriptStringEncode(null); });
        }

        [Fact]
        public void JavaScriptStringEncode_WithCharsRequiringEncodingAtBeginning_Relaxed()
        {
            Assert.Equal(@"\\Hello, there!", JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping.JavaScriptStringEncode("\\Hello, there!"));
        }

        [Fact]
        public void JavaScriptStringEncode_WithCharsRequiringEncodingAtEnd_Relaxed()
        {
            Assert.Equal(@"Hello, there!\\", JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping.JavaScriptStringEncode("Hello, there!\\"));
        }

        [Fact]
        public void JavaScriptStringEncode_WithCharsRequiringEncodingInMiddle_Relaxed()
        {
            Assert.Equal(@"Hello, \\there!", JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping.JavaScriptStringEncode("Hello, \\there!"));
        }

        [Fact]
        public void JavaScriptStringEncode_WithCharsRequiringEncodingInterspersed_Relaxed()
        {
            Assert.Equal("Hello, \\\\there\\\"!", JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping.JavaScriptStringEncode("Hello, \\there\"!"));
        }

        [Fact]
        public void JavaScriptStringEncode_CharArray_Relaxed()
        {
            // Arrange
            JavaScriptStringEncoder encoder = JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping;

            using var output = new StringWriter();

            // Act
            encoder.JavaScriptStringEncode("Hello\\world!".ToCharArray(), 3, 5, output);

            // Assert
            Assert.Equal(@"lo\\wo", output.ToString());
        }

        [Fact]
        public void JavaScriptStringEncode_StringSubstring_Relaxed()
        {
            // Arrange
            JavaScriptStringEncoder encoder = JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping;

            using var output = new StringWriter();

            // Act
            encoder.JavaScriptStringEncode("Hello\\world!", 3, 5, output);

            // Assert
            Assert.Equal(@"lo\\wo", output.ToString());
        }

        [Theory]
        [InlineData("\"", "\\\"")]
        [InlineData("'", "'")]
        public void JavaScriptStringEncode_Quotes_Relaxed(string input, string expected)
        {
            // Arrange
            JavaScriptStringEncoder encoder = JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping;

            // Act
            string retVal = encoder.JavaScriptStringEncode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Theory]
        [InlineData("hello+world", "hello+world")]
        [InlineData("hello<world>", "hello<world>")]
        [InlineData("hello&world", "hello&world")]
        public void JavaScriptStringEncode_DoesOutputHtmlSensitiveCharacters_Relaxed(string input, string expected)
        {
            // Arrange
            JavaScriptStringEncoder encoder = JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping;

            // Act
            string retVal = encoder.JavaScriptStringEncode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptStringEncode_AboveAscii_Relaxed()
        {
            // Arrange
            JavaScriptStringEncoder encoder = JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping;

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

                string javaScriptStringEncoded = encoder.JavaScriptStringEncode(char.ConvertFromUtf32(i));
                Assert.True(char.ConvertFromUtf32(i) == javaScriptStringEncoded, i.ToString());
            }
        }

        [Fact]
        public void JavaScriptStringEncode_ControlCharacters_Relaxed()
        {
            // Arrange
            JavaScriptStringEncoder encoder = JavaScriptStringEncoder.UnsafeRelaxedJsonEscaping;

            // Act & assert
            for (int i = 0; i <= 0x1F; i++)
            {
                // Skip characters that are escaped using '\\' since they are covered in other tests.
                if (i == '\b' || i == '\f' || i == '\n' || i == '\r' || i == '\t')
                {
                    continue;
                }
                string javaScriptStringEncoded = encoder.JavaScriptStringEncode(char.ConvertFromUtf32(i));
                string expected = string.Format("\\u00{0:X2}", i);
                Assert.Equal(expected, javaScriptStringEncoded);
            }
        }
    }
}
