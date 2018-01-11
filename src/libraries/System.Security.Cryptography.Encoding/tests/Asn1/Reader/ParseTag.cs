// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public sealed class ParseTag : Asn1ReaderTests
    {
        [Theory]
        [InlineData(PublicTagClass.Universal, false, 0, "00")]
        [InlineData(PublicTagClass.Universal, false, 1, "01")]
        [InlineData(PublicTagClass.Application, true, 1, "61")]
        [InlineData(PublicTagClass.ContextSpecific, false, 1, "81")]
        [InlineData(PublicTagClass.ContextSpecific, true, 1, "A1")]
        [InlineData(PublicTagClass.Private, false, 1, "C1")]
        [InlineData(PublicTagClass.Universal, false, 30, "1E")]
        [InlineData(PublicTagClass.Application, false, 30, "5E")]
        [InlineData(PublicTagClass.ContextSpecific, false, 30, "9E")]
        [InlineData(PublicTagClass.Private, false, 30, "DE")]
        [InlineData(PublicTagClass.Universal, false, 31, "1F1F")]
        [InlineData(PublicTagClass.Application, false, 31, "5F1F")]
        [InlineData(PublicTagClass.ContextSpecific, false, 31, "9F1F")]
        [InlineData(PublicTagClass.Private, false, 31, "DF1F")]
        [InlineData(PublicTagClass.Private, false, 127, "DF7F")]
        [InlineData(PublicTagClass.Private, false, 128, "DF8100")]
        [InlineData(PublicTagClass.Private, false, 253, "DF817D")]
        [InlineData(PublicTagClass.Private, false, 255, "DF817F")]
        [InlineData(PublicTagClass.Private, false, 256, "DF8200")]
        [InlineData(PublicTagClass.Private, false, 1 << 9, "DF8400")]
        [InlineData(PublicTagClass.Private, false, 1 << 10, "DF8800")]
        [InlineData(PublicTagClass.Private, false, 0b0011_1101_1110_0111, "DFFB67")]
        [InlineData(PublicTagClass.Private, false, 1 << 14, "DF818000")]
        [InlineData(PublicTagClass.Private, false, 1 << 18, "DF908000")]
        [InlineData(PublicTagClass.Private, false, 1 << 18 | 1 << 9, "DF908400")]
        [InlineData(PublicTagClass.Private, false, 1 << 20, "DFC08000")]
        [InlineData(PublicTagClass.Private, false, 0b0001_1110_1010_0111_0000_0001, "DFFACE01")]
        [InlineData(PublicTagClass.Private, false, 1 << 21, "DF81808000")]
        [InlineData(PublicTagClass.Private, false, 1 << 27, "DFC0808000")]
        [InlineData(PublicTagClass.Private, false, 1 << 28, "DF8180808000")]
        [InlineData(PublicTagClass.Private, true, int.MaxValue, "FF87FFFFFF7F")]
        [InlineData(PublicTagClass.Universal, false, 119, "1F77")]
        public static void ParseValidTag(
            PublicTagClass tagClass,
            bool isConstructed,
            int tagValue,
            string inputHex)
        {
            byte[] inputBytes = inputHex.HexToByteArray();

            bool parsed = Asn1Tag.TryParse(inputBytes, out Asn1Tag tag, out int bytesRead);

            Assert.True(parsed, "Asn1Tag.TryParse");
            Assert.Equal(inputBytes.Length, bytesRead);
            Assert.Equal((TagClass)tagClass, tag.TagClass);
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
            Assert.False(tag.TryWrite(secondBytes.AsSpan().Slice(0, inputBytes.Length - 1), out written));
            Assert.Equal(0, written);
            Assert.True(tag.TryWrite(secondBytes, out written));
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
            byte[] inputBytes = inputHex.HexToByteArray();

            Assert.False(Asn1Tag.TryParse(inputBytes, out Asn1Tag tag, out var bytesRead));

            Assert.Equal(default(Asn1Tag), tag);
            Assert.Equal(0, bytesRead);
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
        [InlineData(PublicTagClass.Universal, false, 0, "00")]
        [InlineData(PublicTagClass.ContextSpecific, true, 1, "A1")]
        [InlineData(PublicTagClass.Application, false, 31, "5F1F")]
        [InlineData(PublicTagClass.Private, false, 128, "DF8100")]
        [InlineData(PublicTagClass.Private, false, 0b0001_1110_1010_0111_0000_0001, "DFFACE01")]
        [InlineData(PublicTagClass.Private, true, int.MaxValue, "FF87FFFFFF7F")]
        public static void ParseTagWithMoreData(
            PublicTagClass tagClass,
            bool isConstructed,
            int tagValue,
            string inputHex)
        {
            byte[] inputBytes = inputHex.HexToByteArray();
            Array.Resize(ref inputBytes, inputBytes.Length + 3);

            bool parsed = Asn1Tag.TryParse(inputBytes, out Asn1Tag tag, out int bytesRead);

            Assert.True(parsed, "Asn1Tag.TryParse");
            Assert.Equal(inputHex.Length / 2, bytesRead);
            Assert.Equal((TagClass)tagClass, tag.TagClass);
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
