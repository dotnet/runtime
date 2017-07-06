// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            UnicodeEncoding encoding = new UnicodeEncoding();
            VerifyUnicodeEncoding(encoding, bigEndian: false, byteOrderMark: true, throwOnInvalidBytes: false);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void Ctor_Bool_Bool(bool bigEndian, bool byteOrderMark)
        {
            UnicodeEncoding encoding = new UnicodeEncoding(bigEndian, byteOrderMark);
            VerifyUnicodeEncoding(encoding, bigEndian, byteOrderMark, throwOnInvalidBytes: false);

            Ctor_Bool_Bool_Bool(bigEndian, byteOrderMark, throwOnInvalidBytes: true);
            Ctor_Bool_Bool_Bool(bigEndian, byteOrderMark, throwOnInvalidBytes: false);
        }

        public void Ctor_Bool_Bool_Bool(bool bigEndian, bool byteOrderMark, bool throwOnInvalidBytes)
        {
            UnicodeEncoding encoding = new UnicodeEncoding(bigEndian, byteOrderMark, throwOnInvalidBytes);
            VerifyUnicodeEncoding(encoding, bigEndian, byteOrderMark, throwOnInvalidBytes);
        }

        public static void VerifyUnicodeEncoding(UnicodeEncoding encoding, bool bigEndian, bool byteOrderMark, bool throwOnInvalidBytes)
        {
            if (byteOrderMark)
            {
                if (bigEndian)
                {
                    Assert.Equal(new byte[] { 0xfe, 0xff }, encoding.GetPreamble());
                }
                else
                {
                    Assert.Equal(new byte[] { 0xff, 0xfe }, encoding.GetPreamble());
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
            yield return new object[] { new UnicodeEncoding(true, true, true), "utf-16BE" };
            yield return new object[] { new UnicodeEncoding(true, true, false), "utf-16BE" };
            yield return new object[] { new UnicodeEncoding(true, false, true), "utf-16BE" };
            yield return new object[] { new UnicodeEncoding(true, false, false), "utf-16BE" };
            yield return new object[] { new UnicodeEncoding(false, true, true), "utf-16" };
            yield return new object[] { new UnicodeEncoding(false, true, false), "utf-16" };
            yield return new object[] { new UnicodeEncoding(false, false, true), "utf-16" };
            yield return new object[] { new UnicodeEncoding(false, false, false), "utf-16" };

            yield return new object[] { Encoding.Unicode, "utf-16" };
            yield return new object[] { Encoding.GetEncoding("utf-16"), "utf-16" };
            yield return new object[] { Encoding.GetEncoding("utf-16LE"), "utf-16" };
            yield return new object[] { Encoding.GetEncoding("utf-16BE"), "utf-16BE" };
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void WebName(UnicodeEncoding encoding, string expected)
        {
            Assert.Equal(expected, encoding.WebName);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void CodePage(UnicodeEncoding encoding, string webName)
        {
            Assert.Equal(webName == "utf-16" ? 1200 : 1201, encoding.CodePage);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void EncodingName(UnicodeEncoding encoding, string _)
        {
            Assert.NotEmpty(encoding.EncodingName); // Unicode (UTF-16) in en-US
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void IsSingleByte(UnicodeEncoding encoding, string _)
        {
            Assert.False(encoding.IsSingleByte);
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void Clone(UnicodeEncoding encoding, string _)
        {
            UnicodeEncoding clone = (UnicodeEncoding)encoding.Clone();
            Assert.NotSame(encoding, clone);
            Assert.Equal(encoding, clone);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new UnicodeEncoding(), new UnicodeEncoding(), true };
            yield return new object[] { new UnicodeEncoding(), new UnicodeEncoding(false, true), true };
            yield return new object[] { new UnicodeEncoding(), new UnicodeEncoding(false, false), false };

            yield return new object[] { Encoding.Unicode, Encoding.Unicode, true };
            yield return new object[] { Encoding.Unicode, Encoding.GetEncoding("Unicode"), true };
            yield return new object[] { Encoding.Unicode, Encoding.GetEncoding("utf-16"), true };
            yield return new object[] { Encoding.Unicode, Encoding.GetEncoding("utf-16LE"), true };
            yield return new object[] { Encoding.Unicode, new UnicodeEncoding(false, true), true };
            yield return new object[] { Encoding.Unicode, new UnicodeEncoding(true, true), false };

            yield return new object[] { Encoding.BigEndianUnicode, Encoding.BigEndianUnicode, true };
            yield return new object[] { Encoding.BigEndianUnicode, Encoding.GetEncoding("utf-16BE"), true };
            yield return new object[] { Encoding.BigEndianUnicode, new UnicodeEncoding(true, true), true };
            yield return new object[] { Encoding.BigEndianUnicode, new UnicodeEncoding(false, true), false };

            yield return new object[] { new UnicodeEncoding(), new TimeSpan(), false };
            yield return new object[] { new UnicodeEncoding(), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(UnicodeEncoding encoding, object value, bool expected)
        {
            Assert.Equal(expected, encoding.Equals(value));
            Assert.Equal(expected, encoding.GetHashCode().Equals(value?.GetHashCode()));
        }
    }
}
