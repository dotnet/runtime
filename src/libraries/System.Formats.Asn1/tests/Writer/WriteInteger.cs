// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class WriteInteger : Asn1WriterTests
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER, 0, "020100")]
        [InlineData(AsnEncodingRules.CER, 0, "020100")]
        [InlineData(AsnEncodingRules.DER, 0, "020100")]
        [InlineData(AsnEncodingRules.BER, -1, "0201FF")]
        [InlineData(AsnEncodingRules.CER, -1, "0201FF")]
        [InlineData(AsnEncodingRules.DER, -1, "0201FF")]
        [InlineData(AsnEncodingRules.BER, -2, "0201FE")]
        [InlineData(AsnEncodingRules.DER, sbyte.MinValue, "020180")]
        [InlineData(AsnEncodingRules.BER, sbyte.MinValue + 1, "020181")]
        [InlineData(AsnEncodingRules.CER, sbyte.MinValue - 1, "0202FF7F")]
        [InlineData(AsnEncodingRules.DER, sbyte.MinValue - 2, "0202FF7E")]
        [InlineData(AsnEncodingRules.BER, -256, "0202FF00")]
        [InlineData(AsnEncodingRules.CER, -257, "0202FEFF")]
        [InlineData(AsnEncodingRules.DER, short.MinValue, "02028000")]
        [InlineData(AsnEncodingRules.BER, short.MinValue + 1, "02028001")]
        [InlineData(AsnEncodingRules.CER, short.MinValue + byte.MaxValue, "020280FF")]
        [InlineData(AsnEncodingRules.DER, short.MinValue - 1, "0203FF7FFF")]
        [InlineData(AsnEncodingRules.BER, short.MinValue - 2, "0203FF7FFE")]
        [InlineData(AsnEncodingRules.CER, -65281, "0203FF00FF")]
        [InlineData(AsnEncodingRules.DER, -8388608, "0203800000")]
        [InlineData(AsnEncodingRules.BER, -8388607, "0203800001")]
        [InlineData(AsnEncodingRules.CER, -8388609, "0204FF7FFFFF")]
        [InlineData(AsnEncodingRules.DER, -16777216, "0204FF000000")]
        [InlineData(AsnEncodingRules.BER, -16777217, "0204FEFFFFFF")]
        [InlineData(AsnEncodingRules.CER, int.MinValue, "020480000000")]
        [InlineData(AsnEncodingRules.DER, int.MinValue + 1, "020480000001")]
        [InlineData(AsnEncodingRules.BER, (long)int.MinValue - 1, "0205FF7FFFFFFF")]
        [InlineData(AsnEncodingRules.CER, (long)int.MinValue - 2, "0205FF7FFFFFFE")]
        [InlineData(AsnEncodingRules.DER, -4294967296, "0205FF00000000")]
        [InlineData(AsnEncodingRules.BER, -4294967295, "0205FF00000001")]
        [InlineData(AsnEncodingRules.CER, -4294967294, "0205FF00000002")]
        [InlineData(AsnEncodingRules.DER, -4294967297, "0205FEFFFFFFFF")]
        [InlineData(AsnEncodingRules.BER, -549755813888, "02058000000000")]
        [InlineData(AsnEncodingRules.CER, -549755813887, "02058000000001")]
        [InlineData(AsnEncodingRules.DER, -549755813889, "0206FF7FFFFFFFFF")]
        [InlineData(AsnEncodingRules.BER, -549755813890, "0206FF7FFFFFFFFE")]
        [InlineData(AsnEncodingRules.CER, -140737488355328, "0206800000000000")]
        [InlineData(AsnEncodingRules.DER, -140737488355327, "0206800000000001")]
        [InlineData(AsnEncodingRules.BER, -140737488355329, "0207FF7FFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.CER, -281474976710656, "0207FF000000000000")]
        [InlineData(AsnEncodingRules.DER, -281474976710655, "0207FF000000000001")]
        [InlineData(AsnEncodingRules.BER, -281474976710657, "0207FEFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.CER, -36028797018963968, "020780000000000000")]
        [InlineData(AsnEncodingRules.DER, -36028797018963967, "020780000000000001")]
        [InlineData(AsnEncodingRules.DER, -36028797018963969, "0208FF7FFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.BER, -36028797018963970, "0208FF7FFFFFFFFFFFFE")]
        [InlineData(AsnEncodingRules.CER, -72057594037927936, "0208FF00000000000000")]
        [InlineData(AsnEncodingRules.DER, -72057594037927935, "0208FF00000000000001")]
        [InlineData(AsnEncodingRules.BER, -72057594037927937, "0208FEFFFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.CER, long.MinValue + 1, "02088000000000000001")]
        [InlineData(AsnEncodingRules.DER, long.MinValue, "02088000000000000000")]
        [InlineData(AsnEncodingRules.BER, 1, "020101")]
        [InlineData(AsnEncodingRules.CER, 127, "02017F")]
        [InlineData(AsnEncodingRules.DER, 126, "02017E")]
        [InlineData(AsnEncodingRules.BER, 128, "02020080")]
        [InlineData(AsnEncodingRules.CER, 129, "02020081")]
        [InlineData(AsnEncodingRules.DER, 254, "020200FE")]
        [InlineData(AsnEncodingRules.BER, 255, "020200FF")]
        [InlineData(AsnEncodingRules.CER, 256, "02020100")]
        [InlineData(AsnEncodingRules.DER, 32767, "02027FFF")]
        [InlineData(AsnEncodingRules.BER, 32766, "02027FFE")]
        [InlineData(AsnEncodingRules.CER, 32768, "0203008000")]
        [InlineData(AsnEncodingRules.DER, 32769, "0203008001")]
        [InlineData(AsnEncodingRules.BER, 65535, "020300FFFF")]
        [InlineData(AsnEncodingRules.CER, 65534, "020300FFFE")]
        [InlineData(AsnEncodingRules.DER, 65536, "0203010000")]
        [InlineData(AsnEncodingRules.BER, 65537, "0203010001")]
        [InlineData(AsnEncodingRules.CER, 8388607, "02037FFFFF")]
        [InlineData(AsnEncodingRules.DER, 8388606, "02037FFFFE")]
        [InlineData(AsnEncodingRules.BER, 8388608, "020400800000")]
        [InlineData(AsnEncodingRules.CER, 8388609, "020400800001")]
        [InlineData(AsnEncodingRules.DER, 16777215, "020400FFFFFF")]
        [InlineData(AsnEncodingRules.BER, 16777214, "020400FFFFFE")]
        [InlineData(AsnEncodingRules.CER, 16777216, "020401000000")]
        [InlineData(AsnEncodingRules.DER, 16777217, "020401000001")]
        [InlineData(AsnEncodingRules.BER, 2147483647, "02047FFFFFFF")]
        [InlineData(AsnEncodingRules.CER, 2147483646, "02047FFFFFFE")]
        [InlineData(AsnEncodingRules.DER, 2147483648, "02050080000000")]
        [InlineData(AsnEncodingRules.BER, 2147483649, "02050080000001")]
        [InlineData(AsnEncodingRules.BER, 4294967295, "020500FFFFFFFF")]
        [InlineData(AsnEncodingRules.CER, 4294967294, "020500FFFFFFFE")]
        [InlineData(AsnEncodingRules.DER, 4294967296, "02050100000000")]
        [InlineData(AsnEncodingRules.BER, 4294967297, "02050100000001")]
        [InlineData(AsnEncodingRules.CER, 549755813887, "02057FFFFFFFFF")]
        [InlineData(AsnEncodingRules.DER, 549755813886, "02057FFFFFFFFE")]
        [InlineData(AsnEncodingRules.BER, 549755813888, "0206008000000000")]
        [InlineData(AsnEncodingRules.CER, 549755813889, "0206008000000001")]
        [InlineData(AsnEncodingRules.DER, 1099511627775, "020600FFFFFFFFFF")]
        [InlineData(AsnEncodingRules.BER, 1099511627774, "020600FFFFFFFFFE")]
        [InlineData(AsnEncodingRules.CER, 1099511627776, "0206010000000000")]
        [InlineData(AsnEncodingRules.DER, 1099511627777, "0206010000000001")]
        [InlineData(AsnEncodingRules.BER, 140737488355327, "02067FFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.CER, 140737488355326, "02067FFFFFFFFFFE")]
        [InlineData(AsnEncodingRules.DER, 140737488355328, "020700800000000000")]
        [InlineData(AsnEncodingRules.BER, 140737488355329, "020700800000000001")]
        [InlineData(AsnEncodingRules.CER, 281474976710655, "020700FFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.DER, 281474976710654, "020700FFFFFFFFFFFE")]
        [InlineData(AsnEncodingRules.BER, 281474976710656, "020701000000000000")]
        [InlineData(AsnEncodingRules.CER, 281474976710657, "020701000000000001")]
        [InlineData(AsnEncodingRules.DER, 36028797018963967, "02077FFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.BER, 36028797018963966, "02077FFFFFFFFFFFFE")]
        [InlineData(AsnEncodingRules.CER, 36028797018963968, "02080080000000000000")]
        [InlineData(AsnEncodingRules.DER, 36028797018963969, "02080080000000000001")]
        [InlineData(AsnEncodingRules.BER, 72057594037927935, "020800FFFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.CER, 72057594037927934, "020800FFFFFFFFFFFFFE")]
        [InlineData(AsnEncodingRules.DER, 72057594037927936, "02080100000000000000")]
        [InlineData(AsnEncodingRules.BER, 72057594037927937, "02080100000000000001")]
        [InlineData(AsnEncodingRules.CER, 9223372036854775807, "02087FFFFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.DER, 9223372036854775806, "02087FFFFFFFFFFFFFFE")]
        public void VerifyWriteInteger_Long(AsnEncodingRules ruleSet, long value, string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteInteger(value);

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, 0, "020100")]
        [InlineData(AsnEncodingRules.CER, 0, "020100")]
        [InlineData(AsnEncodingRules.DER, 0, "020100")]
        [InlineData(AsnEncodingRules.BER, 1, "020101")]
        [InlineData(AsnEncodingRules.CER, 127, "02017F")]
        [InlineData(AsnEncodingRules.DER, 126, "02017E")]
        [InlineData(AsnEncodingRules.BER, 128, "02020080")]
        [InlineData(AsnEncodingRules.CER, 129, "02020081")]
        [InlineData(AsnEncodingRules.DER, 254, "020200FE")]
        [InlineData(AsnEncodingRules.BER, 255, "020200FF")]
        [InlineData(AsnEncodingRules.CER, 256, "02020100")]
        [InlineData(AsnEncodingRules.DER, 32767, "02027FFF")]
        [InlineData(AsnEncodingRules.BER, 32766, "02027FFE")]
        [InlineData(AsnEncodingRules.CER, 32768, "0203008000")]
        [InlineData(AsnEncodingRules.DER, 32769, "0203008001")]
        [InlineData(AsnEncodingRules.BER, 65535, "020300FFFF")]
        [InlineData(AsnEncodingRules.CER, 65534, "020300FFFE")]
        [InlineData(AsnEncodingRules.DER, 65536, "0203010000")]
        [InlineData(AsnEncodingRules.BER, 65537, "0203010001")]
        [InlineData(AsnEncodingRules.CER, 8388607, "02037FFFFF")]
        [InlineData(AsnEncodingRules.DER, 8388606, "02037FFFFE")]
        [InlineData(AsnEncodingRules.BER, 8388608, "020400800000")]
        [InlineData(AsnEncodingRules.CER, 8388609, "020400800001")]
        [InlineData(AsnEncodingRules.DER, 16777215, "020400FFFFFF")]
        [InlineData(AsnEncodingRules.BER, 16777214, "020400FFFFFE")]
        [InlineData(AsnEncodingRules.CER, 16777216, "020401000000")]
        [InlineData(AsnEncodingRules.DER, 16777217, "020401000001")]
        [InlineData(AsnEncodingRules.BER, 2147483647, "02047FFFFFFF")]
        [InlineData(AsnEncodingRules.CER, 2147483646, "02047FFFFFFE")]
        [InlineData(AsnEncodingRules.DER, 2147483648, "02050080000000")]
        [InlineData(AsnEncodingRules.BER, 2147483649, "02050080000001")]
        [InlineData(AsnEncodingRules.BER, 4294967295, "020500FFFFFFFF")]
        [InlineData(AsnEncodingRules.CER, 4294967294, "020500FFFFFFFE")]
        [InlineData(AsnEncodingRules.DER, 4294967296, "02050100000000")]
        [InlineData(AsnEncodingRules.BER, 4294967297, "02050100000001")]
        [InlineData(AsnEncodingRules.CER, 549755813887, "02057FFFFFFFFF")]
        [InlineData(AsnEncodingRules.DER, 549755813886, "02057FFFFFFFFE")]
        [InlineData(AsnEncodingRules.BER, 549755813888, "0206008000000000")]
        [InlineData(AsnEncodingRules.CER, 549755813889, "0206008000000001")]
        [InlineData(AsnEncodingRules.DER, 1099511627775, "020600FFFFFFFFFF")]
        [InlineData(AsnEncodingRules.BER, 1099511627774, "020600FFFFFFFFFE")]
        [InlineData(AsnEncodingRules.CER, 1099511627776, "0206010000000000")]
        [InlineData(AsnEncodingRules.DER, 1099511627777, "0206010000000001")]
        [InlineData(AsnEncodingRules.BER, 140737488355327, "02067FFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.CER, 140737488355326, "02067FFFFFFFFFFE")]
        [InlineData(AsnEncodingRules.DER, 140737488355328, "020700800000000000")]
        [InlineData(AsnEncodingRules.BER, 140737488355329, "020700800000000001")]
        [InlineData(AsnEncodingRules.CER, 281474976710655, "020700FFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.DER, 281474976710654, "020700FFFFFFFFFFFE")]
        [InlineData(AsnEncodingRules.BER, 281474976710656, "020701000000000000")]
        [InlineData(AsnEncodingRules.CER, 281474976710657, "020701000000000001")]
        [InlineData(AsnEncodingRules.DER, 36028797018963967, "02077FFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.BER, 36028797018963966, "02077FFFFFFFFFFFFE")]
        [InlineData(AsnEncodingRules.CER, 36028797018963968, "02080080000000000000")]
        [InlineData(AsnEncodingRules.DER, 36028797018963969, "02080080000000000001")]
        [InlineData(AsnEncodingRules.BER, 72057594037927935, "020800FFFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.CER, 72057594037927934, "020800FFFFFFFFFFFFFE")]
        [InlineData(AsnEncodingRules.DER, 72057594037927936, "02080100000000000000")]
        [InlineData(AsnEncodingRules.BER, 72057594037927937, "02080100000000000001")]
        [InlineData(AsnEncodingRules.CER, 9223372036854775807, "02087FFFFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.DER, 9223372036854775806, "02087FFFFFFFFFFFFFFE")]
        [InlineData(AsnEncodingRules.BER, 9223372036854775808, "0209008000000000000000")]
        [InlineData(AsnEncodingRules.CER, 9223372036854775809, "0209008000000000000001")]
        [InlineData(AsnEncodingRules.DER, ulong.MaxValue, "020900FFFFFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.BER, ulong.MaxValue-1, "020900FFFFFFFFFFFFFFFE")]
        public void VerifyWriteInteger_ULong(AsnEncodingRules ruleSet, ulong value, string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteInteger(value);

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "0", "020100")]
        [InlineData(AsnEncodingRules.CER, "127", "02017F")]
        [InlineData(AsnEncodingRules.DER, "128", "02020080")]
        [InlineData(AsnEncodingRules.BER, "32767", "02027FFF")]
        [InlineData(AsnEncodingRules.CER, "32768", "0203008000")]
        [InlineData(AsnEncodingRules.DER, "9223372036854775807", "02087FFFFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.BER, "9223372036854775808", "0209008000000000000000")]
        [InlineData(AsnEncodingRules.CER, "18446744073709551615", "020900FFFFFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.DER, "18446744073709551616", "0209010000000000000000")]
        [InlineData(AsnEncodingRules.BER, "1339673755198158349044581307228491520", "02100102030405060708090A0B0C0D0E0F00")]
        [InlineData(AsnEncodingRules.CER, "320182027492359845421654932427609477120", "021100F0E0D0C0B0A090807060504030201000")]
        [InlineData(AsnEncodingRules.DER, "-1339673755198158349044581307228491520", "0210FEFDFCFBFAF9F8F7F6F5F4F3F2F1F100")]
        public void VerifyWriteInteger_BigInteger(AsnEncodingRules ruleSet, string decimalValue, string expectedHex)
        {
            BigInteger value = BigInteger.Parse(decimalValue);

            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteInteger(value);

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, 0, "470100")]
        [InlineData(AsnEncodingRules.CER, long.MinValue + 1, "47088000000000000001")]
        [InlineData(AsnEncodingRules.DER, 9223372036854775806, "47087FFFFFFFFFFFFFFE")]
        public void VerifyWriteInteger_Application7_Long(AsnEncodingRules ruleSet, long value, string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteInteger(value, new Asn1Tag(TagClass.Application, 7));

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, 0, "890100")]
        [InlineData(AsnEncodingRules.CER, 9223372036854775809, "8909008000000000000001")]
        [InlineData(AsnEncodingRules.DER, 9223372036854775806, "89087FFFFFFFFFFFFFFE")]
        public void VerifyWriteInteger_Context9_ULong(AsnEncodingRules ruleSet, ulong value, string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteInteger(value, new Asn1Tag(TagClass.ContextSpecific, 9));

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "0", "D00100")]
        [InlineData(AsnEncodingRules.BER, "1339673755198158349044581307228491520", "D0100102030405060708090A0B0C0D0E0F00")]
        [InlineData(AsnEncodingRules.CER, "320182027492359845421654932427609477120", "D01100F0E0D0C0B0A090807060504030201000")]
        [InlineData(AsnEncodingRules.DER, "-1339673755198158349044581307228491520", "D010FEFDFCFBFAF9F8F7F6F5F4F3F2F1F100")]
        public void VerifyWriteInteger_Private16_BigInteger(
            AsnEncodingRules ruleSet,
            string decimalValue,
            string expectedHex)
        {
            BigInteger value = BigInteger.Parse(decimalValue);

            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteInteger(value, new Asn1Tag(TagClass.Private, 16));

            Verify(writer, expectedHex);
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
            string expectedHex = $"02{valueHex.Length / 2:X2}{valueHex}";

            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            writer.WriteInteger(valueHex.HexToByteArray());

            Verify(writer, expectedHex);
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
            string expectedHex = $"84{valueHex.Length / 2:X2}{valueHex}";

            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            writer.WriteInteger(valueHex.HexToByteArray(), new Asn1Tag(TagClass.ContextSpecific, 4));

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData("00", false)]
        [InlineData("7F", false)]
        [InlineData("80", true)]
        [InlineData("8002030405060708090A0B0C0D0E0F10", true)]
        [InlineData("7F02030405060708090A0B0C0D0E0F10", false)]
        [InlineData("FFFF", true)]
        [InlineData("FFFFFFFFFFFFFFFFFFFFFE", true)]
        [InlineData("FF80", true)]
        public void VerifyWriteUnsignedInteger(string valueHex, bool gainsPaddingByte)
        {
            int contentLength = (valueHex.Length / 2) + (gainsPaddingByte ? 1 : 0);
            string expectedHex = $"02{contentLength:X2}{(gainsPaddingByte ? "00" : "")}{valueHex}";

            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            writer.WriteIntegerUnsigned(valueHex.HexToByteArray());

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData("00", false)]
        [InlineData("7F", false)]
        [InlineData("80", true)]
        [InlineData("8002030405060708090A0B0C0D0E0F10", true)]
        [InlineData("7F02030405060708090A0B0C0D0E0F10", false)]
        [InlineData("FFFF", true)]
        [InlineData("FFFFFFFFFFFFFFFFFFFFFE", true)]
        [InlineData("FF80", true)]
        public void VerifyWriteUnsignedInteger_Private7(string valueHex, bool gainsPaddingByte)
        {
            int contentLength = (valueHex.Length / 2) + (gainsPaddingByte ? 1 : 0);
            string expectedHex = $"C7{contentLength:X2}{(gainsPaddingByte ? "00" : "")}{valueHex}";

            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            writer.WriteIntegerUnsigned(valueHex.HexToByteArray(), new Asn1Tag(TagClass.Private, 7));

            Verify(writer, expectedHex);
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
        public void VerifyWriteInteger_InvalidEncodedValue_Throws(string valueHex)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            AssertExtensions.Throws<ArgumentException>(
                "value",
                () => writer.WriteInteger(valueHex.HexToByteArray()));
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
        public void VerifyWriteInteger_Application3_InvalidEncodedValue_Throws(string valueHex)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            Asn1Tag tag = new Asn1Tag(TagClass.Application, 3);

            AssertExtensions.Throws<ArgumentException>(
                "value",
                () => writer.WriteInteger(valueHex.HexToByteArray(), tag));
        }

        [Theory]
        [InlineData("")]
        [InlineData("0000")]
        [InlineData("0000000000000000000001")]
        [InlineData("0001")]
        [InlineData("007F")]
        public void VerifyWriteIntegerUnsigned_InvalidEncodedValue_Throws(string valueHex)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            AssertExtensions.Throws<ArgumentException>(
                "value",
                () => writer.WriteIntegerUnsigned(valueHex.HexToByteArray()));
        }

        [Theory]
        [InlineData("")]
        [InlineData("0000")]
        [InlineData("0000000000000000000001")]
        [InlineData("0001")]
        [InlineData("007F")]
        public void VerifyWriteIntegerUnsigned_Application3_InvalidEncodedValue_Throws(string valueHex)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            Asn1Tag tag = new Asn1Tag(TagClass.Application, 3);

            AssertExtensions.Throws<ArgumentException>(
                "value",
                () => writer.WriteIntegerUnsigned(valueHex.HexToByteArray(), tag));
        }


        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWriteInteger_Null(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteInteger(0L, Asn1Tag.Null));

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteInteger(0UL, Asn1Tag.Null));

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteInteger(BigInteger.Zero, Asn1Tag.Null));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWriteInteger_ConstructedIgnored(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteInteger(0L, new Asn1Tag(UniversalTagNumber.Integer, isConstructed: true));
            writer.WriteInteger(0L, new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true));
            writer.WriteInteger(0UL, new Asn1Tag(UniversalTagNumber.Integer, isConstructed: true));
            writer.WriteInteger(0UL, new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true));
            writer.WriteInteger(BigInteger.Zero, new Asn1Tag(UniversalTagNumber.Integer, isConstructed: true));
            writer.WriteInteger(BigInteger.Zero, new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true));

            Verify(writer, "020100800100020100800100020100800100");
        }
    }
}
