// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingTests
    {
        [Fact]
        public void Ctor()
        {
            ASCIIEncoding encoding = new ASCIIEncoding();
            Assert.Equal(new EncoderReplacementFallback("?"), encoding.EncoderFallback);
            Assert.Equal(new DecoderReplacementFallback("?"), encoding.DecoderFallback);
        }

        public static IEnumerable<object[]> Encodings_TestData()
        {
            yield return new object[] { new ASCIIEncoding() };
            yield return new object[] { Encoding.ASCII };
            yield return new object[] { Encoding.GetEncoding("ascii") };
            yield return new object[] { Encoding.GetEncoding("us-ascii") };
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void WebName(ASCIIEncoding encoding)
        {
            Assert.Equal("us-ascii", encoding.WebName);
        }
        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void CodePage(ASCIIEncoding encoding)
        {
            Assert.Equal(20127, encoding.CodePage);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void EncodingName(ASCIIEncoding encoding)
        {
            Assert.NotEmpty(encoding.EncodingName); // US-ASCII in en-US
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void IsSingleByte(ASCIIEncoding encoding)
        {
            Assert.True(encoding.IsSingleByte);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void Clone(ASCIIEncoding encoding)
        {
            ASCIIEncoding clone = (ASCIIEncoding)encoding.Clone();
            Assert.NotSame(encoding, clone);
            Assert.Equal(encoding, clone);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new ASCIIEncoding(), new ASCIIEncoding(), true };
            yield return new object[] { Encoding.ASCII, Encoding.ASCII, true };
            yield return new object[] { Encoding.ASCII, new ASCIIEncoding(), true };
            yield return new object[] { Encoding.ASCII, Encoding.GetEncoding("ascii"), true };
            yield return new object[] { Encoding.ASCII, Encoding.GetEncoding("us-ascii"), true };

            yield return new object[] { new ASCIIEncoding(), new object(), false };
            yield return new object[] { new ASCIIEncoding(), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(ASCIIEncoding encoding, object value, bool expected)
        {
            Assert.Equal(expected, encoding.Equals(value));
            Assert.Equal(expected, encoding.GetHashCode().Equals(value?.GetHashCode()));
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void GetPreamble(ASCIIEncoding encoding)
        {
            Assert.Empty(encoding.GetPreamble());
        }
    }
}
