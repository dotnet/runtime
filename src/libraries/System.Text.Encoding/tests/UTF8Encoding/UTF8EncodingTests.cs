// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public static void VerifyUtf8Encoding(UTF8Encoding encoding, bool encoderShouldEmitUTF8Identifier, bool throwOnInvalidBytes)
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
            Assert.NotSame(encoding, clone);
            Assert.Equal(encoding, clone);
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
        public void Equals(UTF8Encoding encoding, object value, bool expected)
        {
            Assert.Equal(expected, encoding.Equals(value));
            if (value is UTF8Encoding)
            {
                Assert.Equal(expected, encoding.GetHashCode().Equals(value.GetHashCode()));
            }
        }
    }
}
