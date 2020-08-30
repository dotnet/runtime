// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ReadEnumerated
    {
        public enum ByteBacked : byte
        {
            Zero = 0,
            NotFluffy = 11,
            Fluff = 12,
        }

        public enum SByteBacked : sbyte
        {
            Zero = 0,
            Fluff = 83,
            Pillow = -17,
        }

        public enum ShortBacked : short
        {
            Zero = 0,
            Fluff = 521,
            Pillow = -1024,
        }

        public enum UShortBacked : ushort
        {
            Zero = 0,
            Fluff = 32768,
        }

        public enum IntBacked : int
        {
            Zero = 0,
            Fluff = 0x010001,
            Pillow = -Fluff,
        }

        public enum UIntBacked : uint
        {
            Zero = 0,
            Fluff = 0x80000005,
        }

        public enum LongBacked : long
        {
            Zero = 0,
            Fluff = 0x0200000441,
            Pillow = -0x100000000L,
        }

        public enum ULongBacked : ulong
        {
            Zero = 0,
            Fluff = 0xFACEF00DCAFEBEEF,
        }

        private static void GetExpectedValue<TEnum>(
            AsnEncodingRules ruleSet,
            TEnum expectedValue,
            string inputHex)
            where TEnum : Enum
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);
            TEnum value = reader.ReadEnumeratedValue<TEnum>();
            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, ByteBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.CER, ByteBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.DER, ByteBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.BER, ByteBacked.Fluff, "0A010C")]
        [InlineData(AsnEncodingRules.CER, ByteBacked.Fluff, "0A010C")]
        [InlineData(AsnEncodingRules.DER, ByteBacked.Fluff, "0A010C")]
        [InlineData(AsnEncodingRules.BER, (ByteBacked)255, "0A0200FF")]
        [InlineData(AsnEncodingRules.CER, (ByteBacked)128, "0A020080")]
        [InlineData(AsnEncodingRules.DER, (ByteBacked)129, "0A020081")]
        [InlineData(AsnEncodingRules.BER, (ByteBacked)254, "0A82000200FE")]
        public static void GetExpectedValue_ByteBacked(
            AsnEncodingRules ruleSet,
            ByteBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, SByteBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.CER, SByteBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.DER, SByteBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.BER, SByteBacked.Fluff, "0A0153")]
        [InlineData(AsnEncodingRules.CER, SByteBacked.Fluff, "0A0153")]
        [InlineData(AsnEncodingRules.DER, SByteBacked.Fluff, "0A0153")]
        [InlineData(AsnEncodingRules.BER, SByteBacked.Pillow, "0A01EF")]
        [InlineData(AsnEncodingRules.CER, (SByteBacked)sbyte.MinValue, "0A0180")]
        [InlineData(AsnEncodingRules.DER, (SByteBacked)sbyte.MinValue + 1, "0A0181")]
        [InlineData(AsnEncodingRules.BER, SByteBacked.Pillow, "0A820001EF")]
        public static void GetExpectedValue_SByteBacked(
            AsnEncodingRules ruleSet,
            SByteBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, ShortBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.CER, ShortBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.DER, ShortBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.BER, ShortBacked.Fluff, "0A020209")]
        [InlineData(AsnEncodingRules.CER, ShortBacked.Fluff, "0A020209")]
        [InlineData(AsnEncodingRules.DER, ShortBacked.Fluff, "0A020209")]
        [InlineData(AsnEncodingRules.BER, ShortBacked.Pillow, "0A02FC00")]
        [InlineData(AsnEncodingRules.CER, (ShortBacked)short.MinValue, "0A028000")]
        [InlineData(AsnEncodingRules.DER, (ShortBacked)short.MinValue + 1, "0A028001")]
        [InlineData(AsnEncodingRules.BER, ShortBacked.Pillow, "0A820002FC00")]
        public static void GetExpectedValue_ShortBacked(
            AsnEncodingRules ruleSet,
            ShortBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, UShortBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.CER, UShortBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.DER, UShortBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.BER, UShortBacked.Fluff, "0A03008000")]
        [InlineData(AsnEncodingRules.CER, UShortBacked.Fluff, "0A03008000")]
        [InlineData(AsnEncodingRules.DER, UShortBacked.Fluff, "0A03008000")]
        [InlineData(AsnEncodingRules.BER, (UShortBacked)255, "0A0200FF")]
        [InlineData(AsnEncodingRules.CER, (UShortBacked)256, "0A020100")]
        [InlineData(AsnEncodingRules.DER, (UShortBacked)0x7FED, "0A027FED")]
        [InlineData(AsnEncodingRules.BER, (UShortBacked)ushort.MaxValue, "0A82000300FFFF")]
        [InlineData(AsnEncodingRules.BER, (UShortBacked)0x8123, "0A820003008123")]
        public static void GetExpectedValue_UShortBacked(
            AsnEncodingRules ruleSet,
            UShortBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, IntBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.CER, IntBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.DER, IntBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.BER, IntBacked.Fluff, "0A03010001")]
        [InlineData(AsnEncodingRules.CER, IntBacked.Fluff, "0A03010001")]
        [InlineData(AsnEncodingRules.DER, IntBacked.Fluff, "0A03010001")]
        [InlineData(AsnEncodingRules.BER, IntBacked.Pillow, "0A03FEFFFF")]
        [InlineData(AsnEncodingRules.CER, (IntBacked)int.MinValue, "0A0480000000")]
        [InlineData(AsnEncodingRules.DER, (IntBacked)int.MinValue + 1, "0A0480000001")]
        [InlineData(AsnEncodingRules.BER, IntBacked.Pillow, "0A820003FEFFFF")]
        public static void GetExpectedValue_IntBacked(
            AsnEncodingRules ruleSet,
            IntBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, UIntBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.CER, UIntBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.DER, UIntBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.BER, UIntBacked.Fluff, "0A050080000005")]
        [InlineData(AsnEncodingRules.CER, UIntBacked.Fluff, "0A050080000005")]
        [InlineData(AsnEncodingRules.DER, UIntBacked.Fluff, "0A050080000005")]
        [InlineData(AsnEncodingRules.BER, (UIntBacked)255, "0A0200FF")]
        [InlineData(AsnEncodingRules.CER, (UIntBacked)256, "0A020100")]
        [InlineData(AsnEncodingRules.DER, (UIntBacked)0x7FED, "0A027FED")]
        [InlineData(AsnEncodingRules.BER, (UIntBacked)uint.MaxValue, "0A82000500FFFFFFFF")]
        [InlineData(AsnEncodingRules.BER, (UIntBacked)0x8123, "0A820003008123")]
        public static void GetExpectedValue_UIntBacked(
            AsnEncodingRules ruleSet,
            UIntBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, LongBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.CER, LongBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.DER, LongBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.BER, LongBacked.Fluff, "0A050200000441")]
        [InlineData(AsnEncodingRules.CER, LongBacked.Fluff, "0A050200000441")]
        [InlineData(AsnEncodingRules.DER, LongBacked.Fluff, "0A050200000441")]
        [InlineData(AsnEncodingRules.BER, LongBacked.Pillow, "0A05FF00000000")]
        [InlineData(AsnEncodingRules.CER, (LongBacked)short.MinValue, "0A028000")]
        [InlineData(AsnEncodingRules.DER, (LongBacked)short.MinValue + 1, "0A028001")]
        [InlineData(AsnEncodingRules.BER, LongBacked.Pillow, "0A820005FF00000000")]
        public static void GetExpectedValue_LongBacked(
            AsnEncodingRules ruleSet,
            LongBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, ULongBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.CER, ULongBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.DER, ULongBacked.Zero, "0A0100")]
        [InlineData(AsnEncodingRules.BER, ULongBacked.Fluff, "0A0900FACEF00DCAFEBEEF")]
        [InlineData(AsnEncodingRules.CER, ULongBacked.Fluff, "0A0900FACEF00DCAFEBEEF")]
        [InlineData(AsnEncodingRules.DER, ULongBacked.Fluff, "0A0900FACEF00DCAFEBEEF")]
        [InlineData(AsnEncodingRules.BER, (ULongBacked)255, "0A0200FF")]
        [InlineData(AsnEncodingRules.CER, (ULongBacked)256, "0A020100")]
        [InlineData(AsnEncodingRules.DER, (ULongBacked)0x7FED, "0A027FED")]
        [InlineData(AsnEncodingRules.BER, (ULongBacked)uint.MaxValue, "0A82000500FFFFFFFF")]
        [InlineData(AsnEncodingRules.BER, (ULongBacked)ulong.MaxValue, "0A82000900FFFFFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.BER, (ULongBacked)0x8123, "0A820003008123")]
        public static void GetExpectedValue_ULongBacked(
            AsnEncodingRules ruleSet,
            ULongBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "")]
        [InlineData(AsnEncodingRules.CER, "")]
        [InlineData(AsnEncodingRules.DER, "")]
        [InlineData(AsnEncodingRules.BER, "0A")]
        [InlineData(AsnEncodingRules.CER, "0A")]
        [InlineData(AsnEncodingRules.DER, "0A")]
        [InlineData(AsnEncodingRules.BER, "0A00")]
        [InlineData(AsnEncodingRules.CER, "0A00")]
        [InlineData(AsnEncodingRules.DER, "0A00")]
        [InlineData(AsnEncodingRules.BER, "0A01")]
        [InlineData(AsnEncodingRules.CER, "0A01")]
        [InlineData(AsnEncodingRules.DER, "0A01")]
        [InlineData(AsnEncodingRules.BER, "0A81")]
        [InlineData(AsnEncodingRules.CER, "0A81")]
        [InlineData(AsnEncodingRules.DER, "0A81")]
        [InlineData(AsnEncodingRules.BER, "9F00")]
        [InlineData(AsnEncodingRules.CER, "9F00")]
        [InlineData(AsnEncodingRules.DER, "9F00")]
        [InlineData(AsnEncodingRules.BER, "0A01FF")]
        [InlineData(AsnEncodingRules.CER, "0A01FF")]
        [InlineData(AsnEncodingRules.DER, "0A01FF")]
        [InlineData(AsnEncodingRules.BER, "0A02007F")]
        [InlineData(AsnEncodingRules.CER, "0A02007F")]
        [InlineData(AsnEncodingRules.DER, "0A02007F")]
        [InlineData(AsnEncodingRules.BER, "0A020102")]
        [InlineData(AsnEncodingRules.CER, "0A020102")]
        [InlineData(AsnEncodingRules.DER, "0A020102")]
        [InlineData(AsnEncodingRules.BER, "0A02FF80")]
        [InlineData(AsnEncodingRules.CER, "0A02FF80")]
        [InlineData(AsnEncodingRules.DER, "0A02FF80")]
        [InlineData(AsnEncodingRules.BER, "0A03010203")]
        [InlineData(AsnEncodingRules.CER, "0A03010203")]
        [InlineData(AsnEncodingRules.DER, "0A03010203")]
        [InlineData(AsnEncodingRules.BER, "0A0401020304")]
        [InlineData(AsnEncodingRules.CER, "0A0401020304")]
        [InlineData(AsnEncodingRules.DER, "0A0401020304")]
        [InlineData(AsnEncodingRules.BER, "0A050102030405")]
        [InlineData(AsnEncodingRules.CER, "0A050102030405")]
        [InlineData(AsnEncodingRules.DER, "0A050102030405")]
        [InlineData(AsnEncodingRules.BER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.CER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.DER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_Byte(AsnEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadEnumeratedValue<ByteBacked>());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "")]
        [InlineData(AsnEncodingRules.CER, "")]
        [InlineData(AsnEncodingRules.DER, "")]
        [InlineData(AsnEncodingRules.BER, "0A")]
        [InlineData(AsnEncodingRules.CER, "0A")]
        [InlineData(AsnEncodingRules.DER, "0A")]
        [InlineData(AsnEncodingRules.BER, "0A00")]
        [InlineData(AsnEncodingRules.CER, "0A00")]
        [InlineData(AsnEncodingRules.DER, "0A00")]
        [InlineData(AsnEncodingRules.BER, "0A01")]
        [InlineData(AsnEncodingRules.CER, "0A01")]
        [InlineData(AsnEncodingRules.DER, "0A01")]
        [InlineData(AsnEncodingRules.BER, "0A81")]
        [InlineData(AsnEncodingRules.CER, "0A81")]
        [InlineData(AsnEncodingRules.DER, "0A81")]
        [InlineData(AsnEncodingRules.BER, "9F00")]
        [InlineData(AsnEncodingRules.CER, "9F00")]
        [InlineData(AsnEncodingRules.DER, "9F00")]
        [InlineData(AsnEncodingRules.BER, "0A02007F")]
        [InlineData(AsnEncodingRules.CER, "0A02007F")]
        [InlineData(AsnEncodingRules.DER, "0A02007F")]
        [InlineData(AsnEncodingRules.BER, "0A020102")]
        [InlineData(AsnEncodingRules.CER, "0A020102")]
        [InlineData(AsnEncodingRules.DER, "0A020102")]
        [InlineData(AsnEncodingRules.BER, "0A02FF80")]
        [InlineData(AsnEncodingRules.CER, "0A02FF80")]
        [InlineData(AsnEncodingRules.DER, "0A02FF80")]
        [InlineData(AsnEncodingRules.BER, "0A03010203")]
        [InlineData(AsnEncodingRules.CER, "0A03010203")]
        [InlineData(AsnEncodingRules.DER, "0A03010203")]
        [InlineData(AsnEncodingRules.BER, "0A0401020304")]
        [InlineData(AsnEncodingRules.CER, "0A0401020304")]
        [InlineData(AsnEncodingRules.DER, "0A0401020304")]
        [InlineData(AsnEncodingRules.BER, "0A050102030405")]
        [InlineData(AsnEncodingRules.CER, "0A050102030405")]
        [InlineData(AsnEncodingRules.DER, "0A050102030405")]
        [InlineData(AsnEncodingRules.BER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.CER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.DER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_SByte(AsnEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadEnumeratedValue<SByteBacked>());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "")]
        [InlineData(AsnEncodingRules.CER, "")]
        [InlineData(AsnEncodingRules.DER, "")]
        [InlineData(AsnEncodingRules.BER, "0A")]
        [InlineData(AsnEncodingRules.CER, "0A")]
        [InlineData(AsnEncodingRules.DER, "0A")]
        [InlineData(AsnEncodingRules.BER, "0A00")]
        [InlineData(AsnEncodingRules.CER, "0A00")]
        [InlineData(AsnEncodingRules.DER, "0A00")]
        [InlineData(AsnEncodingRules.BER, "0A01")]
        [InlineData(AsnEncodingRules.CER, "0A01")]
        [InlineData(AsnEncodingRules.DER, "0A01")]
        [InlineData(AsnEncodingRules.BER, "0A81")]
        [InlineData(AsnEncodingRules.CER, "0A81")]
        [InlineData(AsnEncodingRules.DER, "0A81")]
        [InlineData(AsnEncodingRules.BER, "9F00")]
        [InlineData(AsnEncodingRules.CER, "9F00")]
        [InlineData(AsnEncodingRules.DER, "9F00")]
        [InlineData(AsnEncodingRules.BER, "0A02007F")]
        [InlineData(AsnEncodingRules.CER, "0A02007F")]
        [InlineData(AsnEncodingRules.DER, "0A02007F")]
        [InlineData(AsnEncodingRules.BER, "0A02FF80")]
        [InlineData(AsnEncodingRules.CER, "0A02FF80")]
        [InlineData(AsnEncodingRules.DER, "0A02FF80")]
        [InlineData(AsnEncodingRules.BER, "0A03010203")]
        [InlineData(AsnEncodingRules.CER, "0A03010203")]
        [InlineData(AsnEncodingRules.DER, "0A03010203")]
        [InlineData(AsnEncodingRules.BER, "0A0401020304")]
        [InlineData(AsnEncodingRules.CER, "0A0401020304")]
        [InlineData(AsnEncodingRules.DER, "0A0401020304")]
        [InlineData(AsnEncodingRules.BER, "0A050102030405")]
        [InlineData(AsnEncodingRules.CER, "0A050102030405")]
        [InlineData(AsnEncodingRules.DER, "0A050102030405")]
        [InlineData(AsnEncodingRules.BER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.CER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.DER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_Short(AsnEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadEnumeratedValue<ShortBacked>());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "")]
        [InlineData(AsnEncodingRules.CER, "")]
        [InlineData(AsnEncodingRules.DER, "")]
        [InlineData(AsnEncodingRules.BER, "0A")]
        [InlineData(AsnEncodingRules.CER, "0A")]
        [InlineData(AsnEncodingRules.DER, "0A")]
        [InlineData(AsnEncodingRules.BER, "0A00")]
        [InlineData(AsnEncodingRules.CER, "0A00")]
        [InlineData(AsnEncodingRules.DER, "0A00")]
        [InlineData(AsnEncodingRules.BER, "0A01")]
        [InlineData(AsnEncodingRules.CER, "0A01")]
        [InlineData(AsnEncodingRules.DER, "0A01")]
        [InlineData(AsnEncodingRules.BER, "0A81")]
        [InlineData(AsnEncodingRules.CER, "0A81")]
        [InlineData(AsnEncodingRules.DER, "0A81")]
        [InlineData(AsnEncodingRules.BER, "9F00")]
        [InlineData(AsnEncodingRules.CER, "9F00")]
        [InlineData(AsnEncodingRules.DER, "9F00")]
        [InlineData(AsnEncodingRules.BER, "0A01FF")]
        [InlineData(AsnEncodingRules.CER, "0A01FF")]
        [InlineData(AsnEncodingRules.DER, "0A01FF")]
        [InlineData(AsnEncodingRules.BER, "0A02007F")]
        [InlineData(AsnEncodingRules.CER, "0A02007F")]
        [InlineData(AsnEncodingRules.DER, "0A02007F")]
        [InlineData(AsnEncodingRules.BER, "0A02FF80")]
        [InlineData(AsnEncodingRules.CER, "0A02FF80")]
        [InlineData(AsnEncodingRules.DER, "0A02FF80")]
        [InlineData(AsnEncodingRules.BER, "0A03010203")]
        [InlineData(AsnEncodingRules.CER, "0A03010203")]
        [InlineData(AsnEncodingRules.DER, "0A03010203")]
        [InlineData(AsnEncodingRules.BER, "0A0401020304")]
        [InlineData(AsnEncodingRules.CER, "0A0401020304")]
        [InlineData(AsnEncodingRules.DER, "0A0401020304")]
        [InlineData(AsnEncodingRules.BER, "0A050102030405")]
        [InlineData(AsnEncodingRules.CER, "0A050102030405")]
        [InlineData(AsnEncodingRules.DER, "0A050102030405")]
        [InlineData(AsnEncodingRules.BER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.CER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.DER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_UShort(AsnEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadEnumeratedValue<UShortBacked>());
        }


        [Theory]
        [InlineData(AsnEncodingRules.BER, "")]
        [InlineData(AsnEncodingRules.CER, "")]
        [InlineData(AsnEncodingRules.DER, "")]
        [InlineData(AsnEncodingRules.BER, "0A")]
        [InlineData(AsnEncodingRules.CER, "0A")]
        [InlineData(AsnEncodingRules.DER, "0A")]
        [InlineData(AsnEncodingRules.BER, "0A00")]
        [InlineData(AsnEncodingRules.CER, "0A00")]
        [InlineData(AsnEncodingRules.DER, "0A00")]
        [InlineData(AsnEncodingRules.BER, "0A01")]
        [InlineData(AsnEncodingRules.CER, "0A01")]
        [InlineData(AsnEncodingRules.DER, "0A01")]
        [InlineData(AsnEncodingRules.BER, "0A81")]
        [InlineData(AsnEncodingRules.CER, "0A81")]
        [InlineData(AsnEncodingRules.DER, "0A81")]
        [InlineData(AsnEncodingRules.BER, "9F00")]
        [InlineData(AsnEncodingRules.CER, "9F00")]
        [InlineData(AsnEncodingRules.DER, "9F00")]
        [InlineData(AsnEncodingRules.BER, "0A02007F")]
        [InlineData(AsnEncodingRules.CER, "0A02007F")]
        [InlineData(AsnEncodingRules.DER, "0A02007F")]
        [InlineData(AsnEncodingRules.BER, "0A02FF80")]
        [InlineData(AsnEncodingRules.CER, "0A02FF80")]
        [InlineData(AsnEncodingRules.DER, "0A02FF80")]
        [InlineData(AsnEncodingRules.BER, "0A050102030405")]
        [InlineData(AsnEncodingRules.CER, "0A050102030405")]
        [InlineData(AsnEncodingRules.DER, "0A050102030405")]
        [InlineData(AsnEncodingRules.BER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.CER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.DER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_Int(AsnEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadEnumeratedValue<IntBacked>());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "")]
        [InlineData(AsnEncodingRules.CER, "")]
        [InlineData(AsnEncodingRules.DER, "")]
        [InlineData(AsnEncodingRules.BER, "0A")]
        [InlineData(AsnEncodingRules.CER, "0A")]
        [InlineData(AsnEncodingRules.DER, "0A")]
        [InlineData(AsnEncodingRules.BER, "0A00")]
        [InlineData(AsnEncodingRules.CER, "0A00")]
        [InlineData(AsnEncodingRules.DER, "0A00")]
        [InlineData(AsnEncodingRules.BER, "0A01")]
        [InlineData(AsnEncodingRules.CER, "0A01")]
        [InlineData(AsnEncodingRules.DER, "0A01")]
        [InlineData(AsnEncodingRules.BER, "0A81")]
        [InlineData(AsnEncodingRules.CER, "0A81")]
        [InlineData(AsnEncodingRules.DER, "0A81")]
        [InlineData(AsnEncodingRules.BER, "9F00")]
        [InlineData(AsnEncodingRules.CER, "9F00")]
        [InlineData(AsnEncodingRules.DER, "9F00")]
        [InlineData(AsnEncodingRules.BER, "0A01FF")]
        [InlineData(AsnEncodingRules.CER, "0A01FF")]
        [InlineData(AsnEncodingRules.DER, "0A01FF")]
        [InlineData(AsnEncodingRules.BER, "0A02007F")]
        [InlineData(AsnEncodingRules.CER, "0A02007F")]
        [InlineData(AsnEncodingRules.DER, "0A02007F")]
        [InlineData(AsnEncodingRules.BER, "0A050102030405")]
        [InlineData(AsnEncodingRules.CER, "0A050102030405")]
        [InlineData(AsnEncodingRules.DER, "0A050102030405")]
        [InlineData(AsnEncodingRules.BER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.CER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.DER, "0A080102030405060708")]
        [InlineData(AsnEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_UInt(AsnEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadEnumeratedValue<UIntBacked>());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "")]
        [InlineData(AsnEncodingRules.CER, "")]
        [InlineData(AsnEncodingRules.DER, "")]
        [InlineData(AsnEncodingRules.BER, "0A")]
        [InlineData(AsnEncodingRules.CER, "0A")]
        [InlineData(AsnEncodingRules.DER, "0A")]
        [InlineData(AsnEncodingRules.BER, "0A00")]
        [InlineData(AsnEncodingRules.CER, "0A00")]
        [InlineData(AsnEncodingRules.DER, "0A00")]
        [InlineData(AsnEncodingRules.BER, "0A01")]
        [InlineData(AsnEncodingRules.CER, "0A01")]
        [InlineData(AsnEncodingRules.DER, "0A01")]
        [InlineData(AsnEncodingRules.BER, "0A81")]
        [InlineData(AsnEncodingRules.CER, "0A81")]
        [InlineData(AsnEncodingRules.DER, "0A81")]
        [InlineData(AsnEncodingRules.BER, "9F00")]
        [InlineData(AsnEncodingRules.CER, "9F00")]
        [InlineData(AsnEncodingRules.DER, "9F00")]
        [InlineData(AsnEncodingRules.BER, "0A02007F")]
        [InlineData(AsnEncodingRules.CER, "0A02007F")]
        [InlineData(AsnEncodingRules.DER, "0A02007F")]
        [InlineData(AsnEncodingRules.BER, "0A02FF80")]
        [InlineData(AsnEncodingRules.CER, "0A02FF80")]
        [InlineData(AsnEncodingRules.DER, "0A02FF80")]
        [InlineData(AsnEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_Long(AsnEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadEnumeratedValue<LongBacked>());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "")]
        [InlineData(AsnEncodingRules.CER, "")]
        [InlineData(AsnEncodingRules.DER, "")]
        [InlineData(AsnEncodingRules.BER, "0A")]
        [InlineData(AsnEncodingRules.CER, "0A")]
        [InlineData(AsnEncodingRules.DER, "0A")]
        [InlineData(AsnEncodingRules.BER, "0A00")]
        [InlineData(AsnEncodingRules.CER, "0A00")]
        [InlineData(AsnEncodingRules.DER, "0A00")]
        [InlineData(AsnEncodingRules.BER, "0A01")]
        [InlineData(AsnEncodingRules.CER, "0A01")]
        [InlineData(AsnEncodingRules.DER, "0A01")]
        [InlineData(AsnEncodingRules.BER, "0A81")]
        [InlineData(AsnEncodingRules.CER, "0A81")]
        [InlineData(AsnEncodingRules.DER, "0A81")]
        [InlineData(AsnEncodingRules.BER, "9F00")]
        [InlineData(AsnEncodingRules.CER, "9F00")]
        [InlineData(AsnEncodingRules.DER, "9F00")]
        [InlineData(AsnEncodingRules.BER, "0A01FF")]
        [InlineData(AsnEncodingRules.CER, "0A01FF")]
        [InlineData(AsnEncodingRules.DER, "0A01FF")]
        [InlineData(AsnEncodingRules.BER, "0A02007F")]
        [InlineData(AsnEncodingRules.CER, "0A02007F")]
        [InlineData(AsnEncodingRules.DER, "0A02007F")]
        [InlineData(AsnEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(AsnEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_ULong(AsnEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadEnumeratedValue<ULongBacked>());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadEnumeratedValue_RequiresTypeArg(AsnEncodingRules ruleSet)
        {
            byte[] data = { 0x0A, 0x01, 0x00 };
            AsnReader reader = new AsnReader(data, ruleSet);

            AssertExtensions.Throws<ArgumentNullException>(
                "enumType",
                () => reader.ReadEnumeratedValue(null!));

            Assert.True(reader.HasData, "reader.HasData");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadEnumeratedValue_NonEnumType(AsnEncodingRules ruleSet)
        {
            byte[] data = { 0x0A, 0x01, 0x00 };
            AsnReader reader = new AsnReader(data, ruleSet);

            Assert.Throws<ArgumentException>(() => reader.ReadEnumeratedValue(typeof(Guid)));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadEnumeratedValue_FlagsEnum(AsnEncodingRules ruleSet)
        {
            byte[] data = { 0x0A, 0x01, 0x00 };
            AsnReader reader = new AsnReader(data, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "enumType",
                () => reader.ReadEnumeratedValue<AssemblyFlags>());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadEnumeratedBytes(AsnEncodingRules ruleSet)
        {
            const string Payload = "0102030405060708090A0B0C0D0E0F10";

            // ENUMERATED (payload) followed by INTEGER (0)
            byte[] data = ("0A10" + Payload + "020100").HexToByteArray();
            AsnReader reader = new AsnReader(data, ruleSet);

            ReadOnlyMemory<byte> contents = reader.ReadEnumeratedBytes();
            Assert.Equal(0x10, contents.Length);
            Assert.Equal(Payload, contents.ByteArrayToHex());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "")]
        [InlineData(AsnEncodingRules.CER, "")]
        [InlineData(AsnEncodingRules.DER, "")]
        [InlineData(AsnEncodingRules.BER, "0A")]
        [InlineData(AsnEncodingRules.CER, "0A")]
        [InlineData(AsnEncodingRules.DER, "0A")]
        [InlineData(AsnEncodingRules.BER, "0A00")]
        [InlineData(AsnEncodingRules.CER, "0A00")]
        [InlineData(AsnEncodingRules.DER, "0A00")]
        [InlineData(AsnEncodingRules.BER, "0A01")]
        [InlineData(AsnEncodingRules.CER, "0A01")]
        [InlineData(AsnEncodingRules.DER, "0A01")]
        [InlineData(AsnEncodingRules.BER, "010100")]
        [InlineData(AsnEncodingRules.CER, "010100")]
        [InlineData(AsnEncodingRules.DER, "010100")]
        [InlineData(AsnEncodingRules.BER, "9F00")]
        [InlineData(AsnEncodingRules.CER, "9F00")]
        [InlineData(AsnEncodingRules.DER, "9F00")]
        [InlineData(AsnEncodingRules.BER, "0A81")]
        [InlineData(AsnEncodingRules.CER, "0A81")]
        [InlineData(AsnEncodingRules.DER, "0A81")]
        public static void ReadEnumeratedBytes_Throws(AsnEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadEnumeratedBytes());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(AsnEncodingRules ruleSet)
        {
            byte[] inputData = { 0x0A, 1, 0x7E };
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadEnumeratedValue<ShortBacked>(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadEnumeratedValue<ShortBacked>(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            ShortBacked value = reader.ReadEnumeratedValue<ShortBacked>();
            Assert.Equal((ShortBacked)0x7E, value);
            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom(AsnEncodingRules ruleSet)
        {
            byte[] inputData = { 0x87, 2, 0, 0x80 };
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadEnumeratedValue<ShortBacked>(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.ReadEnumeratedValue<ShortBacked>());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadEnumeratedValue<ShortBacked>(new Asn1Tag(TagClass.Application, 0)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(
                () => reader.ReadEnumeratedValue<ShortBacked>(new Asn1Tag(TagClass.ContextSpecific, 1)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            ShortBacked value = reader.ReadEnumeratedValue<ShortBacked>(new Asn1Tag(TagClass.ContextSpecific, 7));
            Assert.Equal((ShortBacked)0x80, value);
            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "0A01FF", TagClass.Universal, 10)]
        [InlineData(AsnEncodingRules.CER, "0A01FF", TagClass.Universal, 10)]
        [InlineData(AsnEncodingRules.DER, "0A01FF", TagClass.Universal, 10)]
        [InlineData(AsnEncodingRules.BER, "8001FF", TagClass.ContextSpecific, 0)]
        [InlineData(AsnEncodingRules.CER, "4C01FF", TagClass.Application, 12)]
        [InlineData(AsnEncodingRules.DER, "DF8A4601FF", TagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            AsnEncodingRules ruleSet,
            string inputHex,
            TagClass tagClass,
            int tagValue)
        {
            Asn1Tag primitiveTag = new Asn1Tag(tagClass, tagValue, false);
            Asn1Tag constructedTag = new Asn1Tag(tagClass, tagValue, true);
            byte[] inputData = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputData, ruleSet);
            ShortBacked val1 = reader.ReadEnumeratedValue<ShortBacked>(constructedTag);
            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);
            ShortBacked val2 = reader.ReadEnumeratedValue<ShortBacked>(primitiveTag);
            Assert.False(reader.HasData);

            Assert.Equal(val1, val2);

            reader = new AsnReader(inputData, ruleSet);
            ShortBacked val3 = (ShortBacked)reader.ReadEnumeratedValue(typeof(ShortBacked), constructedTag);
            Assert.False(reader.HasData);

            Assert.Equal(val1, val3);

            reader = new AsnReader(inputData, ruleSet);
            ShortBacked val4 = (ShortBacked)reader.ReadEnumeratedValue(typeof(ShortBacked), primitiveTag);
            Assert.False(reader.HasData);

            Assert.Equal(val1, val4);

            reader = new AsnReader(inputData, ruleSet);
            ReadOnlyMemory<byte> bytes1 = reader.ReadEnumeratedBytes(constructedTag);
            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);
            ReadOnlyMemory<byte> bytes2 = reader.ReadEnumeratedBytes(primitiveTag);
            Assert.False(reader.HasData);

            Assert.Equal(bytes1.ByteArrayToHex(), bytes2.ByteArrayToHex());
            Assert.Equal("FF", bytes1.ByteArrayToHex());
        }
    }
}
