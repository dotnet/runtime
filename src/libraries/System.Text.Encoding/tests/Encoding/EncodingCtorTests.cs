// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class EncodingCtorTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            CustomEncoding encoding = new CustomEncoding();
            VerifyEncoding(encoding, 0, null, null);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(int.MaxValue)]
        public void Ctor_Int(int codePage)
        {
            CustomEncoding encoding = new CustomEncoding(codePage);
            VerifyEncoding(encoding, codePage, null, null);
        }

        public static IEnumerable<object[]> Ctor_Int_EncoderFallback_DecoderFallback_TestData()
        {
            yield return new object[] { 10, null, null };
            yield return new object[] { int.MaxValue, new EncoderReplacementFallback("abc"), null };
            yield return new object[] { 0, null, new DecoderReplacementFallback("abc") };
            yield return new object[] { 65536, new EncoderReplacementFallback("abc"), new DecoderReplacementFallback("abc") };
        }

        [Theory]
        [MemberData(nameof(Ctor_Int_EncoderFallback_DecoderFallback_TestData))]
        public void Ctor_Int_EncoderFallback_DecoderFallback(int codePage, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
        {
            CustomEncoding encoding = new CustomEncoding(codePage, encoderFallback, decoderFallback);
            VerifyEncoding(encoding, codePage, encoderFallback, decoderFallback);
        }

        [Fact]
        public void Ctor_NegativeCodePage_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("codePage", () => new CustomEncoding(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("codePage", () => new CustomEncoding(-1, null, null));
        }

        [Fact]
        public void Ctor_Int_NoSuchCodePage_WebName_ThrowsNotSupportedException()
        {
            CustomEncoding encoding = new CustomEncoding(54321);
            Assert.Throws<NotSupportedException>(() => encoding.WebName);
        }

        public static void VerifyEncoding(Encoding encoding, int codePage, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
        {
            if (encoderFallback == null)
            {
                Assert.NotNull(encoding.EncoderFallback);
                Assert.Equal(codePage, encoding.EncoderFallback.GetHashCode());
            }
            else
            {
                Assert.Same(encoderFallback, encoding.EncoderFallback);
            }

            if (decoderFallback == null)
            {
                Assert.NotNull(encoding.DecoderFallback);
                Assert.Equal(codePage, encoding.DecoderFallback.GetHashCode());
            }
            else
            {
                Assert.Same(decoderFallback, encoding.DecoderFallback);
            }

            Assert.Empty(encoding.GetPreamble());
            Assert.False(encoding.IsSingleByte);
        }
    }

    public class CustomEncoding : Encoding
    {
        public CustomEncoding() : base() {}
        public CustomEncoding(int codePage): base(codePage) { }
        public CustomEncoding(int codePage, EncoderFallback encoderFallback, DecoderFallback decoderFallback) : base(codePage, encoderFallback, decoderFallback) { }

        public override int GetByteCount(char[] chars, int index, int count) => 1;

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) => 2;

        public override int GetCharCount(byte[] bytes, int index, int count) => 3;

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) => 4;

        public override int GetMaxByteCount(int charCount) => 5;

        public override int GetMaxCharCount(int byteCount) => 6;
    }
}
