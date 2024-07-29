// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class WriteObjectIdentifier : Asn1WriterTests
    {
        [Theory]
        [MemberData(nameof(ValidOidData))]
        public void VerifyWriteObjectIdentifier_String(
            AsnEncodingRules ruleSet,
            string oidValue,
            string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteObjectIdentifier(oidValue);

            Verify(writer, expectedHex);
        }

        [Theory]
        [MemberData(nameof(ValidOidData))]
        public void VerifyWriteObjectIdentifier_Span(
            AsnEncodingRules ruleSet,
            string oidValue,
            string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteObjectIdentifier(oidValue.AsSpan());

            Verify(writer, expectedHex);
        }

        [Theory]
        [MemberData(nameof(InvalidOidData))]
        public void VerifyWriteOid_InvalidValue_String(
            string description,
            AsnEncodingRules ruleSet,
            string nonOidValue)
        {
            _ = description;
            AsnWriter writer = new AsnWriter(ruleSet);

            if (nonOidValue == null)
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "oidValue",
                    () => writer.WriteObjectIdentifier(nonOidValue));
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>(
                    "oidValue",
                    () => writer.WriteObjectIdentifier(nonOidValue));
            }
        }

        [Theory]
        [MemberData(nameof(InvalidOidData))]
        public void VerifyWriteOid_InvalidValue_Span(
            string description,
            AsnEncodingRules ruleSet,
            string nonOidValue)
        {
            _ = description;
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "oidValue",
                () => writer.WriteObjectIdentifier(nonOidValue.AsSpan()));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void WriteObjectIdentifier_CustomTag_String(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteObjectIdentifier("1.3.14.3.2.26", new Asn1Tag(TagClass.ContextSpecific, 3));

            Verify(writer, "83052B0E03021A");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void WriteObjectIdentifier_CustomTag_Span(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteObjectIdentifier("1.3.14.3.2.26".AsSpan(), new Asn1Tag(TagClass.Application, 2));

            Verify(writer, "42052B0E03021A");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteObjectIdentifier_NullString(bool defaultTag)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);

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
                        writer.WriteObjectIdentifier((string)null, new Asn1Tag(TagClass.ContextSpecific, 6));
                    }
                });
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWriteObjectIdentifier_Null(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteObjectIdentifier("1.1", Asn1Tag.Null));

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteObjectIdentifier("1.1".AsSpan(), Asn1Tag.Null));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWriteObjectIdentifier_ConstructedIgnored(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            const string OidValue = "1.1";
            Asn1Tag constructedOid = new Asn1Tag(UniversalTagNumber.ObjectIdentifier, isConstructed: true);
            Asn1Tag constructedContext0 = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);

            writer.WriteObjectIdentifier(OidValue, constructedOid);
            writer.WriteObjectIdentifier(OidValue, constructedContext0);
            writer.WriteObjectIdentifier(OidValue.AsSpan(), constructedOid);
            writer.WriteObjectIdentifier(OidValue.AsSpan(), constructedContext0);

            Verify(writer, "060129800129060129800129");
        }

        public static IEnumerable<object[]> ValidOidData { get; } =
            new object[][]
            {
                new object[]
                {
                    AsnEncodingRules.BER,
                    "0.0",
                    "060100",
                },
                new object[]
                {
                    AsnEncodingRules.CER,
                    "1.0",
                    "060128",
                },
                new object[]
                {
                    AsnEncodingRules.DER,
                    "2.0",
                    "060150",
                },
                new object[]
                {
                    AsnEncodingRules.BER,
                    "1.3.14.3.2.26",
                    "06052B0E03021A",
                },
                new object[]
                {
                    AsnEncodingRules.CER,
                    "2.999.19427512891.25",
                    "06088837C8AFE1A43B19",
                },
                new object[]
                {
                    AsnEncodingRules.DER,
                    "1.2.840.113549.1.1.10",
                    "06092A864886F70D01010A",
                },
                new object[]
                {
                    // Using the rules of ITU-T-REC-X.667-201210 for 2.25.{UUID} unregistered arcs, and
                    // their sample value of f81d4fae-7dec-11d0-a765-00a0c91e6bf6
                    // this is
                    // { joint-iso-itu-t(2) uuid(255) thatuuid(329800735698586629295641978511506172918) three(3) }
                    AsnEncodingRules.DER,
                    "2.25.329800735698586629295641978511506172918.3",
                    "06156983F09DA7EBCFDEE0C7A1A7B2C0948CC8F9D77603",
                },
            };

        public static IEnumerable<object[]> InvalidOidData { get; } =
            new object[][]
            {
                new object[] { "Null", AsnEncodingRules.BER, null },
                new object[] { "Empty string", AsnEncodingRules.BER, "" },
                new object[] { "No period", AsnEncodingRules.CER, "1" },
                new object[] { "No second RID", AsnEncodingRules.DER, "1." },
                new object[] { "Invalid first RID", AsnEncodingRules.BER, "3.0" },
                new object[] { "Invalid first RID - multichar", AsnEncodingRules.CER, "27.0" },
                new object[] { "Double zero - First RID", AsnEncodingRules.DER, "00.0" },
                new object[] { "Leading zero - First RID", AsnEncodingRules.BER, "01.0" },
                new object[] { "Double zero - second RID", AsnEncodingRules.CER, "0.00" },
                new object[] { "Leading zero - second RID", AsnEncodingRules.DER, "0.01" },
                new object[] { "Double-period", AsnEncodingRules.BER, "0..0" },
                new object[] { "Ends with period - second RID", AsnEncodingRules.BER, "0.0." },
                new object[] { "Ends with period - third RID", AsnEncodingRules.BER, "0.1.30." },
                new object[] { "Double zero - third RID", AsnEncodingRules.CER, "0.1.00" },
                new object[] { "Leading zero - third RID", AsnEncodingRules.DER, "0.1.023" },
                new object[] { "Invalid character first position", AsnEncodingRules.BER, "a.1.23" },
                new object[] { "Invalid character second position", AsnEncodingRules.CER, "0,1.23" },
                new object[] { "Invalid character second rid", AsnEncodingRules.DER, "0.1q.23" },
                new object[] { "Invalid character third rid", AsnEncodingRules.BER, "0.1.23q" },
                new object[] { "Invalid second RID for first arc 0", AsnEncodingRules.DER, "0.40.1.2.3" },
                new object[] { "Invalid second RID for first arc 1", AsnEncodingRules.BER, "1.40.1.2.3" },
            };
    }
}
