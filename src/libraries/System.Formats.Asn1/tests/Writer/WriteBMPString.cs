// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class WriteBMPString : WriteCharacterString
    {
        public static IEnumerable<object[]> ShortValidCases { get; } = new object[][]
        {
            new object[]
            {
                string.Empty,
                "00",
            },
            new object[]
            {
                "hi",
                "0400680069",
            },
            new object[]
            {
                "Dr. & Mrs. Smith\u2010Jones \uFE60 children",
                "42" +
                    "00440072002E002000260020004D00720073002E00200053006D0069" +
                    "007400682010004A006F006E006500730020FE600020006300680069" +
                    "006C006400720065006E",
            },
        };

        public static IEnumerable<object[]> LongValidCases { get; } = new object[][]
        {
            new object[]
            {
                // 498 Han-fragrant, 497 Han-dark, 23 Han-rich
                new string('\u9999', 498) + new string('\u6666', 497) + new string('\u4444', 23),
                "8207F4" + new string('9', 498 * 4) + new string('6', 497 * 4) + new string('4', 23 * 4),
            },
        };

        public static IEnumerable<object[]> CERSegmentedCases { get; } = new object[][]
        {
            new object[]
            {
                GettysburgAddress,
                1458 * 2,
            },
            new object[]
            {
                // 498 Han-fragrant, 497 Han-dark, 5 Han-rich
                new string('\u9999', 498) + new string('\u6666', 497) + new string('\u4444', 5),
                2000,
            },
        };

        public static IEnumerable<object[]> InvalidInputs { get; } = new object[][]
        {
            // Surrogate pair for "Deseret Small Letter Yee" (U+10437)
            new object[] { "\uD801\uDC37" },
        };

        internal override void WriteString(AsnWriter writer, string s) =>
            writer.WriteCharacterString(UniversalTagNumber.BMPString, s);

        internal override void WriteString(AsnWriter writer, Asn1Tag tag, string s) =>
            writer.WriteCharacterString(UniversalTagNumber.BMPString, s, tag);

        internal override void WriteSpan(AsnWriter writer, ReadOnlySpan<char> s) =>
            writer.WriteCharacterString(UniversalTagNumber.BMPString, s);

        internal override void WriteSpan(AsnWriter writer, Asn1Tag tag, ReadOnlySpan<char> s) =>
            writer.WriteCharacterString(UniversalTagNumber.BMPString, s, tag);

        internal override Asn1Tag StandardTag => new Asn1Tag(UniversalTagNumber.BMPString);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_BER_String(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_String_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_BER_String_CustomTag(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_String_CustomTag_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public void VerifyWrite_CER_String(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_String_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public void VerifyWrite_CER_String_CustomTag(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_String_CustomTag_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_DER_String(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_String_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_DER_String_CustomTag(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_String_CustomTag_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_BER_Span(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_Span_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_BER_Span_CustomTag(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_Span_CustomTag_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public void VerifyWrite_CER_Span(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_Span_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public void VerifyWrite_CER_Span_CustomTag(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_Span_CustomTag_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_DER_Span(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_Span_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_DER_Span_CustomTag(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_Span_CustomTag_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_BER_String_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_String_ClearsConstructed_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_BER_String_CustomTag_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_String_CustomTag_ClearsConstructed_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_BER_Span_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_Span_ClearsConstructed_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_BER_Span_CustomTag_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_Span_CustomTag_ClearsConstructed_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public void VerifyWrite_CER_String_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_String_ClearsConstructed_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public void VerifyWrite_CER_String_CustomTag_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_String_CustomTag_ClearsConstructed_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public void VerifyWrite_CER_Span_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_Span_ClearsConstructed_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public void VerifyWrite_CER_Span_CustomTag_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_Span_CustomTag_ClearsConstructed_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_DER_String_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_String_ClearsConstructed_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_DER_String_CustomTag_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_String_CustomTag_ClearsConstructed_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_DER_Span_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_Span_ClearsConstructed_Helper(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public void VerifyWrite_DER_Span_CustomTag_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_Span_CustomTag_ClearsConstructed_Helper(input, expectedPayloadHex);

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWrite_String_Null(AsnEncodingRules ruleSet) =>
            base.VerifyWrite_String_Null_Helper(ruleSet);

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWrite_String_Null_CustomTag(AsnEncodingRules ruleSet) =>
            base.VerifyWrite_String_Null_CustomTag_Helper(ruleSet);

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWrite_Null_String(AsnEncodingRules ruleSet) =>
            base.VerifyWrite_Null_String_Helper(ruleSet);

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWrite_Null_Span(AsnEncodingRules ruleSet) =>
            base.VerifyWrite_Null_Span_Helper(ruleSet);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public void VerifyWrite_CERSegmented_String(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_String_Helper(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public void VerifyWrite_CERSegmented_String_CustomTag(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_String_CustomTag_Helper(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public void VerifyWrite_CERSegmented_String_ConstructedTag(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_String_ConstructedTag_Helper(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public void VerifyWrite_CERSegmented_String_CustomPrimitiveTag(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_String_CustomPrimitiveTag_Helper(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public void VerifyWrite_CERSegmented_Span(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_Span_Helper(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public void VerifyWrite_CERSegmented_Span_CustomTag(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_Span_CustomTag_Helper(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public void VerifyWrite_CERSegmented_Span_ConstructedTag(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_Span_ConstructedTag_Helper(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public void VerifyWrite_CERSegmented_Span_CustomPrimitiveTag(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_Span_CustomPrimitiveTag_Helper(input, contentByteCount);

        [Theory]
        [MemberData(nameof(InvalidInputs))]
        public void VerifyWrite_String_NonEncodable(string input) =>
            base.VerifyWrite_String_NonEncodable_Helper(input);

        [Theory]
        [MemberData(nameof(InvalidInputs))]
        public void VerifyWrite_Span_NonEncodable(string input) =>
            base.VerifyWrite_Span_NonEncodable_Helper(input);
    }
}
