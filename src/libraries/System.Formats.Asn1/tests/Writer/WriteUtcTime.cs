// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class WriteUtcTime : Asn1WriterTests
    {
        public static IEnumerable<object[]> TestCases { get; } = new object[][]
        {
            new object[]
            {
                new DateTimeOffset(2017, 10, 16, 8, 24, 3, TimeSpan.FromHours(-7)),
                "0D3137313031363135323430335A",
            },
            new object[]
            {
                new DateTimeOffset(1817, 10, 16, 21, 24, 3, TimeSpan.FromHours(6)),
                "0D3137313031363135323430335A",
            },
            new object[]
            {
                new DateTimeOffset(3000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                "0D3030303130313030303030305A",
            },
        };

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteUtcTime_BER(DateTimeOffset input, string expectedHexPayload)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            writer.WriteUtcTime(input);

            Verify(writer, "17" + expectedHexPayload);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteUtcTime_BER_CustomTag(DateTimeOffset input, string expectedHexPayload)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            Asn1Tag tag = new Asn1Tag(TagClass.Application, 11);
            writer.WriteUtcTime(input, tag);

            Verify(writer, Stringify(tag) + expectedHexPayload);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteUtcTime_CER(DateTimeOffset input, string expectedHexPayload)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.CER);
            writer.WriteUtcTime(input);

            Verify(writer, "17" + expectedHexPayload);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteUtcTime_CER_CustomTag(DateTimeOffset input, string expectedHexPayload)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.CER);
            Asn1Tag tag = new Asn1Tag(TagClass.Private, 95);
            writer.WriteUtcTime(input, tag);

            Verify(writer, Stringify(tag) + expectedHexPayload);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteUtcTime_DER(DateTimeOffset input, string expectedHexPayload)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteUtcTime(input);

            Verify(writer, "17" + expectedHexPayload);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteUtcTime_DER_CustomTag(DateTimeOffset input, string expectedHexPayload)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 3);
            writer.WriteUtcTime(input, tag);

            Verify(writer, Stringify(tag) + expectedHexPayload);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWriteUtcTime_Null(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteUtcTime(DateTimeOffset.Now, Asn1Tag.Null));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWriteUtcTime_IgnoresConstructed(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            DateTimeOffset value = new DateTimeOffset(2017, 11, 16, 17, 35, 1, TimeSpan.Zero);

            writer.WriteUtcTime(value, new Asn1Tag(UniversalTagNumber.UtcTime, true));
            writer.WriteUtcTime(value, new Asn1Tag(TagClass.ContextSpecific, 3, true));
            Verify(writer, "170D3137313131363137333530315A" + "830D3137313131363137333530315A");
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteUtcTime_RespectsYearMax_DER(DateTimeOffset input, string expectedHexPayload)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            Assert.Equal(0, writer.GetEncodedLength());

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "value",
                () => writer.WriteUtcTime(input, input.Year - 1));

            Assert.Equal(0, writer.GetEncodedLength());

            writer.WriteUtcTime(input, input.Year);
            Assert.Equal(15, writer.GetEncodedLength());

            writer.WriteUtcTime(input, input.Year + 99);
            Assert.Equal(30, writer.GetEncodedLength());

            writer.Reset();

            _ = expectedHexPayload;
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "value",
                () => writer.WriteUtcTime(input, input.Year + 100));

            Assert.Equal(0, writer.GetEncodedLength());
        }

        [Fact]
        public void VerifyWriteUtcTime_RespectsYearMax_UniversalLimit()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.CER);
            Asn1Tag tag = new Asn1Tag(TagClass.Private, 11);

            // 1950 after ToUniversal
            writer.WriteUtcTime(
                new DateTimeOffset(1949, 12, 31, 23, 11, 19, TimeSpan.FromHours(-8)),
                2049,
                tag);

            Assert.Equal(15, writer.GetEncodedLength());

            // 1949 after ToUniversal
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "value",
                () =>
                    writer.WriteUtcTime(
                        new DateTimeOffset(1950, 1, 1, 3, 11, 19, TimeSpan.FromHours(8)),
                        2049,
                        tag));

            Assert.Equal(15, writer.GetEncodedLength());

            // 2050 after ToUniversal
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "value",
                () =>
                    writer.WriteUtcTime(
                        new DateTimeOffset(2049, 12, 31, 23, 11, 19, TimeSpan.FromHours(-8)),
                        2049,
                        tag));

            Assert.Equal(15, writer.GetEncodedLength());

            // 1950 after ToUniversal
            writer.WriteUtcTime(
                new DateTimeOffset(2050, 1, 1, 3, 11, 19, TimeSpan.FromHours(8)),
                2049,
                tag);

            Assert.Equal(30, writer.GetEncodedLength());

            string hex =
                Stringify(tag) + "0D3530303130313037313131395A" +
                Stringify(tag) + "0D3439313233313139313131395A";

            Verify(writer, hex);
        }
    }
}
