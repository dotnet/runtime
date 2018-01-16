// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Numerics;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public static class SimpleSerialize
    {
        [Fact]
        public static void SerializeAlgorithmIdentifier()
        {
            AlgorithmIdentifier identifier = new AlgorithmIdentifier
            {
                Algorithm = new Oid("2.16.840.1.101.3.4.2.1", "SHA-2-256"),
                Parameters = new byte[] { 5, 0 },
            };

            using (AsnWriter writer = AsnSerializer.Serialize(identifier, AsnEncodingRules.DER))
            {

                const string ExpectedHex =
                    "300D" +
                        "0609608648016503040201" +
                        "0500";

                Assert.Equal(ExpectedHex, writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeAlgorithmIdentifier_CER()
        {
            AlgorithmIdentifier identifier = new AlgorithmIdentifier
            {
                Algorithm = new Oid("2.16.840.1.101.3.4.2.1", "SHA-2-256"),
                Parameters = new byte[] { 5, 0 },
            };

            using (AsnWriter writer = AsnSerializer.Serialize(identifier, AsnEncodingRules.CER))
            {
                const string ExpectedHex =
                    "3080" +
                        "0609608648016503040201" +
                        "0500" +
                        "0000";

                Assert.Equal(ExpectedHex, writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeAllTheSimpleThings_CER()
        {
            const string ExpectedHex =
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
                  // This is different than what we read in deserialize,
                  // because we don't write back the .0004 second.
                  "181332303136313130363031323334352E3736355A" +
                  "180F32303136313130363031323334355A" +
                  "020F0102030405060708090A0B0C0D0E0F" +
                  "0000";

            const string UnicodeVerifier = "Dr. & Mrs. Smith\u2010Jones \uFE60 children";
            const string AsciiVerifier = "Dr. & Mrs. Smith-Jones & children";

            var allTheThings = new AllTheSimpleThings
            {
                NotBool = false,
                SByte =  -2,
                Byte = 1,
                Short = unchecked((short)0xFEFF),
                UShort = 0x0101,
                Int = unchecked((int)0xFEFFFFFF),
                UInt = 0x01000001U,
                Long = unchecked((long)0xFEFFFFFFFFFFFFFF),
                ULong = 0x0100000000000001UL,
                BigIntBytes = "010000000000000001".HexToByteArray(),
                BitStringBytes = new byte[] { 1, 2 },
                OctetStringBytes = new byte[] { 0xFF, 0, 0x55, 0xAA },
                Null = new byte[] { 5, 0 },
                UnattrOid = new Oid("1.2.840.10045.3.1.7", "1.2.840.10045.3.1.7"),
                WithName = new Oid("1.2.840.10045.2.1", "ECC"),
                OidString = "1.2.840.113549.1.1.1",
                LinearEnum = UniversalTagNumber.BMPString,
                Utf8Encoded = UnicodeVerifier,
                Ia5Encoded = AsciiVerifier,
                BmpEncoded = UnicodeVerifier,
                Bools = new[] { false, false, true, true, false },
                Ints = new [] { 0, 1, -2, -1, 256 },
                LittleUInts = new byte[] { 0, 1, 254, 127, 255 },
                UtcTime2049 = new DateTimeOffset(1950, 1, 2, 12, 34, 56, TimeSpan.Zero),
                // 1950 is out of range for the reader, but the writer just does mod 100.
                UtcTime2099 = new DateTimeOffset(1950, 1, 2, 12, 34, 56, TimeSpan.Zero),
                GeneralizedTimeWithFractions = new DateTimeOffset(2016, 11, 6, 1, 23, 45, 765, TimeSpan.Zero),
                // The fractions will be dropped off by the serializer/writer, to simplify
                // the cases where the time was computed and isn't an integer number of seconds.
                GeneralizedTimeNoFractions = new DateTimeOffset(2016, 11, 6, 1, 23, 45, 765, TimeSpan.Zero),
                BigInteger = BigInteger.Parse("0102030405060708090A0B0C0D0E0F", NumberStyles.HexNumber),
            };

            using (AsnWriter writer = AsnSerializer.Serialize(allTheThings, AsnEncodingRules.CER))
            {
                Assert.Equal(ExpectedHex, writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeChoice_Null()
        {
            DirectoryStringClass directoryString = default;

            using (AsnWriter writer = AsnSerializer.Serialize(directoryString, AsnEncodingRules.DER))
            {
                Assert.Equal("0500", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeChoice_First()
        {
            DirectoryStringClass directoryString = new DirectoryStringClass
            {
                Utf8String = "UTF8",
            };

            using (AsnWriter writer = AsnSerializer.Serialize(directoryString, AsnEncodingRules.DER))
            {
                Assert.Equal("0C0455544638", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeChoice_Second()
        {
            DirectoryStringClass directoryString = new DirectoryStringClass
            {
                BmpString = "BMP",
            };

            using (AsnWriter writer = AsnSerializer.Serialize(directoryString, AsnEncodingRules.DER))
            {
                Assert.Equal("1E060042004D0050", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeChoice_Third()
        {
            DirectoryStringClass directoryString = new DirectoryStringClass
            {
                PrintableString = "Printable",
            };

            using (AsnWriter writer = AsnSerializer.Serialize(directoryString, AsnEncodingRules.DER))
            {
                Assert.Equal("13095072696E7461626C65", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeChoice_NoSelection()
        {
            DirectoryStringClass directoryString = new DirectoryStringClass();

            Assert.ThrowsAny<CryptographicException>(
                () => AsnSerializer.Serialize(directoryString, AsnEncodingRules.DER));
        }

        [Fact]
        public static void SerializeChoice_MultipleSelections()
        {
            DirectoryStringClass directoryString = new DirectoryStringClass
            {
                BmpString = "BMP",
                PrintableString = "Printable",
            };

            Assert.ThrowsAny<CryptographicException>(
                () => AsnSerializer.Serialize(directoryString, AsnEncodingRules.DER));
        }

        [Fact]
        public static void SerializeChoice_WithinChoice()
        {
            var hybrid = new FlexibleStringClassHybrid
            {
                Ascii = "IA5",
            };

            using (AsnWriter writer = AsnSerializer.Serialize(hybrid, AsnEncodingRules.DER))
            {
                Assert.Equal("1603494135", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeChoice_WithinChoice2()
        {
            var hybrid = new FlexibleStringClassHybrid
            {
                DirectoryString = new DirectoryString
                {
                    Utf8String = "Marco",
                },
            };

            using (AsnWriter writer = AsnSerializer.Serialize(hybrid, AsnEncodingRules.DER))
            {
                Assert.Equal("0C054D6172636F", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeChoice_WithinChoice3()
        {
            var hybrid = new FlexibleStringClassHybrid
            {
                DirectoryString = new DirectoryString
                {
                    BmpString = "Polo",
                },
            };

            using (AsnWriter writer = AsnSerializer.Serialize(hybrid, AsnEncodingRules.DER))
            {
                Assert.Equal("1E080050006F006C006F", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeChoice_WithinChoice4()
        {
            var hybrid = new FlexibleStringStructHybrid
            {
                DirectoryString = new DirectoryStringClass
                {
                    BmpString = "Polo",
                },
            };

            using (AsnWriter writer = AsnSerializer.Serialize(hybrid, AsnEncodingRules.DER))
            {
                Assert.Equal("1E080050006F006C006F", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeChoice_WithinChoice5()
        {
            var hybrid = new FlexibleStringStructHybrid
            {
                DirectoryString = new DirectoryStringClass
                {
                    Utf8String = "Marco",
                },
            };

            using (AsnWriter writer = AsnSerializer.Serialize(hybrid, AsnEncodingRules.DER))
            {
                Assert.Equal("0C054D6172636F", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeChoice_WithinChoice6()
        {
            var hybrid = new FlexibleStringStructHybrid
            {
                Ascii = "IA5",
            };

            using (AsnWriter writer = AsnSerializer.Serialize(hybrid, AsnEncodingRules.DER))
            {
                Assert.Equal("1603494135", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeNamedBitList()
        {
            var flagsContainer = new NamedBitListModeVariants
            {
                DefaultMode = SomeFlagsEnum.BitEleven | SomeFlagsEnum.BitTwo | SomeFlagsEnum.BitFourteen
            };

            using (AsnWriter writer = AsnSerializer.Serialize(flagsContainer, AsnEncodingRules.DER))
            {
                Assert.Equal("30050303012012", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeDefaultValue_AsDefault()
        {
            var extension = new X509DeserializeTests.Extension
            {
                ExtnId = "2.5.29.19",
                Critical = false,
                ExtnValue = new byte[] { 0x30, 0x00 },
            };

            using (AsnWriter writer = AsnSerializer.Serialize(extension, AsnEncodingRules.DER))
            {
                Assert.Equal("30090603551D1304023000", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeDefaultValue_AsNonDefault()
        {
            var extension = new X509DeserializeTests.Extension
            {
                ExtnId = "2.5.29.15",
                Critical = true,
                ExtnValue = new byte[] { 0x03, 0x02, 0x05, 0xA0 },
            };

            using (AsnWriter writer = AsnSerializer.Serialize(extension, AsnEncodingRules.DER))
            {
                Assert.Equal("300E0603551D0F0101FF0404030205A0", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void SerializeExplicitValue()
        {
            var data = new ExplicitValueStruct
            {
                ExplicitInt = 3,
                ImplicitInt = 0x17,
            };

            using (AsnWriter writer = AsnSerializer.Serialize(data, AsnEncodingRules.DER))
            {
                Assert.Equal("3008A003020103020117", writer.Encode().ByteArrayToHex());
            }
        }

        [Fact]
        public static void WriteAnyValueWithExpectedTag()
        {
            byte[] anyValue = "3003010100".HexToByteArray();

            var data = new AnyWithExpectedTag
            {
                Id = "0.0",
                Data = anyValue,
            };

            using (AsnWriter writer = AsnSerializer.Serialize(data, AsnEncodingRules.DER))
            {
                Assert.Equal("30080601003003010100", writer.Encode().ByteArrayToHex());
            }

            anyValue[0] = 0xA0;

            Assert.Throws<CryptographicException>(() => AsnSerializer.Serialize(data, AsnEncodingRules.DER));
        }

        [Theory]
        [InlineData("3000", false, false)]
        [InlineData("30051603494135", false, true)]
        [InlineData("30060C0455544638", true, false)]
        [InlineData("300B0C04555446381603494135", true, true)]
        public static void WriteOptionals(string expectedHex, bool hasUtf8, bool hasIa5)
        {
            var data = new OptionalValues
            {
                Utf8String = hasUtf8 ? "UTF8" : null,
                IA5String = hasIa5 ? "IA5" : null,
            };

            using (AsnWriter writer = AsnSerializer.Serialize(data, AsnEncodingRules.DER))
            {
                Assert.Equal(expectedHex, writer.Encode().ByteArrayToHex());
            }
        }
    }
}
