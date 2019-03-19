// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    internal static class AsnReaderExtensions
    {
        private delegate Asn1Tag ReadTagAndLengthDelegate(out int? parsedLength, out int bytesRead);

        public static Asn1Tag ReadTagAndLength(this AsnReader reader, out int? parsedLength, out int bytesRead)
        {
            return ((ReadTagAndLengthDelegate)
                typeof(AsnReader).GetMethod("ReadTagAndLength", BindingFlags.Instance | BindingFlags.NonPublic)
                    .CreateDelegate(typeof(ReadTagAndLengthDelegate), reader)).Invoke(out parsedLength, out bytesRead);
        }
    }

    public sealed class ReadLength : Asn1ReaderTests
    {
        [Theory]
        [InlineData(4, 0, "0400")]
        [InlineData(1, 1, "0101")]
        [InlineData(4, 127, "047F")]
        [InlineData(4, 128, "048180")]
        [InlineData(4, 255, "0481FF")]
        [InlineData(2, 256, "02820100")]
        [InlineData(4, int.MaxValue, "04847FFFFFFF")]
        public static void MinimalPrimitiveLength(int tagValue, int length, string inputHex)
        {
            byte[] inputBytes = inputHex.HexToByteArray();
            
            foreach (PublicEncodingRules rules in Enum.GetValues(typeof(PublicEncodingRules)))
            {
                AsnReader reader = new AsnReader(inputBytes, (AsnEncodingRules)rules);

                Asn1Tag tag = reader.ReadTagAndLength(out int ? parsedLength, out int bytesRead);

                Assert.Equal(inputBytes.Length, bytesRead);
                Assert.False(tag.IsConstructed, "tag.IsConstructed");
                Assert.Equal(tagValue, tag.TagValue);
                Assert.Equal(length, parsedLength.Value);

                // ReadTagAndLength doesn't move the _data span forward.
                Assert.True(reader.HasData, "reader.HasData");
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public static void ReadWithUnknownRuleSet(int invalidRuleSetValue)
        {
            byte[] data = { 0x05, 0x00 };

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new AsnReader(data, (AsnEncodingRules)invalidRuleSetValue));
        }

        [Theory]
        [InlineData("")]
        [InlineData("05")]
        [InlineData("0481")]
        [InlineData("048201")]
        [InlineData("04830102")]
        [InlineData("0484010203")]
        public static void ReadWithInsufficientData(string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, AsnEncodingRules.DER);

            Assert.Throws<CryptographicException>(() => reader.ReadTagAndLength(out _, out _));
        }

        [Theory]
        [InlineData("DER indefinite constructed", PublicEncodingRules.DER, "3080" + "0500" + "0000")]
        [InlineData("0xFF-BER", PublicEncodingRules.BER, "04FF")]
        [InlineData("0xFF-CER", PublicEncodingRules.CER, "04FF")]
        [InlineData("0xFF-DER", PublicEncodingRules.DER, "04FF")]
        [InlineData("CER definite constructed", PublicEncodingRules.CER, "30820500")]
        [InlineData("BER indefinite primitive", PublicEncodingRules.BER, "0480" + "0000")]
        [InlineData("CER indefinite primitive", PublicEncodingRules.CER, "0480" + "0000")]
        [InlineData("DER indefinite primitive", PublicEncodingRules.DER, "0480" + "0000")]
        [InlineData("DER non-minimal 0", PublicEncodingRules.DER, "048100")]
        [InlineData("DER non-minimal 7F", PublicEncodingRules.DER, "04817F")]
        [InlineData("DER non-minimal 80", PublicEncodingRules.DER, "04820080")]
        [InlineData("CER non-minimal 0", PublicEncodingRules.CER, "048100")]
        [InlineData("CER non-minimal 7F", PublicEncodingRules.CER, "04817F")]
        [InlineData("CER non-minimal 80", PublicEncodingRules.CER, "04820080")]
        [InlineData("BER too large", PublicEncodingRules.BER, "048480000000")]
        [InlineData("CER too large", PublicEncodingRules.CER, "048480000000")]
        [InlineData("DER too large", PublicEncodingRules.DER, "048480000000")]
        [InlineData("BER padded too large", PublicEncodingRules.BER, "0486000080000000")]
        [InlineData("BER uint.MaxValue", PublicEncodingRules.BER, "0484FFFFFFFF")]
        [InlineData("CER uint.MaxValue", PublicEncodingRules.CER, "0484FFFFFFFF")]
        [InlineData("DER uint.MaxValue", PublicEncodingRules.DER, "0484FFFFFFFF")]
        [InlineData("BER padded uint.MaxValue", PublicEncodingRules.BER, "048800000000FFFFFFFF")]
        [InlineData("BER 5 byte spread", PublicEncodingRules.BER, "04850100000000")]
        [InlineData("CER 5 byte spread", PublicEncodingRules.CER, "04850100000000")]
        [InlineData("DER 5 byte spread", PublicEncodingRules.DER, "04850100000000")]
        [InlineData("BER padded 5 byte spread", PublicEncodingRules.BER, "0486000100000000")]
        public static void InvalidLengths(
            string description,
            PublicEncodingRules rules,
            string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)rules);

            Assert.Throws<CryptographicException>(() => reader.ReadTagAndLength(out _, out _));
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        public static void IndefiniteLength(PublicEncodingRules ruleSet)
        {
            // SEQUENCE (indefinite)
            //   NULL
            //   End-of-Contents
            byte[] data = { 0x30, 0x80, 0x05, 0x00, 0x00, 0x00 };
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            Asn1Tag tag = reader.ReadTagAndLength(out int? length, out int bytesRead);

            Assert.Equal(2, bytesRead);
            Assert.False(length.HasValue, "length.HasValue");
            Assert.Equal((int)UniversalTagNumber.Sequence, tag.TagValue);
            Assert.True(tag.IsConstructed, "tag.IsConstructed");
        }

        [Theory]
        [InlineData(0, "0483000000")]
        [InlineData(1, "048A00000000000000000001")]
        [InlineData(128, "049000000000000000000000000000000080")]
        public static void BerNonMinimalLength(int expectedLength, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, AsnEncodingRules.BER);

            Asn1Tag tag = reader.ReadTagAndLength(out int? length, out int bytesRead);

            Assert.Equal(inputData.Length, bytesRead);
            Assert.Equal(expectedLength, length.Value);
            // ReadTagAndLength doesn't move the _data span forward.
            Assert.True(reader.HasData, "reader.HasData");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, 4, 0, 5, "0483000000" + "0500")]
        [InlineData(PublicEncodingRules.DER, 1, 1, 2, "0101" + "FF")]
        [InlineData(PublicEncodingRules.CER, 0x10, null, 2, "3080" + "0500" + "0000")]
        public static void ReadWithDataRemaining(
            PublicEncodingRules ruleSet,
            int tagValue,
            int? expectedLength,
            int expectedBytesRead,
            string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Asn1Tag tag = reader.ReadTagAndLength(out int? length, out int bytesRead);

            Assert.Equal(expectedBytesRead, bytesRead);
            Assert.Equal(tagValue, tag.TagValue);
            Assert.Equal(expectedLength, length);
        }
    }
}
