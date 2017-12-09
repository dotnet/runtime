// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public sealed class ReadInteger : Asn1ReaderTests
    {
        [Theory]
        [InlineData("Constructed Encoding", PublicEncodingRules.BER, "2203020100")]
        [InlineData("Constructed Encoding-Indefinite", PublicEncodingRules.BER, "228002010000")]
        [InlineData("Constructed Encoding-Indefinite", PublicEncodingRules.CER, "228002010000")]
        [InlineData("Constructed Encoding", PublicEncodingRules.DER, "2203020100")]
        [InlineData("Wrong Universal Tag", PublicEncodingRules.BER, "030100")]
        [InlineData("Bad Length", PublicEncodingRules.BER, "02030102")]
        [InlineData("Incorrect Zero Encoding", PublicEncodingRules.BER, "0200")]
        [InlineData("Incorrect Zero Encoding", PublicEncodingRules.CER, "0200")]
        [InlineData("Incorrect Zero Encoding", PublicEncodingRules.DER, "0200")]
        [InlineData("Redundant Leading 0x00", PublicEncodingRules.BER, "0202007F")]
        [InlineData("Redundant Leading 0x00", PublicEncodingRules.CER, "0202007F")]
        [InlineData("Redundant Leading 0x00", PublicEncodingRules.DER, "0202007F")]
        [InlineData("Redundant Leading 0xFF", PublicEncodingRules.BER, "0202FF80")]
        [InlineData("Redundant Leading 0xFF", PublicEncodingRules.CER, "0202FF80")]
        [InlineData("Redundant Leading 0xFF", PublicEncodingRules.DER, "0202FF80")]
        public static void InvalidData(
            string description,
            PublicEncodingRules ruleSet,
            string inputHex)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(() => reader.GetIntegerBytes());
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "020100", 0)]
        [InlineData(PublicEncodingRules.CER, "020100", 0)]
        [InlineData(PublicEncodingRules.DER, "020100", 0)]
        [InlineData(PublicEncodingRules.DER, "02017F", sbyte.MaxValue)]
        [InlineData(PublicEncodingRules.DER, "020180", sbyte.MinValue)]
        [InlineData(PublicEncodingRules.DER, "0201FF", -1)]
        public static void ReadInt8_Success(
            PublicEncodingRules ruleSet,
            string inputHex,
            sbyte expectedValue)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadInt8(out sbyte value);

            Assert.True(didRead, "reader.TryReadInt8");
            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "02020102")]
        [InlineData(PublicEncodingRules.CER, "02020102")]
        [InlineData(PublicEncodingRules.DER, "02020102")]
        public static void ReadInt8_TooMuchData(
            PublicEncodingRules ruleSet,
            string inputHex)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadInt8(out sbyte value);

            Assert.False(didRead, "reader.TryReadInt8");
            Assert.Equal(0, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "020100", 0)]
        [InlineData(PublicEncodingRules.CER, "02017F", 0x7F)]
        [InlineData(PublicEncodingRules.CER, "02020080", 0x80)]
        [InlineData(PublicEncodingRules.CER, "020200FF", 0xFF)]
        public static void ReadUInt8_Success(
            PublicEncodingRules ruleSet,
            string inputHex,
            byte expectedValue)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadUInt8(out byte value);

            Assert.True(didRead, "reader.TryReadUInt8");
            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "020180")]
        [InlineData(PublicEncodingRules.CER, "020180")]
        [InlineData(PublicEncodingRules.DER, "020180")]
        [InlineData(PublicEncodingRules.BER, "0201FF")]
        [InlineData(PublicEncodingRules.CER, "0201FF")]
        [InlineData(PublicEncodingRules.DER, "0201FF")]
        public static void ReadUInt8_Failure(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadUInt8(out byte value);

            Assert.False(didRead, "reader.TryReadUInt8");
            Assert.Equal((byte)0, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "020100", 0)]
        [InlineData(PublicEncodingRules.CER, "020100", 0)]
        [InlineData(PublicEncodingRules.DER, "020100", 0)]
        [InlineData(PublicEncodingRules.DER, "0201FF", -1)]
        [InlineData(PublicEncodingRules.CER, "0202FEFF", unchecked((short)0xFEFF))]
        [InlineData(PublicEncodingRules.BER, "028102FEEF", unchecked((short)0xFEEF))]
        [InlineData(PublicEncodingRules.BER, "0281028000", short.MinValue)]
        [InlineData(PublicEncodingRules.CER, "02028000", short.MinValue)]
        [InlineData(PublicEncodingRules.DER, "02027FFF", short.MaxValue)]
        [InlineData(PublicEncodingRules.DER, "02026372", 0x6372)]
        [InlineData(PublicEncodingRules.CER, "0202008A", 0x8A)]
        [InlineData(PublicEncodingRules.CER, "02028ACE", unchecked((short)0x8ACE))]
        public static void ReadInt16_Success(
            PublicEncodingRules ruleSet,
            string inputHex,
            short expectedValue)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadInt16(out short value);

            Assert.True(didRead, "reader.TryReadInt16");
            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "0203010203")]
        [InlineData(PublicEncodingRules.CER, "0203010203")]
        [InlineData(PublicEncodingRules.DER, "0203010203")]
        public static void ReadInt16_TooMuchData(
            PublicEncodingRules ruleSet,
            string inputHex)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadInt16(out short value);

            Assert.False(didRead, "reader.TryReadInt16");
            Assert.Equal(0, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "020100", 0)]
        [InlineData(PublicEncodingRules.CER, "02020080", 0x80)]
        [InlineData(PublicEncodingRules.DER, "02027F80", 0x7F80)]
        [InlineData(PublicEncodingRules.DER, "0203008180", 0x8180)]
        public static void ReadUInt16_Success(
            PublicEncodingRules ruleSet,
            string inputHex,
            ushort expectedValue)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadUInt16(out ushort value);

            Assert.True(didRead, "reader.TryReadUInt16");
            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "020180")]
        [InlineData(PublicEncodingRules.CER, "020180")]
        [InlineData(PublicEncodingRules.DER, "020180")]
        [InlineData(PublicEncodingRules.BER, "0201FF")]
        [InlineData(PublicEncodingRules.CER, "0201FF")]
        [InlineData(PublicEncodingRules.DER, "0201FF")]
        [InlineData(PublicEncodingRules.DER, "02028000")]
        public static void ReadUInt16_Failure(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadUInt16(out ushort value);

            Assert.False(didRead, "reader.TryReadUInt16");
            Assert.Equal((ushort)0, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "020100", 0)]
        [InlineData(PublicEncodingRules.CER, "020100", 0)]
        [InlineData(PublicEncodingRules.DER, "020100", 0)]
        [InlineData(PublicEncodingRules.DER, "0201FF", -1)]
        [InlineData(PublicEncodingRules.CER, "0202FEFF", unchecked((int)0xFFFF_FEFF))]
        [InlineData(PublicEncodingRules.BER, "028102FEEF", unchecked((int)0xFFFF_FEEF))]
        [InlineData(PublicEncodingRules.BER, "02810480000000", int.MinValue)]
        [InlineData(PublicEncodingRules.CER, "020480000000", int.MinValue)]
        [InlineData(PublicEncodingRules.DER, "02047FFFFFFF", int.MaxValue)]
        [InlineData(PublicEncodingRules.DER, "02026372", 0x6372)]
        [InlineData(PublicEncodingRules.CER, "0203008ACE", 0x8ACE)]
        [InlineData(PublicEncodingRules.BER, "0203FACE01", unchecked((int)0xFFFA_CE01))]
        [InlineData(PublicEncodingRules.BER, "02820003FACE01", unchecked((int)0xFFFA_CE01))]
        public static void ReadInt32_Success(
            PublicEncodingRules ruleSet,
            string inputHex,
            int expectedValue)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadInt32(out int value);

            Assert.True(didRead, "reader.TryReadInt32");
            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "02050102030405")]
        [InlineData(PublicEncodingRules.CER, "02050102030405")]
        [InlineData(PublicEncodingRules.DER, "02050102030405")]
        public static void ReadInt32_TooMuchData(
            PublicEncodingRules ruleSet,
            string inputHex)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadInt32(out int value);

            Assert.False(didRead, "reader.TryReadInt32");
            Assert.Equal(0, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "020100", 0)]
        [InlineData(PublicEncodingRules.CER, "02020080", 0x80)]
        [InlineData(PublicEncodingRules.DER, "02027F80", 0x7F80)]
        [InlineData(PublicEncodingRules.DER, "0203008180", 0x8180)]
        [InlineData(PublicEncodingRules.DER, "02030A8180", 0xA8180)]
        [InlineData(PublicEncodingRules.DER, "020400828180", 0x828180)]
        [InlineData(PublicEncodingRules.DER, "020475828180", 0x75828180)]
        [InlineData(PublicEncodingRules.DER, "02050083828180", 0x83828180)]
        [InlineData(PublicEncodingRules.BER, "02830000050083828180", 0x83828180)]
        public static void ReadUInt32_Success(
            PublicEncodingRules ruleSet,
            string inputHex,
            uint expectedValue)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadUInt32(out uint value);

            Assert.True(didRead, "reader.TryReadUInt32");
            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "020180")]
        [InlineData(PublicEncodingRules.CER, "020180")]
        [InlineData(PublicEncodingRules.DER, "020180")]
        [InlineData(PublicEncodingRules.BER, "0201FF")]
        [InlineData(PublicEncodingRules.CER, "0201FF")]
        [InlineData(PublicEncodingRules.DER, "0201FF")]
        [InlineData(PublicEncodingRules.DER, "02028000")]
        [InlineData(PublicEncodingRules.DER, "0203800000")]
        [InlineData(PublicEncodingRules.DER, "020480000000")]
        [InlineData(PublicEncodingRules.DER, "02050100000000")]
        public static void ReadUInt32_Failure(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadUInt32(out uint value);

            Assert.False(didRead, "reader.TryReadUInt32");
            Assert.Equal((uint)0, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "020100", 0)]
        [InlineData(PublicEncodingRules.CER, "02020080", 0x80)]
        [InlineData(PublicEncodingRules.DER, "02027F80", 0x7F80)]
        [InlineData(PublicEncodingRules.DER, "0203008180", 0x8180)]
        [InlineData(PublicEncodingRules.DER, "02030A8180", 0xA8180)]
        [InlineData(PublicEncodingRules.DER, "020400828180", 0x828180)]
        [InlineData(PublicEncodingRules.DER, "020475828180", 0x75828180)]
        [InlineData(PublicEncodingRules.DER, "02050083828180", 0x83828180)]
        [InlineData(PublicEncodingRules.BER, "02830000050083828180", 0x83828180)]
        [InlineData(PublicEncodingRules.DER, "02050183828180", 0x0183828180)]
        [InlineData(PublicEncodingRules.DER, "0206018483828180", 0x018483828180)]
        [InlineData(PublicEncodingRules.DER, "020701858483828180", 0x01858483828180)]
        [InlineData(PublicEncodingRules.DER, "02080186858483828180", 0x0186858483828180)]
        [InlineData(PublicEncodingRules.DER, "02087F86858483828180", 0x7F86858483828180)]
        [InlineData(PublicEncodingRules.DER, "02087FFFFFFFFFFFFFFF", long.MaxValue)]
        [InlineData(PublicEncodingRules.DER, "0201FF", -1)]
        [InlineData(PublicEncodingRules.DER, "0201FE", -2)]
        [InlineData(PublicEncodingRules.DER, "02028012", unchecked((long)0xFFFFFFFF_FFFF8012))]
        [InlineData(PublicEncodingRules.DER, "0203818012", unchecked((long)0xFFFFFFFF_FF818012))]
        [InlineData(PublicEncodingRules.DER, "020482818012", unchecked((long)0xFFFFFFFF_82818012))]
        [InlineData(PublicEncodingRules.DER, "02058382818012", unchecked((long)0xFFFFFF83_82818012))]
        [InlineData(PublicEncodingRules.DER, "0206848382818012", unchecked((long)0xFFFF8483_82818012))]
        [InlineData(PublicEncodingRules.DER, "020785848382818012", unchecked((long)0xFF858483_82818012))]
        [InlineData(PublicEncodingRules.DER, "02088685848382818012", unchecked((long)0x86858483_82818012))]
        [InlineData(PublicEncodingRules.DER, "02088000000000000000", long.MinValue)]
        [InlineData(PublicEncodingRules.BER, "028800000000000000088000000000000000", long.MinValue)]
        public static void ReadInt64_Success(
            PublicEncodingRules ruleSet,
            string inputHex,
            long expectedValue)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadInt64(out long value);

            Assert.True(didRead, "reader.TryReadInt64");
            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "0209010203040506070809")]
        [InlineData(PublicEncodingRules.CER, "0209010203040506070809")]
        [InlineData(PublicEncodingRules.DER, "0209010203040506070809")]
        public static void ReadInt64_TooMuchData(
            PublicEncodingRules ruleSet,
            string inputHex)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadInt64(out long value);

            Assert.False(didRead, "reader.TryReadInt64");
            Assert.Equal(0, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "020100", 0)]
        [InlineData(PublicEncodingRules.CER, "02020080", 0x80)]
        [InlineData(PublicEncodingRules.DER, "02027F80", 0x7F80)]
        [InlineData(PublicEncodingRules.DER, "0203008180", 0x8180)]
        [InlineData(PublicEncodingRules.DER, "02030A8180", 0xA8180)]
        [InlineData(PublicEncodingRules.DER, "020400828180", 0x828180)]
        [InlineData(PublicEncodingRules.DER, "020475828180", 0x75828180)]
        [InlineData(PublicEncodingRules.DER, "02050083828180", 0x83828180)]
        [InlineData(PublicEncodingRules.BER, "02830000050083828180", 0x83828180)]
        [InlineData(PublicEncodingRules.DER, "02050183828180", 0x0183828180)]
        [InlineData(PublicEncodingRules.DER, "0206018483828180", 0x018483828180)]
        [InlineData(PublicEncodingRules.DER, "020701858483828180", 0x01858483828180)]
        [InlineData(PublicEncodingRules.DER, "02080186858483828180", 0x0186858483828180)]
        [InlineData(PublicEncodingRules.DER, "02087F86858483828180", 0x7F86858483828180)]
        [InlineData(PublicEncodingRules.DER, "02087FFFFFFFFFFFFFFF", long.MaxValue)]
        [InlineData(PublicEncodingRules.DER, "0209008000000000000000", 0x80000000_00000000)]
        [InlineData(PublicEncodingRules.DER, "020900FFFFFFFFFFFFFFFF", ulong.MaxValue)]
        public static void ReadUInt64_Success(
            PublicEncodingRules ruleSet,
            string inputHex,
            ulong expectedValue)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadUInt64(out ulong value);

            Assert.True(didRead, "reader.TryReadUInt64");
            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "020180")]
        [InlineData(PublicEncodingRules.CER, "020180")]
        [InlineData(PublicEncodingRules.DER, "020180")]
        [InlineData(PublicEncodingRules.BER, "0201FF")]
        [InlineData(PublicEncodingRules.CER, "0201FF")]
        [InlineData(PublicEncodingRules.DER, "0201FF")]
        [InlineData(PublicEncodingRules.DER, "02028000")]
        [InlineData(PublicEncodingRules.DER, "0203800000")]
        [InlineData(PublicEncodingRules.DER, "020480000000")]
        [InlineData(PublicEncodingRules.DER, "02058000000000")]
        [InlineData(PublicEncodingRules.DER, "0206800000000000")]
        [InlineData(PublicEncodingRules.DER, "020780000000000000")]
        [InlineData(PublicEncodingRules.DER, "02088000000000000000")]
        [InlineData(PublicEncodingRules.DER, "0209010000000000000000")]
        public static void ReadUInt64_Failure(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] data = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadUInt64(out ulong value);

            Assert.False(didRead, "reader.TryReadUInt64");
            Assert.Equal((uint)0, value);
        }

        [Fact]
        public static void GetIntegerBytes()
        {
            const string Payload = "0102030405060708090A0B0C0D0E0F10";

            // INTEGER (payload) followed by INTEGER (0)
            byte[] data = ("0210" + Payload + "020100").HexToByteArray();
            AsnReader reader = new AsnReader(data, AsnEncodingRules.DER);

            ReadOnlyMemory<byte> contents = reader.GetIntegerBytes();
            Assert.Equal(0x10, contents.Length);
            Assert.Equal(Payload, contents.ByteArrayToHex());
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(PublicEncodingRules ruleSet)
        {
            byte[] inputData = { 2, 1, 0x7E };
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.GetIntegerBytes(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(() => reader.GetIntegerBytes(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            ReadOnlyMemory<byte> value = reader.GetIntegerBytes();
            Assert.Equal("7E", value.ByteArrayToHex());
            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom(PublicEncodingRules ruleSet)
        {
            byte[] inputData = { 0x87, 2, 0, 0x80 };
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.GetIntegerBytes(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(() => reader.GetIntegerBytes());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<CryptographicException>(() => reader.GetIntegerBytes(new Asn1Tag(TagClass.Application, 0)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<CryptographicException>(() => reader.GetIntegerBytes(new Asn1Tag(TagClass.ContextSpecific, 1)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            ReadOnlyMemory<byte> value = reader.GetIntegerBytes(new Asn1Tag(TagClass.ContextSpecific, 7));
            Assert.Equal("0080", value.ByteArrayToHex());
            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "0201FF", PublicTagClass.Universal, 2)]
        [InlineData(PublicEncodingRules.CER, "0201FF", PublicTagClass.Universal, 2)]
        [InlineData(PublicEncodingRules.DER, "0201FF", PublicTagClass.Universal, 2)]
        [InlineData(PublicEncodingRules.BER, "8001FF", PublicTagClass.ContextSpecific, 0)]
        [InlineData(PublicEncodingRules.CER, "4C01FF", PublicTagClass.Application, 12)]
        [InlineData(PublicEncodingRules.DER, "DF8A4601FF", PublicTagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            PublicEncodingRules ruleSet,
            string inputHex,
            PublicTagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);
            ReadOnlyMemory<byte> val1 = reader.GetIntegerBytes(new Asn1Tag((TagClass)tagClass, tagValue, true));
            Assert.False(reader.HasData);
            reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);
            ReadOnlyMemory<byte> val2 = reader.GetIntegerBytes(new Asn1Tag((TagClass)tagClass, tagValue, false));
            Assert.False(reader.HasData);

            Assert.Equal(val1.ByteArrayToHex(), val2.ByteArrayToHex());
        }
    }
}
