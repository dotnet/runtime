// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class WriteNull : Asn1WriterTests
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.DER)]
        [InlineData(AsnEncodingRules.CER)]
        public void VerifyWriteNull(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteNull();

            Verify(writer, "0500");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.DER)]
        [InlineData(AsnEncodingRules.CER)]
        public void VerifyWriteNull_OctetString(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteNull(Asn1Tag.PrimitiveOctetString));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.DER)]
        [InlineData(AsnEncodingRules.CER)]
        public void VerifyWriteNull_ConstructedIgnored(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteNull(new Asn1Tag(TagClass.ContextSpecific, 7, true));
            writer.WriteNull(new Asn1Tag(UniversalTagNumber.Null, true));

            Verify(writer, "87000500");
        }
    }
}
