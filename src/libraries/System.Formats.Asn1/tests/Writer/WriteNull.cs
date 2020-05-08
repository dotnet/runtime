// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public class WriteNull : Asn1WriterTests
    {
        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.DER)]
        [InlineData(PublicEncodingRules.CER)]
        public void VerifyWriteNull(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteNull();

                Verify(writer, "0500");
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.DER)]
        [InlineData(PublicEncodingRules.CER)]
        public void VerifyWriteNull_EndOfContents(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteNull(Asn1Tag.EndOfContents));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.DER)]
        [InlineData(PublicEncodingRules.CER)]
        public void VerifyWriteNull_ConstructedIgnored(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteNull(new Asn1Tag(TagClass.ContextSpecific, 7, true));
                writer.WriteNull(new Asn1Tag(UniversalTagNumber.Null, true));

                Verify(writer, "87000500");
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
                    writer.WriteBoolean(true);
                }

                writer.Dispose();

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteNull());

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteNull(Asn1Tag.Integer));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteNull(new Asn1Tag(TagClass.Private, 3)));
            }
        }
    }
}
