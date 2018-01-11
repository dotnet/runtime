// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Cryptography.Asn1;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public class WriteIA5String : WriteCharacterString
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
                "Dr. & Mrs. Smith-Jones & children",
                "2144722E2026204D72732E20536D6974682D4A6F6E65732026206368696C6472656E",
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
                new string ('Q', 2000),
                2000,
            },
        };

        public static IEnumerable<object[]> InvalidInputs { get; } = new object[][]
        {
            new object[] { "Dr. & Mrs. Smith\u2010Jones \uFE60 children", },
        };

        internal override void WriteString(AsnWriter writer, string s) =>
            writer.WriteCharacterString(UniversalTagNumber.IA5String, s);

        internal override void WriteString(AsnWriter writer, Asn1Tag tag, string s) =>
            writer.WriteCharacterString(tag, UniversalTagNumber.IA5String, s);

        internal override void WriteSpan(AsnWriter writer, ReadOnlySpan<char> s) =>
            writer.WriteCharacterString(UniversalTagNumber.IA5String, s);

        internal override void WriteSpan(AsnWriter writer, Asn1Tag tag, ReadOnlySpan<char> s) =>
            writer.WriteCharacterString(tag, UniversalTagNumber.IA5String, s);

        internal override Asn1Tag StandardTag => new Asn1Tag(UniversalTagNumber.IA5String);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_BER_String(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_String(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_BER_String_CustomTag(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_String_CustomTag(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public new void VerifyWrite_CER_String(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_String(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public new void VerifyWrite_CER_String_CustomTag(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_String_CustomTag(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_DER_String(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_String(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_DER_String_CustomTag(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_String_CustomTag(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_BER_Span(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_Span(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_BER_Span_CustomTag(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_Span_CustomTag(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public new void VerifyWrite_CER_Span(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_Span(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public new void VerifyWrite_CER_Span_CustomTag(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_Span_CustomTag(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_DER_Span(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_Span(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_DER_Span_CustomTag(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_Span_CustomTag(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_BER_String_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_String_ClearsConstructed(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_BER_String_CustomTag_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_String_CustomTag_ClearsConstructed(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_BER_Span_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_Span_ClearsConstructed(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_BER_Span_CustomTag_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_BER_Span_CustomTag_ClearsConstructed(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public new void VerifyWrite_CER_String_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_String_ClearsConstructed(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public new void VerifyWrite_CER_String_CustomTag_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_String_CustomTag_ClearsConstructed(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public new void VerifyWrite_CER_Span_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_Span_ClearsConstructed(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        public new void VerifyWrite_CER_Span_CustomTag_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_CER_Span_CustomTag_ClearsConstructed(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_DER_String_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_String_ClearsConstructed(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_DER_String_CustomTag_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_String_CustomTag_ClearsConstructed(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_DER_Span_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_Span_ClearsConstructed(input, expectedPayloadHex);

        [Theory]
        [MemberData(nameof(ShortValidCases))]
        [MemberData(nameof(LongValidCases))]
        public new void VerifyWrite_DER_Span_CustomTag_ClearsConstructed(string input, string expectedPayloadHex) =>
            base.VerifyWrite_DER_Span_CustomTag_ClearsConstructed(input, expectedPayloadHex);

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public new void VerifyWrite_String_Null(PublicEncodingRules ruleSet) =>
            base.VerifyWrite_String_Null(ruleSet);

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public new void VerifyWrite_String_Null_CustomTag(PublicEncodingRules ruleSet) =>
            base.VerifyWrite_String_Null_CustomTag(ruleSet);

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public new void VerifyWrite_EndOfContents_String(PublicEncodingRules ruleSet) =>
            base.VerifyWrite_EndOfContents_String(ruleSet);

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public new void VerifyWrite_EndOfContents_Span(PublicEncodingRules ruleSet) =>
            base.VerifyWrite_EndOfContents_Span(ruleSet);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public new void VerifyWrite_CERSegmented_String(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_String(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public new void VerifyWrite_CERSegmented_String_CustomTag(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_String_CustomTag(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public new void VerifyWrite_CERSegmented_String_ConstructedTag(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_String_ConstructedTag(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public new void VerifyWrite_CERSegmented_String_CustomPrimitiveTag(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_String_CustomPrimitiveTag(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public new void VerifyWrite_CERSegmented_Span(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_Span(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public new void VerifyWrite_CERSegmented_Span_CustomTag(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_Span_CustomTag(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public new void VerifyWrite_CERSegmented_Span_ConstructedTag(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_Span_ConstructedTag(input, contentByteCount);

        [Theory]
        [MemberData(nameof(CERSegmentedCases))]
        public new void VerifyWrite_CERSegmented_Span_CustomPrimitiveTag(string input, int contentByteCount) =>
            base.VerifyWrite_CERSegmented_Span_CustomPrimitiveTag(input, contentByteCount);

        [Theory]
        [MemberData(nameof(InvalidInputs))]
        public new void VerifyWrite_String_NonEncodable(string input) =>
            base.VerifyWrite_String_NonEncodable(input);

        [Theory]
        [MemberData(nameof(InvalidInputs))]
        public new void VerifyWrite_Span_NonEncodable(string input) =>
            base.VerifyWrite_Span_NonEncodable(input);
    }
}
