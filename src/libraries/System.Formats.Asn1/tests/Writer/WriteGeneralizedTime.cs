// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class WriteGeneralizedTime : Asn1WriterTests
    {
        public static IEnumerable<object[]> TestCases { get; } = new object[][]
        {
            new object[]
            {
                new DateTimeOffset(2017, 10, 16, 8, 24, 3, TimeSpan.FromHours(-7)),
                false,
                "0F32303137313031363135323430335A",
            },
            new object[]
            {
                new DateTimeOffset(1817, 10, 16, 21, 24, 3, TimeSpan.FromHours(6)),
                false,
                "0F31383137313031363135323430335A",
            },
            new object[]
            {
                new DateTimeOffset(3000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                false,
                "0F33303030303130313030303030305A",
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 999, TimeSpan.Zero),
                false,
                "1331393939313233313233353935392E3939395A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 999, TimeSpan.Zero),
                true,
                "0F31393939313233313233353935395A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 880, TimeSpan.Zero),
                false,
                "1231393939313233313233353935392E38385A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 880, TimeSpan.Zero),
                true,
                "0F31393939313233313233353935395A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 700, TimeSpan.Zero),
                false,
                "1131393939313233313233353935392E375A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 700, TimeSpan.Zero),
                true,
                "0F31393939313233313233353935395A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 123, TimeSpan.Zero) + TimeSpan.FromTicks(4567),
                false,
                "1731393939313233313233353935392E313233343536375A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 123, TimeSpan.Zero) + TimeSpan.FromTicks(4567),
                true,
                "0F31393939313233313233353935395A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 12, TimeSpan.Zero) + TimeSpan.FromTicks(3450),
                false,
                "1631393939313233313233353935392E3031323334355A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 12, TimeSpan.Zero) + TimeSpan.FromTicks(3450),
                true,
                "0F31393939313233313233353935395A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 1, TimeSpan.Zero) + TimeSpan.FromTicks(2300),
                false,
                "1531393939313233313233353935392E30303132335A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 1, TimeSpan.Zero) + TimeSpan.FromTicks(2300),
                true,
                "0F31393939313233313233353935395A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 0, TimeSpan.Zero) + TimeSpan.FromTicks(1000),
                false,
                "1431393939313233313233353935392E303030315A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, 0, TimeSpan.Zero) + TimeSpan.FromTicks(1000),
                true,
                "0F31393939313233313233353935395A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, TimeSpan.Zero) + TimeSpan.FromTicks(1),
                false,
                "1731393939313233313233353935392E303030303030315A"
            },
            new object[]
            {
                new DateTimeOffset(1999, 12, 31, 23, 59, 59, TimeSpan.Zero) + TimeSpan.FromTicks(1),
                true,
                "0F31393939313233313233353935395A"
            },
        };

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteGeneralizedTime_BER(
            DateTimeOffset input,
            bool omitFractionalSeconds,
            string expectedHexPayload)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            writer.WriteGeneralizedTime(input, omitFractionalSeconds: omitFractionalSeconds);

            Verify(writer, "18" + expectedHexPayload);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteGeneralizedTime_BER_CustomTag(
            DateTimeOffset input,
            bool omitFractionalSeconds,
            string expectedHexPayload)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            Asn1Tag tag = new Asn1Tag(TagClass.Application, 11);
            writer.WriteGeneralizedTime(input, omitFractionalSeconds, tag);

            Verify(writer, Stringify(tag) + expectedHexPayload);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteGeneralizedTime_CER(
            DateTimeOffset input,
            bool omitFractionalSeconds,
            string expectedHexPayload)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.CER);
            writer.WriteGeneralizedTime(input, omitFractionalSeconds);

            Verify(writer, "18" + expectedHexPayload);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteGeneralizedTime_CER_CustomTag(
            DateTimeOffset input,
            bool omitFractionalSeconds,
            string expectedHexPayload)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.CER);
            Asn1Tag tag = new Asn1Tag(TagClass.Private, 95);
            writer.WriteGeneralizedTime(input, omitFractionalSeconds, tag);

            Verify(writer, Stringify(tag) + expectedHexPayload);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteGeneralizedTime_DER(
            DateTimeOffset input,
            bool omitFractionalSeconds,
            string expectedHexPayload)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteGeneralizedTime(input, omitFractionalSeconds);

            Verify(writer, "18" + expectedHexPayload);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteGeneralizedTime_DER_CustomTag(
            DateTimeOffset input,
            bool omitFractionalSeconds,
            string expectedHexPayload)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 3);
            writer.WriteGeneralizedTime(input, omitFractionalSeconds, tag);

            Verify(writer, Stringify(tag) + expectedHexPayload);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, false)]
        [InlineData(AsnEncodingRules.CER, false)]
        [InlineData(AsnEncodingRules.DER, false)]
        [InlineData(AsnEncodingRules.BER, true)]
        [InlineData(AsnEncodingRules.CER, true)]
        [InlineData(AsnEncodingRules.DER, true)]
        public void VerifyWriteGeneralizedTime_Null(
            AsnEncodingRules ruleSet,
            bool omitFractionalSeconds)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteGeneralizedTime(DateTimeOffset.Now, omitFractionalSeconds, Asn1Tag.Null));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWriteGeneralizedTime_IgnoresConstructed(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            DateTimeOffset value = new DateTimeOffset(2017, 11, 16, 17, 35, 1, TimeSpan.Zero);

            writer.WriteGeneralizedTime(value, tag: new Asn1Tag(UniversalTagNumber.GeneralizedTime, true));
            writer.WriteGeneralizedTime(value, tag: new Asn1Tag(TagClass.ContextSpecific, 3, true));
            Verify(writer, "180F32303137313131363137333530315A" + "830F32303137313131363137333530315A");
        }
    }
}
