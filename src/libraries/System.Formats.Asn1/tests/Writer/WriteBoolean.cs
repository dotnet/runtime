// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class WriteBoolean : Asn1WriterTests
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER, false, "010100")]
        [InlineData(AsnEncodingRules.BER, true, "0101FF")]
        [InlineData(AsnEncodingRules.CER, false, "010100")]
        [InlineData(AsnEncodingRules.CER, true, "0101FF")]
        [InlineData(AsnEncodingRules.DER, false, "010100")]
        [InlineData(AsnEncodingRules.DER, true, "0101FF")]
        public void VerifyWriteBoolean(AsnEncodingRules ruleSet, bool value, string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteBoolean(value);

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, false, "830100")]
        [InlineData(AsnEncodingRules.BER, true, "8301FF")]
        [InlineData(AsnEncodingRules.CER, false, "830100")]
        [InlineData(AsnEncodingRules.CER, true, "8301FF")]
        [InlineData(AsnEncodingRules.DER, false, "830100")]
        [InlineData(AsnEncodingRules.DER, true, "8301FF")]
        public void VerifyWriteBoolean_Context3(AsnEncodingRules ruleSet, bool value, string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteBoolean(value, new Asn1Tag(TagClass.ContextSpecific, 3));

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, false)]
        [InlineData(AsnEncodingRules.BER, true)]
        [InlineData(AsnEncodingRules.CER, false)]
        [InlineData(AsnEncodingRules.CER, true)]
        [InlineData(AsnEncodingRules.DER, false)]
        [InlineData(AsnEncodingRules.DER, true)]
        public void VerifyWriteBoolean_Null(AsnEncodingRules ruleSet, bool value)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteBoolean(value, Asn1Tag.Null));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, false)]
        [InlineData(AsnEncodingRules.BER, true)]
        [InlineData(AsnEncodingRules.CER, false)]
        [InlineData(AsnEncodingRules.CER, true)]
        [InlineData(AsnEncodingRules.DER, false)]
        [InlineData(AsnEncodingRules.DER, true)]
        public void VerifyWriteBoolean_ConstructedIgnored(AsnEncodingRules ruleSet, bool value)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteBoolean(value, new Asn1Tag(TagClass.ContextSpecific, 7, true));
            writer.WriteBoolean(value, new Asn1Tag(UniversalTagNumber.Boolean, true));

            if (value)
            {
                Verify(writer, "8701FF0101FF");
            }
            else
            {
                Verify(writer, "870100010100");
            }
        }
    }
}
