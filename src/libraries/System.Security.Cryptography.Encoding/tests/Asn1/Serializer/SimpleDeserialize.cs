// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

using PublicEncodingRules=System.Security.Cryptography.Tests.Asn1.Asn1ReaderTests.PublicEncodingRules;

namespace System.Security.Cryptography.Tests.Asn1
{
    public static class SimpleDeserialize
    {
        [Theory]
        [InlineData(
            PublicEncodingRules.BER,
            "3080" + "06072A8648CE3D0201" + "06082A8648CE3D030107" + "0000",
            "1.2.840.10045.3.1.7")]
        // More!
        public static void AlgorithmIdentifier_ECC_WithCurves(
            PublicEncodingRules ruleSet,
            string inputHex,
            string curveOid)
        {
            byte[] inputData = inputHex.HexToByteArray();

            var algorithmIdentifier = AsnSerializer.Deserialize<AlgorithmIdentifier>(
                inputData,
                (AsnEncodingRules)ruleSet);

            Assert.Equal("1.2.840.10045.2.1", algorithmIdentifier.Algorithm.Value);
            
            var reader = new AsnReader(algorithmIdentifier.Parameters, (AsnEncodingRules)ruleSet);
            Oid curveId = reader.ReadObjectIdentifier(skipFriendlyName: true);
            Assert.Equal(curveOid, curveId.Value);
        }

        [Fact]
        public static void AllTheSimpleThings()
        {
            const string InputHex =
                "3080" +
                  "0101FF" +
                  "0201FE" +
                  "020101" +
                  "0202FEFF" +
                  "02020101" +
                  "0204FEFFFFFF" +
                  "020401000001" +
                  "0208FEFFFFFFFFFFFFFF" +
                  "02080100000000000001" +
                  "0209010000000000000001" +
                  "0303000102" +
                  "0404FF0055AA" +
                  "0500" +
                  "06082A8648CE3D030107" +
                  "06072A8648CE3D0201" +
                  "06092A864886F70D010101" +
                  "0A011E" +
                  "0C2544722E2026204D72732E20536D697468E280904A6F6E657320EFB9A0206368696C6472656E" +
                  "162144722E2026204D72732E20536D6974682D4A6F6E65732026206368696C6472656E" +
                  "1E42" +
                    "00440072002E002000260020004D00720073002E00200053006D006900740068" +
                    "2010004A006F006E006500730020FE600020006300680069006C006400720065" +
                    "006E" +
                  "3080" +
                    "010100" +
                    "010100" +
                    "0101FF" +
                    "0101FF" +
                    "010100" +
                    "0000" +
                  "3180" +
                    "020100" +
                    "020101" +
                    "0201FE" +
                    "0201FF" +
                    "02020100" +
                    "0000" +
                  "3080" +
                    "020100" +
                    "020101" +
                    "020200FE" +
                    "02017F" +
                    "020200FF" +
                    "0000" +
                  "170D3530303130323132333435365A" +
                  "170D3530303130323132333435365A" +
                  "181432303136313130363031323334352E373635345A" +
                  "180F32303136313130363031323334355A" +
                  "020F0102030405060708090A0B0C0D0E0F" +
                  "0000";

            byte[] inputData = InputHex.HexToByteArray();

            var atst = AsnSerializer.Deserialize<AllTheSimpleThings>(
                inputData,
                AsnEncodingRules.BER);

            const string UnicodeVerifier = "Dr. & Mrs. Smith\u2010Jones \uFE60 children";
            const string AsciiVerifier = "Dr. & Mrs. Smith-Jones & children";

            Assert.False(atst.NotBool, "atst.NotBool");
            Assert.Equal(-2, atst.SByte);
            Assert.Equal(1, atst.Byte);
            Assert.Equal(unchecked((short)0xFEFF), atst.Short);
            Assert.Equal(0x0101, atst.UShort);
            Assert.Equal(unchecked((int)0xFEFFFFFF), atst.Int);
            Assert.Equal((uint)0x01000001, atst.UInt);
            Assert.Equal(unchecked((long)0xFEFFFFFFFFFFFFFF), atst.Long);
            Assert.Equal(0x0100000000000001UL, atst.ULong);
            Assert.Equal("010000000000000001", atst.BigIntBytes.ByteArrayToHex());
            Assert.Equal("0102", atst.BitStringBytes.ByteArrayToHex());
            Assert.Equal("FF0055AA", atst.OctetStringBytes.ByteArrayToHex());
            Assert.Equal("0500", atst.Null.ByteArrayToHex());
            Assert.Equal("1.2.840.10045.3.1.7", atst.UnattrOid.Value);
            Assert.Equal("1.2.840.10045.3.1.7", atst.UnattrOid.FriendlyName);
            Assert.Equal("1.2.840.10045.2.1", atst.WithName.Value);
            Assert.Equal("ECC", atst.WithName.FriendlyName);
            Assert.Equal("1.2.840.113549.1.1.1", atst.OidString);
            Assert.Equal(UniversalTagNumber.BMPString, atst.LinearEnum);
            Assert.Equal(UnicodeVerifier, atst.Utf8Encoded);
            Assert.Equal(AsciiVerifier, atst.Ia5Encoded);
            Assert.Equal(UnicodeVerifier, atst.BmpEncoded);
            Assert.Equal(new[] { false, false, true, true, false }, atst.Bools);
            Assert.Equal(new[] { 0, 1, -2, -1, 256 }, atst.Ints);
            Assert.Equal(new byte[] { 0, 1, 254, 127, 255 }, atst.LittleUInts);
            Assert.Equal(new DateTimeOffset(1950, 1, 2, 12, 34, 56, TimeSpan.Zero), atst.UtcTime2049);
            Assert.Equal(new DateTimeOffset(2050, 1, 2, 12, 34, 56, TimeSpan.Zero), atst.UtcTime2099);
            Assert.Equal(new DateTimeOffset(2016, 11, 6, 1, 23, 45, TimeSpan.Zero) + new TimeSpan(7654000), atst.GeneralizedTimeWithFractions);
            Assert.Equal(new DateTimeOffset(2016, 11, 6, 1, 23, 45, TimeSpan.Zero), atst.GeneralizedTimeNoFractions);
            Assert.Equal(BigInteger.Parse("0102030405060708090A0B0C0D0E0F", NumberStyles.HexNumber), atst.BigInteger);
        }

