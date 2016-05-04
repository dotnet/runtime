// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class Latin1EncodingTests
    {
        [Fact]
        public void Ctor()
        {
            Encoding encoding = Encoding.GetEncoding("latin1");
            Assert.Equal(1, encoding.EncoderFallback.MaxCharCount);
            Assert.Equal(28591, encoding.EncoderFallback.GetHashCode());
            Assert.Equal(1, encoding.DecoderFallback.MaxCharCount);
            Assert.Equal(28591, encoding.DecoderFallback.GetHashCode());
        }

        public static IEnumerable<object[]> Encodings_TestData()
        {
            yield return new object[] { Encoding.GetEncoding("latin1") };
            yield return new object[] { Encoding.GetEncoding("iso-8859-1") };
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void WebName(Encoding encoding)
        {
            Assert.Equal("iso-8859-1", encoding.WebName);
        }
        
        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void CodePage(Encoding encoding)
        {
            Assert.Equal(28591, encoding.CodePage);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void EncodingName(Encoding encoding)
        {
            Assert.NotEmpty(encoding.EncodingName); // Western European (ISO) in en-US
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void IsSingleByte(Encoding encoding)
        {
            Assert.True(encoding.IsSingleByte);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void Clone(Encoding encoding)
        {
            Encoding clone = (Encoding)encoding.Clone();
            Assert.NotSame(encoding, clone);
            Assert.Equal(encoding, clone);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { Encoding.GetEncoding("latin1"), Encoding.GetEncoding("latin1"), true };
            yield return new object[] { Encoding.GetEncoding("latin1"), Encoding.GetEncoding("iso-8859-1"), true };

            yield return new object[] { Encoding.GetEncoding("latin1"), new object(), false };
            yield return new object[] { Encoding.GetEncoding("latin1"), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(Encoding encoding, object value, bool expected)
        {
            Assert.Equal(expected, encoding.Equals(value));
            Assert.Equal(expected, encoding.GetHashCode().Equals(value?.GetHashCode()));
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void GetPreamble(Encoding encoding)
        {
            Assert.Empty(encoding.GetPreamble());
        }
    }
}
