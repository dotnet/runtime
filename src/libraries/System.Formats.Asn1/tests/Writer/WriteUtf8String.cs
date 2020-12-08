// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class WriteUtf8String : WriteCharacterString
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
                "026869",
            },
            new object[]
            {
                "Dr. & Mrs. Smith\u2010Jones \uFE60 children",
                "2544722E2026204D72732E20536D697468E280904A6F6E657320EFB9A0206368696C6472656E",
            },
        };

        public static IEnumerable<object[]> LongValidCases { get; } = new object[][]
        {
            new object[]
            {
                new string('f', 957) + new string('w', 182),
                "820473" + new string('6', 957 * 2) + new string('7', 182 * 2),
            },
        };

        public static IEnumerable<object[]> CERSegmentedCases { get; } = new object[][]
        {
            new object[]
            {
                GettysburgAddress,
                1458,
            },
            new object[]
            {
                // A whole bunch of "small ampersand" values (3 bytes each UTF-8),
                // then one inverted exclamation (2 bytes UTF-8)
                new string('\uFE60', 2000 / 3) + '\u00A1',
                2000,
            },
        };

        public static IEnumerable<object[]> InvalidInputs => Array.Empty<object[]>();

        internal override void WriteString(AsnWriter writer, string s) =>
            writer.WriteCharacterString(UniversalTagNumber.UTF8String, s);

        internal override void WriteString(AsnWriter writer, Asn1Tag tag, string s) =>
            writer.WriteCharacterString(UniversalTagNumber.UTF8String, s, tag);

        internal override void WriteSpan(AsnWriter writer, ReadOnlySpan<char> s) =>
            writer.WriteCharacterString(UniversalTagNumber.UTF8String, s);

        internal override void WriteSpan(AsnWriter writer, Asn1Tag tag, ReadOnlySpan<char> s) =>
            writer.WriteCharacterString(UniversalTagNumber.UTF8String, s, tag);

        internal override Asn1Tag StandardTag => new Asn1Tag(UniversalTagNumber.UTF8String);

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

        // UTF8 has no non-encodable values.
    }
}