        [Fact]
        public static void ReadEcPublicKey()
        {
            const string PublicKeyValue =
                "04" +
                "2363DD131DA65E899A2E63E9E05E50C830D4994662FFE883DB2B9A767DCCABA2" +
                "F07081B5711BE1DEE90DFC8DE17970C2D937A16CD34581F52B8D59C9E9532D13";

            const string InputHex =
                "3059" +
                  "3013" +
                    "06072A8648CE3D0201" +
                    "06082A8648CE3D030107" +
                  "0342" +
                    "00" +
                    PublicKeyValue;

            byte[] inputData = InputHex.HexToByteArray();

            var spki = AsnSerializer.Deserialize<SubjectPublicKeyInfo>(
                inputData,
                AsnEncodingRules.DER);

            Assert.Equal("1.2.840.10045.2.1", spki.AlgorithmIdentifier.Algorithm.Value);
            Assert.Equal(PublicKeyValue, spki.PublicKey.ByteArrayToHex());

            AsnReader reader = new AsnReader(spki.AlgorithmIdentifier.Parameters, AsnEncodingRules.DER);
            string curveOid = reader.ReadObjectIdentifierAsString();
            Assert.False(reader.HasData, "reader.HasData");
            Assert.Equal("1.2.840.10045.3.1.7", curveOid);
        }

