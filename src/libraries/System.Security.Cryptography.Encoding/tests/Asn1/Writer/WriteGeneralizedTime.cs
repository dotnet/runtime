// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Cryptography.Asn1;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
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
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                writer.WriteGeneralizedTime(input, omitFractionalSeconds);

                Verify(writer, "18" + expectedHexPayload);
            }
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteGeneralizedTime_BER_CustomTag(
            DateTimeOffset input,
            bool omitFractionalSeconds,
            string expectedHexPayload)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Application, 11);
                writer.WriteGeneralizedTime(tag, input, omitFractionalSeconds);

                Verify(writer, Stringify(tag) + expectedHexPayload);
            }
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteGeneralizedTime_CER(
            DateTimeOffset input,
            bool omitFractionalSeconds,
            string expectedHexPayload)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                writer.WriteGeneralizedTime(input, omitFractionalSeconds);

                Verify(writer, "18" + expectedHexPayload);
            }
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteGeneralizedTime_CER_CustomTag(
            DateTimeOffset input,
            bool omitFractionalSeconds,
            string expectedHexPayload)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Private, 95);
                writer.WriteGeneralizedTime(tag, input, omitFractionalSeconds);

                Verify(writer, Stringify(tag) + expectedHexPayload);
            }
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteGeneralizedTime_DER(
            DateTimeOffset input,
            bool omitFractionalSeconds,
            string expectedHexPayload)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.WriteGeneralizedTime(input, omitFractionalSeconds);

                Verify(writer, "18" + expectedHexPayload);
            }
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyWriteGeneralizedTime_DER_CustomTag(
            DateTimeOffset input,
            bool omitFractionalSeconds,
            string expectedHexPayload)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 3);
                writer.WriteGeneralizedTime(tag, input, omitFractionalSeconds);

                Verify(writer, Stringify(tag) + expectedHexPayload);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, false)]
        [InlineData(PublicEncodingRules.CER, false)]
        [InlineData(PublicEncodingRules.DER, false)]
        [InlineData(PublicEncodingRules.BER, true)]
        [InlineData(PublicEncodingRules.CER, true)]
        [InlineData(PublicEncodingRules.DER, true)]
        public void VerifyWriteGeneralizedTime_EndOfContents(
            PublicEncodingRules ruleSet,
            bool omitFractionalSeconds)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteGeneralizedTime(Asn1Tag.EndOfContents, DateTimeOffset.Now, omitFractionalSeconds));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void VerifyWriteGeneralizedTime_IgnoresConstructed(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                DateTimeOffset value = new DateTimeOffset(2017, 11, 16, 17, 35, 1, TimeSpan.Zero);

                writer.WriteGeneralizedTime(new Asn1Tag(UniversalTagNumber.GeneralizedTime, true), value);
                writer.WriteGeneralizedTime(new Asn1Tag(TagClass.ContextSpecific, 3, true), value);
                Verify(writer, "180F32303137313131363137333530315A" + "830F32303137313131363137333530315A");
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void WriteAfterDispose(bool empty)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                if (!empty)
                {
                    writer.WriteNull();
                }

                writer.Dispose();

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteGeneralizedTime(DateTimeOffset.UtcNow));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteGeneralizedTime(DateTimeOffset.UtcNow, true));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteGeneralizedTime(Asn1Tag.Integer, DateTimeOffset.Now));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteGeneralizedTime(Asn1Tag.Integer, DateTimeOffset.Now, true));

                Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 18);

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteGeneralizedTime(tag, DateTimeOffset.Now));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteGeneralizedTime(tag, DateTimeOffset.Now, true));
            }
        }
    }
}
