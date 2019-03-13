// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public sealed class ReadEnumerated : Asn1ReaderTests
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
            PublicEncodingRules ruleSet,
            TEnum expectedValue,
            string inputHex)
            where TEnum : struct
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);
            TEnum value = reader.ReadEnumeratedValue<TEnum>();
            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, ByteBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.CER, ByteBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.DER, ByteBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.BER, ByteBacked.Fluff, "0A010C")]
        [InlineData(PublicEncodingRules.CER, ByteBacked.Fluff, "0A010C")]
        [InlineData(PublicEncodingRules.DER, ByteBacked.Fluff, "0A010C")]
        [InlineData(PublicEncodingRules.BER, (ByteBacked)255, "0A0200FF")]
        [InlineData(PublicEncodingRules.CER, (ByteBacked)128, "0A020080")]
        [InlineData(PublicEncodingRules.DER, (ByteBacked)129, "0A020081")]
        [InlineData(PublicEncodingRules.BER, (ByteBacked)254, "0A82000200FE")]
        public static void GetExpectedValue_ByteBacked(
            PublicEncodingRules ruleSet,
            ByteBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, SByteBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.CER, SByteBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.DER, SByteBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.BER, SByteBacked.Fluff, "0A0153")]
        [InlineData(PublicEncodingRules.CER, SByteBacked.Fluff, "0A0153")]
        [InlineData(PublicEncodingRules.DER, SByteBacked.Fluff, "0A0153")]
        [InlineData(PublicEncodingRules.BER, SByteBacked.Pillow, "0A01EF")]
        [InlineData(PublicEncodingRules.CER, (SByteBacked)sbyte.MinValue, "0A0180")]
        [InlineData(PublicEncodingRules.DER, (SByteBacked)sbyte.MinValue + 1, "0A0181")]
        [InlineData(PublicEncodingRules.BER, SByteBacked.Pillow, "0A820001EF")]
        public static void GetExpectedValue_SByteBacked(
            PublicEncodingRules ruleSet,
            SByteBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, ShortBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.CER, ShortBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.DER, ShortBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.BER, ShortBacked.Fluff, "0A020209")]
        [InlineData(PublicEncodingRules.CER, ShortBacked.Fluff, "0A020209")]
        [InlineData(PublicEncodingRules.DER, ShortBacked.Fluff, "0A020209")]
        [InlineData(PublicEncodingRules.BER, ShortBacked.Pillow, "0A02FC00")]
        [InlineData(PublicEncodingRules.CER, (ShortBacked)short.MinValue, "0A028000")]
        [InlineData(PublicEncodingRules.DER, (ShortBacked)short.MinValue + 1, "0A028001")]
        [InlineData(PublicEncodingRules.BER, ShortBacked.Pillow, "0A820002FC00")]
        public static void GetExpectedValue_ShortBacked(
            PublicEncodingRules ruleSet,
            ShortBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, UShortBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.CER, UShortBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.DER, UShortBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.BER, UShortBacked.Fluff, "0A03008000")]
        [InlineData(PublicEncodingRules.CER, UShortBacked.Fluff, "0A03008000")]
        [InlineData(PublicEncodingRules.DER, UShortBacked.Fluff, "0A03008000")]
        [InlineData(PublicEncodingRules.BER, (UShortBacked)255, "0A0200FF")]
        [InlineData(PublicEncodingRules.CER, (UShortBacked)256, "0A020100")]
        [InlineData(PublicEncodingRules.DER, (UShortBacked)0x7FED, "0A027FED")]
        [InlineData(PublicEncodingRules.BER, (UShortBacked)ushort.MaxValue, "0A82000300FFFF")]
        [InlineData(PublicEncodingRules.BER, (UShortBacked)0x8123, "0A820003008123")]
        public static void GetExpectedValue_UShortBacked(
            PublicEncodingRules ruleSet,
            UShortBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, IntBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.CER, IntBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.DER, IntBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.BER, IntBacked.Fluff, "0A03010001")]
        [InlineData(PublicEncodingRules.CER, IntBacked.Fluff, "0A03010001")]
        [InlineData(PublicEncodingRules.DER, IntBacked.Fluff, "0A03010001")]
        [InlineData(PublicEncodingRules.BER, IntBacked.Pillow, "0A03FEFFFF")]
        [InlineData(PublicEncodingRules.CER, (IntBacked)int.MinValue, "0A0480000000")]
        [InlineData(PublicEncodingRules.DER, (IntBacked)int.MinValue + 1, "0A0480000001")]
        [InlineData(PublicEncodingRules.BER, IntBacked.Pillow, "0A820003FEFFFF")]
        public static void GetExpectedValue_IntBacked(
            PublicEncodingRules ruleSet,
            IntBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, UIntBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.CER, UIntBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.DER, UIntBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.BER, UIntBacked.Fluff, "0A050080000005")]
        [InlineData(PublicEncodingRules.CER, UIntBacked.Fluff, "0A050080000005")]
        [InlineData(PublicEncodingRules.DER, UIntBacked.Fluff, "0A050080000005")]
        [InlineData(PublicEncodingRules.BER, (UIntBacked)255, "0A0200FF")]
        [InlineData(PublicEncodingRules.CER, (UIntBacked)256, "0A020100")]
        [InlineData(PublicEncodingRules.DER, (UIntBacked)0x7FED, "0A027FED")]
        [InlineData(PublicEncodingRules.BER, (UIntBacked)uint.MaxValue, "0A82000500FFFFFFFF")]
        [InlineData(PublicEncodingRules.BER, (UIntBacked)0x8123, "0A820003008123")]
        public static void GetExpectedValue_UIntBacked(
            PublicEncodingRules ruleSet,
            UIntBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, LongBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.CER, LongBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.DER, LongBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.BER, LongBacked.Fluff, "0A050200000441")]
        [InlineData(PublicEncodingRules.CER, LongBacked.Fluff, "0A050200000441")]
        [InlineData(PublicEncodingRules.DER, LongBacked.Fluff, "0A050200000441")]
        [InlineData(PublicEncodingRules.BER, LongBacked.Pillow, "0A05FF00000000")]
        [InlineData(PublicEncodingRules.CER, (LongBacked)short.MinValue, "0A028000")]
        [InlineData(PublicEncodingRules.DER, (LongBacked)short.MinValue + 1, "0A028001")]
        [InlineData(PublicEncodingRules.BER, LongBacked.Pillow, "0A820005FF00000000")]
        public static void GetExpectedValue_LongBacked(
            PublicEncodingRules ruleSet,
            LongBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, ULongBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.CER, ULongBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.DER, ULongBacked.Zero, "0A0100")]
        [InlineData(PublicEncodingRules.BER, ULongBacked.Fluff, "0A0900FACEF00DCAFEBEEF")]
        [InlineData(PublicEncodingRules.CER, ULongBacked.Fluff, "0A0900FACEF00DCAFEBEEF")]
        [InlineData(PublicEncodingRules.DER, ULongBacked.Fluff, "0A0900FACEF00DCAFEBEEF")]
        [InlineData(PublicEncodingRules.BER, (ULongBacked)255, "0A0200FF")]
        [InlineData(PublicEncodingRules.CER, (ULongBacked)256, "0A020100")]
        [InlineData(PublicEncodingRules.DER, (ULongBacked)0x7FED, "0A027FED")]
        [InlineData(PublicEncodingRules.BER, (ULongBacked)uint.MaxValue, "0A82000500FFFFFFFF")]
        [InlineData(PublicEncodingRules.BER, (ULongBacked)ulong.MaxValue, "0A82000900FFFFFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.BER, (ULongBacked)0x8123, "0A820003008123")]
        public static void GetExpectedValue_ULongBacked(
            PublicEncodingRules ruleSet,
            ULongBacked expectedValue,
            string inputHex)
        {
            GetExpectedValue(ruleSet, expectedValue, inputHex);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "")]
        [InlineData(PublicEncodingRules.CER, "")]
        [InlineData(PublicEncodingRules.DER, "")]
        [InlineData(PublicEncodingRules.BER, "0A")]
        [InlineData(PublicEncodingRules.CER, "0A")]
        [InlineData(PublicEncodingRules.DER, "0A")]
        [InlineData(PublicEncodingRules.BER, "0A00")]
        [InlineData(PublicEncodingRules.CER, "0A00")]
        [InlineData(PublicEncodingRules.DER, "0A00")]
        [InlineData(PublicEncodingRules.BER, "0A01")]
        [InlineData(PublicEncodingRules.CER, "0A01")]
        [InlineData(PublicEncodingRules.DER, "0A01")]
        [InlineData(PublicEncodingRules.BER, "0A81")]
        [InlineData(PublicEncodingRules.CER, "0A81")]
        [InlineData(PublicEncodingRules.DER, "0A81")]
        [InlineData(PublicEncodingRules.BER, "9F00")]
        [InlineData(PublicEncodingRules.CER, "9F00")]
        [InlineData(PublicEncodingRules.DER, "9F00")]
        [InlineData(PublicEncodingRules.BER, "0A01FF")]
        [InlineData(PublicEncodingRules.CER, "0A01FF")]
        [InlineData(PublicEncodingRules.DER, "0A01FF")]
        [InlineData(PublicEncodingRules.BER, "0A02007F")]
        [InlineData(PublicEncodingRules.CER, "0A02007F")]
        [InlineData(PublicEncodingRules.DER, "0A02007F")]
        [InlineData(PublicEncodingRules.BER, "0A020102")]
        [InlineData(PublicEncodingRules.CER, "0A020102")]
        [InlineData(PublicEncodingRules.DER, "0A020102")]
        [InlineData(PublicEncodingRules.BER, "0A02FF80")]
        [InlineData(PublicEncodingRules.CER, "0A02FF80")]
        [InlineData(PublicEncodingRules.DER, "0A02FF80")]
        [InlineData(PublicEncodingRules.BER, "0A03010203")]
        [InlineData(PublicEncodingRules.CER, "0A03010203")]
        [InlineData(PublicEncodingRules.DER, "0A03010203")]
        [InlineData(PublicEncodingRules.BER, "0A0401020304")]
        [InlineData(PublicEncodingRules.CER, "0A0401020304")]
        [InlineData(PublicEncodingRules.DER, "0A0401020304")]
        [InlineData(PublicEncodingRules.BER, "0A050102030405")]
        [InlineData(PublicEncodingRules.CER, "0A050102030405")]
        [InlineData(PublicEncodingRules.DER, "0A050102030405")]
        [InlineData(PublicEncodingRules.BER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.CER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.DER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_Byte(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(() => reader.ReadEnumeratedValue<ByteBacked>());
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "")]
        [InlineData(PublicEncodingRules.CER, "")]
        [InlineData(PublicEncodingRules.DER, "")]
        [InlineData(PublicEncodingRules.BER, "0A")]
        [InlineData(PublicEncodingRules.CER, "0A")]
        [InlineData(PublicEncodingRules.DER, "0A")]
        [InlineData(PublicEncodingRules.BER, "0A00")]
        [InlineData(PublicEncodingRules.CER, "0A00")]
        [InlineData(PublicEncodingRules.DER, "0A00")]
        [InlineData(PublicEncodingRules.BER, "0A01")]
        [InlineData(PublicEncodingRules.CER, "0A01")]
        [InlineData(PublicEncodingRules.DER, "0A01")]
        [InlineData(PublicEncodingRules.BER, "0A81")]
        [InlineData(PublicEncodingRules.CER, "0A81")]
        [InlineData(PublicEncodingRules.DER, "0A81")]
        [InlineData(PublicEncodingRules.BER, "9F00")]
        [InlineData(PublicEncodingRules.CER, "9F00")]
        [InlineData(PublicEncodingRules.DER, "9F00")]
        [InlineData(PublicEncodingRules.BER, "0A02007F")]
        [InlineData(PublicEncodingRules.CER, "0A02007F")]
        [InlineData(PublicEncodingRules.DER, "0A02007F")]
        [InlineData(PublicEncodingRules.BER, "0A020102")]
        [InlineData(PublicEncodingRules.CER, "0A020102")]
        [InlineData(PublicEncodingRules.DER, "0A020102")]
        [InlineData(PublicEncodingRules.BER, "0A02FF80")]
        [InlineData(PublicEncodingRules.CER, "0A02FF80")]
        [InlineData(PublicEncodingRules.DER, "0A02FF80")]
        [InlineData(PublicEncodingRules.BER, "0A03010203")]
        [InlineData(PublicEncodingRules.CER, "0A03010203")]
        [InlineData(PublicEncodingRules.DER, "0A03010203")]
        [InlineData(PublicEncodingRules.BER, "0A0401020304")]
        [InlineData(PublicEncodingRules.CER, "0A0401020304")]
        [InlineData(PublicEncodingRules.DER, "0A0401020304")]
        [InlineData(PublicEncodingRules.BER, "0A050102030405")]
        [InlineData(PublicEncodingRules.CER, "0A050102030405")]
        [InlineData(PublicEncodingRules.DER, "0A050102030405")]
        [InlineData(PublicEncodingRules.BER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.CER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.DER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_SByte(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(() => reader.ReadEnumeratedValue<SByteBacked>());
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "")]
        [InlineData(PublicEncodingRules.CER, "")]
        [InlineData(PublicEncodingRules.DER, "")]
        [InlineData(PublicEncodingRules.BER, "0A")]
        [InlineData(PublicEncodingRules.CER, "0A")]
        [InlineData(PublicEncodingRules.DER, "0A")]
        [InlineData(PublicEncodingRules.BER, "0A00")]
        [InlineData(PublicEncodingRules.CER, "0A00")]
        [InlineData(PublicEncodingRules.DER, "0A00")]
        [InlineData(PublicEncodingRules.BER, "0A01")]
        [InlineData(PublicEncodingRules.CER, "0A01")]
        [InlineData(PublicEncodingRules.DER, "0A01")]
        [InlineData(PublicEncodingRules.BER, "0A81")]
        [InlineData(PublicEncodingRules.CER, "0A81")]
        [InlineData(PublicEncodingRules.DER, "0A81")]
        [InlineData(PublicEncodingRules.BER, "9F00")]
        [InlineData(PublicEncodingRules.CER, "9F00")]
        [InlineData(PublicEncodingRules.DER, "9F00")]
        [InlineData(PublicEncodingRules.BER, "0A02007F")]
        [InlineData(PublicEncodingRules.CER, "0A02007F")]
        [InlineData(PublicEncodingRules.DER, "0A02007F")]
        [InlineData(PublicEncodingRules.BER, "0A02FF80")]
        [InlineData(PublicEncodingRules.CER, "0A02FF80")]
        [InlineData(PublicEncodingRules.DER, "0A02FF80")]
        [InlineData(PublicEncodingRules.BER, "0A03010203")]
        [InlineData(PublicEncodingRules.CER, "0A03010203")]
        [InlineData(PublicEncodingRules.DER, "0A03010203")]
        [InlineData(PublicEncodingRules.BER, "0A0401020304")]
        [InlineData(PublicEncodingRules.CER, "0A0401020304")]
        [InlineData(PublicEncodingRules.DER, "0A0401020304")]
        [InlineData(PublicEncodingRules.BER, "0A050102030405")]
        [InlineData(PublicEncodingRules.CER, "0A050102030405")]
        [InlineData(PublicEncodingRules.DER, "0A050102030405")]
        [InlineData(PublicEncodingRules.BER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.CER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.DER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_Short(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(() => reader.ReadEnumeratedValue<ShortBacked>());
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "")]
        [InlineData(PublicEncodingRules.CER, "")]
        [InlineData(PublicEncodingRules.DER, "")]
        [InlineData(PublicEncodingRules.BER, "0A")]
        [InlineData(PublicEncodingRules.CER, "0A")]
        [InlineData(PublicEncodingRules.DER, "0A")]
        [InlineData(PublicEncodingRules.BER, "0A00")]
        [InlineData(PublicEncodingRules.CER, "0A00")]
        [InlineData(PublicEncodingRules.DER, "0A00")]
        [InlineData(PublicEncodingRules.BER, "0A01")]
        [InlineData(PublicEncodingRules.CER, "0A01")]
        [InlineData(PublicEncodingRules.DER, "0A01")]
        [InlineData(PublicEncodingRules.BER, "0A81")]
        [InlineData(PublicEncodingRules.CER, "0A81")]
        [InlineData(PublicEncodingRules.DER, "0A81")]
        [InlineData(PublicEncodingRules.BER, "9F00")]
        [InlineData(PublicEncodingRules.CER, "9F00")]
        [InlineData(PublicEncodingRules.DER, "9F00")]
        [InlineData(PublicEncodingRules.BER, "0A01FF")]
        [InlineData(PublicEncodingRules.CER, "0A01FF")]
        [InlineData(PublicEncodingRules.DER, "0A01FF")]
        [InlineData(PublicEncodingRules.BER, "0A02007F")]
        [InlineData(PublicEncodingRules.CER, "0A02007F")]
        [InlineData(PublicEncodingRules.DER, "0A02007F")]
        [InlineData(PublicEncodingRules.BER, "0A02FF80")]
        [InlineData(PublicEncodingRules.CER, "0A02FF80")]
        [InlineData(PublicEncodingRules.DER, "0A02FF80")]
        [InlineData(PublicEncodingRules.BER, "0A03010203")]
        [InlineData(PublicEncodingRules.CER, "0A03010203")]
        [InlineData(PublicEncodingRules.DER, "0A03010203")]
        [InlineData(PublicEncodingRules.BER, "0A0401020304")]
        [InlineData(PublicEncodingRules.CER, "0A0401020304")]
        [InlineData(PublicEncodingRules.DER, "0A0401020304")]
        [InlineData(PublicEncodingRules.BER, "0A050102030405")]
        [InlineData(PublicEncodingRules.CER, "0A050102030405")]
        [InlineData(PublicEncodingRules.DER, "0A050102030405")]
        [InlineData(PublicEncodingRules.BER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.CER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.DER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_UShort(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(() => reader.ReadEnumeratedValue<UShortBacked>());
        }


        [Theory]
        [InlineData(PublicEncodingRules.BER, "")]
        [InlineData(PublicEncodingRules.CER, "")]
        [InlineData(PublicEncodingRules.DER, "")]
        [InlineData(PublicEncodingRules.BER, "0A")]
        [InlineData(PublicEncodingRules.CER, "0A")]
        [InlineData(PublicEncodingRules.DER, "0A")]
        [InlineData(PublicEncodingRules.BER, "0A00")]
        [InlineData(PublicEncodingRules.CER, "0A00")]
        [InlineData(PublicEncodingRules.DER, "0A00")]
        [InlineData(PublicEncodingRules.BER, "0A01")]
        [InlineData(PublicEncodingRules.CER, "0A01")]
        [InlineData(PublicEncodingRules.DER, "0A01")]
        [InlineData(PublicEncodingRules.BER, "0A81")]
        [InlineData(PublicEncodingRules.CER, "0A81")]
        [InlineData(PublicEncodingRules.DER, "0A81")]
        [InlineData(PublicEncodingRules.BER, "9F00")]
        [InlineData(PublicEncodingRules.CER, "9F00")]
        [InlineData(PublicEncodingRules.DER, "9F00")]
        [InlineData(PublicEncodingRules.BER, "0A02007F")]
        [InlineData(PublicEncodingRules.CER, "0A02007F")]
        [InlineData(PublicEncodingRules.DER, "0A02007F")]
        [InlineData(PublicEncodingRules.BER, "0A02FF80")]
        [InlineData(PublicEncodingRules.CER, "0A02FF80")]
        [InlineData(PublicEncodingRules.DER, "0A02FF80")]
        [InlineData(PublicEncodingRules.BER, "0A050102030405")]
        [InlineData(PublicEncodingRules.CER, "0A050102030405")]
        [InlineData(PublicEncodingRules.DER, "0A050102030405")]
        [InlineData(PublicEncodingRules.BER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.CER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.DER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_Int(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(() => reader.ReadEnumeratedValue<IntBacked>());
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "")]
        [InlineData(PublicEncodingRules.CER, "")]
        [InlineData(PublicEncodingRules.DER, "")]
        [InlineData(PublicEncodingRules.BER, "0A")]
        [InlineData(PublicEncodingRules.CER, "0A")]
        [InlineData(PublicEncodingRules.DER, "0A")]
        [InlineData(PublicEncodingRules.BER, "0A00")]
        [InlineData(PublicEncodingRules.CER, "0A00")]
        [InlineData(PublicEncodingRules.DER, "0A00")]
        [InlineData(PublicEncodingRules.BER, "0A01")]
        [InlineData(PublicEncodingRules.CER, "0A01")]
        [InlineData(PublicEncodingRules.DER, "0A01")]
        [InlineData(PublicEncodingRules.BER, "0A81")]
        [InlineData(PublicEncodingRules.CER, "0A81")]
        [InlineData(PublicEncodingRules.DER, "0A81")]
        [InlineData(PublicEncodingRules.BER, "9F00")]
        [InlineData(PublicEncodingRules.CER, "9F00")]
        [InlineData(PublicEncodingRules.DER, "9F00")]
        [InlineData(PublicEncodingRules.BER, "0A01FF")]
        [InlineData(PublicEncodingRules.CER, "0A01FF")]
        [InlineData(PublicEncodingRules.DER, "0A01FF")]
        [InlineData(PublicEncodingRules.BER, "0A02007F")]
        [InlineData(PublicEncodingRules.CER, "0A02007F")]
        [InlineData(PublicEncodingRules.DER, "0A02007F")]
        [InlineData(PublicEncodingRules.BER, "0A050102030405")]
        [InlineData(PublicEncodingRules.CER, "0A050102030405")]
        [InlineData(PublicEncodingRules.DER, "0A050102030405")]
        [InlineData(PublicEncodingRules.BER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.CER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.DER, "0A080102030405060708")]
        [InlineData(PublicEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_UInt(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(() => reader.ReadEnumeratedValue<UIntBacked>());
        }
        
        [Theory]
        [InlineData(PublicEncodingRules.BER, "")]
        [InlineData(PublicEncodingRules.CER, "")]
        [InlineData(PublicEncodingRules.DER, "")]
        [InlineData(PublicEncodingRules.BER, "0A")]
        [InlineData(PublicEncodingRules.CER, "0A")]
        [InlineData(PublicEncodingRules.DER, "0A")]
        [InlineData(PublicEncodingRules.BER, "0A00")]
        [InlineData(PublicEncodingRules.CER, "0A00")]
        [InlineData(PublicEncodingRules.DER, "0A00")]
        [InlineData(PublicEncodingRules.BER, "0A01")]
        [InlineData(PublicEncodingRules.CER, "0A01")]
        [InlineData(PublicEncodingRules.DER, "0A01")]
        [InlineData(PublicEncodingRules.BER, "0A81")]
        [InlineData(PublicEncodingRules.CER, "0A81")]
        [InlineData(PublicEncodingRules.DER, "0A81")]
        [InlineData(PublicEncodingRules.BER, "9F00")]
        [InlineData(PublicEncodingRules.CER, "9F00")]
        [InlineData(PublicEncodingRules.DER, "9F00")]
        [InlineData(PublicEncodingRules.BER, "0A02007F")]
        [InlineData(PublicEncodingRules.CER, "0A02007F")]
        [InlineData(PublicEncodingRules.DER, "0A02007F")]
        [InlineData(PublicEncodingRules.BER, "0A02FF80")]
        [InlineData(PublicEncodingRules.CER, "0A02FF80")]
        [InlineData(PublicEncodingRules.DER, "0A02FF80")]
        [InlineData(PublicEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_Long(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(() => reader.ReadEnumeratedValue<LongBacked>());
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "")]
        [InlineData(PublicEncodingRules.CER, "")]
        [InlineData(PublicEncodingRules.DER, "")]
        [InlineData(PublicEncodingRules.BER, "0A")]
        [InlineData(PublicEncodingRules.CER, "0A")]
        [InlineData(PublicEncodingRules.DER, "0A")]
        [InlineData(PublicEncodingRules.BER, "0A00")]
        [InlineData(PublicEncodingRules.CER, "0A00")]
        [InlineData(PublicEncodingRules.DER, "0A00")]
        [InlineData(PublicEncodingRules.BER, "0A01")]
        [InlineData(PublicEncodingRules.CER, "0A01")]
        [InlineData(PublicEncodingRules.DER, "0A01")]
        [InlineData(PublicEncodingRules.BER, "0A81")]
        [InlineData(PublicEncodingRules.CER, "0A81")]
        [InlineData(PublicEncodingRules.DER, "0A81")]
        [InlineData(PublicEncodingRules.BER, "9F00")]
        [InlineData(PublicEncodingRules.CER, "9F00")]
        [InlineData(PublicEncodingRules.DER, "9F00")]
        [InlineData(PublicEncodingRules.BER, "0A01FF")]
        [InlineData(PublicEncodingRules.CER, "0A01FF")]
        [InlineData(PublicEncodingRules.DER, "0A01FF")]
        [InlineData(PublicEncodingRules.BER, "0A02007F")]
        [InlineData(PublicEncodingRules.CER, "0A02007F")]
        [InlineData(PublicEncodingRules.DER, "0A02007F")]
        [InlineData(PublicEncodingRules.BER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.CER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.DER, "0A09010203040506070809")]
        [InlineData(PublicEncodingRules.BER, "2A030A0100")]
        public static void ReadEnumeratedValue_Invalid_ULong(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(() => reader.ReadEnumeratedValue<ULongBacked>());
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void ReadEnumeratedValue_NonEnumType(PublicEncodingRules ruleSet)
        {
            byte[] data = { 0x0A, 0x01, 0x00 };
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            Assert.Throws<ArgumentException>(() => reader.ReadEnumeratedValue<Guid>());
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void ReadEnumeratedValue_FlagsEnum(PublicEncodingRules ruleSet)
        {
            byte[] data = { 0x0A, 0x01, 0x00 };
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "tEnum",
                () => reader.ReadEnumeratedValue<AssemblyFlags>());
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void ReadEnumeratedBytes(PublicEncodingRules ruleSet)
        {
            const string Payload = "0102030405060708090A0B0C0D0E0F10";

            // ENUMERATED (payload) followed by INTEGER (0)
            byte[] data = ("0A10" + Payload + "020100").HexToByteArray();
            AsnReader reader = new AsnReader(data, (AsnEncodingRules)ruleSet);

            ReadOnlyMemory<byte> contents = reader.ReadEnumeratedBytes();
            Assert.Equal(0x10, contents.Length);
            Assert.Equal(Payload, contents.ByteArrayToHex());
        }
        
        [Theory]
        [InlineData(PublicEncodingRules.BER, "")]
        [InlineData(PublicEncodingRules.CER, "")]
        [InlineData(PublicEncodingRules.DER, "")]
        [InlineData(PublicEncodingRules.BER, "0A")]
        [InlineData(PublicEncodingRules.CER, "0A")]
        [InlineData(PublicEncodingRules.DER, "0A")]
        [InlineData(PublicEncodingRules.BER, "0A00")]
        [InlineData(PublicEncodingRules.CER, "0A00")]
        [InlineData(PublicEncodingRules.DER, "0A00")]
        [InlineData(PublicEncodingRules.BER, "0A01")]
        [InlineData(PublicEncodingRules.CER, "0A01")]
        [InlineData(PublicEncodingRules.DER, "0A01")]
        [InlineData(PublicEncodingRules.BER, "010100")]
        [InlineData(PublicEncodingRules.CER, "010100")]
        [InlineData(PublicEncodingRules.DER, "010100")]
        [InlineData(PublicEncodingRules.BER, "9F00")]
        [InlineData(PublicEncodingRules.CER, "9F00")]
        [InlineData(PublicEncodingRules.DER, "9F00")]
        [InlineData(PublicEncodingRules.BER, "0A81")]
        [InlineData(PublicEncodingRules.CER, "0A81")]
        [InlineData(PublicEncodingRules.DER, "0A81")]
        public static void ReadEnumeratedBytes_Throws(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(() => reader.ReadEnumeratedBytes());
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(PublicEncodingRules ruleSet)
        {
            byte[] inputData = { 0x0A, 1, 0x7E };
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadEnumeratedValue<ShortBacked>(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(
                () => reader.ReadEnumeratedValue<ShortBacked>(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            ShortBacked value = reader.ReadEnumeratedValue<ShortBacked>();
            Assert.Equal((ShortBacked)0x7E, value);
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
                () => reader.ReadEnumeratedValue<ShortBacked>(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(() => reader.ReadEnumeratedValue<ShortBacked>());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<CryptographicException>(
                () => reader.ReadEnumeratedValue<ShortBacked>(new Asn1Tag(TagClass.Application, 0)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<CryptographicException>(
                () => reader.ReadEnumeratedValue<ShortBacked>(new Asn1Tag(TagClass.ContextSpecific, 1)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            ShortBacked value = reader.ReadEnumeratedValue<ShortBacked>(new Asn1Tag(TagClass.ContextSpecific, 7));
            Assert.Equal((ShortBacked)0x80, value);
            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "0A01FF", PublicTagClass.Universal, 10)]
        [InlineData(PublicEncodingRules.CER, "0A01FF", PublicTagClass.Universal, 10)]
        [InlineData(PublicEncodingRules.DER, "0A01FF", PublicTagClass.Universal, 10)]
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
            ShortBacked val1 = reader.ReadEnumeratedValue<ShortBacked>(new Asn1Tag((TagClass)tagClass, tagValue, true));
            Assert.False(reader.HasData);
            reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);
            ShortBacked val2 = reader.ReadEnumeratedValue<ShortBacked>(new Asn1Tag((TagClass)tagClass, tagValue, false));
            Assert.False(reader.HasData);

            Assert.Equal(val1, val2);
        }
    }
}