        [Fact]
        public static void ReadDirectoryString()
        {
            const string BmpInputHex = "1E0400480069";
            const string Utf8InputHex = "0C024869";

            var ds1 = AsnSerializer.Deserialize<DirectoryString>(
                BmpInputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            var ds2 = AsnSerializer.Deserialize<DirectoryString>(
                Utf8InputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            Assert.NotNull(ds1);
            Assert.NotNull(ds2);
            Assert.Null(ds1.Utf8String);
            Assert.Null(ds2.BmpString);
            Assert.Equal("Hi", ds1.BmpString);
            Assert.Equal("Hi", ds2.Utf8String);
        }

        [Fact]
        public static void ReadFlexibleString()
        {
            const string BmpInputHex = "1E0400480069";
            const string Utf8InputHex = "0C024869";
            const string Ia5InputHex = "16024869";

            var fs1 = AsnSerializer.Deserialize<FlexibleString>(
                BmpInputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            var fs2 = AsnSerializer.Deserialize<FlexibleString>(
                Utf8InputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            var fs3 = AsnSerializer.Deserialize<FlexibleString>(
                Ia5InputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            Assert.Null(fs1.DirectoryString?.Utf8String);
            Assert.Null(fs1.Ascii);
            Assert.Null(fs2.DirectoryString?.BmpString);
            Assert.Null(fs2.Ascii);
            Assert.Null(fs3.DirectoryString?.BmpString);
            Assert.Null(fs3.DirectoryString?.Utf8String);
            Assert.False(fs3.DirectoryString.HasValue, "fs3.DirectoryString.HasValue");
            Assert.Equal("Hi", fs1.DirectoryString?.BmpString);
            Assert.Equal("Hi", fs2.DirectoryString?.Utf8String);
            Assert.Equal("Hi", fs3.Ascii);
        }

        [Fact]
        public static void ReadFlexibleString_Class()
        {
            const string BmpInputHex = "1E0400480069";
            const string Utf8InputHex = "0C024869";
            const string Ia5InputHex = "16024869";

            var fs1 = AsnSerializer.Deserialize<FlexibleStringClass>(
                BmpInputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            var fs2 = AsnSerializer.Deserialize<FlexibleStringClass>(
                Utf8InputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            var fs3 = AsnSerializer.Deserialize<FlexibleStringClass>(
                Ia5InputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            Assert.Null(fs1.DirectoryString?.Utf8String);
            Assert.Null(fs1.Ascii);
            Assert.Null(fs2.DirectoryString?.BmpString);
            Assert.Null(fs2.Ascii);
            Assert.Null(fs3.DirectoryString?.BmpString);
            Assert.Null(fs3.DirectoryString?.Utf8String);
            Assert.Null(fs3.DirectoryString);
            Assert.Equal("Hi", fs1.DirectoryString?.BmpString);
            Assert.Equal("Hi", fs2.DirectoryString?.Utf8String);
            Assert.Equal("Hi", fs3.Ascii);
        }

        [Fact]
        public static void ReadFlexibleString_ClassHybrid()
        {
            const string BmpInputHex = "1E0400480069";
            const string Utf8InputHex = "0C024869";
            const string Ia5InputHex = "16024869";

            var fs1 = AsnSerializer.Deserialize<FlexibleStringClassHybrid>(
                BmpInputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            var fs2 = AsnSerializer.Deserialize<FlexibleStringClassHybrid>(
                Utf8InputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            var fs3 = AsnSerializer.Deserialize<FlexibleStringClassHybrid>(
                Ia5InputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            Assert.Null(fs1.DirectoryString?.Utf8String);
            Assert.Null(fs1.Ascii);
            Assert.Null(fs2.DirectoryString?.BmpString);
            Assert.Null(fs2.Ascii);
            Assert.Null(fs3.DirectoryString?.BmpString);
            Assert.Null(fs3.DirectoryString?.Utf8String);
            Assert.False(fs3.DirectoryString.HasValue, "fs3.DirectoryString.HasValue");
            Assert.Equal("Hi", fs1.DirectoryString?.BmpString);
            Assert.Equal("Hi", fs2.DirectoryString?.Utf8String);
            Assert.Equal("Hi", fs3.Ascii);
        }

        [Fact]
        public static void ReadFlexibleString_StructHybrid()
        {
            const string BmpInputHex = "1E0400480069";
            const string Utf8InputHex = "0C024869";
            const string Ia5InputHex = "16024869";

            var fs1 = AsnSerializer.Deserialize<FlexibleStringStructHybrid>(
                BmpInputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            var fs2 = AsnSerializer.Deserialize<FlexibleStringStructHybrid>(
                Utf8InputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            var fs3 = AsnSerializer.Deserialize<FlexibleStringStructHybrid>(
                Ia5InputHex.HexToByteArray(),
                AsnEncodingRules.DER);

            Assert.Null(fs1.DirectoryString?.Utf8String);
            Assert.Null(fs1.Ascii);
            Assert.Null(fs2.DirectoryString?.BmpString);
            Assert.Null(fs2.Ascii);
            Assert.Null(fs3.DirectoryString?.BmpString);
            Assert.Null(fs3.DirectoryString?.Utf8String);
            Assert.Null(fs3.DirectoryString);
            Assert.Equal("Hi", fs1.DirectoryString?.BmpString);
            Assert.Equal("Hi", fs2.DirectoryString?.Utf8String);
            Assert.Equal("Hi", fs3.Ascii);
        }

        [Fact]
        public static void Choice_CycleRoot_Throws()
        {
            byte[] inputBytes = { 0x01, 0x01, 0x00 };

            Assert.Throws<AsnSerializationConstraintException>(
                () =>
                    AsnSerializer.Deserialize<CycleRoot>(
                        inputBytes,
                        AsnEncodingRules.DER)
            );
        }

        [Fact]
        public static void DirectoryStringClass_AsNull()
        {
            byte[] inputBytes = { 0x05, 0x00 };

            DirectoryStringClass ds = AsnSerializer.Deserialize<DirectoryStringClass>(
                inputBytes,
                AsnEncodingRules.DER);

            Assert.Null(ds);
        }

        [Fact]
        public static void Deserialize_ContextSpecific_Choice()
        {
            byte[] inputBytes = { 0x82, 0x00 };

            ContextSpecificChoice choice = AsnSerializer.Deserialize<ContextSpecificChoice>(
                inputBytes,
                AsnEncodingRules.DER);

            Assert.Null(choice.Utf8String);
            Assert.Equal(string.Empty, choice.IA5String);
        }

        [Fact]
        public static void Deserialize_UtcTime_WithTwoYearMax()
        {
            const string UtcTimeValue = "170D3132303130323233353935395A";

            const string InputHex =
                "3080" + UtcTimeValue + UtcTimeValue + UtcTimeValue + "0000";

            byte[] inputBytes = InputHex.HexToByteArray();

            UtcTimeTwoDigitYears dates = AsnSerializer.Deserialize<UtcTimeTwoDigitYears>(
                inputBytes,
                AsnEncodingRules.BER);

            Assert.Equal(new DateTimeOffset(1912, 1, 2, 23, 59, 59, TimeSpan.Zero), dates.ErnestoSabatoLifetime);
            Assert.Equal(new DateTimeOffset(2012, 1, 2, 23, 59, 59, TimeSpan.Zero), dates.MayanPhenomenon);
            Assert.Equal(new DateTimeOffset(2012, 1, 2, 23, 59, 59, TimeSpan.Zero), dates.ImplicitMax);
        }

        [Fact]
        public static void Deserialize_NamedBitLists()
        {
            const string InputHex =
                "3080" +
                    "0303000841" +
                    "0000";

            byte[] inputBytes = InputHex.HexToByteArray();

            var variants = AsnSerializer.Deserialize<NamedBitListModeVariants>(
                inputBytes,
                AsnEncodingRules.BER);

            Assert.Equal(
                SomeFlagsEnum.BitFour | SomeFlagsEnum.BitNine | SomeFlagsEnum.BitFifteen,
                variants.DefaultMode);
        }

        [Fact]
        public static void ReadAnyValueWithExpectedTag()
        {
            byte[] inputData = "308006010030030101000000".HexToByteArray();

            var data = AsnSerializer.Deserialize<AnyWithExpectedTag>(
                inputData,
                AsnEncodingRules.BER);

            Assert.Equal("0.0", data.Id);
            Assert.Equal(5, data.Data.Length);
            Assert.True(Unsafe.AreSame(ref MemoryMarshal.GetReference(data.Data.Span), ref inputData[5]));

            // Change [Constructed] SEQUENCE to [Constructed] Context-Specific 0.
            inputData[5] = 0xA0;

            Assert.Throws<CryptographicException>(
                () => AsnSerializer.Deserialize<AnyWithExpectedTag>(inputData, AsnEncodingRules.BER));
        }

        [Theory]
        [InlineData("3000", false, false)]
        [InlineData("30051603494135", false, true)]
        [InlineData("30060C0455544638", true, false)]
        [InlineData("300B0C04555446381603494135", true, true)]
        public static void ReadOptionals(string inputHex, bool hasUtf8, bool hasIa5)
        {
            byte[] inputData = inputHex.HexToByteArray();
            var data = AsnSerializer.Deserialize<OptionalValues>(inputData, AsnEncodingRules.BER);

            if (hasUtf8)
            {
                Assert.Equal("UTF8", data.Utf8String);
            }
            else
            {
                Assert.Null(data.Utf8String);
            }

            if (hasIa5)
            {
                Assert.Equal("IA5", data.IA5String);
            }
            else
            {
                Assert.Null(data.IA5String);
            }
        }

        [Fact]
        public static void TooMuchData()
        {
            // This is { IA5String("IA5"), UTF8String("UTF8") }, which is the opposite
            // of the field order of OptionalValues.  SO it will see the UTF8String as null,
            // then the IA5String as present, but then data remains.
            byte[] inputData = "300B16034941350C0455544638".HexToByteArray();

            Assert.Throws<CryptographicException>(
                () => AsnSerializer.Deserialize<OptionalValues>(inputData, AsnEncodingRules.BER));
        }
    }

    // RFC 3280 / ITU-T X.509
    [StructLayout(LayoutKind.Sequential)]
    internal struct AlgorithmIdentifier
    {
        public Oid Algorithm;
        [AnyValue]
        public ReadOnlyMemory<byte> Parameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SubjectPublicKeyInfo
    {
        public AlgorithmIdentifier AlgorithmIdentifier;
        [BitString]
        public ReadOnlyMemory<byte> PublicKey;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class AllTheSimpleThings
    {
        private bool _bool;
        private sbyte _sbyte;
        private byte _byte;
        private short _short;
        private ushort _ushort;
        private int _int;
        private uint _uint;
        private long _long;
        private ulong _ulong;
        [Integer]
        private ReadOnlyMemory<byte> _bigInt;
        [BitString]
        private ReadOnlyMemory<byte> _bitString;
        [OctetString]
        private ReadOnlyMemory<byte> _octetString;
        [AnyValue]
        private ReadOnlyMemory<byte> _null;
        private Oid _oidNoName;
        [ObjectIdentifier(PopulateFriendlyName = true)]
        private Oid _oid;
        [ObjectIdentifier]
        private string _oidString;
        private UniversalTagNumber _nonFlagsEnum;
        [UTF8String]
        private string _utf8String;
        [IA5String]
        private string _ia5String;
        [BMPString]
        private string _bmpString;
        private bool[] _bools;
        [SetOf]
        private int[] _ints;
        [SequenceOf]
        private byte[] _littleUInts;
        [UtcTime]
        public DateTimeOffset UtcTime2049;
        [UtcTime(TwoDigitYearMax = 2099)]
        public DateTimeOffset UtcTime2099;
        [GeneralizedTime]
        public DateTimeOffset GeneralizedTimeWithFractions;
        [GeneralizedTime(DisallowFractions = true)]
        public DateTimeOffset GeneralizedTimeNoFractions;
        public BigInteger BigInteger;

        public bool NotBool
        {
            get => !_bool;
            set => _bool = !value;
        }

        public sbyte SByte
        {
            get => _sbyte;
            set => _sbyte = value;
        }

        public byte Byte
        {
            get => _byte;
            set => _byte = value;
        }

        public short Short
        {
            get => _short;
            set => _short = value;
        }

        public ushort UShort
        {
            get => _ushort;
            set => _ushort = value;
        }

        public int Int
        {
            get => _int;
            set => _int = value;
        }

        public uint UInt
        {
            get => _uint;
            set => _uint = value;
        }

        public long Long
        {
            get => _long;
            set => _long = value;
        }

        public ulong ULong
        {
            get => _ulong;
            set => _ulong = value;
        }

        public ReadOnlyMemory<byte> BigIntBytes
        {
            get => _bigInt;
            set => _bigInt = value.ToArray();
        }

        public ReadOnlyMemory<byte> BitStringBytes
        {
            get => _bitString;
            set => _bitString = value.ToArray();
        }

        public ReadOnlyMemory<byte> OctetStringBytes
        {
            get => _octetString;
            set => _octetString = value.ToArray();
        }

        public ReadOnlyMemory<byte> Null
        {
            get => _null;
            set => _null = value.ToArray();
        }

        public Oid UnattrOid
        {
            get => _oidNoName;
            set => _oidNoName = value;
        }

        public Oid WithName
        {
            get => _oid;
            set => _oid = value;
        }

        public string OidString
        {
            get => _oidString;
            set => _oidString = value;
        }

        public UniversalTagNumber LinearEnum
        {
            get => _nonFlagsEnum;
            set => _nonFlagsEnum = value;
        }

        public string Utf8Encoded
        {
            get => _utf8String;
            set => _utf8String = value;
        }

        public string Ia5Encoded
        {
            get => _ia5String;
            set => _ia5String = value;
        }

        public string BmpEncoded
        {
            get => _bmpString;
            set => _bmpString = value;
        }

        public bool[] Bools
        {
            get => _bools;
            set => _bools = value;
        }

        public int[] Ints
        {
            get => _ints;
            set => _ints = value;
        }

        public byte[] LittleUInts
        {
            get => _littleUInts;
            set => _littleUInts = value;
        }
    }

    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    public struct DirectoryString
    {
        [UTF8String]
        public string Utf8String;
        [BMPString]
        public string BmpString;
    }

    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    public struct FlexibleString
    {
        public DirectoryString? DirectoryString;

        [IA5String]
        public string Ascii;
    }

    [Choice(AllowNull = true)]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class DirectoryStringClass
    {
        [UTF8String]
        public string Utf8String;
        [BMPString]
        public string BmpString;
        [PrintableString]
        public string PrintableString;
    }

    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class FlexibleStringClass
    {
        public DirectoryStringClass DirectoryString;

        [IA5String]
        public string Ascii;
    }

    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class FlexibleStringClassHybrid
    {
        public DirectoryString? DirectoryString;

        [IA5String]
        public string Ascii;
    }

    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    public struct FlexibleStringStructHybrid
    {
        public DirectoryStringClass DirectoryString;

        [IA5String]
        public string Ascii;
    }

    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class CycleRoot
    {
        public Cycle2 C2;
    }

    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class Cycle2
    {
        public Cycle3 C3;
    }

    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class Cycle3
    {
        public CycleRoot CycleRoot;
    }

    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    public struct ContextSpecificChoice
    {
        [UTF8String]
        [ExpectedTag(3)]
        public string Utf8String;

        [IA5String]
        [ExpectedTag(2)]
        public string IA5String;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UtcTimeTwoDigitYears
    {
        [UtcTime(TwoDigitYearMax = 2011)]
        public DateTimeOffset ErnestoSabatoLifetime;

        [UtcTime(TwoDigitYearMax = 2012)]
        public DateTimeOffset MayanPhenomenon;

        [UtcTime]
        public DateTimeOffset ImplicitMax;
    }

    [Flags]
    public enum SomeFlagsEnum : short
    {
        None = 0,
        BitZero = 1 << 0,
        BitOne = 1 << 1,
        BitTwo = 1 << 2,
        BitThree = 1 << 3,
        BitFour = 1 << 4,
        BitFive = 1 << 5,
        BitSix = 1 << 6,
        BitSeven = 1 << 7,
        BitEight = 1 << 8,
        BitNine = 1 << 9,
        BitTen = 1 << 10,
        BitEleven = 1 << 11,
        BitTwelve = 1 << 12,
        BitThirteen = 1 << 13,
        BitFourteen = 1 << 14,
        BitFifteen = short.MinValue,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NamedBitListModeVariants
    {
        public SomeFlagsEnum DefaultMode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ExplicitValueStruct
    {
        [ExpectedTag(0, ExplicitTag = true)]
        public int ExplicitInt;

        public int ImplicitInt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AnyWithExpectedTag
    {
        [ObjectIdentifier]
        public string Id;

        [AnyValue]
        [ExpectedTag(TagClass.Universal, (int)UniversalTagNumber.Sequence)]
        public ReadOnlyMemory<byte> Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OptionalValues
    {
        [UTF8String, OptionalValue]
        public string Utf8String;

        [IA5String, OptionalValue]
        public string IA5String;
    }
}
