// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            UTF7Encoding encoding = new UTF7Encoding();
            VerifyUtf7Encoding(encoding, allowOptionals: false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool allowOptionals)
        {
            UTF7Encoding encoding = new UTF7Encoding(allowOptionals);
            VerifyUtf7Encoding(encoding, allowOptionals);
        }

        public static void VerifyUtf7Encoding(UTF7Encoding encoding, bool allowOptionals)
        {
            Assert.Empty(encoding.GetPreamble());

            Assert.Equal(new EncoderReplacementFallback(string.Empty), encoding.EncoderFallback);
            Assert.Equal(1, encoding.DecoderFallback.MaxCharCount);
            Assert.Equal(984, encoding.DecoderFallback.GetHashCode());

            if (allowOptionals)
            {
                Assert.Equal(new byte[] { 33 }, encoding.GetBytes("!"));
            }
            else
            {
                Assert.Equal(new byte[] { 43, 65, 67, 69, 45 }, encoding.GetBytes("!"));
            }
            
        }

        public static IEnumerable<object[]> Encodings_TestData()
        {
            yield return new object[] { new UTF7Encoding(true) };
            yield return new object[] { new UTF7Encoding(false) };

            yield return new object[] { Encoding.UTF7 };
            yield return new object[] { Encoding.GetEncoding("utf-7") };
        }
        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void WebName(UTF7Encoding encoding)
        {
            Assert.Equal("utf-7", encoding.WebName);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void CodePage(UTF7Encoding encoding)
        {
            Assert.Equal(65000, encoding.CodePage);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void EncodingName(UTF7Encoding encoding)
        {
            Assert.NotEmpty(encoding.EncodingName); // Unicode (UTF-7) in en-US
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void IsSingleByte(UTF7Encoding encoding)
        {
            Assert.False(encoding.IsSingleByte);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void Clone(UTF7Encoding encoding)
        {
            UTF7Encoding clone = (UTF7Encoding)encoding.Clone();
            Assert.NotSame(encoding, clone);
            Assert.Equal(encoding, clone);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            UTF7Encoding encoding = new UTF7Encoding();
            yield return new object[] { encoding, encoding, true };
            yield return new object[] { new UTF7Encoding(), new UTF7Encoding(), true };
            
            yield return new object[] { new UTF7Encoding(), new UTF7Encoding(true), false };
            yield return new object[] { new UTF7Encoding(), new UTF7Encoding(false), true };

            yield return new object[] { new UTF7Encoding(true), new UTF7Encoding(true), true };
            yield return new object[] { new UTF7Encoding(true), new UTF7Encoding(false), false };

            yield return new object[] { new UTF7Encoding(false), new UTF7Encoding(false), true };
            yield return new object[] { new UTF7Encoding(false), new UTF7Encoding(true), false };

            yield return new object[] { Encoding.UTF7, Encoding.UTF7, true };
            yield return new object[] { Encoding.UTF7, Encoding.GetEncoding("utf-7"), true };
            yield return new object[] { Encoding.UTF7, new UTF7Encoding(false), true };
            yield return new object[] { Encoding.UTF7, new UTF7Encoding(true), false };

            yield return new object[] { new UTF7Encoding(), new TimeSpan(), false };
            yield return new object[] { new UTF7Encoding(), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(UTF7Encoding encoding, object value, bool expected)
        {
            Assert.Equal(expected, encoding.Equals(value));
            Assert.Equal(value is UTF7Encoding, encoding.GetHashCode().Equals(value?.GetHashCode()));
        }
    }
}
