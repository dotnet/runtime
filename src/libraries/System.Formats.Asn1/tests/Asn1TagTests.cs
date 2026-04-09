// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests
{
    public sealed class Asn1TagTests
    {
        [Fact]
        public static void Universal15UndefinedFromEnum()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "universalTagNumber",
                () => new Asn1Tag((UniversalTagNumber)15));
        }

        [Fact]
        public static void Universal15OKFromVerbose()
        {
            Asn1Tag tag = new Asn1Tag(TagClass.Universal, 15);
            Span<byte> encoded = stackalloc byte[1];

            Assert.Equal(1, tag.CalculateEncodedSize());
            tag.Encode(encoded);

            Assert.Equal("0F", encoded.ByteArrayToHex());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-2)]
        [InlineData(int.MinValue)]
        [InlineData(37)]
        [InlineData(38)]
        [InlineData(int.MaxValue)]
        public static void UniversalValuesLimitedToEnum(int value)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "universalTagNumber",
                () => new Asn1Tag((UniversalTagNumber)value));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-2)]
        [InlineData(int.MinValue)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(int.MaxValue)]
        public static void TagClassIsVerified(int value)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "tagClass",
                () => new Asn1Tag((TagClass)value, 1));
        }

        [Theory]
        [InlineData(TagClass.Universal, -1)]
        [InlineData(TagClass.ContextSpecific, -1)]
        [InlineData(TagClass.Application, -1)]
        [InlineData(TagClass.Private, -1)]
        [InlineData(TagClass.Universal, -2)]
        [InlineData(TagClass.ContextSpecific, -2)]
        [InlineData(TagClass.Application, -2)]
        [InlineData(TagClass.Private, -2)]
        [InlineData(TagClass.Universal, int.MinValue)]
        [InlineData(TagClass.ContextSpecific, int.MinValue)]
        [InlineData(TagClass.Application, int.MinValue)]
        [InlineData(TagClass.Private, int.MinValue)]
        public static void NoNegativeTagNumbers(TagClass tagClass, int value)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "tagValue",
                () => new Asn1Tag(tagClass, value));
        }

        [Fact]
        public static void EqualsIsExact()
        {
            Assert.False(Asn1Tag.PrimitiveOctetString.Equals(Asn1Tag.ConstructedOctetString));
            Assert.False(Asn1Tag.PrimitiveOctetString.Equals((object)Asn1Tag.ConstructedOctetString));
            Assert.True(Asn1Tag.PrimitiveOctetString.Equals(Asn1Tag.PrimitiveOctetString));
            Assert.True(Asn1Tag.PrimitiveOctetString.Equals((object)Asn1Tag.PrimitiveOctetString));
        }

        [Fact]
        public static void HasSameClassAndValueIsSoft()
        {
            Assert.True(Asn1Tag.PrimitiveOctetString.HasSameClassAndValue(Asn1Tag.ConstructedOctetString));
            Assert.True(Asn1Tag.PrimitiveOctetString.HasSameClassAndValue(Asn1Tag.PrimitiveOctetString));
            Assert.False(Asn1Tag.PrimitiveOctetString.HasSameClassAndValue(Asn1Tag.PrimitiveBitString));
            Assert.False(Asn1Tag.PrimitiveOctetString.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 4)));
        }

        public static IEnumerable<object[]> EncodeDecodeCases { get; } =
            new[]
            {
                new object[] { TagClass.Universal, 4, false, "04" },
                new object[] { TagClass.Universal, 4, true, "24" },
                new object[] { TagClass.Application, 4, false, "44" },
                new object[] { TagClass.Application, 4, true, "64" },
                new object[] { TagClass.ContextSpecific, 4, false, "84" },
                new object[] { TagClass.ContextSpecific, 4, true, "A4" },
                new object[] { TagClass.Private, 4, false, "C4" },
                new object[] { TagClass.Private, 4, true, "E4" },
                new object[] { TagClass.Universal, 30, false, "1E" },
                new object[] { TagClass.Universal, 30, true, "3E" },
                new object[] { TagClass.Application, 30, false, "5E" },
                new object[] { TagClass.Application, 30, true, "7E" },
                new object[] { TagClass.ContextSpecific, 30, false, "9E" },
                new object[] { TagClass.ContextSpecific, 30, true, "BE" },
                new object[] { TagClass.Private, 30, false, "DE" },
                new object[] { TagClass.Private, 30, true, "FE" },
                new object[] { TagClass.Universal, 31, false, "1F1F" },
                new object[] { TagClass.Universal, 31, true, "3F1F" },
                new object[] { TagClass.Application, 31, false, "5F1F" },
                new object[] { TagClass.Application, 31, true, "7F1F" },
                new object[] { TagClass.ContextSpecific, 31, false, "9F1F" },
                new object[] { TagClass.ContextSpecific, 31, true, "BF1F" },
                new object[] { TagClass.Private, 31, false, "DF1F" },
                new object[] { TagClass.Private, 31, true, "FF1F" },
                new object[] { TagClass.Universal, 127, false, "1F7F" },
                new object[] { TagClass.Universal, 127, true, "3F7F" },
                new object[] { TagClass.Application, 127, false, "5F7F" },
                new object[] { TagClass.Application, 127, true, "7F7F" },
                new object[] { TagClass.ContextSpecific, 127, false, "9F7F" },
                new object[] { TagClass.ContextSpecific, 127, true, "BF7F" },
                new object[] { TagClass.Private, 127, false, "DF7F" },
                new object[] { TagClass.Private, 127, true, "FF7F" },
                new object[] { TagClass.Universal, 128, false, "1F8100" },
                new object[] { TagClass.Universal, 128, true, "3F8100" },
                new object[] { TagClass.Application, 128, false, "5F8100" },
                new object[] { TagClass.Application, 128, true, "7F8100" },
                new object[] { TagClass.ContextSpecific, 128, false, "9F8100" },
                new object[] { TagClass.ContextSpecific, 128, true, "BF8100" },
                new object[] { TagClass.Private, 128, false, "DF8100" },
                new object[] { TagClass.Private, 128, true, "FF8100" },
                new object[] { TagClass.Universal, 16383, false, "1FFF7F" },
                new object[] { TagClass.Universal, 16383, true, "3FFF7F" },
                new object[] { TagClass.Application, 16383, false, "5FFF7F" },
                new object[] { TagClass.Application, 16383, true, "7FFF7F" },
                new object[] { TagClass.ContextSpecific, 16383, false, "9FFF7F" },
                new object[] { TagClass.ContextSpecific, 16383, true, "BFFF7F" },
                new object[] { TagClass.Private, 16383, false, "DFFF7F" },
                new object[] { TagClass.Private, 16383, true, "FFFF7F" },
                new object[] { TagClass.Universal, 16384, false, "1F818000" },
                new object[] { TagClass.Universal, 16384, true, "3F818000" },
                new object[] { TagClass.Application, 16384, false, "5F818000" },
                new object[] { TagClass.Application, 16384, true, "7F818000" },
                new object[] { TagClass.ContextSpecific, 16384, false, "9F818000" },
                new object[] { TagClass.ContextSpecific, 16384, true, "BF818000" },
                new object[] { TagClass.Private, 16384, false, "DF818000" },
                new object[] { TagClass.Private, 16384, true, "FF818000" },
                new object[] { TagClass.Universal, int.MaxValue, false, "1F87FFFFFF7F" },
                new object[] { TagClass.Universal, int.MaxValue, true, "3F87FFFFFF7F" },
                new object[] { TagClass.Application, int.MaxValue, false, "5F87FFFFFF7F" },
                new object[] { TagClass.Application, int.MaxValue, true, "7F87FFFFFF7F" },
                new object[] { TagClass.ContextSpecific, int.MaxValue, false, "9F87FFFFFF7F" },
                new object[] { TagClass.ContextSpecific, int.MaxValue, true, "BF87FFFFFF7F" },
                new object[] { TagClass.Private, int.MaxValue, false, "DF87FFFFFF7F" },
                new object[] { TagClass.Private, int.MaxValue, true, "FF87FFFFFF7F" },
            };

        [Theory]
        [MemberData(nameof(EncodeDecodeCases))]
        public static void VerifyEncode(TagClass tagClass, int tagValue, bool constructed, string expectedHex)
        {
            Asn1Tag tag = new Asn1Tag(tagClass, tagValue, constructed);
            Span<byte> buf = stackalloc byte[10];

            Assert.False(tag.TryEncode(Span<byte>.Empty, out int written));
            Assert.Equal(0, written);

            int expectedSize = expectedHex.Length / 2;
            Assert.Equal(expectedSize, tag.CalculateEncodedSize());

            Assert.False(tag.TryEncode(buf.Slice(0, expectedSize - 1), out written));
            Assert.Equal(0, written);

            AssertExtensions.Throws<ArgumentException>(
                "destination",
                () =>
                {
                    Span<byte> tmp = stackalloc byte[expectedSize - 1];
                    return tag.Encode(tmp);
                });

            Assert.True(tag.TryEncode(buf, out written));
            Assert.Equal(expectedSize, written);
            Assert.Equal(expectedHex, buf.Slice(0, written).ByteArrayToHex());

            written = tag.Encode(buf.Slice(1));
            Assert.Equal(expectedSize, written);
            Assert.Equal(expectedHex, buf.Slice(1, written).ByteArrayToHex());

            Assert.True(tag.TryEncode(buf.Slice(0, expectedSize), out written));
            Assert.Equal(expectedSize, written);
            Assert.Equal(expectedHex, buf.Slice(0, written).ByteArrayToHex());

            written = tag.Encode(buf.Slice(1, expectedSize));
            Assert.Equal(expectedSize, written);
            Assert.Equal(expectedHex, buf.Slice(1, written).ByteArrayToHex());
        }

        [Theory]
        [MemberData(nameof(EncodeDecodeCases))]
        public static void VerifyDecode(TagClass tagClass, int tagValue, bool constructed, string inputHex)
        {
            Asn1Tag expectedTag = new Asn1Tag(tagClass, tagValue, constructed);
            byte[] input = inputHex.HexToByteArray();
            byte[] padded = input;
            Array.Resize(ref padded, input.Length + 3);

            int consumed;
            Asn1Tag tag;

            Assert.False(Asn1Tag.TryDecode(input.AsSpan(0, input.Length - 1), out tag, out consumed));
            Assert.Equal(0, consumed);
            Assert.Equal(default(Asn1Tag), tag);

            Assert.Throws<AsnContentException>(() => Asn1Tag.Decode(input.AsSpan(0, input.Length - 1), out consumed));
            Assert.Equal(0, consumed);

            Assert.True(Asn1Tag.TryDecode(padded, out tag, out consumed));
            Assert.Equal(input.Length, consumed);
            Assert.Equal(expectedTag, tag);

            Assert.True(Asn1Tag.TryDecode(input, out tag, out consumed));
            Assert.Equal(input.Length, consumed);
            Assert.Equal(expectedTag, tag);

            tag = Asn1Tag.Decode(padded, out consumed);
            Assert.Equal(input.Length, consumed);
            Assert.Equal(expectedTag, tag);

            tag = Asn1Tag.Decode(input, out consumed);
            Assert.Equal(input.Length, consumed);
            Assert.Equal(expectedTag, tag);
        }
    }
}
