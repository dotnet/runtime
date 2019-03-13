// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public class WriteInteger : Asn1WriterTests
    {
        [Theory]
        [InlineData(PublicEncodingRules.BER, 0, "020100")]
        [InlineData(PublicEncodingRules.CER, 0, "020100")]
        [InlineData(PublicEncodingRules.DER, 0, "020100")]
        [InlineData(PublicEncodingRules.BER, -1, "0201FF")]
        [InlineData(PublicEncodingRules.CER, -1, "0201FF")]
        [InlineData(PublicEncodingRules.DER, -1, "0201FF")]
        [InlineData(PublicEncodingRules.BER, -2, "0201FE")]
        [InlineData(PublicEncodingRules.DER, sbyte.MinValue, "020180")]
        [InlineData(PublicEncodingRules.BER, sbyte.MinValue + 1, "020181")]
        [InlineData(PublicEncodingRules.CER, sbyte.MinValue - 1, "0202FF7F")]
        [InlineData(PublicEncodingRules.DER, sbyte.MinValue - 2, "0202FF7E")]
        [InlineData(PublicEncodingRules.BER, -256, "0202FF00")]
        [InlineData(PublicEncodingRules.CER, -257, "0202FEFF")]
        [InlineData(PublicEncodingRules.DER, short.MinValue, "02028000")]
        [InlineData(PublicEncodingRules.BER, short.MinValue + 1, "02028001")]
        [InlineData(PublicEncodingRules.CER, short.MinValue + byte.MaxValue, "020280FF")]
        [InlineData(PublicEncodingRules.DER, short.MinValue - 1, "0203FF7FFF")]
        [InlineData(PublicEncodingRules.BER, short.MinValue - 2, "0203FF7FFE")]
        [InlineData(PublicEncodingRules.CER, -65281, "0203FF00FF")]
        [InlineData(PublicEncodingRules.DER, -8388608, "0203800000")]
        [InlineData(PublicEncodingRules.BER, -8388607, "0203800001")]
        [InlineData(PublicEncodingRules.CER, -8388609, "0204FF7FFFFF")]
        [InlineData(PublicEncodingRules.DER, -16777216, "0204FF000000")]
        [InlineData(PublicEncodingRules.BER, -16777217, "0204FEFFFFFF")]
        [InlineData(PublicEncodingRules.CER, int.MinValue, "020480000000")]
        [InlineData(PublicEncodingRules.DER, int.MinValue + 1, "020480000001")]
        [InlineData(PublicEncodingRules.BER, (long)int.MinValue - 1, "0205FF7FFFFFFF")]
        [InlineData(PublicEncodingRules.CER, (long)int.MinValue - 2, "0205FF7FFFFFFE")]
        [InlineData(PublicEncodingRules.DER, -4294967296, "0205FF00000000")]
        [InlineData(PublicEncodingRules.BER, -4294967295, "0205FF00000001")]
        [InlineData(PublicEncodingRules.CER, -4294967294, "0205FF00000002")]
        [InlineData(PublicEncodingRules.DER, -4294967297, "0205FEFFFFFFFF")]
        [InlineData(PublicEncodingRules.BER, -549755813888, "02058000000000")]
        [InlineData(PublicEncodingRules.CER, -549755813887, "02058000000001")]
        [InlineData(PublicEncodingRules.DER, -549755813889, "0206FF7FFFFFFFFF")]
        [InlineData(PublicEncodingRules.BER, -549755813890, "0206FF7FFFFFFFFE")]
        [InlineData(PublicEncodingRules.CER, -140737488355328, "0206800000000000")]
        [InlineData(PublicEncodingRules.DER, -140737488355327, "0206800000000001")]
        [InlineData(PublicEncodingRules.BER, -140737488355329, "0207FF7FFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.CER, -281474976710656, "0207FF000000000000")]
        [InlineData(PublicEncodingRules.DER, -281474976710655, "0207FF000000000001")]
        [InlineData(PublicEncodingRules.BER, -281474976710657, "0207FEFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.CER, -36028797018963968, "020780000000000000")]
        [InlineData(PublicEncodingRules.DER, -36028797018963967, "020780000000000001")]
        [InlineData(PublicEncodingRules.DER, -36028797018963969, "0208FF7FFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.BER, -36028797018963970, "0208FF7FFFFFFFFFFFFE")]
        [InlineData(PublicEncodingRules.CER, -72057594037927936, "0208FF00000000000000")]
        [InlineData(PublicEncodingRules.DER, -72057594037927935, "0208FF00000000000001")]
        [InlineData(PublicEncodingRules.BER, -72057594037927937, "0208FEFFFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.CER, long.MinValue + 1, "02088000000000000001")]
        [InlineData(PublicEncodingRules.DER, long.MinValue, "02088000000000000000")]
        [InlineData(PublicEncodingRules.BER, 1, "020101")]
        [InlineData(PublicEncodingRules.CER, 127, "02017F")]
        [InlineData(PublicEncodingRules.DER, 126, "02017E")]
        [InlineData(PublicEncodingRules.BER, 128, "02020080")]
        [InlineData(PublicEncodingRules.CER, 129, "02020081")]
        [InlineData(PublicEncodingRules.DER, 254, "020200FE")]
        [InlineData(PublicEncodingRules.BER, 255, "020200FF")]
        [InlineData(PublicEncodingRules.CER, 256, "02020100")]
        [InlineData(PublicEncodingRules.DER, 32767, "02027FFF")]
        [InlineData(PublicEncodingRules.BER, 32766, "02027FFE")]
        [InlineData(PublicEncodingRules.CER, 32768, "0203008000")]
        [InlineData(PublicEncodingRules.DER, 32769, "0203008001")]
        [InlineData(PublicEncodingRules.BER, 65535, "020300FFFF")]
        [InlineData(PublicEncodingRules.CER, 65534, "020300FFFE")]
        [InlineData(PublicEncodingRules.DER, 65536, "0203010000")]
        [InlineData(PublicEncodingRules.BER, 65537, "0203010001")]
        [InlineData(PublicEncodingRules.CER, 8388607, "02037FFFFF")]
        [InlineData(PublicEncodingRules.DER, 8388606, "02037FFFFE")]
        [InlineData(PublicEncodingRules.BER, 8388608, "020400800000")]
        [InlineData(PublicEncodingRules.CER, 8388609, "020400800001")]
        [InlineData(PublicEncodingRules.DER, 16777215, "020400FFFFFF")]
        [InlineData(PublicEncodingRules.BER, 16777214, "020400FFFFFE")]
        [InlineData(PublicEncodingRules.CER, 16777216, "020401000000")]
        [InlineData(PublicEncodingRules.DER, 16777217, "020401000001")]
        [InlineData(PublicEncodingRules.BER, 2147483647, "02047FFFFFFF")]
        [InlineData(PublicEncodingRules.CER, 2147483646, "02047FFFFFFE")]
        [InlineData(PublicEncodingRules.DER, 2147483648, "02050080000000")]
        [InlineData(PublicEncodingRules.BER, 2147483649, "02050080000001")]
        [InlineData(PublicEncodingRules.BER, 4294967295, "020500FFFFFFFF")]
        [InlineData(PublicEncodingRules.CER, 4294967294, "020500FFFFFFFE")]
        [InlineData(PublicEncodingRules.DER, 4294967296, "02050100000000")]
        [InlineData(PublicEncodingRules.BER, 4294967297, "02050100000001")]
        [InlineData(PublicEncodingRules.CER, 549755813887, "02057FFFFFFFFF")]
        [InlineData(PublicEncodingRules.DER, 549755813886, "02057FFFFFFFFE")]
        [InlineData(PublicEncodingRules.BER, 549755813888, "0206008000000000")]
        [InlineData(PublicEncodingRules.CER, 549755813889, "0206008000000001")]
        [InlineData(PublicEncodingRules.DER, 1099511627775, "020600FFFFFFFFFF")]
        [InlineData(PublicEncodingRules.BER, 1099511627774, "020600FFFFFFFFFE")]
        [InlineData(PublicEncodingRules.CER, 1099511627776, "0206010000000000")]
        [InlineData(PublicEncodingRules.DER, 1099511627777, "0206010000000001")]
        [InlineData(PublicEncodingRules.BER, 140737488355327, "02067FFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.CER, 140737488355326, "02067FFFFFFFFFFE")]
        [InlineData(PublicEncodingRules.DER, 140737488355328, "020700800000000000")]
        [InlineData(PublicEncodingRules.BER, 140737488355329, "020700800000000001")]
        [InlineData(PublicEncodingRules.CER, 281474976710655, "020700FFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.DER, 281474976710654, "020700FFFFFFFFFFFE")]
        [InlineData(PublicEncodingRules.BER, 281474976710656, "020701000000000000")]
        [InlineData(PublicEncodingRules.CER, 281474976710657, "020701000000000001")]
        [InlineData(PublicEncodingRules.DER, 36028797018963967, "02077FFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.BER, 36028797018963966, "02077FFFFFFFFFFFFE")]
        [InlineData(PublicEncodingRules.CER, 36028797018963968, "02080080000000000000")]
        [InlineData(PublicEncodingRules.DER, 36028797018963969, "02080080000000000001")]
        [InlineData(PublicEncodingRules.BER, 72057594037927935, "020800FFFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.CER, 72057594037927934, "020800FFFFFFFFFFFFFE")]
        [InlineData(PublicEncodingRules.DER, 72057594037927936, "02080100000000000000")]
        [InlineData(PublicEncodingRules.BER, 72057594037927937, "02080100000000000001")]
        [InlineData(PublicEncodingRules.CER, 9223372036854775807, "02087FFFFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.DER, 9223372036854775806, "02087FFFFFFFFFFFFFFE")]
        public void VerifyWriteInteger_Long(PublicEncodingRules ruleSet, long value, string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteInteger(value);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, 0, "020100")]
        [InlineData(PublicEncodingRules.CER, 0, "020100")]
        [InlineData(PublicEncodingRules.DER, 0, "020100")]
        [InlineData(PublicEncodingRules.BER, 1, "020101")]
        [InlineData(PublicEncodingRules.CER, 127, "02017F")]
        [InlineData(PublicEncodingRules.DER, 126, "02017E")]
        [InlineData(PublicEncodingRules.BER, 128, "02020080")]
        [InlineData(PublicEncodingRules.CER, 129, "02020081")]
        [InlineData(PublicEncodingRules.DER, 254, "020200FE")]
        [InlineData(PublicEncodingRules.BER, 255, "020200FF")]
        [InlineData(PublicEncodingRules.CER, 256, "02020100")]
        [InlineData(PublicEncodingRules.DER, 32767, "02027FFF")]
        [InlineData(PublicEncodingRules.BER, 32766, "02027FFE")]
        [InlineData(PublicEncodingRules.CER, 32768, "0203008000")]
        [InlineData(PublicEncodingRules.DER, 32769, "0203008001")]
        [InlineData(PublicEncodingRules.BER, 65535, "020300FFFF")]
        [InlineData(PublicEncodingRules.CER, 65534, "020300FFFE")]
        [InlineData(PublicEncodingRules.DER, 65536, "0203010000")]
        [InlineData(PublicEncodingRules.BER, 65537, "0203010001")]
        [InlineData(PublicEncodingRules.CER, 8388607, "02037FFFFF")]
        [InlineData(PublicEncodingRules.DER, 8388606, "02037FFFFE")]
        [InlineData(PublicEncodingRules.BER, 8388608, "020400800000")]
        [InlineData(PublicEncodingRules.CER, 8388609, "020400800001")]
        [InlineData(PublicEncodingRules.DER, 16777215, "020400FFFFFF")]
        [InlineData(PublicEncodingRules.BER, 16777214, "020400FFFFFE")]
        [InlineData(PublicEncodingRules.CER, 16777216, "020401000000")]
        [InlineData(PublicEncodingRules.DER, 16777217, "020401000001")]
        [InlineData(PublicEncodingRules.BER, 2147483647, "02047FFFFFFF")]
        [InlineData(PublicEncodingRules.CER, 2147483646, "02047FFFFFFE")]
        [InlineData(PublicEncodingRules.DER, 2147483648, "02050080000000")]
        [InlineData(PublicEncodingRules.BER, 2147483649, "02050080000001")]
        [InlineData(PublicEncodingRules.BER, 4294967295, "020500FFFFFFFF")]
        [InlineData(PublicEncodingRules.CER, 4294967294, "020500FFFFFFFE")]
        [InlineData(PublicEncodingRules.DER, 4294967296, "02050100000000")]
        [InlineData(PublicEncodingRules.BER, 4294967297, "02050100000001")]
        [InlineData(PublicEncodingRules.CER, 549755813887, "02057FFFFFFFFF")]
        [InlineData(PublicEncodingRules.DER, 549755813886, "02057FFFFFFFFE")]
        [InlineData(PublicEncodingRules.BER, 549755813888, "0206008000000000")]
        [InlineData(PublicEncodingRules.CER, 549755813889, "0206008000000001")]
        [InlineData(PublicEncodingRules.DER, 1099511627775, "020600FFFFFFFFFF")]
        [InlineData(PublicEncodingRules.BER, 1099511627774, "020600FFFFFFFFFE")]
        [InlineData(PublicEncodingRules.CER, 1099511627776, "0206010000000000")]
        [InlineData(PublicEncodingRules.DER, 1099511627777, "0206010000000001")]
        [InlineData(PublicEncodingRules.BER, 140737488355327, "02067FFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.CER, 140737488355326, "02067FFFFFFFFFFE")]
        [InlineData(PublicEncodingRules.DER, 140737488355328, "020700800000000000")]
        [InlineData(PublicEncodingRules.BER, 140737488355329, "020700800000000001")]
        [InlineData(PublicEncodingRules.CER, 281474976710655, "020700FFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.DER, 281474976710654, "020700FFFFFFFFFFFE")]
        [InlineData(PublicEncodingRules.BER, 281474976710656, "020701000000000000")]
        [InlineData(PublicEncodingRules.CER, 281474976710657, "020701000000000001")]
        [InlineData(PublicEncodingRules.DER, 36028797018963967, "02077FFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.BER, 36028797018963966, "02077FFFFFFFFFFFFE")]
        [InlineData(PublicEncodingRules.CER, 36028797018963968, "02080080000000000000")]
        [InlineData(PublicEncodingRules.DER, 36028797018963969, "02080080000000000001")]
        [InlineData(PublicEncodingRules.BER, 72057594037927935, "020800FFFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.CER, 72057594037927934, "020800FFFFFFFFFFFFFE")]
        [InlineData(PublicEncodingRules.DER, 72057594037927936, "02080100000000000000")]
        [InlineData(PublicEncodingRules.BER, 72057594037927937, "02080100000000000001")]
        [InlineData(PublicEncodingRules.CER, 9223372036854775807, "02087FFFFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.DER, 9223372036854775806, "02087FFFFFFFFFFFFFFE")]
        [InlineData(PublicEncodingRules.BER, 9223372036854775808, "0209008000000000000000")]
        [InlineData(PublicEncodingRules.CER, 9223372036854775809, "0209008000000000000001")]
        [InlineData(PublicEncodingRules.DER, ulong.MaxValue, "020900FFFFFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.BER, ulong.MaxValue-1, "020900FFFFFFFFFFFFFFFE")]
        public void VerifyWriteInteger_ULong(PublicEncodingRules ruleSet, ulong value, string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteInteger(value);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "0", "020100")]
        [InlineData(PublicEncodingRules.CER, "127", "02017F")]
        [InlineData(PublicEncodingRules.DER, "128", "02020080")]
        [InlineData(PublicEncodingRules.BER, "32767", "02027FFF")]
        [InlineData(PublicEncodingRules.CER, "32768", "0203008000")]
        [InlineData(PublicEncodingRules.DER, "9223372036854775807", "02087FFFFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.BER, "9223372036854775808", "0209008000000000000000")]
        [InlineData(PublicEncodingRules.CER, "18446744073709551615", "020900FFFFFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.DER, "18446744073709551616", "0209010000000000000000")]
        [InlineData(PublicEncodingRules.BER, "1339673755198158349044581307228491520", "02100102030405060708090A0B0C0D0E0F00")]
        [InlineData(PublicEncodingRules.CER, "320182027492359845421654932427609477120", "021100F0E0D0C0B0A090807060504030201000")]
        [InlineData(PublicEncodingRules.DER, "-1339673755198158349044581307228491520", "0210FEFDFCFBFAF9F8F7F6F5F4F3F2F1F100")]
        public void VerifyWriteInteger_BigInteger(PublicEncodingRules ruleSet, string decimalValue, string expectedHex)
        {
            BigInteger value = BigInteger.Parse(decimalValue);

            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteInteger(value);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, 0, "470100")]
        [InlineData(PublicEncodingRules.CER, long.MinValue + 1, "47088000000000000001")]
        [InlineData(PublicEncodingRules.DER, 9223372036854775806, "47087FFFFFFFFFFFFFFE")]
        public void VerifyWriteInteger_Application7_Long(PublicEncodingRules ruleSet, long value, string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteInteger(new Asn1Tag(TagClass.Application, 7), value);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, 0, "890100")]
        [InlineData(PublicEncodingRules.CER, 9223372036854775809, "8909008000000000000001")]
        [InlineData(PublicEncodingRules.DER, 9223372036854775806, "89087FFFFFFFFFFFFFFE")]
        public void VerifyWriteInteger_Context9_ULong(PublicEncodingRules ruleSet, ulong value, string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteInteger(new Asn1Tag(TagClass.ContextSpecific, 9), value);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, 0, "D00100")]
        [InlineData(PublicEncodingRules.BER, "1339673755198158349044581307228491520", "D0100102030405060708090A0B0C0D0E0F00")]
        [InlineData(PublicEncodingRules.CER, "320182027492359845421654932427609477120", "D01100F0E0D0C0B0A090807060504030201000")]
        [InlineData(PublicEncodingRules.DER, "-1339673755198158349044581307228491520", "D010FEFDFCFBFAF9F8F7F6F5F4F3F2F1F100")]
        public void VerifyWriteInteger_Private16_BigInteger(
            PublicEncodingRules ruleSet,
            string decimalValue,
            string expectedHex)
        {
            BigInteger value = BigInteger.Parse(decimalValue);

            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteInteger(new Asn1Tag(TagClass.Private, 16), value);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData("00")]
        [InlineData("01")]
        [InlineData("80")]
        [InlineData("FF")]
        [InlineData("0080")]
        [InlineData("00FF")]
        [InlineData("8000")]
        [InlineData("00F0E0D0C0B0A090807060504030201000")]
        [InlineData("FEFDFCFBFAF9F8F7F6F5F4F3F2F1F100")]
        public void VerifyWriteInteger_EncodedBytes(string valueHex)
        {
            string expectedHex = "02" + (valueHex.Length / 2).ToString("X2") + valueHex;

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                writer.WriteInteger(valueHex.HexToByteArray());

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData("00")]
        [InlineData("01")]
        [InlineData("80")]
        [InlineData("FF")]
        [InlineData("0080")]
        [InlineData("00FF")]
        [InlineData("8000")]
        [InlineData("00F0E0D0C0B0A090807060504030201000")]
        [InlineData("FEFDFCFBFAF9F8F7F6F5F4F3F2F1F100")]
        public void VerifyWriteInteger_Context4_EncodedBytes(string valueHex)
        {
            string expectedHex = "84" + (valueHex.Length / 2).ToString("X2") + valueHex;

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                writer.WriteInteger(new Asn1Tag(TagClass.ContextSpecific, 4), valueHex.HexToByteArray());

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("0000")]
        [InlineData("0000000000000000000001")]
        [InlineData("0001")]
        [InlineData("007F")]
        [InlineData("FFFF")]
        [InlineData("FFFFFFFFFFFFFFFFFFFFFE")]
        [InlineData("FF80")]
        public void VerifyWriteInteger_InvalidEncodedValue_Throws(string valuHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Assert.ThrowsAny<CryptographicException>(() => writer.WriteInteger(valuHex.HexToByteArray()));
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("0000")]
        [InlineData("0000000000000000000001")]
        [InlineData("0001")]
        [InlineData("007F")]
        [InlineData("FFFF")]
        [InlineData("FFFFFFFFFFFFFFFFFFFFFE")]
        [InlineData("FF80")]
        public void VerifyWriteInteger_Application3_InvalidEncodedValue_Throws(string valuHex)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.Application, 3);

                Assert.ThrowsAny<CryptographicException>(
                    () => writer.WriteInteger(tag, valuHex.HexToByteArray()));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void VerifyWriteInteger_EndOfContents(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteInteger(Asn1Tag.EndOfContents, 0L));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteInteger(Asn1Tag.EndOfContents, 0UL));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteInteger(Asn1Tag.EndOfContents, BigInteger.Zero));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void VerifyWriteInteger_ConstructedIgnored(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteInteger(new Asn1Tag(UniversalTagNumber.Integer, isConstructed: true), 0L);
                writer.WriteInteger(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true), 0L);
                writer.WriteInteger(new Asn1Tag(UniversalTagNumber.Integer, isConstructed: true), 0UL);
                writer.WriteInteger(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true), 0UL);
                writer.WriteInteger(new Asn1Tag(UniversalTagNumber.Integer, isConstructed: true), BigInteger.Zero);
                writer.WriteInteger(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true), BigInteger.Zero);

                Verify(writer, "020100800100020100800100020100800100");
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
                    writer.WriteNull();
                }

                writer.Dispose();

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(1));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(1UL));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(BigInteger.One));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(BigInteger.One.ToByteArray()));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(Array.Empty<byte>()));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(new byte[] { 0, 0 }));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(new byte[] { 0xFF, 0xFF }));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteInteger(Asn1Tag.Boolean, 1));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteInteger(Asn1Tag.Boolean, 1UL));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteInteger(Asn1Tag.Boolean, BigInteger.One));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteInteger(Asn1Tag.Boolean, BigInteger.One.ToByteArray()));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteInteger(Asn1Tag.Boolean, Array.Empty<byte>()));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteInteger(Asn1Tag.Boolean, new byte[] { 0, 0 }));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteInteger(Asn1Tag.Boolean, new byte[] { 0xFF, 0xFF }));

                Asn1Tag tag = new Asn1Tag(TagClass.Application, 0);

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(tag, 1));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(tag, 1UL));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(tag, BigInteger.One));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(tag, BigInteger.One.ToByteArray()));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(tag, Array.Empty<byte>()));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(tag, new byte[] { 0, 0 }));

                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteInteger(tag, new byte[] { 0xFF, 0xFF }));
            }
        }
    }
}
