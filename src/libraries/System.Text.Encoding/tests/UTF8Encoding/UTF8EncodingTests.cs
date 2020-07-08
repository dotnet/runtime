// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            UTF8Encoding encoding = new UTF8Encoding();
            VerifyUtf8Encoding(encoding, encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool encoderShouldEmitUTF8Identifier)
        {
            UTF8Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier);
            VerifyUtf8Encoding(encoding, encoderShouldEmitUTF8Identifier, throwOnInvalidBytes: false);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void Ctor_Bool_Bool(bool encoderShouldEmitUTF8Identifier, bool throwOnInvalidBytes)
        {
            UTF8Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier, throwOnInvalidBytes);
            VerifyUtf8Encoding(encoding, encoderShouldEmitUTF8Identifier, throwOnInvalidBytes);
        }

        private static void VerifyUtf8Encoding(UTF8Encoding encoding, bool encoderShouldEmitUTF8Identifier, bool throwOnInvalidBytes)
        {
            if (encoderShouldEmitUTF8Identifier)
            {
                Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, encoding.GetPreamble());
            }
            else
            {
                Assert.Empty(encoding.GetPreamble());
            }

            if (throwOnInvalidBytes)
            {
                Assert.Equal(EncoderFallback.ExceptionFallback, encoding.EncoderFallback);
                Assert.Equal(DecoderFallback.ExceptionFallback, encoding.DecoderFallback);
            }
            else
            {
                Assert.Equal(new EncoderReplacementFallback("\uFFFD"), encoding.EncoderFallback);
                Assert.Equal(new DecoderReplacementFallback("\uFFFD"), encoding.DecoderFallback);
            }
        }

        public static IEnumerable<object[]> Encodings_TestData()
        {
            yield return new object[] { Encoding.UTF8 };
            yield return new object[] { new UTF8Encoding(true, true) };
            yield return new object[] { new UTF8Encoding(true, false) };
            yield return new object[] { new UTF8Encoding(false, true) };
            yield return new object[] { new UTF8Encoding(false, false) };
            yield return new object[] { Encoding.GetEncoding("utf-8") };
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void WebName(UTF8Encoding encoding)
        {
            Assert.Equal("utf-8", encoding.WebName);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void CodePage(UTF8Encoding encoding)
        {
            Assert.Equal(65001, encoding.CodePage);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void EncodingName(UTF8Encoding encoding)
        {
            Assert.NotEmpty(encoding.EncodingName); // Unicode (UTF-8) in en-US
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void IsSingleByte(UTF8Encoding encoding)
        {
            Assert.False(encoding.IsSingleByte);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void Clone(UTF8Encoding encoding)
        {
            UTF8Encoding clone = (UTF8Encoding)encoding.Clone();
            Assert.False(clone.IsReadOnly);
            Assert.NotSame(encoding, clone);
            Assert.Equal(encoding, clone);
        }

        [Fact]
        public void Clone_CanCloneUtf8SingletonAndSetFallbacks()
        {
            // The Encoding.UTF8 singleton is a sealed subclass of UTF8Encoding that has
            // its own internal fast-track logic that might make assumptions about any
            // configured replacement behavior. This test clones the singleton instance
            // to ensure that we can properly set custom fallbacks and that they're
            // honored by the Encoding.

            UTF8Encoding clone = (UTF8Encoding)Encoding.UTF8.Clone();

            clone.DecoderFallback = new DecoderReplacementFallback("[BAD]");
            clone.EncoderFallback = new EncoderReplacementFallback("?");

            Assert.Equal("ab[BAD]xy", clone.GetString(new byte[] { (byte)'a', (byte)'b', 0xC0, (byte)'x', (byte)'y' }));
            Assert.Equal(new byte[] { (byte)'a', (byte)'?', (byte)'c' }, clone.GetBytes("a\ud800c"));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            UTF8Encoding encoding = new UTF8Encoding();
            yield return new object[] { encoding, encoding, true };

            yield return new object[] { new UTF8Encoding(), new UTF8Encoding(), true };

            yield return new object[] { new UTF8Encoding(), new UTF8Encoding(false), true };
            yield return new object[] { new UTF8Encoding(), new UTF8Encoding(true), false };

            yield return new object[] { new UTF8Encoding(false), new UTF8Encoding(false), true };
            yield return new object[] { new UTF8Encoding(true), new UTF8Encoding(true), true };
            yield return new object[] { new UTF8Encoding(true), new UTF8Encoding(false), false };

            yield return new object[] { new UTF8Encoding(), new UTF8Encoding(false, false), true };
            yield return new object[] { new UTF8Encoding(), new UTF8Encoding(false, true), false };

            yield return new object[] { new UTF8Encoding(false), new UTF8Encoding(false, false), true };
            yield return new object[] { new UTF8Encoding(true), new UTF8Encoding(true, false), true };
            yield return new object[] { new UTF8Encoding(false), new UTF8Encoding(false, true), false };

            yield return new object[] { new UTF8Encoding(true, true), new UTF8Encoding(true, true), true };
            yield return new object[] { new UTF8Encoding(false, false), new UTF8Encoding(false, false), true };
            yield return new object[] { new UTF8Encoding(true, false), new UTF8Encoding(true, false), true };
            yield return new object[] { new UTF8Encoding(true, false), new UTF8Encoding(false, true), false };

            yield return new object[] { Encoding.UTF8, Encoding.UTF8, true };
            yield return new object[] { Encoding.GetEncoding("utf-8"), Encoding.UTF8, true };
            yield return new object[] { Encoding.UTF8, new UTF8Encoding(true), true };
            yield return new object[] { Encoding.UTF8, new UTF8Encoding(false), false };

            yield return new object[] { new UTF8Encoding(), new TimeSpan(), false };
            yield return new object[] { new UTF8Encoding(), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(UTF8Encoding encoding, object value, bool expected)
        {
            Assert.Equal(expected, encoding.Equals(value));
            if (value is UTF8Encoding)
            {
                Assert.Equal(expected, encoding.GetHashCode().Equals(value.GetHashCode()));
            }
        }

        [Fact]
        public void CustomSubclassMethodOverrides()
        {
            // Ensures that we don't inadvertently implement methods like UTF8Encoding.GetBytes(string)
            // without accounting for the fact that a subclassed type may have overridden GetBytes(char[], int, int).

            UTF8Encoding encoding = new CustomUTF8Encoding();

            Assert.Equal(new byte[] { (byte)'!', (byte)'a', (byte)'!', (byte)'b', (byte)'!', (byte)'c', (byte)'!' }, encoding.GetBytes("abc"));
            Assert.Equal("*a*b*c*".ToCharArray(), encoding.GetChars(new byte[] { (byte)'a', (byte)'b', (byte)'c' }));
            Assert.Equal("~a~b~c~", encoding.GetString(new byte[] { (byte)'a', (byte)'b', (byte)'c' }));
        }

        /// <summary>
        /// An Encoding which is not actually legitimate UTF-8, but which nevertheless
        /// subclasses UTF8Encoding. A realistic scenario where one might do this would be to
        /// support "wobbly" data (where the code points U+D800..DFFF are round-trippable).
        /// </summary>
        private class CustomUTF8Encoding : UTF8Encoding
        {
            public override int GetByteCount(string chars)
            {
                return chars.Length * 2 + 1;
            }

            public override int GetBytes(string chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
            {
                // We'll narrow chars to bytes and surround each char with an exclamation point.

                List<byte> builder = new List<byte>()
                {
                    (byte)'!'
                };

                foreach (char ch in chars.AsSpan(charIndex, charCount))
                {
                    builder.Add((byte)ch);
                    builder.Add((byte)'!');
                }

                builder.CopyTo(bytes, byteIndex);
                return builder.Count;
            }

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                return count * 2 + 1;
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                // We'll widen bytes to chars and surround each char with an asterisk.

                List<char> builder = new List<char>()
                {
                    '*'
                };

                foreach (byte b in bytes.AsSpan(byteIndex, byteCount))
                {
                    builder.Add((char)b);
                    builder.Add('*');
                }

                builder.CopyTo(chars, charIndex);
                return builder.Count;
            }

            public override string GetString(byte[] bytes, int byteIndex, int byteCount)
            {
                // We'll widen bytes to chars and surround each char with a tilde.

                StringBuilder builder = new StringBuilder("~");

                foreach (byte b in bytes.AsSpan(byteIndex, byteCount))
                {
                    builder.Append((char)b);
                    builder.Append('~');
                }

                return builder.ToString();
            }

            public override int GetMaxByteCount(int charCount)
            {
                return charCount * 2 + 1;
            }

            public override int GetMaxCharCount(int byteCount)
            {
                return byteCount * 2 + 1;
            }
        }
    }
}
