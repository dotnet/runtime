// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF32EncodingTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            UTF32Encoding encoding = new UTF32Encoding();
            VerifyUtf32Encoding(encoding, bigEndian: false, byteOrderMark: true, throwOnInvalidBytes: false);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void Ctor_Bool_Bool(bool bigEndian, bool byteOrderMark)
        {
            UTF32Encoding encoding = new UTF32Encoding(bigEndian, byteOrderMark);
            VerifyUtf32Encoding(encoding, bigEndian, byteOrderMark, throwOnInvalidBytes: false);

            Ctor_Bool_Bool_Bool(bigEndian, byteOrderMark, throwOnInvalidBytes: true);
            Ctor_Bool_Bool_Bool(bigEndian, byteOrderMark, throwOnInvalidBytes: false);
        }

        public void Ctor_Bool_Bool_Bool(bool bigEndian, bool byteOrderMark, bool throwOnInvalidBytes)
        {
            UTF32Encoding encoding = new UTF32Encoding(bigEndian, byteOrderMark, throwOnInvalidBytes);
            VerifyUtf32Encoding(encoding, bigEndian, byteOrderMark, throwOnInvalidBytes);
        }

        public static void VerifyUtf32Encoding(UTF32Encoding encoding, bool bigEndian, bool byteOrderMark, bool throwOnInvalidBytes)
        {
            if (byteOrderMark)
            {
                if (bigEndian)
                {
                    Assert.Equal(new byte[] { 0, 0, 254, 255 }, encoding.GetPreamble());
                }
                else
                {
                    Assert.Equal(new byte[] { 255, 254, 0, 0 }, encoding.GetPreamble());
                }
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
            yield return new object[] { new UTF32Encoding(true, true, true), "utf-32BE" };
            yield return new object[] { new UTF32Encoding(true, true, false), "utf-32BE" };
            yield return new object[] { new UTF32Encoding(true, false, true), "utf-32BE" };
            yield return new object[] { new UTF32Encoding(true, false, false), "utf-32BE" };
            yield return new object[] { new UTF32Encoding(false, true, true), "utf-32" };
            yield return new object[] { new UTF32Encoding(false, true, false), "utf-32" };
            yield return new object[] { new UTF32Encoding(false, false, true), "utf-32" };
            yield return new object[] { new UTF32Encoding(false, false, false), "utf-32" };

            yield return new object[] { Encoding.UTF32, "utf-32" };
            yield return new object[] { Encoding.GetEncoding("utf-32"), "utf-32" };
            yield return new object[] { Encoding.GetEncoding("utf-32LE"), "utf-32" };
            yield return new object[] { Encoding.GetEncoding("utf-32BE"), "utf-32BE" };
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void WebName(UTF32Encoding encoding, string webName)
        {
            Assert.Equal(webName, encoding.WebName);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void CodePage(UTF32Encoding encoding, string webName)
        {
            Assert.Equal(webName == "utf-32" ? 12000 : 12001, encoding.CodePage);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void EncodingName(UTF32Encoding encoding, string _)
        {
            Assert.NotEmpty(encoding.EncodingName); // Unicode (UTF-32) in en-US
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void IsSingleByte(UTF32Encoding encoding, string _)
        {
            Assert.False(encoding.IsSingleByte);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void Clone(UTF32Encoding encoding, string _)
        {
            UTF32Encoding clone = (UTF32Encoding)encoding.Clone();
            Assert.NotSame(encoding, clone);
            Assert.Equal(encoding, clone);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new UTF32Encoding(), new UTF32Encoding(false, true, false), true };
            yield return new object[] { new UTF32Encoding(), new UTF32Encoding(false, false, false), false };
            yield return new object[] { new UTF32Encoding(), new UTF32Encoding(true, true, false), false };
            yield return new object[] { new UTF32Encoding(), new UTF32Encoding(false, true, true), false };

            yield return new object[] { Encoding.UTF32, Encoding.UTF32, true };
            yield return new object[] { Encoding.UTF32, new UTF32Encoding(false, true), true };
            yield return new object[] { Encoding.UTF32, Encoding.GetEncoding("utf-32"), true };
            yield return new object[] { Encoding.UTF32, Encoding.GetEncoding("utf-32LE"), true };
            yield return new object[] { Encoding.UTF32, Encoding.GetEncoding("utf-32BE"), false };

            yield return new object[] { Encoding.GetEncoding("utf-32BE"), new UTF32Encoding(true, true), true };
            yield return new object[] { Encoding.GetEncoding("utf-32BE"), Encoding.GetEncoding("utf-32"), false };

            yield return new object[] { new UTF32Encoding(), new object(), false };
            yield return new object[] { new UTF32Encoding(), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(UTF32Encoding encoding, object value, bool expected)
        {
            Assert.Equal(expected, encoding.Equals(value));
            Assert.Equal(expected, encoding.GetHashCode().Equals(value?.GetHashCode()));
        }
    }
}
