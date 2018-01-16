// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Cryptography.Asn1;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public class WriteObjectIdentifier : Asn1WriterTests
    {
        [Theory]
        [MemberData(nameof(ValidOidData))]
        public void VerifyWriteObjectIdentifier_String(
            PublicEncodingRules ruleSet,
            string oidValue,
            string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteObjectIdentifier(oidValue);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [MemberData(nameof(ValidOidData))]
        public void VerifyWriteObjectIdentifier_Span(
            PublicEncodingRules ruleSet,
            string oidValue,
            string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteObjectIdentifier(oidValue.AsReadOnlySpan());

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [MemberData(nameof(ValidOidData))]
        public void VerifyWriteObjectIdentifier_Oid(
            PublicEncodingRules ruleSet,
            string oidValue,
            string expectedHex)
        {
            Oid oidObj = new Oid(oidValue, "FriendlyName does not matter");

            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteObjectIdentifier(oidObj);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidOidData))]
        public void VerifyWriteOid_InvalidValue_String(
            string description,
            PublicEncodingRules ruleSet,
            string nonOidValue)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                Assert.Throws<CryptographicException>(
                    () => writer.WriteObjectIdentifier(nonOidValue));
            }
        }

        [Theory]
        [MemberData(nameof(InvalidOidData))]
        public void VerifyWriteOid_InvalidValue_Span(
            string description,
            PublicEncodingRules ruleSet,
            string nonOidValue)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                Assert.Throws<CryptographicException>(
                    () => writer.WriteObjectIdentifier(nonOidValue.AsReadOnlySpan()));
            }
        }

        [Theory]
        [MemberData(nameof(InvalidOidData))]
        public void VerifyWriteOid_InvalidValue_Oid(
            string description,
            PublicEncodingRules ruleSet,
            string nonOidValue)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                Oid nonOidObj = new Oid(nonOidValue, "FriendlyName does not matter");

                Assert.Throws<CryptographicException>(
                    () => writer.WriteObjectIdentifier(nonOidObj));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void WriteObjectIdentifier_CustomTag_String(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteObjectIdentifier(new Asn1Tag(TagClass.ContextSpecific, 3), "1.3.14.3.2.26");

                Verify(writer, "83052B0E03021A");
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void WriteObjectIdentifier_CustomTag_Span(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteObjectIdentifier(new Asn1Tag(TagClass.Application, 2), "1.3.14.3.2.26".AsReadOnlySpan());

                Verify(writer, "42052B0E03021A");
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void WriteObjectIdentifier_CustomTag_Oid(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteObjectIdentifier(
                    new Asn1Tag(TagClass.Private, 36),
                    Oid.FromFriendlyName("SHA1", OidGroup.HashAlgorithm));

                Verify(writer, "DF24052B0E03021A");
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteObjectIdentifier_NullString(bool defaultTag)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "oidValue",
                    () =>
                    {
                        if (defaultTag)
                        {
                            writer.WriteObjectIdentifier((string)null);
                        }
                        else
                        {
                            writer.WriteObjectIdentifier(new Asn1Tag(TagClass.ContextSpecific, 6), (string)null);
                        }
                    });
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteObjectIdentifier_NullOid(bool defaultTag)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "oid",
                    () =>
                    {
                        if (defaultTag)
                        {
                            writer.WriteObjectIdentifier((Oid)null);
                        }
                        else
                        {
                            writer.WriteObjectIdentifier(new Asn1Tag(TagClass.Application, 2), (Oid)null);
                        }
                    });
            }
        }


        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void VerifyWriteObjectIdentifier_EndOfContents(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteObjectIdentifier(Asn1Tag.EndOfContents, "1.1"));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteObjectIdentifier(Asn1Tag.EndOfContents, "1.1".AsReadOnlySpan()));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteObjectIdentifier(Asn1Tag.EndOfContents, new Oid("1.1", "1.1")));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void VerifyWriteObjectIdentifier_ConstructedIgnored(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                const string OidValue = "1.1";
                Asn1Tag constructedOid = new Asn1Tag(UniversalTagNumber.ObjectIdentifier, isConstructed: true);
                Asn1Tag constructedContext0 = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);

                writer.WriteObjectIdentifier(constructedOid, OidValue);
                writer.WriteObjectIdentifier(constructedContext0, OidValue);
                writer.WriteObjectIdentifier(constructedOid, OidValue.AsReadOnlySpan());
                writer.WriteObjectIdentifier(constructedContext0, OidValue.AsReadOnlySpan());
                writer.WriteObjectIdentifier(constructedOid, new Oid(OidValue, OidValue));
                writer.WriteObjectIdentifier(constructedContext0, new Oid(OidValue, OidValue));

                Verify(writer, "060129800129060129800129060129800129");
            }
        }

        public static IEnumerable<object[]> ValidOidData { get; } =
            new object[][]
            {
                new object[]
                {
                    PublicEncodingRules.BER,
                    "0.0",
                    "060100",
                },
                new object[]
                {
                    PublicEncodingRules.CER,
                    "1.0",
                    "060128",
                },
                new object[]
                {
                    PublicEncodingRules.DER,
                    "2.0",
                    "060150",
                },
                new object[]
                {
                    PublicEncodingRules.BER,
                    "1.3.14.3.2.26",
                    "06052B0E03021A",
                },
                new object[]
                {
                    PublicEncodingRules.CER,
                    "2.999.19427512891.25",
                    "06088837C8AFE1A43B19",
                },
                new object[]
                {
                    PublicEncodingRules.DER,
                    "1.2.840.113549.1.1.10",
                    "06092A864886F70D01010A",
                },
                new object[]
                {
                    // Using the rules of ITU-T-REC-X.667-201210 for 2.25.{UUID} unregistered arcs, and
                    // their sample value of f81d4fae-7dec-11d0-a765-00a0c91e6bf6
                    // this is
                    // { joint-iso-itu-t(2) uuid(255) thatuuid(329800735698586629295641978511506172918) three(3) }
                    PublicEncodingRules.DER,
                    "2.25.329800735698586629295641978511506172918.3",
                    "06156983F09DA7EBCFDEE0C7A1A7B2C0948CC8F9D77603",
                },
            };

        public static IEnumerable<object[]> InvalidOidData { get; } =
            new object[][]
            {
                new object[] { "Empty string", PublicEncodingRules.BER, "" },
                new object[] { "No period", PublicEncodingRules.CER, "1" },
                new object[] { "No second RID", PublicEncodingRules.DER, "1." },
                new object[] { "Invalid first RID", PublicEncodingRules.BER, "3.0" },
                new object[] { "Invalid first RID - multichar", PublicEncodingRules.CER, "27.0" },
                new object[] { "Double zero - First RID", PublicEncodingRules.DER, "00.0" },
                new object[] { "Leading zero - First RID", PublicEncodingRules.BER, "01.0" },
                new object[] { "Double zero - second RID", PublicEncodingRules.CER, "0.00" },
                new object[] { "Leading zero - second RID", PublicEncodingRules.DER, "0.01" },
                new object[] { "Ends with period - second RID", PublicEncodingRules.BER, "0.0." },
                new object[] { "Ends with period - third RID", PublicEncodingRules.BER, "0.1.30." },
                new object[] { "Double zero - third RID", PublicEncodingRules.CER, "0.1.00" },
                new object[] { "Leading zero - third RID", PublicEncodingRules.DER, "0.1.023" },
                new object[] { "Invalid character first position", PublicEncodingRules.BER, "a.1.23" },
                new object[] { "Invalid character second position", PublicEncodingRules.CER, "0,1.23" },
                new object[] { "Invalid character second rid", PublicEncodingRules.DER, "0.1q.23" },
                new object[] { "Invalid character third rid", PublicEncodingRules.BER, "0.1.23q" },
            };
    }
}
