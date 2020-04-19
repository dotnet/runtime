// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborReaderTests
    {
        // Data points taken from https://tools.ietf.org/html/rfc7049#appendix-A
        // Additional pairs generated using http://cbor.me/

        public const string Map = CborWriterTests.Helpers.MapPrefixIdentifier;

        [Theory]
        [InlineData(new object[] { Map }, "a0")]
        [InlineData(new object[] { Map, 1, 2, 3, 4 }, "a201020304")]
        [InlineData(new object[] { Map, "a", "A", "b", "B", "c", "C", "d", "D", "e", "E" }, "a56161614161626142616361436164614461656145")]
        [InlineData(new object[] { Map, "a", "A", -1, 2, new byte[] { }, new byte[] { 1 } }, "a3616161412002404101")]
        public static void ReadMap_SimpleValues_HappyPath(object[] expectedValues, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Helpers.VerifyMap(reader, expectedValues);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData(new object[] { Map, "a", 1, "b", new object[] { Map, 2, 3 } }, "a26161016162a10203")]
        [InlineData(new object[] { Map, "a", new object[] { Map, 2, 3 }, "b", new object[] { Map, "x", -1, "y", new object[] { Map, "z", 0 } } }, "a26161a102036162a26178206179a1617a00")]
        [InlineData(new object[] { Map, new object[] { Map, "x", 2 }, 42 }, "a1a1617802182a")] // using maps as keys
        public static void ReadMap_NestedValues_HappyPath(object[] expectedValues, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Helpers.VerifyMap(reader, expectedValues);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData(new object[] { Map, "a", 1, "b", new object[] { 2, 3 } }, "a26161016162820203")]
        [InlineData(new object[] { Map, "a", new object[] { 2, 3, "b", new object[] { Map, "x", -1, "y", new object[] { "z", 0 } } } }, "a161618402036162a2617820617982617a00")]
        [InlineData(new object[] { "a", new object[] { Map, "b", "c" } }, "826161a161626163")]
        [InlineData(new object[] { Map, new object[] { 1 }, 42 }, "a18101182a")] // using arrays as keys
        public static void ReadMap_NestedListValues_HappyPath(object expectedValue, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Helpers.VerifyValue(reader, expectedValue);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData(new object[] { Map }, "bfff")]
        [InlineData(new object[] { Map, 1, 2, 3, 4 }, "bf01020304ff")]
        [InlineData(new object[] { Map, "a", "A", "b", "B", "c", "C", "d", "D", "e", "E" }, "bf6161614161626142616361436164614461656145ff")]
        [InlineData(new object[] { Map, "a", "A", -1, 2, new byte[] { }, new byte[] { 1 } }, "bf616161412002404101ff")]
        public static void ReadMap_IndefiniteLength_SimpleValues_HappyPath(object[] exoectedValues, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Helpers.VerifyMap(reader, exoectedValues, expectDefiniteLengthCollections: false);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }


        [Theory]
        [InlineData(new object[] { Map, "a", 1, "a", 2 }, "a2616101616102")]
        public static void ReadMap_DuplicateKeys_ShouldSucceed(object[] values, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Helpers.VerifyMap(reader, values);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData("a0", 0)]
        [InlineData("a10102", 1)]
        [InlineData("a3010203040506", 3)]
        public static void ReadMap_DefiniteLengthExceeded_ShouldThrowInvalidOperationException(string hexEncoding, int expectedLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ulong? length = reader.ReadStartMap();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 0; i < expectedLength; i++)
            {
                reader.ReadInt64(); // key
                reader.ReadInt64(); // value
            }

            Assert.Throws<InvalidOperationException>(() => reader.ReadInt64());
        }

        [Theory]
        [InlineData("a101a10101", 1)]
        [InlineData("a301a1010102a1020203a10303", 3)]
        public static void ReadMap_DefiniteLengthExceeded_WithNestedData_ShouldThrowInvalidOperationException(string hexEncoding, int expectedLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ulong? length = reader.ReadStartMap();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 0; i < expectedLength; i++)
            {
                reader.ReadInt64(); // key

                // value
                ulong? nestedLength = reader.ReadStartMap();
                Assert.Equal(1, (int)nestedLength!.Value);
                reader.ReadInt64();
                reader.ReadInt64();
                reader.ReadEndMap();
            }

            Assert.Throws<InvalidOperationException>(() => reader.ReadInt64());
        }

        [Theory]
        [InlineData("a10101", 1)]
        [InlineData("a3010203040506", 3)]
        public static void ReadEndMap_DefiniteLengthNotMet_ShouldThrowInvalidOperationException(string hexEncoding, int expectedLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ulong? length = reader.ReadStartMap();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 1; i < expectedLength; i++)
            {
                reader.ReadInt64(); // key
                reader.ReadInt64(); // value
            }

            Assert.Throws<InvalidOperationException>(() => reader.ReadEndMap());
        }

        [Theory]
        [InlineData("a101a10101", 1)]
        [InlineData("a301a1010102a10202a3a10303", 3)]
        public static void ReadEndMap_DefiniteLengthNotMet_WithNestedData_ShouldThrowInvalidOperationException(string hexEncoding, int expectedLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ulong? length = reader.ReadStartMap();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 1; i < expectedLength; i++)
            {
                reader.ReadInt64(); // key

                ulong? nestedLength = reader.ReadStartMap();
                Assert.Equal(1, (int)nestedLength!.Value);
                reader.ReadInt64();
                reader.ReadInt64();
                reader.ReadEndMap();
            }

            Assert.Throws<InvalidOperationException>(() => reader.ReadEndMap());
        }

        [Theory]
        [InlineData("80", 0)]
        [InlineData("80", 1)]
        [InlineData("8180", 2)]
        public static void ReadEndMap_ImbalancedCall_ShouldThrowInvalidOperationException(string hexEncoding, int depth)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            for (int i = 0; i < depth; i++)
            {
                reader.ReadStartArray();
            }

            Assert.Throws<InvalidOperationException>(() => reader.ReadEndMap());
        }

        [Theory]
        [InlineData("a2011907e4", 2, 1)]
        [InlineData("a6011a01344224031a01344224", 6, 2)]
        public static void ReadMap_IncorrectDefiniteLength_ShouldThrowFormatException(string hexEncoding, int expectedLength, int actualLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ulong? length = reader.ReadStartMap();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 0; i < actualLength; i++)
            {
                reader.ReadInt64(); // key
                reader.ReadInt64(); // value
            }

            Assert.Throws<FormatException>(() => reader.ReadInt64());
        }

        [Theory]
        [InlineData("bf")]
        [InlineData("bf0102")]
        [InlineData("bf01020304")]
        public static void ReadMap_IndefiniteLength_MissingBreakByte_ShouldReportEndOfData(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            reader.ReadStartMap();
            while (reader.Peek() == CborReaderState.UnsignedInteger)
            {
                reader.ReadInt64();
            }

            Assert.Equal(CborReaderState.EndOfData, reader.Peek());
        }

        [Theory]
        [InlineData("bf0102ff", 1)]
        [InlineData("bf01020304ff", 2)]
        [InlineData("bf010203040506ff", 3)]
        public static void ReadMap_IndefiniteLength_PrematureEndArrayCall_ShouldThrowInvalidOperationException(string hexEncoding, int length)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            reader.ReadStartMap();
            for (int i = 1; i < length; i++)
            {
                reader.ReadInt64();
            }

            Assert.Equal(CborReaderState.UnsignedInteger, reader.Peek());
            Assert.Throws<InvalidOperationException>(() => reader.ReadEndMap());
        }

        [Theory]
        [InlineData("bf01ff", 1)]
        [InlineData("bf010203ff", 3)]
        [InlineData("bf0102030405ff", 5)]
        public static void ReadMap_IndefiniteLength_OddKeyValuePairs_ShouldThrowFormatException(string hexEncoding, int length)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            reader.ReadStartMap();
            for (int i = 0; i < length; i++)
            {
                reader.ReadInt64();
            }

            Assert.Equal(CborReaderState.FormatError, reader.Peek()); // don't want this to fail
            Assert.Throws<FormatException>(() => reader.ReadEndMap());
        }

        [Theory]
        [InlineData("a201811907e4", 2, 1)]
        [InlineData("a61907e4811907e402811907e4", 6, 2)]
        public static void ReadMap_IncorrectDefiniteLength_NestedValues_ShouldThrowFormatException(string hexEncoding, int expectedLength, int actualLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ulong? length = reader.ReadStartMap();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 0; i < actualLength; i++)
            {
                reader.ReadInt64(); // key

                ulong? innerLength = reader.ReadStartArray();
                Assert.Equal(1, (int)innerLength!.Value);
                reader.ReadInt64();
                reader.ReadEndArray();
            }

            Assert.Throws<FormatException>(() => reader.ReadInt64());
        }

        [Fact]
        public static void ReadStartMap_EmptyBuffer_ShouldThrowFormatException()
        {
            byte[] encoding = Array.Empty<byte>();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadStartMap());
        }

        [Theory]
        [InlineData("00")] // 0
        [InlineData("20")] // -1
        [InlineData("40")] // empty byte string
        [InlineData("60")] // empty text string
        [InlineData("f6")] // null
        [InlineData("80")] // []
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        public static void ReadStartMap_InvalidType_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Throws<InvalidOperationException>(() => reader.ReadStartMap());
        }

        [Theory]
        // Invalid initial bytes with map major type
        [InlineData("bc")]
        [InlineData("bd")]
        [InlineData("be")]
        // valid initial bytes missing required definite length data
        [InlineData("b8")]
        [InlineData("b912")]
        [InlineData("ba000000")]
        [InlineData("bb00000000000000")]
        public static void ReadStartMap_InvalidData_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<FormatException>(() => reader.ReadStartMap());
        }

        [Theory]
        [InlineData("b1")]
        [InlineData("b20101")]
        [InlineData("bb7fffffffffffffff")] // long.MaxValue
        public static void ReadStartMap_BufferTooSmall_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<FormatException>(() => reader.ReadStartMap());
        }

        [Theory]
        [InlineData("bb8000000000000000")] // long.MaxValue + 1
        [InlineData("bbffffffffffffffff")] // ulong.MaxValue
        public static void ReadStartMap_LargeFieldCount_ShouldThrowOverflowException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<OverflowException>(() => reader.ReadStartMap());
        }
    }
}
