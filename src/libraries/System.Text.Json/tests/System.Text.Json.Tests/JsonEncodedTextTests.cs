// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class JsonEncodedTextTests
    {
        [Fact]
        public static void LatinCharsSameAsDefaultEncoder()
        {
            for (int i = 0; i <= 127; i++)
            {
                JsonEncodedText textBuiltin = JsonEncodedText.Encode(((char)i).ToString());
                JsonEncodedText textEncoder = JsonEncodedText.Encode(((char)i).ToString(), JavaScriptEncoder.Default);

                Assert.Equal(textEncoder, textBuiltin);
            }
        }

        [Fact]
        public static void Default()
        {
            JsonEncodedText text = default;
            Assert.True(text.EncodedUtf8Bytes.IsEmpty);

            Assert.Equal(0, text.GetHashCode());
            Assert.Equal("", text.ToString());
            Assert.True(text.Equals(default));
            Assert.True(text.Equals(text));
            Assert.False(text.Equals(null));

            JsonEncodedText defaultText = default;
            object obj = defaultText;
            Assert.True(text.Equals(obj));
            Assert.True(text.Equals(defaultText));
            Assert.True(defaultText.Equals(text));

            JsonEncodedText textByteEmpty = JsonEncodedText.Encode(Array.Empty<byte>());
            Assert.True(textByteEmpty.EncodedUtf8Bytes.IsEmpty);
            Assert.Equal("", textByteEmpty.ToString());

            JsonEncodedText textCharEmpty = JsonEncodedText.Encode(Array.Empty<char>());
            Assert.True(textCharEmpty.EncodedUtf8Bytes.IsEmpty);
            Assert.Equal("", textCharEmpty.ToString());

            Assert.True(textCharEmpty.Equals(textByteEmpty));
            Assert.Equal(textByteEmpty.GetHashCode(), textCharEmpty.GetHashCode());
        }

        [Theory]
        [MemberData(nameof(JsonEncodedTextStrings))]
        public static void NullEncoder(string message, string expectedMessage)
        {
            JsonEncodedText text = JsonEncodedText.Encode(message, null);
            JsonEncodedText textSpan = JsonEncodedText.Encode(message.AsSpan(), null);
            JsonEncodedText textUtf8Span = JsonEncodedText.Encode(Encoding.UTF8.GetBytes(message), null);

            Assert.Equal(expectedMessage, text.ToString());
            Assert.Equal(expectedMessage, textSpan.ToString());
            Assert.Equal(expectedMessage, textUtf8Span.ToString());

            Assert.True(text.Equals(textSpan));
            Assert.True(text.Equals(textUtf8Span));
            Assert.Equal(text.GetHashCode(), textSpan.GetHashCode());
            Assert.Equal(text.GetHashCode(), textUtf8Span.GetHashCode());
        }

        [Theory]
        [MemberData(nameof(JsonEncodedTextStringsCustom))]
        public static void CustomEncoder(string message, string expectedMessage)
        {
            // Latin-1 Supplement block starts from U+0080 and ends at U+00FF
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRange.Create((char)0x0080, (char)0x00FF));
            JsonEncodedText text = JsonEncodedText.Encode(message, encoder);
            JsonEncodedText textSpan = JsonEncodedText.Encode(message.AsSpan(), encoder);
            JsonEncodedText textUtf8Span = JsonEncodedText.Encode(Encoding.UTF8.GetBytes(message), encoder);

            Assert.Equal(expectedMessage, text.ToString());
            Assert.Equal(expectedMessage, textSpan.ToString());
            Assert.Equal(expectedMessage, textUtf8Span.ToString());

            Assert.True(text.Equals(textSpan));
            Assert.True(text.Equals(textUtf8Span));
            Assert.Equal(text.GetHashCode(), textSpan.GetHashCode());
            Assert.Equal(text.GetHashCode(), textUtf8Span.GetHashCode());
        }

        [Theory]
        [MemberData(nameof(JsonEncodedTextStrings))]
        public static void CustomEncoderCantOverrideHtml(string message, string expectedMessage)
        {
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRange.Create(' ', '}'));
            JsonEncodedText text = JsonEncodedText.Encode(message, encoder);
            JsonEncodedText textSpan = JsonEncodedText.Encode(message.AsSpan(), encoder);
            JsonEncodedText textUtf8Span = JsonEncodedText.Encode(Encoding.UTF8.GetBytes(message), encoder);

            Assert.Equal(expectedMessage, text.ToString());
            Assert.Equal(expectedMessage, textSpan.ToString());
            Assert.Equal(expectedMessage, textUtf8Span.ToString());

            Assert.True(text.Equals(textSpan));
            Assert.True(text.Equals(textUtf8Span));
            Assert.Equal(text.GetHashCode(), textSpan.GetHashCode());
            Assert.Equal(text.GetHashCode(), textUtf8Span.GetHashCode());
        }

        [Fact]
        public static void EqualsTest()
        {
            string message = "message";

            JsonEncodedText text = JsonEncodedText.Encode(message);
            JsonEncodedText textCopy = text;
            JsonEncodedText textDuplicate = JsonEncodedText.Encode(message);
            JsonEncodedText textDuplicateDiffStringRef = JsonEncodedText.Encode(string.Concat("mess", "age"));
            JsonEncodedText differentText = JsonEncodedText.Encode("message1");

            Assert.True(text.Equals(text));

            Assert.True(text.Equals(textCopy));
            Assert.True(textCopy.Equals(text));

            Assert.True(text.Equals(textDuplicate));
            Assert.True(textDuplicate.Equals(text));

            Assert.True(text.Equals(textDuplicateDiffStringRef));
            Assert.True(textDuplicateDiffStringRef.Equals(text));

            Assert.False(text.Equals(differentText));
            Assert.False(differentText.Equals(text));
        }

        [Fact]
        public static void EqualsObject()
        {
            string message = "message";

            JsonEncodedText text = JsonEncodedText.Encode(message);
            object textCopy = text;
            object textDuplicate = JsonEncodedText.Encode(message);
            object textDuplicateDiffStringRef = JsonEncodedText.Encode(string.Concat("mess", "age"));
            object differentText = JsonEncodedText.Encode("message1");

            Assert.True(text.Equals(text));

            Assert.True(text.Equals(textCopy));
            Assert.True(textCopy.Equals(text));

            Assert.True(text.Equals(textDuplicate));
            Assert.True(textDuplicate.Equals(text));

            Assert.True(text.Equals(textDuplicateDiffStringRef));
            Assert.True(textDuplicateDiffStringRef.Equals(text));

            Assert.False(text.Equals(differentText));
            Assert.False(differentText.Equals(text));

            Assert.False(text.Equals(null));
        }

        [Fact]
        public static void GetHashCodeTest()
        {
            string message = "message";

            JsonEncodedText text = JsonEncodedText.Encode(message);
            JsonEncodedText textCopy = text;
            JsonEncodedText textDuplicate = JsonEncodedText.Encode(message);
            JsonEncodedText textDuplicateDiffStringRef = JsonEncodedText.Encode(string.Concat("mess", "age"));
            JsonEncodedText differentText = JsonEncodedText.Encode("message1");

            int expectedHashCode = text.GetHashCode();

            Assert.NotEqual(0, expectedHashCode);
            Assert.Equal(expectedHashCode, textCopy.GetHashCode());
            Assert.Equal(expectedHashCode, textDuplicate.GetHashCode());
            Assert.Equal(expectedHashCode, textDuplicateDiffStringRef.GetHashCode());
            Assert.NotEqual(expectedHashCode, differentText.GetHashCode());
        }

        [Theory]
        [MemberData(nameof(JsonEncodedTextStrings))]
        public static void ToStringTest(string message, string expectedMessage)
        {
            JsonEncodedText text = JsonEncodedText.Encode(message);
            JsonEncodedText textSpan = JsonEncodedText.Encode(message.AsSpan());
            JsonEncodedText textUtf8Span = JsonEncodedText.Encode(Encoding.UTF8.GetBytes(message));

            Assert.Equal(expectedMessage, text.ToString());
            Assert.Equal(expectedMessage, textSpan.ToString());
            Assert.Equal(expectedMessage, textUtf8Span.ToString());

            Assert.True(text.Equals(textSpan));
            Assert.True(text.Equals(textUtf8Span));
            Assert.Equal(text.GetHashCode(), textSpan.GetHashCode());
            Assert.Equal(text.GetHashCode(), textUtf8Span.GetHashCode());
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1_000)]
        [InlineData(10_000)]
        public static void ToStringLargeTest(int stringLength)
        {
            {
                var message = new string('a', stringLength);
                var expectedMessage = new string('a', stringLength);

                JsonEncodedText text = JsonEncodedText.Encode(message);
                JsonEncodedText textSpan = JsonEncodedText.Encode(message.AsSpan());
                JsonEncodedText textUtf8Span = JsonEncodedText.Encode(Encoding.UTF8.GetBytes(message));

                Assert.Equal(expectedMessage, text.ToString());
                Assert.Equal(expectedMessage, textSpan.ToString());
                Assert.Equal(expectedMessage, textUtf8Span.ToString());

                Assert.True(text.Equals(textSpan));
                Assert.True(text.Equals(textUtf8Span));
                Assert.Equal(text.GetHashCode(), textSpan.GetHashCode());
                Assert.Equal(text.GetHashCode(), textUtf8Span.GetHashCode());
            }
            {
                var message = new string('>', stringLength);
                var builder = new StringBuilder();
                for (int i = 0; i < stringLength; i++)
                {
                    builder.Append("\\u003E");
                }
                string expectedMessage = builder.ToString();

                JsonEncodedText text = JsonEncodedText.Encode(message);
                JsonEncodedText textSpan = JsonEncodedText.Encode(message.AsSpan());
                JsonEncodedText textUtf8Span = JsonEncodedText.Encode(Encoding.UTF8.GetBytes(message));

                Assert.Equal(expectedMessage, text.ToString());
                Assert.Equal(expectedMessage, textSpan.ToString());
                Assert.Equal(expectedMessage, textUtf8Span.ToString());

                Assert.True(text.Equals(textSpan));
                Assert.True(text.Equals(textUtf8Span));
                Assert.Equal(text.GetHashCode(), textSpan.GetHashCode());
                Assert.Equal(text.GetHashCode(), textUtf8Span.GetHashCode());
            }
        }

        [Theory]
        [MemberData(nameof(JsonEncodedTextStrings))]
        public static void GetUtf8BytesTest(string message, string expectedMessage)
        {
            byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedMessage);

            JsonEncodedText text = JsonEncodedText.Encode(message);
            JsonEncodedText textSpan = JsonEncodedText.Encode(message.AsSpan());
            JsonEncodedText textUtf8Span = JsonEncodedText.Encode(Encoding.UTF8.GetBytes(message));

            Assert.True(text.EncodedUtf8Bytes.SequenceEqual(expectedBytes));
            Assert.True(textSpan.EncodedUtf8Bytes.SequenceEqual(expectedBytes));
            Assert.True(textUtf8Span.EncodedUtf8Bytes.SequenceEqual(expectedBytes));

            Assert.True(text.Equals(textSpan));
            Assert.True(text.Equals(textUtf8Span));
            Assert.Equal(text.GetHashCode(), textSpan.GetHashCode());
            Assert.Equal(text.GetHashCode(), textUtf8Span.GetHashCode());
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1_000)]
        [InlineData(10_000)]
        public static void GetUtf8BytesLargeTest(int stringLength)
        {
            {
                var message = new string('a', stringLength);
                byte[] expectedBytes = Encoding.UTF8.GetBytes(message);

                JsonEncodedText text = JsonEncodedText.Encode(message);
                JsonEncodedText textSpan = JsonEncodedText.Encode(message.AsSpan());
                JsonEncodedText textUtf8Span = JsonEncodedText.Encode(Encoding.UTF8.GetBytes(message));

                Assert.True(text.EncodedUtf8Bytes.SequenceEqual(expectedBytes));
                Assert.True(textSpan.EncodedUtf8Bytes.SequenceEqual(expectedBytes));
                Assert.True(textUtf8Span.EncodedUtf8Bytes.SequenceEqual(expectedBytes));

                Assert.True(text.Equals(textSpan));
                Assert.True(text.Equals(textUtf8Span));
                Assert.Equal(text.GetHashCode(), textSpan.GetHashCode());
                Assert.Equal(text.GetHashCode(), textUtf8Span.GetHashCode());
            }
            {
                var message = new string('>', stringLength);
                var builder = new StringBuilder();
                for (int i = 0; i < stringLength; i++)
                {
                    builder.Append("\\u003E");
                }
                byte[] expectedBytes = Encoding.UTF8.GetBytes(builder.ToString());

                JsonEncodedText text = JsonEncodedText.Encode(message);
                JsonEncodedText textSpan = JsonEncodedText.Encode(message.AsSpan());
                JsonEncodedText textUtf8Span = JsonEncodedText.Encode(Encoding.UTF8.GetBytes(message));

                Assert.True(text.EncodedUtf8Bytes.SequenceEqual(expectedBytes));
                Assert.True(textSpan.EncodedUtf8Bytes.SequenceEqual(expectedBytes));
                Assert.True(textUtf8Span.EncodedUtf8Bytes.SequenceEqual(expectedBytes));

                Assert.True(text.Equals(textSpan));
                Assert.True(text.Equals(textUtf8Span));
                Assert.Equal(text.GetHashCode(), textSpan.GetHashCode());
                Assert.Equal(text.GetHashCode(), textUtf8Span.GetHashCode());
            }
        }

        [Fact]
        public static void InvalidUTF16()
        {
            var invalid = new char[5] { 'a', 'b', 'c', (char)0xDC00, 'a' };
            Assert.Throws<ArgumentException>(() => JsonEncodedText.Encode(invalid));

            invalid = new char[5] { 'a', 'b', 'c', (char)0xD800, 'a' };
            Assert.Throws<ArgumentException>(() => JsonEncodedText.Encode(invalid));

            invalid = new char[5] { 'a', 'b', 'c', (char)0xDC00, (char)0xD800 };
            Assert.Throws<ArgumentException>(() => JsonEncodedText.Encode(invalid));

            var valid = new char[5] { 'a', 'b', 'c', (char)0xD800, (char)0xDC00 };
            JsonEncodedText _ = JsonEncodedText.Encode(valid);

            Assert.Throws<ArgumentException>(() => JsonEncodedText.Encode(new string(valid).Substring(0, 4)));
        }

        [Theory]
        [MemberData(nameof(UTF8ReplacementCharacterStrings))]
        public static void ReplacementCharacterUTF8(byte[] dataUtf8, string expected)
        {
            JsonEncodedText text = JsonEncodedText.Encode(dataUtf8);
            Assert.Equal(expected, text.ToString());
        }

        [Fact]
        public static void InvalidEncode()
        {
            Assert.Throws<ArgumentNullException>(() => JsonEncodedText.Encode((string)null));
        }

        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))]
        [OuterLoop]
        public static void InvalidLargeEncode()
        {
            try
            {
                var largeValueString = new string('a', 400_000_000);
                var utf8Value = new byte[400_000_000];
                utf8Value.AsSpan().Fill((byte)'a');

                Assert.Throws<ArgumentException>(() => JsonEncodedText.Encode(largeValueString));
                Assert.Throws<ArgumentException>(() => JsonEncodedText.Encode(largeValueString.AsSpan()));
                Assert.Throws<ArgumentException>(() => JsonEncodedText.Encode(utf8Value));
            }
            catch (OutOfMemoryException)
            {
                return;
            }
        }

        public static IEnumerable<object[]> UTF8ReplacementCharacterStrings
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { new byte[] { 34, 97, 0xc3, 0x28, 98, 34 }, "\\u0022a\\uFFFD(b\\u0022" },
                    new object[] { new byte[] { 34, 97, 0xa0, 0xa1, 98, 34 }, "\\u0022a\\uFFFD\\uFFFDb\\u0022" },
                    new object[] { new byte[] { 34, 97, 0xe2, 0x28, 0xa1, 98, 34 }, "\\u0022a\\uFFFD(\\uFFFDb\\u0022" },
                    new object[] { new byte[] { 34, 97, 0xe2, 0x82, 0x28, 98, 34 }, "\\u0022a\\uFFFD(b\\u0022" },
                    new object[] { new byte[] { 34, 97, 0xf0, 0x28, 0x8c, 0xbc, 98, 34 }, "\\u0022a\\uFFFD(\\uFFFD\\uFFFDb\\u0022" },
                    new object[] { new byte[] { 34, 97, 0xf0, 0x90, 0x28, 0xbc, 98, 34 }, "\\u0022a\\uFFFD(\\uFFFDb\\u0022" },
                    new object[] { new byte[] { 34, 97, 0xf0, 0x28, 0x8c, 0x28, 98, 34 }, "\\u0022a\\uFFFD(\\uFFFD(b\\u0022" },
                };
            }
        }

        public static IEnumerable<object[]> JsonEncodedTextStrings
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"", "" },
                    new object[] { "message", "message" },
                    new object[] { "mess\"age", "mess\\u0022age" },
                    new object[] { "mess\\u0022age", "mess\\\\u0022age" },
                    new object[] { ">>>>>", "\\u003E\\u003E\\u003E\\u003E\\u003E" },
                    new object[] { "\\u003e\\u003e\\u003e\\u003e\\u003e", "\\\\u003e\\\\u003e\\\\u003e\\\\u003e\\\\u003e" },
                    new object[] { "\\u003E\\u003E\\u003E\\u003E\\u003E", "\\\\u003E\\\\u003E\\\\u003E\\\\u003E\\\\u003E" },
                };
            }
        }

        public static IEnumerable<object[]> JsonEncodedTextStringsCustom
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"", "" },
                    new object[] { "age", "\\u0061\\u0067\\u0065" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\"\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\u0022\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\\u0022\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\\\\\u0075\\u0030\\u0030\\u0032\\u0032\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9>>>>>\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\u003E\\u003E\\u003E\\u003E\\u003E\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\\u003e\\u003e\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\\\\\u0075\\u0030\\u0030\\u0033\\u0065\\\\\\u0075\\u0030\\u0030\\u0033\\u0065\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\\u003E\\u003E\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\\\\\u0075\\u0030\\u0030\\u0033\\u0045\\\\\\u0075\\u0030\\u0030\\u0033\\u0045\u00EA\u00EA\u00EA\u00EA\u00EA" },
                };
            }
        }

        /// <summary>
        /// This is not a recommended way to customize the escaping, but is present here for test purposes.
        /// </summary>
        public sealed class CustomEncoderAllowingPlusSign : JavaScriptEncoder
        {
            public CustomEncoderAllowingPlusSign() { }

            public override bool WillEncode(int unicodeScalar)
            {
                if (unicodeScalar == '+')
                {
                    return false;
                }

                return Default.WillEncode(unicodeScalar);
            }

            public unsafe override int FindFirstCharacterToEncode(char* text, int textLength)
            {
                return Default.FindFirstCharacterToEncode(text, textLength);
            }


            public override int MaxOutputCharactersPerInputCharacter
            {
                get
                {
                    return Default.MaxOutputCharactersPerInputCharacter;
                }
            }

            public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
            {
                return Default.TryEncodeUnicodeScalar(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten);
            }
        }

        [Fact]
        public static void CustomEncoderClass()
        {
            const string message = "a+";
            const string expected = "a\\u002B";
            JsonEncodedText text;

            text = JsonEncodedText.Encode(message);
            Assert.Equal(expected, text.ToString());

            text = JsonEncodedText.Encode(message, null);
            Assert.Equal(expected, text.ToString());

            text = JsonEncodedText.Encode(message, JavaScriptEncoder.Default);
            Assert.Equal(expected, text.ToString());

            text = JsonEncodedText.Encode(message, new CustomEncoderAllowingPlusSign());
            Assert.Equal("a+", text.ToString());
        }
    }
}
