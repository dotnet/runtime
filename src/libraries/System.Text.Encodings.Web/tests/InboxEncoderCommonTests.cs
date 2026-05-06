// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Text.Encodings.Web.Tests
{
    public class HtmlEncoderDefaultCommonTests : InboxEncoderCommonTestBase
    {
        public HtmlEncoderDefaultCommonTests()
            : base(HtmlEncoder.Default, allowedChar: 'a', disallowedChar: '&')
        {
        }

        private protected override string GetExpectedEscapedRepresentation(Rune value)
        {
            switch (value.Value)
            {
                case '<': return "&lt;";
                case '>': return "&gt;";
                case '&': return "&amp;";
                case '\"': return "&quot;";
                default:
                    return FormattableString.Invariant($"&#x{(uint)value.Value:X};");
            }
        }

        [Fact]
        public void EncodeUtf16_Battery()
        {
            string[] inputs = new string[]
            {
                "\n",
                "<",
                "a",
                "\u0234", // U+0234 LATIN SMALL LETTER L WITH CURL
                "\ud800", // standalone high surrogate
                "\U0001F415", // U+1F415 DOG
                "\udfff", // standalone low surrogate
                "\uFFFF", // end of BMP range
                "\U00010000", // beginning of supplementary range
                "\U0010FFFF", // end of supplementary range
            };

            // expected outputs correspond to the escaped form of the inputs above
            string[] expectedOutputs = new string[]
            {
                "&#xA;",
                "&lt;",
                "a",
                "&#x234;",
                "&#xFFFD;", // replaced standalone high surrogate with replacement char
                "&#x1F415;",
                "&#xFFFD;", // replaced standalone low surrogate with replacement char
                "&#xFFFF;",
                "&#x10000;",
                "&#x10FFFF;",
            };

            _RunEncodeUtf16_Battery(inputs, expectedOutputs);
        }

        [Fact]
        public void EncodeUtf8_Battery()
        {
            byte[][] inputs = new byte[][]
            {
                new byte[] { (byte)'\n' },
                new byte[] { (byte)'<' },
                new byte[] { (byte)'a' },
                new byte[] { 0xC8, 0xB4 }, // U+0234 LATIN SMALL LETTER L WITH CURL
                new byte[] { 0xFF }, // invalid byte
                new byte[] { 0xF0, 0x9F, 0x90, 0x95 }, // U+1F415 DOG
                new byte[] { 0x80 }, // standalone continuation character
                new byte[] { 0xC2 }, // standalone multi-byte sequence marker
                new byte[] { 0xEF, 0xBF, 0xBF }, // end of BMP range
                new byte[] { 0xF0, 0x90, 0x80, 0x80 }, // beginning of supplementary range
                new byte[] { 0xF4, 0x8F, 0xBF, 0xBF }, // end of supplementary range
            };

            // expected outputs correspond to the escaped form of the inputs above
            string[] expectedOutputs = new string[]
            {
                "&#xA;",
                "&lt;",
                "a",
                "&#x234;",
                "&#xFFFD;", // replaced invalid byte
                "&#x1F415;",
                "&#xFFFD;", // replaced standalone continuation char
                "&#xFFFD;", // replaced standalone multi-byte sequence marker
                "&#xFFFF;",
                "&#x10000;",
                "&#x10FFFF;",
            };

            _RunEncodeUtf8_Battery(inputs, expectedOutputs);
        }
    }

    public class JavaScriptEncoderDefaultCommonTests : InboxEncoderCommonTestBase
    {
        public JavaScriptEncoderDefaultCommonTests()
           : base(JavaScriptEncoder.Default, allowedChar: 'a', disallowedChar: '\"')
        {
        }

        private protected override string GetExpectedEscapedRepresentation(Rune value)
        {
            switch (value.Value)
            {
                case '\b': return "\\b";
                case '\t': return "\\t";
                case '\n': return "\\n";
                case '\f': return "\\f";
                case '\r': return "\\r";
                case '\\': return "\\\\";
                default:
                    if (value.IsBmp)
                    {
                        return FormattableString.Invariant($"\\u{(uint)value.Value:X4}");
                    }
                    else
                    {
                        Span<char> asUtf16 = stackalloc char[2];
                        bool succeeded = value.TryEncodeToUtf16(asUtf16, out int utf16CodeUnitCount);
                        Assert.True(succeeded);
                        Assert.Equal(2, utf16CodeUnitCount);
                        return FormattableString.Invariant($"\\u{(uint)asUtf16[0]:X4}\\u{(uint)asUtf16[1]:X4}");
                    }
            }
        }

        [Fact]
        public void EncodeUtf16_Battery()
        {
            string[] inputs = new string[]
            {
                "\n",
                "a",
                "\u0234", // U+0234 LATIN SMALL LETTER L WITH CURL
                "\ud800", // standalone high surrogate
                "\U0001F415", // U+1F415 DOG
                "\udfff", // standalone low surrogate
                "\uFFFF", // end of BMP range
                "\U00010000", // beginning of supplementary range
                "\U0010FFFF", // end of supplementary range
            };

            // expected outputs correspond to the escaped form of the inputs above
            string[] expectedOutputs = new string[]
            {
                "\\n",
                "a",
                "\\u0234",
                "\\uFFFD", // replaced standalone high surrogate with replacement char
                "\\uD83D\\uDC15",
                "\\uFFFD", // replaced standalone low surrogate with replacement char
                "\\uFFFF",
                "\\uD800\\uDC00",
                "\\uDBFF\\uDFFF",
            };

            _RunEncodeUtf16_Battery(inputs, expectedOutputs);
        }

        [Fact]
        public void EncodeUtf8_Battery()
        {
            byte[][] inputs = new byte[][]
            {
                new byte[] { (byte)'\n' },
                new byte[] { (byte)'a' },
                new byte[] { 0xC8, 0xB4 }, // U+0234 LATIN SMALL LETTER L WITH CURL
                new byte[] { 0xFF }, // invalid byte
                new byte[] { 0xF0, 0x9F, 0x90, 0x95 }, // U+1F415 DOG
                new byte[] { 0x80 }, // standalone continuation character
                new byte[] { 0xC2 }, // standalone multi-byte sequence marker
                new byte[] { 0xEF, 0xBF, 0xBF }, // end of BMP range
                new byte[] { 0xF0, 0x90, 0x80, 0x80 }, // beginning of supplementary range
                new byte[] { 0xF4, 0x8F, 0xBF, 0xBF }, // end of supplementary range
            };

            // expected outputs correspond to the escaped form of the inputs above
            string[] expectedOutputs = new string[]
            {
                "\\n",
                "a",
                "\\u0234",
                "\\uFFFD", // replaced invalid byte
                "\\uD83D\\uDC15",
                "\\uFFFD", // replaced standalone continuation char
                "\\uFFFD", // replaced standalone multi-byte sequence marker
                "\\uFFFF",
                "\\uD800\\uDC00",
                "\\uDBFF\\uDFFF",
            };

            _RunEncodeUtf8_Battery(inputs, expectedOutputs);
        }
    }

    public class JavaScriptEncoderRelaxedCommonTests : InboxEncoderCommonTestBase
    {
        public JavaScriptEncoderRelaxedCommonTests()
           : base(JavaScriptEncoder.UnsafeRelaxedJsonEscaping, allowedChar: 'a', disallowedChar: '\"')
        {
        }

        private protected override string GetExpectedEscapedRepresentation(Rune value)
        {
            switch (value.Value)
            {
                case '\b': return "\\b";
                case '\t': return "\\t";
                case '\n': return "\\n";
                case '\f': return "\\f";
                case '\r': return "\\r";
                case '\\': return "\\\\";
                case '\"': return "\\\"";
                default:
                    if (value.IsBmp)
                    {
                        return FormattableString.Invariant($"\\u{(uint)value.Value:X4}");
                    }
                    else
                    {
                        Span<char> asUtf16 = stackalloc char[2];
                        bool succeeded = value.TryEncodeToUtf16(asUtf16, out int utf16CodeUnitCount);
                        Assert.True(succeeded);
                        Assert.Equal(2, utf16CodeUnitCount);
                        return FormattableString.Invariant($"\\u{(uint)asUtf16[0]:X4}\\u{(uint)asUtf16[1]:X4}");
                    }
            }
        }

        [Fact]
        public void EncodeUtf16_Battery()
        {
            string[] inputs = new string[]
            {
                "\n",
                "a",
                "\u0234", // U+0234 LATIN SMALL LETTER L WITH CURL
                "\ud800", // standalone high surrogate
                "\U0001F415", // U+1F415 DOG
                "\udfff", // standalone low surrogate
                "\uFFFF", // end of BMP range
                "\U00010000", // beginning of supplementary range
                "\U0010FFFF", // end of supplementary range
            };

            // expected outputs correspond to the escaped form of the inputs above
            string[] expectedOutputs = new string[]
            {
                "\\n",
                "a",
                "\u0234", // not escaped
                "\\uFFFD", // replaced standalone high surrogate with replacement char
                "\\uD83D\\uDC15",
                "\\uFFFD", // replaced standalone low surrogate with replacement char
                "\\uFFFF",
                "\\uD800\\uDC00",
                "\\uDBFF\\uDFFF",
            };

            _RunEncodeUtf16_Battery(inputs, expectedOutputs);
        }

        [Fact]
        public void EncodeUtf8_Battery()
        {
            byte[][] inputs = new byte[][]
            {
                new byte[] { (byte)'\n' },
                new byte[] { (byte)'a' },
                new byte[] { 0xC8, 0xB4 }, // U+0234 LATIN SMALL LETTER L WITH CURL
                new byte[] { 0xFF }, // invalid byte
                new byte[] { 0xF0, 0x9F, 0x90, 0x95 }, // U+1F415 DOG
                new byte[] { 0x80 }, // standalone continuation character
                new byte[] { 0xC2 }, // standalone multi-byte sequence marker
                new byte[] { 0xEF, 0xBF, 0xBF }, // end of BMP range
                new byte[] { 0xF0, 0x90, 0x80, 0x80 }, // beginning of supplementary range
                new byte[] { 0xF4, 0x8F, 0xBF, 0xBF }, // end of supplementary range
            };

            // expected outputs correspond to the escaped form of the inputs above
            string[] expectedOutputs = new string[]
            {
                "\\n",
                "a",
                "\u0234", // not escaped
                "\\uFFFD", // replaced invalid byte
                "\\uD83D\\uDC15",
                "\\uFFFD", // replaced standalone continuation char
                "\\uFFFD", // replaced standalone multi-byte sequence marker
                "\\uFFFF",
                "\\uD800\\uDC00",
                "\\uDBFF\\uDFFF",
            };

            _RunEncodeUtf8_Battery(inputs, expectedOutputs);
        }

        [Fact]
        public void GetIndexOfFirstCharacterToEncodeUtf16_BmpExtendedValidCharsOnly()
        {
            _RunGetIndexOfFirstCharacterToEncodeUtf16_BmpExtendedValidCharsOnly('\u2663'); // U+2663 BLACK CLUB SUIT
        }

        [Fact]
        public void GetIndexOfFirstCharacterToEncodeUtf16_BmpExtendedSomeCharsNeedEscaping()
        {
            _RunGetIndexOfFirstCharacterToEncodeUtf16_BmpExtendedSomeCharsNeedEscaping('\u2663'); // U+2663 BLACK CLUB SUIT
        }

        [Fact]
        public void GetIndexOfFirstCharacterToEncodeUtf8_BmpExtendedAllValidChars()
        {
            _RunGetIndexOfFirstCharacterToEncodeUtf8_BmpExtendedAllValidChars('\u2663'); // U+2663 BLACK CLUB SUIT
        }

        [Fact]
        public void GetIndexOfFirstCharacterToEncodeUtf8_BmpExtendedSomeCharsNeedEncoding()
        {
            _RunGetIndexOfFirstCharacterToEncodeUtf8_BmpExtendedSomeCharsNeedEncoding('\u2663'); // U+2663 BLACK CLUB SUIT
        }
    }

    public class UrlEncoderDefaultCommonTests : InboxEncoderCommonTestBase
    {
        public UrlEncoderDefaultCommonTests()
          : base(UrlEncoder.Default, allowedChar: 'a', disallowedChar: '?')
        {
        }

        private protected override string GetExpectedEscapedRepresentation(Rune value)
        {
            Span<byte> asUtf8Bytes = stackalloc byte[4];
            Span<char> hexEscaped = stackalloc char[12]; // worst-case 3 output chars per input UTF-8 code unit

            bool succeeded = value.TryEncodeToUtf8(asUtf8Bytes, out int utf8CodeUnitCount);
            Assert.True(succeeded);

            for (int i = 0; i < utf8CodeUnitCount; i++)
            {
                hexEscaped[i * 3] = '%';
                HexConverter.ToCharsBuffer(asUtf8Bytes[i], hexEscaped, startingIndex: (i * 3) + 1);
            }

            return hexEscaped.Slice(0, utf8CodeUnitCount * 3).ToString();
        }

        [Fact]
        public void EncodeUtf16_Battery()
        {
            string[] inputs = new string[]
            {
                "\n",
                "%",
                "a",
                "\u0234", // U+0234 LATIN SMALL LETTER L WITH CURL
                "\ud800", // standalone high surrogate
                "\U0001F415", // U+1F415 DOG
                "\udfff", // standalone low surrogate
                "\uFFFF", // end of BMP range
                "\U00010000", // beginning of supplementary range
                "\U0010FFFF", // end of supplementary range
            };

            // expected outputs correspond to the escaped form of the inputs above
            string[] expectedOutputs = new string[]
            {
                "%0A",
                "%25",
                "a",
                "%C8%B4",
                "%EF%BF%BD", // replaced standalone high surrogate with replacement char
                "%F0%9F%90%95",
                "%EF%BF%BD", // replaced standalone low surrogate with replacement char
                "%EF%BF%BF",
                "%F0%90%80%80",
                "%F4%8F%BF%BF",
            };

            _RunEncodeUtf16_Battery(inputs, expectedOutputs);
        }

        [Fact]
        public void EncodeUtf8_Battery()
        {
            byte[][] inputs = new byte[][]
            {
                new byte[] { (byte)'\n' },
                new byte[] { (byte)'%' },
                new byte[] { (byte)'a' },
                new byte[] { 0xC8, 0xB4 }, // U+0234 LATIN SMALL LETTER L WITH CURL
                new byte[] { 0xFF }, // invalid byte
                new byte[] { 0xF0, 0x9F, 0x90, 0x95 }, // U+1F415 DOG
                new byte[] { 0x80 }, // standalone continuation character
                new byte[] { 0xC2 }, // standalone multi-byte sequence marker
                new byte[] { 0xEF, 0xBF, 0xBF }, // end of BMP range
                new byte[] { 0xF0, 0x90, 0x80, 0x80 }, // beginning of supplementary range
                new byte[] { 0xF4, 0x8F, 0xBF, 0xBF }, // end of supplementary range
            };

            // expected outputs correspond to the escaped form of the inputs above
            string[] expectedOutputs = new string[]
            {
                "%0A",
                "%25",
                "a",
                "%C8%B4",
                "%EF%BF%BD", // replaced invalid byte
                "%F0%9F%90%95",
                "%EF%BF%BD", // replaced standalone continuation char
                "%EF%BF%BD", // replaced standalone multi-byte sequence marker
                "%EF%BF%BF",
                "%F0%90%80%80",
                "%F4%8F%BF%BF",
            };

            _RunEncodeUtf8_Battery(inputs, expectedOutputs);
        }
    }

    public abstract class InboxEncoderCommonTestBase : IDisposable
    {
        private readonly TextEncoder _encoder;
        private readonly BoundedMemory<byte> _boundedBytes = BoundedMemory.Allocate<byte>(4096);
        private readonly BoundedMemory<char> _boundedChars = BoundedMemory.Allocate<char>(4096);

        private readonly char _allowedChar; // representative allowed char for this encoder
        private readonly char _disallowedChar; // representative never-allowed char for this encoder

        // U+2D2E is in the Georgian Supplement block but is not currently assigned, hence disallowed by all inbox encoders.
        // U+2D2E is an interesting test case because both U+002D ('-') and U+002E ('.') are allowed by all inbox encoders,
        // so using U+2D2E exercises our UTF-16 -> ASCII narrowing paths to make sure that the narrowing process doesn't
        // inadvertently treat a single non-ASCII BMP char as two independent ASCII chars. IF U+2D2E is ever assigned in
        // the future, this could cause unit tests to fail, but we'll deal with that problem when (if?) the time comes.
        private const char BmpExtendedDisallowedChar = '\u2d2e';

        protected InboxEncoderCommonTestBase(TextEncoder encoder, char allowedChar, char disallowedChar)
        {
            Assert.NotNull(encoder);
            _encoder = encoder;

            Assert.True(allowedChar <= 0x7F, "Test setup failure: Allowed char should be ASCII.");
            Assert.False(encoder.WillEncode(allowedChar), "Test setup failure: Encoder must say this character is allowed.");
            _allowedChar = allowedChar;

            Assert.True(disallowedChar <= 0x7F, "Test setup failure: Disallowed char should be ASCII.");
            Assert.True(encoder.WillEncode(disallowedChar), "Test setup failure: Encoder must say this character is disallowed.");
            _disallowedChar = disallowedChar;
        }

        [Fact]
        public void GetIndexOfFirstCharacterToEncodeUtf16_AllDataValid()
            => _RunGetIndexOfFirstCharacterToEncodeUtf16_BmpExtendedValidCharsOnly(_allowedChar);

        protected void _RunGetIndexOfFirstCharacterToEncodeUtf16_BmpExtendedValidCharsOnly(char bmpAllowedChar)
        {
            // Loop from 96 elements all the way down to 0 elements, which tests that we're
            // not overrunning our read buffers.

            var span = _boundedChars.Span;

            _boundedChars.MakeWriteable();
            span.Fill(bmpAllowedChar); // make buffer all-valid
            _boundedChars.MakeReadonly();

            for (int i = 96; i >= 0; i--)
            {
                Assert.Equal(-1, _encoder.FindFirstCharacterToEncodeUtf16(span.Slice(span.Length - i)));
            }

            // Also check from the beginning of the buffer just in case there's some alignment weirdness
            // in the SIMD-optimized code that causes us to read past where we should.

            _boundedChars.MakeWriteable();

            for (int i = 96; i >= 0; i--)
            {
                span[i] = _disallowedChar; // make this char invalid (ASCII)
                Assert.Equal(-1, _encoder.FindFirstCharacterToEncodeUtf16(span.Slice(0, i)));
            }
        }

        [Fact]
        public void GetIndexOfFirstCharacterToEncodeUtf16_SomeCharsNeedEscaping()
            => _RunGetIndexOfFirstCharacterToEncodeUtf16_BmpExtendedSomeCharsNeedEscaping(_allowedChar);

        protected void _RunGetIndexOfFirstCharacterToEncodeUtf16_BmpExtendedSomeCharsNeedEscaping(char bmpAllowedChar)
        {
            // Use a 31-element buffer since it will exercise all the different unrolled code paths.

            var span = _boundedChars.Span.Slice(0, 31);

            for (int i = 0; i < span.Length - 1; i++)
            {
                // First make this the only invalid char in the whole buffer.
                // Make sure we correctly identify the index which requires escaping.

                _boundedChars.MakeWriteable();
                span.Fill(bmpAllowedChar); // make buffer all-valid
                span[i] = _disallowedChar; //  make this char invalid (ASCII)
                _boundedChars.MakeReadonly();
                Assert.Equal(i, _encoder.FindFirstCharacterToEncodeUtf16(span));

                _boundedChars.MakeWriteable();
                span[i] = BmpExtendedDisallowedChar; // make this char invalid (BMP extended)
                _boundedChars.MakeReadonly();
                Assert.Equal(i, _encoder.FindFirstCharacterToEncodeUtf16(span));

                // Use a bad standalone surrogate char instead of a disallowed char
                // and ensure we get the same index back.

                _boundedChars.MakeWriteable();
                span[i] = '\ud800';
                _boundedChars.MakeReadonly();
                Assert.Equal(i, _encoder.FindFirstCharacterToEncodeUtf16(span));

                // Then make sure that we correctly identify this char as the *first*
                // char which requires escaping, even if the buffer contains more
                // requires-escaping chars after this.

                if (i < span.Length - 2)
                {
                    _boundedChars.MakeWriteable();
                    span[i] = _disallowedChar;
                    span[i + 1] = _disallowedChar;
                    _boundedChars.MakeReadonly();
                }
                Assert.Equal(i, _encoder.FindFirstCharacterToEncodeUtf16(span));
            }
        }

        [Fact]
        public void GetIndexOfFirstCharacterToEncodeUtf8_AllDataValid()
        {
            // Loop from 96 elements all the way down to 0 elements, which tests that we're
            // not overrunning our read buffers.

            var span = _boundedBytes.Span;

            _boundedBytes.MakeWriteable();
            span.Fill((byte)_allowedChar); // make buffer all-valid
            _boundedBytes.MakeReadonly();

            for (int i = 96; i >= 0; i--)
            {
                Assert.Equal(-1, _encoder.FindFirstCharacterToEncodeUtf8(span.Slice(span.Length - i)));
            }

            // Also check from the beginning of the buffer just in case there's some alignment weirdness
            // in the SIMD-optimized code that causes us to read past where we should.

            _boundedBytes.MakeWriteable();

            for (int i = 96; i >= 0; i--)
            {
                span[i] = (byte)_disallowedChar; // make this char invalid
                Assert.Equal(-1, _encoder.FindFirstCharacterToEncodeUtf8(span.Slice(0, i)));
            }
        }

        [Fact]
        public void GetIndexOfFirstCharacterToEncodeUtf8_SomeCharsNeedEscaping()
        {
            // Use a 31-element buffer since it will exercise all the different vectorized loops.

            var span = _boundedBytes.Span.Slice(0, 31);

            for (int i = 0; i < span.Length - 1; i++)
            {
                // First make this the only invalid char in the whole buffer.
                // Make sure we correctly identify the index which requires escaping.

                _boundedBytes.MakeWriteable();
                span.Fill((byte)_allowedChar); // make buffer all-valid
                span[i] = (byte)_disallowedChar; //  make this char invalid
                _boundedBytes.MakeReadonly();
                Assert.Equal(i, _encoder.FindFirstCharacterToEncodeUtf8(span));

                // Then make sure that we correctly identify this char as the *first*
                // char which requires escaping, even if the buffer contains more
                // requires-escaping chars after this.

                if (i < span.Length - 2)
                {
                    _boundedBytes.MakeWriteable();
                    span[i + 1] = (byte)_disallowedChar;
                    _boundedBytes.MakeReadonly();
                }
                Assert.Equal(i, _encoder.FindFirstCharacterToEncodeUtf8(span));
            }
        }

        protected void _RunGetIndexOfFirstCharacterToEncodeUtf8_BmpExtendedAllValidChars(char bmpAllowedChar)
        {
            Span<byte> allowedCharAsUtf8 = stackalloc byte[3];
            Assert.True(new Rune(bmpAllowedChar).TryEncodeToUtf8(allowedCharAsUtf8, out int allowedCharUtf8CodeUnitCount));
            allowedCharAsUtf8 = allowedCharAsUtf8.Slice(0, allowedCharUtf8CodeUnitCount);

            // Copy this character to the end of the buffer 12 times

            var span = _boundedBytes.Span;
            span = span.Slice(span.Length - allowedCharAsUtf8.Length * 12);

            _boundedBytes.MakeWriteable();
            span.Clear();
            for (int i = 0; i < 12; i++)
            {
                allowedCharAsUtf8.CopyTo(span.Slice(allowedCharAsUtf8.Length * i));
            }
            _boundedBytes.MakeReadonly();

            // And now make sure we identify all chars as allowed.

            for (int i = 0; i < 12; i++)
            {
                Assert.Equal(-1, _encoder.FindFirstCharacterToEncodeUtf8(span.Slice(allowedCharAsUtf8.Length * i)));
            }
        }

        protected void _RunGetIndexOfFirstCharacterToEncodeUtf8_BmpExtendedSomeCharsNeedEncoding(char bmpAllowedChar)
        {
            Assert.True(bmpAllowedChar >= 0x80, "Must be a non-ASCII char.");

            Span<byte> allowedCharAsUtf8 = stackalloc byte[3];
            Assert.True(new Rune(bmpAllowedChar).TryEncodeToUtf8(allowedCharAsUtf8, out int allowedCharUtf8CodeUnitCount));
            allowedCharAsUtf8 = allowedCharAsUtf8.Slice(0, allowedCharUtf8CodeUnitCount);

            // Copy this character to the end of the buffer 12 times

            var span = _boundedBytes.Span;
            span = span.Slice(span.Length - allowedCharAsUtf8.Length * 12);

            _boundedBytes.MakeWriteable();
            span.Clear();
            for (int i = 0; i < 12; i++)
            {
                allowedCharAsUtf8.CopyTo(span.Slice(allowedCharAsUtf8.Length * i));
            }

            // And now make sure we identify bad chars as disallowed.
            // The last element in the span will be invalid, and we'll keep shrinking the span
            // so that the returned index changes on each iteration.

            for (int i = 0; i < 12; i++)
            {
                // First, corrupt the element by making it a standalone continuation byte.
                span[span.Length - allowedCharAsUtf8.Length] = 0xBF;
                Assert.Equal((11 - i) * allowedCharAsUtf8.Length, _encoder.FindFirstCharacterToEncodeUtf8(span.Slice(allowedCharAsUtf8.Length * i)));

                // Then, uncorrupt the element by making it a well-formed but never-allowed code point (U+009F is a never-allowed C1 control code point)
                span[span.Length - allowedCharAsUtf8.Length] = 0xC2;
                span[span.Length - allowedCharAsUtf8.Length + 1] = 0x9F;
                Assert.Equal((11 - i) * allowedCharAsUtf8.Length, _encoder.FindFirstCharacterToEncodeUtf8(span.Slice(allowedCharAsUtf8.Length * i)));
            }
        }

        [Fact]
        public unsafe void TryEncodeUnicodeScalar_AllowedBmpChar()
        {
            _boundedChars.MakeWriteable();

            // First, try with enough space (two chars) in the destination buffer

            var destination = _boundedChars.Span;
            destination = destination.Slice(destination.Length - 2);
            destination.Clear();

            fixed (char* pBuf = &MemoryMarshal.GetReference(destination))
            {
                bool succeeded = _encoder.TryEncodeUnicodeScalar(_allowedChar, pBuf, destination.Length, out int numCharsWritten);
                Assert.True(succeeded);
                Assert.Equal(1, numCharsWritten);
                Assert.Equal(_allowedChar, destination[0]); // Should reflect char as-is
            }

            // Then, try with enough space (one char) in the destination buffer

            destination.Clear();
            destination = destination.Slice(1);

            fixed (char* pBuf = &MemoryMarshal.GetReference(destination))
            {
                bool succeeded = _encoder.TryEncodeUnicodeScalar(_allowedChar, pBuf, destination.Length, out int numCharsWritten);
                Assert.True(succeeded);
                Assert.Equal(1, numCharsWritten);
                Assert.Equal(_allowedChar, destination[0]); // Should reflect char as-is
            }

            // Finally, try with not enough space in the destination buffer

            destination.Clear();
            destination = destination.Slice(1);

            fixed (char* pBuf = &MemoryMarshal.GetReference(destination)) // use MemoryMarshal so as to get a valid pointer
            {
                bool succeeded = _encoder.TryEncodeUnicodeScalar(_allowedChar, pBuf, destination.Length, out int numCharsWritten);
                Assert.False(succeeded);
                Assert.Equal(0, numCharsWritten);
            }
        }

        [Fact]
        public unsafe void TryEncodeUnicodeScalar_DisallowedBmpChar()
        {
            TryEncodeUnicodeScalar_DisallowedScalarCommon(new Rune(_disallowedChar));
        }

        [Fact]
        public unsafe void TryEncodeUnicodeScalar_DisallowedSupplementaryChar()
        {
            TryEncodeUnicodeScalar_DisallowedScalarCommon(new Rune(0x1F604)); // U+1F604 SMILING FACE WITH OPEN MOUTH AND SMILING EYES
        }

        private unsafe void TryEncodeUnicodeScalar_DisallowedScalarCommon(Rune value)
        {
            _boundedChars.MakeWriteable();
            string expectedEscaping = GetExpectedEscapedRepresentation(value);

            // First, try with enough space +1 in the destination buffer

            var destination = _boundedChars.Span;
            destination = destination.Slice(destination.Length - expectedEscaping.Length - 1);
            destination.Clear();

            fixed (char* pBuf = &MemoryMarshal.GetReference(destination))
            {
                bool succeeded = _encoder.TryEncodeUnicodeScalar(value.Value, pBuf, destination.Length, out int numCharsWritten);
                Assert.True(succeeded);
                Assert.Equal(expectedEscaping.Length, numCharsWritten);
                Assert.Equal(expectedEscaping, destination.Slice(0, expectedEscaping.Length).ToString());
            }

            // Then, try with enough space +0 in the destination buffer

            destination.Clear();
            destination = destination.Slice(1);

            fixed (char* pBuf = &MemoryMarshal.GetReference(destination))
            {
                bool succeeded = _encoder.TryEncodeUnicodeScalar(value.Value, pBuf, destination.Length, out int numCharsWritten);
                Assert.True(succeeded);
                Assert.Equal(expectedEscaping.Length, numCharsWritten);
                Assert.Equal(expectedEscaping, destination.ToString());
            }

            // Finally, try with not enough space in the destination buffer

            destination.Clear();
            destination = destination.Slice(1);

            fixed (char* pBuf = &MemoryMarshal.GetReference(destination)) // use MemoryMarshal so as to get a valid pointer
            {
                bool succeeded = _encoder.TryEncodeUnicodeScalar(value.Value, pBuf, destination.Length, out int numCharsWritten);
                Assert.False(succeeded);
                Assert.Equal(0, numCharsWritten);
            }
        }

        protected void _RunEncodeUtf16_Battery(string[] inputs, string[] expectedOutputs)
        {
            string accumInput = _disallowedChar.ToString();
            string accumExpectedOutput = GetExpectedEscapedRepresentation(new Rune(_disallowedChar));

            // First, make sure we handle the simple "can't escape a single char to the buffer" case

            OperationStatus opStatus = _encoder.Encode(accumInput.AsSpan(), new char[accumExpectedOutput.Length - 1], out int charsConsumed, out int charsWritten);
            Assert.Equal(OperationStatus.DestinationTooSmall, opStatus);
            Assert.Equal(0, charsConsumed);
            Assert.Equal(0, charsWritten);

            // Then, escape a single char to the destination buffer.
            // This skips the "find the first char to encode" fast path in TextEncoder.cs.

            char[] destination = new char[accumExpectedOutput.Length];
            opStatus = _encoder.Encode(accumInput.AsSpan(), destination, out charsConsumed, out charsWritten);
            Assert.Equal(OperationStatus.Done, opStatus);
            Assert.Equal(1, charsConsumed);
            Assert.Equal(destination.Length, charsWritten);
            Assert.Equal(accumExpectedOutput, new string(destination));

            // Now, in a loop, append inputs to the source span and test various edge cases of
            // destination too small vs. destination properly sized.

            Assert.Equal(expectedOutputs.Length, inputs.Length);
            for (int i = 0; i < inputs.Length; i++)
            {
                accumInput += inputs[i];
                string outputToAppend = expectedOutputs[i];

                // Test destination too small - we should make progress up until
                // the very last thing we appended to the input.

                destination = new char[accumExpectedOutput.Length + outputToAppend.Length - 1];
                opStatus = _encoder.Encode(accumInput.AsSpan(), destination, out charsConsumed, out charsWritten);
                Assert.Equal(OperationStatus.DestinationTooSmall, opStatus);
                Assert.Equal(accumInput.Length - inputs[i].Length, charsConsumed); // should've consumed everything except the most recent appended data
                Assert.Equal(accumExpectedOutput.Length, charsWritten); // should've escaped everything we consumed
                Assert.Equal(accumExpectedOutput, new string(destination, 0, charsWritten));

                // Now test destination just right - we should consume the entire buffer successfully.

                accumExpectedOutput += outputToAppend;
                destination = new char[accumExpectedOutput.Length];
                opStatus = _encoder.Encode(accumInput.AsSpan(), destination, out charsConsumed, out charsWritten);
                Assert.Equal(OperationStatus.Done, opStatus);
                Assert.Equal(accumInput.Length, charsConsumed);
                Assert.Equal(accumExpectedOutput.Length, charsWritten);
                Assert.Equal(accumExpectedOutput, new string(destination));

                // Now test destination oversized - we should consume the entire buffer successfully.

                destination = new char[accumExpectedOutput.Length + 1];
                opStatus = _encoder.Encode(accumInput.AsSpan(), destination, out charsConsumed, out charsWritten);
                Assert.Equal(OperationStatus.Done, opStatus);
                Assert.Equal(accumInput.Length, charsConsumed);
                Assert.Equal(accumExpectedOutput.Length, charsWritten);
                Assert.Equal(accumExpectedOutput, new string(destination, 0, charsWritten));

                // Special-case: if the buffer ended with a legal supplementary scalar value, slice off
                // the last low surrogate char now and ensure the escaper can handle reading partial
                // surrogates, returning "Needs More Data".

                if (EndsWithValidSurrogatePair(accumInput))
                {
                    destination.AsSpan().Clear();
                    opStatus = _encoder.Encode(accumInput.AsSpan(0, accumInput.Length - 1), destination, out charsConsumed, out charsWritten, isFinalBlock: false);
                    Assert.Equal(OperationStatus.NeedMoreData, opStatus);
                    Assert.Equal(accumInput.Length - 2, charsConsumed);
                    Assert.Equal(accumExpectedOutput.Length - outputToAppend.Length, charsWritten);
                    Assert.Equal(accumExpectedOutput.Substring(0, accumExpectedOutput.Length - outputToAppend.Length), new string(destination, 0, charsWritten));
                }
            }
        }

        protected void _RunEncodeUtf8_Battery(byte[][] inputs, string[] expectedOutputsAsUtf16)
        {
            byte[] accumInput = new byte[] { (byte)_disallowedChar };
            byte[] accumExpectedOutput = Encoding.UTF8.GetBytes(GetExpectedEscapedRepresentation(new Rune(_disallowedChar)));
            byte[][] expectedOutputs = expectedOutputsAsUtf16.Select(Encoding.UTF8.GetBytes).ToArray();

            // First, make sure we handle the simple "can't escape a single char to the buffer" case

            OperationStatus opStatus = _encoder.EncodeUtf8(accumInput, new byte[accumExpectedOutput.Length - 1], out int bytesConsumed, out int bytesWritten);
            Assert.Equal(OperationStatus.DestinationTooSmall, opStatus);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);

            // Then, escape a single char to the destination buffer.
            // This skips the "find the first char to encode" fast path in TextEncoder.cs.

            byte[] destination = new byte[accumExpectedOutput.Length];
            opStatus = _encoder.EncodeUtf8(accumInput, destination, out bytesConsumed, out bytesWritten);
            Assert.Equal(OperationStatus.Done, opStatus);
            Assert.Equal(1, bytesConsumed);
            Assert.Equal(destination.Length, bytesWritten);
            Assert.Equal(accumExpectedOutput, destination.ToArray());

            // Now, in a loop, append inputs to the source span and test various edge cases of
            // destination too small vs. destination properly sized.

            Assert.Equal(expectedOutputs.Length, inputs.Length);
            for (int i = 0; i < inputs.Length; i++)
            {
                accumInput = accumInput.Concat(inputs[i]).ToArray();
                byte[] outputToAppend = expectedOutputs[i];

                // Test destination too small - we should make progress up until
                // the very last thing we appended to the input.

                destination = new byte[accumExpectedOutput.Length + outputToAppend.Length - 1];
                opStatus = _encoder.EncodeUtf8(accumInput, destination, out bytesConsumed, out bytesWritten);
                Assert.Equal(OperationStatus.DestinationTooSmall, opStatus);
                Assert.Equal(accumInput.Length - inputs[i].Length, bytesConsumed); // should've consumed everything except the most recent appended data
                Assert.Equal(accumExpectedOutput.Length, bytesWritten); // should've escaped everything we consumed
                Assert.Equal(accumExpectedOutput, destination.AsSpan(0, bytesWritten).ToArray());

                // Now test destination just right - we should consume the entire buffer successfully.

                accumExpectedOutput = accumExpectedOutput.Concat(outputToAppend).ToArray();
                destination = new byte[accumExpectedOutput.Length];
                opStatus = _encoder.EncodeUtf8(accumInput, destination, out bytesConsumed, out bytesWritten);
                Assert.Equal(OperationStatus.Done, opStatus);
                Assert.Equal(accumInput.Length, bytesConsumed);
                Assert.Equal(accumExpectedOutput.Length, bytesWritten);
                Assert.Equal(accumExpectedOutput, destination);

                // Now test destination oversized - we should consume the entire buffer successfully.

                destination = new byte[accumExpectedOutput.Length + 1];
                opStatus = _encoder.EncodeUtf8(accumInput, destination, out bytesConsumed, out bytesWritten);
                Assert.Equal(OperationStatus.Done, opStatus);
                Assert.Equal(accumInput.Length, bytesConsumed);
                Assert.Equal(accumExpectedOutput.Length, bytesWritten);
                Assert.Equal(accumExpectedOutput, destination.AsSpan(0, bytesWritten).ToArray());

                // Special-case: if the buffer ended with a legal supplementary scalar value, slice off
                // the last few bytes now and ensure the escaper can handle reading partial
                // values, returning "Needs More Data".

                if (EndsWithValidMultiByteUtf8Sequence(accumInput))
                {
                    destination.AsSpan().Clear();
                    opStatus = _encoder.EncodeUtf8(accumInput.AsSpan(0, accumInput.Length - 1), destination, out bytesConsumed, out bytesWritten, isFinalBlock: false);
                    Assert.Equal(OperationStatus.NeedMoreData, opStatus);
                    Assert.Equal(accumInput.Length - inputs[i].Length, bytesConsumed);
                    Assert.Equal(accumExpectedOutput.Length - outputToAppend.Length, bytesWritten);
                    Assert.Equal(accumExpectedOutput.AsSpan(0, accumExpectedOutput.Length - outputToAppend.Length).ToArray(), destination.AsSpan(0, bytesWritten).ToArray());
                }
            }
        }

        private static bool EndsWithValidSurrogatePair(string input)
        {
            return input.Length >= 2
                && char.IsHighSurrogate(input[input.Length - 2])
                && char.IsLowSurrogate(input[input.Length - 1]);
        }

        private static bool EndsWithValidMultiByteUtf8Sequence(byte[] input)
        {
            for (int i = input.Length - 1; i >= 0; i--)
            {
                if (input[i] >= 0xC0)
                {
                    return Rune.DecodeFromUtf8(input.AsSpan(i), out _, out int bytesConsumed) == OperationStatus.Done
                        && i + bytesConsumed == input.Length;
                }
            }

            return false; // input was empty?
        }

        private protected abstract string GetExpectedEscapedRepresentation(Rune value);

        private string GetExpectedEscapedRepresentation(string value)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < value.Length;)
            {
                Rune.DecodeFromUtf16(value.AsSpan(i), out Rune nextRune, out int charsConsumed);
                builder.Append(GetExpectedEscapedRepresentation(nextRune));
                i += charsConsumed;
            }
            return builder.ToString();
        }

        void IDisposable.Dispose()
        {
            _boundedBytes.Dispose();
            _boundedChars.Dispose();
        }
    }
}
