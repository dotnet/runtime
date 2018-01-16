// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public class WriteBoolean : Asn1WriterTests
    {
        [Theory]
        [InlineData(PublicEncodingRules.BER, false, "010100")]
        [InlineData(PublicEncodingRules.BER, true, "0101FF")]
        [InlineData(PublicEncodingRules.CER, false, "010100")]
        [InlineData(PublicEncodingRules.CER, true, "0101FF")]
        [InlineData(PublicEncodingRules.DER, false, "010100")]
        [InlineData(PublicEncodingRules.DER, true, "0101FF")]
        public void VerifyWriteBoolean(PublicEncodingRules ruleSet, bool value, string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteBoolean(value);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, false, "830100")]
        [InlineData(PublicEncodingRules.BER, true, "8301FF")]
        [InlineData(PublicEncodingRules.CER, false, "830100")]
        [InlineData(PublicEncodingRules.CER, true, "8301FF")]
        [InlineData(PublicEncodingRules.DER, false, "830100")]
        [InlineData(PublicEncodingRules.DER, true, "8301FF")]
        public void VerifyWriteBoolean_Context3(PublicEncodingRules ruleSet, bool value, string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteBoolean(new Asn1Tag(TagClass.ContextSpecific, 3), value);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, false)]
        [InlineData(PublicEncodingRules.BER, true)]
        [InlineData(PublicEncodingRules.CER, false)]
        [InlineData(PublicEncodingRules.CER, true)]
        [InlineData(PublicEncodingRules.DER, false)]
        [InlineData(PublicEncodingRules.DER, true)]
        public void VerifyWriteBoolean_EndOfContents(PublicEncodingRules ruleSet, bool value)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteBoolean(Asn1Tag.EndOfContents, value));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, false)]
        [InlineData(PublicEncodingRules.BER, true)]
        [InlineData(PublicEncodingRules.CER, false)]
        [InlineData(PublicEncodingRules.CER, true)]
        [InlineData(PublicEncodingRules.DER, false)]
        [InlineData(PublicEncodingRules.DER, true)]
        public void VerifyWriteBoolean_ConstructedIgnored(PublicEncodingRules ruleSet, bool value)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteBoolean(new Asn1Tag(TagClass.ContextSpecific, 7, true), value);
                writer.WriteBoolean(new Asn1Tag(UniversalTagNumber.Boolean, true), value);

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
}
