// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ParseTag
    {
        [Theory]
        [InlineData(TagClass.Universal, false, 0, "00")]
        [InlineData(TagClass.Universal, false, 1, "01")]
        [InlineData(TagClass.Application, true, 1, "61")]
        [InlineData(TagClass.ContextSpecific, false, 1, "81")]
        [InlineData(TagClass.ContextSpecific, true, 1, "A1")]
        [InlineData(TagClass.Private, false, 1, "C1")]
        [InlineData(TagClass.Universal, false, 30, "1E")]
        [InlineData(TagClass.Application, false, 30, "5E")]
        [InlineData(TagClass.ContextSpecific, false, 30, "9E")]
        [InlineData(TagClass.Private, false, 30, "DE")]
        [InlineData(TagClass.Universal, false, 31, "1F1F")]
        [InlineData(TagClass.Application, false, 31, "5F1F")]
        [InlineData(TagClass.ContextSpecific, false, 31, "9F1F")]
        [InlineData(TagClass.Private, false, 31, "DF1F")]
        [InlineData(TagClass.Private, false, 127, "DF7F")]
        [InlineData(TagClass.Private, false, 128, "DF8100")]
        [InlineData(TagClass.Private, false, 253, "DF817D")]
        [InlineData(TagClass.Private, false, 255, "DF817F")]
        [InlineData(TagClass.Private, false, 256, "DF8200")]
        [InlineData(TagClass.Private, false, 1 << 9, "DF8400")]
        [InlineData(TagClass.Private, false, 1 << 10, "DF8800")]
        [InlineData(TagClass.Private, false, 0b0011_1101_1110_0111, "DFFB67")]
        [InlineData(TagClass.Private, false, 1 << 14, "DF818000")]
        [InlineData(TagClass.Private, false, 1 << 18, "DF908000")]
        [InlineData(TagClass.Private, false, 1 << 18 | 1 << 9, "DF908400")]
        [InlineData(TagClass.Private, false, 1 << 20, "DFC08000")]
        [InlineData(TagClass.Private, false, 0b0001_1110_1010_0111_0000_0001, "DFFACE01")]
        [InlineData(TagClass.Private, false, 1 << 21, "DF81808000")]
        [InlineData(TagClass.Private, false, 1 << 27, "DFC0808000")]
        [InlineData(TagClass.Private, false, 1 << 28, "DF8180808000")]
        [InlineData(TagClass.Private, true, int.MaxValue, "FF87FFFFFF7F")]
        [InlineData(TagClass.Universal, false, 119, "1F77")]
        public static void ParseValidTag(
            TagClass tagClass,
            bool isConstructed,
            int tagValue,
            string inputHex)
        {
            byte[] inputBytes = inputHex.HexToByteArray();

            bool parsed = Asn1Tag.TryDecode(inputBytes, out Asn1Tag tag, out int bytesRead);

            Assert.True(parsed, "Asn1Tag.TryDecode");
            Assert.Equal(inputBytes.Length, bytesRead);
            Assert.Equal(tagClass, tag.TagClass);
            Assert.Equal(tagValue, tag.TagValue);

            if (isConstructed)
            {
                Assert.True(tag.IsConstructed, "tag.IsConstructed");
            }
            else
            {
                Assert.False(tag.IsConstructed, "tag.IsConstructed");
            }

            byte[] secondBytes = new byte[inputBytes.Length];
            int written;
            Assert.False(tag.TryEncode(secondBytes.AsSpan(0, inputBytes.Length - 1), out written));
            Assert.Equal(0, written);
            Assert.True(tag.TryEncode(secondBytes, out written));
            Assert.Equal(inputBytes.Length, written);
            Assert.Equal(inputHex, secondBytes.ByteArrayToHex());
        }

        [Theory]
        [InlineData("Empty", "")]
        [InlineData("MultiByte-NoFollow", "1F")]
        [InlineData("MultiByte-NoFollow2", "1F81")]
        [InlineData("MultiByte-NoFollow3", "1F8180")]
        [InlineData("MultiByte-TooLow", "1F01")]
        [InlineData("MultiByte-TooLowMax", "1F1E")]
        [InlineData("MultiByte-Leading0", "1F807F")]
        [InlineData("MultiByte-ValueTooBig", "FF8880808000")]
        [InlineData("MultiByte-ValueSubtlyTooBig", "DFC1C0808000")]
        public static void ParseCorruptTag(string description, string inputHex)
        {
            _ = description;
            byte[] inputBytes = inputHex.HexToByteArray();

            Assert.False(Asn1Tag.TryDecode(inputBytes, out Asn1Tag tag, out var bytesRead));

            Assert.Equal(default(Asn1Tag), tag);
            Assert.Equal(0, bytesRead);

            Assert.False(
                AsnDecoder.TryReadEncodedValue(
                    inputBytes,
                    AsnEncodingRules.BER,
                    out tag,
                    out int contentOffset,
                    out int contentLength,
                    out int bytesConsumed));

            Assert.Equal(0, contentOffset);
            Assert.Equal(0, contentLength);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(default(Asn1Tag), tag);
        }

        [Fact]
        public static void TestEquals()
        {
            Asn1Tag integer = new Asn1Tag(TagClass.Universal, 2);
            Asn1Tag integerAgain = new Asn1Tag(TagClass.Universal, 2);
            Asn1Tag context2 = new Asn1Tag(TagClass.ContextSpecific, 2);
            Asn1Tag constructedContext2 = new Asn1Tag(TagClass.ContextSpecific, 2, true);
            Asn1Tag application2 = new Asn1Tag(TagClass.Application, 2);

            Assert.False(integer.Equals(null));
            Assert.False(integer.Equals(0x02));
            Assert.False(integer.Equals(context2));
            Assert.False(context2.Equals(constructedContext2));
            Assert.False(context2.Equals(application2));

            Assert.Equal(integer, integerAgain);
            Assert.True(integer == integerAgain);
            Assert.True(integer != context2);
            Assert.False(integer == context2);
            Assert.False(context2 == constructedContext2);
            Assert.False(context2 == application2);

            Assert.NotEqual(integer.GetHashCode(), context2.GetHashCode());
            Assert.NotEqual(context2.GetHashCode(), constructedContext2.GetHashCode());
            Assert.NotEqual(context2.GetHashCode(), application2.GetHashCode());
            Assert.Equal(integer.GetHashCode(), integerAgain.GetHashCode());
        }

        [Theory]
        [InlineData(TagClass.Universal, false, 0, "00")]
        [InlineData(TagClass.ContextSpecific, true, 1, "A1")]
        [InlineData(TagClass.Application, false, 31, "5F1F")]
        [InlineData(TagClass.Private, false, 128, "DF8100")]
        [InlineData(TagClass.Private, false, 0b0001_1110_1010_0111_0000_0001, "DFFACE01")]
        [InlineData(TagClass.Private, true, int.MaxValue, "FF87FFFFFF7F")]
        public static void ParseTagWithMoreData(
            TagClass tagClass,
            bool isConstructed,
            int tagValue,
            string inputHex)
        {
            byte[] inputBytes = inputHex.HexToByteArray();
            Array.Resize(ref inputBytes, inputBytes.Length + 3);

            bool parsed = Asn1Tag.TryDecode(inputBytes, out Asn1Tag tag, out int bytesRead);

            Assert.True(parsed, "Asn1Tag.TryDecode");
            Assert.Equal(inputHex.Length / 2, bytesRead);
            Assert.Equal(tagClass, tag.TagClass);
            Assert.Equal(tagValue, tag.TagValue);

            if (isConstructed)
            {
                Assert.True(tag.IsConstructed, "tag.IsConstructed");
            }
            else
            {
                Assert.False(tag.IsConstructed, "tag.IsConstructed");
            }
        }
    }
}
