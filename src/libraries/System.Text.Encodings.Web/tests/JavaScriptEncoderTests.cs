// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Unicode;
using Xunit;

namespace System.Text.Encodings.Web.Tests
{
    public partial class JavaScriptEncoderTests
    {
        [Fact]
        public void Factory_NullArgs_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => JavaScriptEncoder.Create((TextEncoderSettings)null));
            Assert.Throws<ArgumentNullException>(() => JavaScriptEncoder.Create((UnicodeRange)null));
        }

        [Theory]
        [MemberData(nameof(EscapingTestData))]
        [MemberData(nameof(EscapingTestData_NonAscii))]
        public unsafe void FindFirstCharacterToEncode(char replacementChar, JavaScriptEncoder encoder, bool requiresEscaping)
        {
            Assert.Equal(-1, encoder.FindFirstCharacterToEncodeUtf8(default));
            fixed (char* ptr = string.Empty)
            {
                Assert.Equal(-1, encoder.FindFirstCharacterToEncode(ptr, 0));
            }

            var random = new Random(42);
            for (int dataLength = 0; dataLength < 50; dataLength++)
            {
                char[] str = new char[dataLength];
                for (int i = 0; i < dataLength; i++)
                {
                    str[i] = (char)random.Next(97, 123);
                }
                string baseStr = new string(str);
                byte[] sourceUtf8 = Encoding.UTF8.GetBytes(baseStr);

                Assert.Equal(-1, encoder.FindFirstCharacterToEncodeUtf8(sourceUtf8));
                fixed (char* ptr = baseStr)
                {
                    Assert.Equal(-1, encoder.FindFirstCharacterToEncode(ptr, baseStr.Length));
                }

                for (int i = 0; i < dataLength; i++)
                {
                    char[] changed = baseStr.ToCharArray();
                    changed[i] = replacementChar;
                    string source = new string(changed);
                    sourceUtf8 = Encoding.UTF8.GetBytes(source);

                    Assert.Equal(requiresEscaping ? i : -1, encoder.FindFirstCharacterToEncodeUtf8(sourceUtf8));
                    fixed (char* ptr = source)
                    {
                        Assert.Equal(requiresEscaping ? i : -1, encoder.FindFirstCharacterToEncode(ptr, source.Length));
                    }
                }

                if (dataLength != 0)
                {
                    char[] changed = baseStr.ToCharArray();
                    changed.AsSpan().Fill(replacementChar);
                    string source = new string(changed);
                    sourceUtf8 = Encoding.UTF8.GetBytes(source);

                    Assert.Equal(requiresEscaping ? 0 : -1, encoder.FindFirstCharacterToEncodeUtf8(sourceUtf8));
                    fixed (char* ptr = source)
                    {
                        Assert.Equal(requiresEscaping ? 0 : -1, encoder.FindFirstCharacterToEncode(ptr, source.Length));
                    }
                }
            }
        }

        public static IEnumerable<object[]> EscapingTestData
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { 'a', JavaScriptEncoder.Default, false },              // ASCII not escaped
                    new object[] { '\u001F', JavaScriptEncoder.Default, true },          // control character within single byte range
                    new object[] { '\u007F', JavaScriptEncoder.Default, true },          // control character, sbyte.MaxValue
                    new object[] { '\u2000', JavaScriptEncoder.Default, true },          // space character outside single byte range
                    new object[] { '\u00A2', JavaScriptEncoder.Default, true },          // non-ASCII but < 255
                    new object[] { '\uA686', JavaScriptEncoder.Default, true },          // non-ASCII above short.MaxValue
                    new object[] { '\u6C49', JavaScriptEncoder.Default, true },          // non-ASCII from chinese alphabet - multibyte
                    new object[] { '"', JavaScriptEncoder.Default, true },               // ASCII but must always be escaped in JSON
                    new object[] { '\\', JavaScriptEncoder.Default, true },              // ASCII but must always be escaped in JSON
                    new object[] { '<', JavaScriptEncoder.Default, true },               // ASCII but escaped by default
                    new object[] { '>', JavaScriptEncoder.Default, true },               // ASCII but escaped by default
                    new object[] { '&', JavaScriptEncoder.Default, true },               // ASCII but escaped by default
                    new object[] { '`', JavaScriptEncoder.Default, true },               // ASCII but escaped by default
                    new object[] { '\'', JavaScriptEncoder.Default, true },              // ASCII but escaped by default
                    new object[] { '+', JavaScriptEncoder.Default, true },               // ASCII but escaped by default
                    new object[] { '\uFFFD', JavaScriptEncoder.Default, true },          // Default replacement character

                    new object[] { 'a', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), false },
                    new object[] { '\u001F', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '\u007F', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '\u2000', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '\u00A2', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '\uA686', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '\u6C49', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '"', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '\\', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '<', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '>', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '&', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '`', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '\'', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '+', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                    new object[] { '\uFFFD', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin), true },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EscapingTestData_NonAscii))]
        public unsafe void FindFirstCharacterToEncode_NonAscii(char replacementChar, JavaScriptEncoder encoder, bool requiresEscaping)
        {
            var random = new Random(42);
            for (int dataLength = 1; dataLength < 50; dataLength++)
            {
                char[] str = new char[dataLength];
                for (int i = 0; i < dataLength; i++)
                {
                    str[i] = (char)random.Next(0x2E9B, 0x2EF4); // CJK Radicals Supplement characters
                }
                string baseStr = new string(str);
                byte[] sourceUtf8 = Encoding.UTF8.GetBytes(baseStr);

                Assert.Equal(-1, encoder.FindFirstCharacterToEncodeUtf8(sourceUtf8));
                fixed (char* ptr = baseStr)
                {
                    Assert.Equal(-1, encoder.FindFirstCharacterToEncode(ptr, baseStr.Length));
                }

                for (int i = 0; i < dataLength; i++)
                {
                    string source = baseStr.Insert(i, new string(replacementChar, 1));
                    sourceUtf8 = Encoding.UTF8.GetBytes(source);

                    Assert.Equal(requiresEscaping ? i * 3 : -1, encoder.FindFirstCharacterToEncodeUtf8(sourceUtf8)); // Each CJK character expands to 3 utf-8 bytes.
                    fixed (char* ptr = source)
                    {
                        Assert.Equal(requiresEscaping ? i : -1, encoder.FindFirstCharacterToEncode(ptr, source.Length));
                    }
                }
            }
        }

        public static IEnumerable<object[]> EscapingTestData_NonAscii
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { 'a', JavaScriptEncoder.Create(UnicodeRanges.All), false },
                    new object[] { '\u001F', JavaScriptEncoder.Create(UnicodeRanges.All), true },
                    new object[] { '\u007F', JavaScriptEncoder.Create(UnicodeRanges.All), true },
                    new object[] { '\u2000', JavaScriptEncoder.Create(UnicodeRanges.All), true },
                    new object[] { '\u00A2', JavaScriptEncoder.Create(UnicodeRanges.All), false },
                    new object[] { '\uA686', JavaScriptEncoder.Create(UnicodeRanges.All), false },
                    new object[] { '\u6C49', JavaScriptEncoder.Create(UnicodeRanges.All), false },
                    new object[] { '"', JavaScriptEncoder.Create(UnicodeRanges.All), true },
                    new object[] { '\\', JavaScriptEncoder.Create(UnicodeRanges.All), true },
                    new object[] { '<', JavaScriptEncoder.Create(UnicodeRanges.All), true },
                    new object[] { '>', JavaScriptEncoder.Create(UnicodeRanges.All), true },
                    new object[] { '&', JavaScriptEncoder.Create(UnicodeRanges.All), true },
                    new object[] { '`', JavaScriptEncoder.Create(UnicodeRanges.All), true },
                    new object[] { '\'', JavaScriptEncoder.Create(UnicodeRanges.All), true },
                    new object[] { '+', JavaScriptEncoder.Create(UnicodeRanges.All), true },
                    new object[] { '\uFFFD', JavaScriptEncoder.Create(UnicodeRanges.All), false },

                    new object[] { 'a', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, false },
                    new object[] { '\u001F', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, true },
                    new object[] { '\u007F', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, true },
                    new object[] { '\u2000', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, true },
                    new object[] { '\u00A2', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, false },
                    new object[] { '\uA686', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, false },
                    new object[] { '\u6C49', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, false },
                    new object[] { '"', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, true },
                    new object[] { '\\', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, true },
                    new object[] { '<', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, false },
                    new object[] { '>', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, false },
                    new object[] { '&', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, false },
                    new object[] { '`', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, false },
                    new object[] { '\'', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, false },
                    new object[] { '+', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, false },
                    new object[] { '\uFFFD', JavaScriptEncoder.UnsafeRelaxedJsonEscaping, false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(JavaScriptEncoders))]
        public unsafe void EscapingTestWhileWritingSurrogate(JavaScriptEncoder encoder)
        {
            char highSurrogate = '\uD801';
            char lowSurrogate = '\uDC37';
            var random = new Random(42);
            for (int dataLength = 2; dataLength < 50; dataLength++)
            {
                char[] str = new char[dataLength];
                for (int i = 0; i < dataLength; i++)
                {
                    str[i] = (char)random.Next(97, 123);
                }
                string baseStr = new string(str);
                byte[] sourceUtf8 = Encoding.UTF8.GetBytes(baseStr);

                Assert.Equal(-1, encoder.FindFirstCharacterToEncodeUtf8(sourceUtf8));
                fixed (char* ptr = baseStr)
                {
                    Assert.Equal(-1, encoder.FindFirstCharacterToEncode(ptr, baseStr.Length));
                }

                for (int i = 0; i < dataLength - 1; i++)
                {
                    char[] changed = baseStr.ToCharArray();
                    changed[i] = highSurrogate;
                    changed[i + 1] = lowSurrogate;
                    string newStr = new string(changed);
                    sourceUtf8 = Encoding.UTF8.GetBytes(newStr);

                    Assert.Equal(i, encoder.FindFirstCharacterToEncodeUtf8(sourceUtf8));
                    fixed (char* ptr = newStr)
                    {
                        Assert.Equal(i, encoder.FindFirstCharacterToEncode(ptr, newStr.Length));
                    }
                }

                {
                    char[] changed = baseStr.ToCharArray();

                    for (int i = 0; i < changed.Length - 1; i += 2)
                    {
                        changed[i] = highSurrogate;
                        changed[i + 1] = lowSurrogate;
                    }

                    string newStr = new string(changed);
                    sourceUtf8 = Encoding.UTF8.GetBytes(newStr);

                    Assert.Equal(0, encoder.FindFirstCharacterToEncodeUtf8(sourceUtf8));
                    fixed (char* ptr = newStr)
                    {
                        Assert.Equal(0, encoder.FindFirstCharacterToEncode(ptr, newStr.Length));
                    }
                }
            }
        }

        public static IEnumerable<object[]> JavaScriptEncoders
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { JavaScriptEncoder.Default },
                    new object[] { JavaScriptEncoder.Create(UnicodeRanges.BasicLatin) },
                    new object[] { JavaScriptEncoder.Create(UnicodeRanges.All) },
                    new object[] { JavaScriptEncoder.UnsafeRelaxedJsonEscaping },
                };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidEscapingTestData))]
        public unsafe void InvalidFindFirstCharacterToEncode(char replacementChar, JavaScriptEncoder encoder)
        {
            var random = new Random(42);
            for (int dataLength = 0; dataLength < 47; dataLength++)
            {
                char[] str = new char[dataLength];
                for (int i = 0; i < dataLength; i++)
                {
                    str[i] = (char)random.Next(97, 123);
                }
                string baseStr = new string(str);
                byte[] baseStrUtf8 = Encoding.UTF8.GetBytes(baseStr);

                for (int i = 0; i < dataLength; i++)
                {
                    char[] changed = baseStr.ToCharArray();
                    changed[i] = replacementChar;
                    string source = new string(changed);
                    byte[] sourceUtf8 = new byte[baseStrUtf8.Length];
                    baseStrUtf8.AsSpan().CopyTo(sourceUtf8);
                    sourceUtf8[i] = 0xC3;   // Invalid, first byte of a 2-byte utf-8 character

                    Assert.Equal(i, encoder.FindFirstCharacterToEncodeUtf8(sourceUtf8));
                    fixed (char* ptr = source)
                    {
                        Assert.Equal(i, encoder.FindFirstCharacterToEncode(ptr, source.Length));
                    }
                }
            }
        }

        public static IEnumerable<object[]> InvalidEscapingTestData
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { '\uD801', JavaScriptEncoder.Default },         // Invalid, high surrogate alone
                    new object[] { '\uDC01', JavaScriptEncoder.Default },         // Invalid, low surrogate alone

                    new object[] { '\uD801', JavaScriptEncoder.UnsafeRelaxedJsonEscaping },
                    new object[] { '\uDC01', JavaScriptEncoder.UnsafeRelaxedJsonEscaping },

                    new object[] { '\uD801', JavaScriptEncoder.Create(UnicodeRanges.All) },
                    new object[] { '\uDC01', JavaScriptEncoder.Create(UnicodeRanges.All) },

                    new object[] { '\uD801', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin) },
                    new object[] { '\uDC01', JavaScriptEncoder.Create(UnicodeRanges.BasicLatin) },
                };
            }
        }

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
        public void JavaScriptEncoder_NonEmptySource_EmptyDest_Throws()
        {
            OperationStatus status = System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(
                "\U0001f4a9".AsSpan(), destination: null, out int _, out int _, isFinalBlock: true);

            Assert.Equal(OperationStatus.DestinationTooSmall, status);
        }

        [Fact]
        public void JavaScriptEncoder_EmptySource_EmptyDest()
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

            JavaScriptEncoder encoder = JavaScriptEncoder.Create(filter);

            // Act & assert
            Assert.Equal("a", encoder.Encode("a"));
            Assert.Equal("b", encoder.Encode("b"));
            Assert.Equal(@"\u0063", encoder.Encode("c"));
            Assert.Equal("d", encoder.Encode("d"));
            Assert.Equal(@"\u0000", encoder.Encode("\0")); // we still always encode control chars
            Assert.Equal(@"\u0026", encoder.Encode("&")); // we still always encode HTML-special chars
            Assert.Equal(@"\uFFFF", encoder.Encode("\uFFFF")); // we still always encode non-chars and other forbidden chars
        }

        [Fact]
        public void Ctor_WithUnicodeRanges()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.Latin1Supplement, UnicodeRanges.MiscellaneousSymbols);

            // Act & assert
            Assert.Equal(@"\u0061", encoder.Encode("a"));
            Assert.Equal("\u00E9", encoder.Encode("\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */));
            Assert.Equal("\u2601", encoder.Encode("\u2601" /* CLOUD */));
        }

        [Fact]
        public void DefaultFactory_IsBasicLatin()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Default;

            // Act & assert
            Assert.Equal("a", encoder.Encode("a"));
            Assert.Equal(@"\u00E9", encoder.Encode("\u00E9" /* LATIN SMALL LETTER E WITH ACUTE */));
            Assert.Equal(@"\u2601", encoder.Encode("\u2601" /* CLOUD */));
        }

        [Fact]
        public void Default_EquivalentToBasicLatin()
        {
            // Arrange
            JavaScriptEncoder controlEncoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin);
            JavaScriptEncoder testEncoder = JavaScriptEncoder.Default;

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
        public void JavaScriptEncode_AllRangesAllowed_StillEncodesForbiddenChars_Simple_Escaping()
        {
            // The following two calls could be simply InlineData to the Theory below
            // Unfortunately, the xUnit logger fails to escape the inputs when logging the test results,
            // and so the suite fails despite all tests passing.
            // TODO: I will try to fix it in xUnit, but for now this is a workaround to enable these tests.
            JavaScriptEncode_AllRangesAllowed_StillEncodesForbiddenChars_Simple("\b", @"\b");
            JavaScriptEncode_AllRangesAllowed_StillEncodesForbiddenChars_Simple("\f", @"\f");
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
        public void JavaScriptEncode_AllRangesAllowed_StillEncodesForbiddenChars_Simple(string input, string expected)
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.All);

            // Act
            string retVal = encoder.Encode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptEncode_AllRangesAllowed_StillEncodesForbiddenChars_Extended()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.All);

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
        public void JavaScriptEncode_NoRangesAllowed_EmitsShortFormForCertainCodePoints()
        {
            // This test ensures that when we're encoding, we always emit the "\uXXXX" form of the
            // code point except for very specific code points where we allow a shorter representation.

            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.None); // allow no codepoints

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
            string retVal = encoder.Encode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptEncode_BadSurrogates_ReturnsUnicodeReplacementChar()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.All); // allow all codepoints

            // "a<unpaired leading>b<unpaired trailing>c<trailing before leading>d<unpaired trailing><valid>e<high at end of string>"
            const string Input = "a\uD800b\uDFFFc\uDFFF\uD800d\uDFFF\uD800\uDFFFe\uD800";
            const string Expected = "a\\uFFFDb\\uFFFDc\\uFFFD\\uFFFDd\\uFFFD\\uD800\\uDFFFe\\uFFFD"; // 'D800' 'DFFF' was preserved since it's valid

            // Act
            string retVal = encoder.Encode(Input);

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
            const string Expected = "a\\uFFFDb\\uFFFDc\\uFFFD\\uFFFDd\\uFFFD\\uD800\\uDFFFe\\uFFFD"; // 'D800' 'DFFF' was preserved since it's valid

            // String-based Encode()
            string retVal = encoder.Encode(Input);
            Assert.Equal(Expected, retVal);

            // OperationStatus-based Encode()
            Span<char> destination = new char[Expected.Length + 1];
            OperationStatus status = encoder.Encode(Input.AsSpan(), destination, out int charsConsumed, out int charsWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(Input.Length, charsConsumed);
            Assert.Equal(Expected.Length, charsWritten);
            Assert.Equal(Expected, destination.Slice(0, charsWritten).ToString());
        }

        [Fact]
        public void JavaScriptEncoder_UnpairedSurrogatesReplaced()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.All); // allow all codepoints

            // "a<unpaired leading low><unpaired leading high><unpaired leading high><replacement>"
            const string Input = "a\uDFFF\uD800\uD800\uFFFD";
            const string Expected = "a\\uFFFD\\uFFFD\\uFFFD\uFFFD"; // final U+FFFD left unchanged since it's well-formed

            // String-based Encode()
            string retVal = encoder.Encode(Input);
            Assert.Equal(Expected, retVal);

            // OperationStatus-based Encode()
            OperationStatus status;
            Span<char> destination = new char[Expected.Length + 1];
            status = encoder.Encode(Input.AsSpan(), destination, out int charsConsumed, out int charsWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(Input.Length, charsConsumed);
            Assert.Equal(Expected.Length, charsWritten);
            Assert.Equal(Expected, destination.Slice(0, charsWritten).ToString());
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
        public void JavaScriptEncode_EmptyStringInput_ReturnsEmptyString()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Default;

            // Act & assert
            Assert.Equal("", encoder.Encode(""));
        }

        [Fact]
        public void JavaScriptEncode_InputDoesNotRequireEncoding_ReturnsOriginalStringInstance()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Default;
            string input = "Hello, there!";

            // Act & assert
            Assert.Same(input, encoder.Encode(input));
        }

        [Fact]
        public void JavaScriptEncode_NullInput_Throws()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Default;

            Assert.Throws<ArgumentNullException>(() => { encoder.Encode(null); });
        }

        [Fact]
        public void JavaScriptEncode_WithCharsRequiringEncodingAtBeginning()
        {
            Assert.Equal(@"\u0026Hello, there!", JavaScriptEncoder.Default.Encode("&Hello, there!"));
        }

        [Fact]
        public void JavaScriptEncode_WithCharsRequiringEncodingAtEnd()
        {
            Assert.Equal(@"Hello, there!\u0026", JavaScriptEncoder.Default.Encode("Hello, there!&"));
        }

        [Fact]
        public void JavaScriptEncode_WithCharsRequiringEncodingInMiddle()
        {
            Assert.Equal(@"Hello, \u0026there!", JavaScriptEncoder.Default.Encode("Hello, &there!"));
        }

        [Fact]
        public void JavaScriptEncode_WithCharsRequiringEncodingInterspersed()
        {
            Assert.Equal(@"Hello, \u003Cthere\u003E!", JavaScriptEncoder.Default.Encode("Hello, <there>!"));
        }

        [Fact]
        public void JavaScriptEncode_CharArray()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Default;
            var output = new StringWriter();

            // Act
            encoder.Encode(output, "Hello+world!".ToCharArray(), 3, 5);

            // Assert
            Assert.Equal(@"lo\u002Bwo", output.ToString());
        }

        [Fact]
        public void JavaScriptEncode_StringSubstring()
        {
            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Default;
            var output = new StringWriter();

            // Act
            encoder.Encode(output, "Hello+world!", 3, 5);

            // Assert
            Assert.Equal(@"lo\u002Bwo", output.ToString());
        }

        [Theory]
        [InlineData("\"", @"\u0022")]
        [InlineData("'", @"\u0027")]
        public void JavaScriptEncode_Quotes(string input, string expected)
        {
            // Per the design document, we provide additional defense-in-depth
            // against breaking out of HTML attributes by having the encoders
            // never emit the ' or " characters. This means that we want to
            // \u-escape these characters instead of using \' and \".

            // Arrange
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.All);

            // Act
            string retVal = encoder.Encode(input);

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void JavaScriptEncode_DoesNotOutputHtmlSensitiveCharacters()
        {
            // Per the design document, we provide additional defense-in-depth
            // by never emitting HTML-sensitive characters unescaped.

            // Arrange
            JavaScriptEncoder javaScriptEncoder = JavaScriptEncoder.Create(UnicodeRanges.All);
            HtmlEncoder htmlEncoder = HtmlEncoder.Create(UnicodeRanges.All);

            // Act & assert
            for (int i = 0; i <= 0x10FFFF; i++)
            {
                if (IsSurrogateCodePoint(i))
                {
                    continue; // surrogates don't matter here
                }

                string javaScriptEncoded = javaScriptEncoder.Encode(char.ConvertFromUtf32(i));
                string thenHtmlEncoded = htmlEncoder.Encode(javaScriptEncoded);
                Assert.Equal(javaScriptEncoded, thenHtmlEncoded); // should have contained no HTML-sensitive characters
            }
        }

        private static bool IsSurrogateCodePoint(int codePoint)
        {
            return (0xD800 <= codePoint && codePoint <= 0xDFFF);
        }
    }
}
