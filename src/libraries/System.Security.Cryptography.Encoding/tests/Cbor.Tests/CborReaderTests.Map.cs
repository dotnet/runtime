// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
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
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
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
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
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
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(new object[] { Map }, "bfff")]
        [InlineData(new object[] { Map, 1, 2, 3, 4 }, "bf01020304ff")]
        [InlineData(new object[] { Map, "a", "A", "b", "B", "c", "C", "d", "D", "e", "E" }, "bf6161614161626142616361436164614461656145ff")]
        [InlineData(new object[] { Map, "a", "A", -1, 2, new byte[] { }, new byte[] { 1 } }, "bf616161412002404101ff")]
        public static void ReadMap_IndefiniteLength_SimpleValues_HappyPath(object[] expectedValues, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Helpers.VerifyMap(reader, expectedValues, expectDefiniteLengthCollections: false);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(CborConformanceLevel.Lax, "bfff")]
        [InlineData(CborConformanceLevel.Strict, "bfff")]
        public static void ReadMap_IndefiniteLength_SupportedConformanceLevel_ShouldSucceed(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            int? length = reader.ReadStartMap();
            Assert.Null(length);
        }

        [Theory]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "bfff")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "bfff")]
        public static void ReadMap_IndefiniteLength_UnSupportedConformanceLevel_ShouldThrowFormatException(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            Assert.Throws<FormatException>(() => reader.ReadStartMap());
            Assert.Equal(0, reader.BytesRead);
        }

        [Theory]
        [InlineData(CborConformanceLevel.Lax, "b800")]
        [InlineData(CborConformanceLevel.Lax, "b90000")]
        [InlineData(CborConformanceLevel.Lax, "ba00000000")]
        [InlineData(CborConformanceLevel.Lax, "bb0000000000000000")]
        [InlineData(CborConformanceLevel.Strict, "b800")]
        [InlineData(CborConformanceLevel.Strict, "b90000")]
        [InlineData(CborConformanceLevel.Strict, "ba00000000")]
        [InlineData(CborConformanceLevel.Strict, "bb0000000000000000")]
        public static void ReadMap_NonCanonicalLengths_SupportedConformanceLevel_ShouldSucceed(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            int? length = reader.ReadStartMap();
            Assert.NotNull(length);
            Assert.Equal(0, length!.Value);
            reader.ReadEndMap();
        }

        [Theory]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "b800")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "b90000")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "ba00000000")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "bb0000000000000000")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "b800")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "b90000")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "ba00000000")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "bb0000000000000000")]
        public static void ReadMap_NonCanonicalLengths_UnSupportedConformanceLevel_ShouldThrowFormatException(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            Assert.Throws<FormatException>(() => reader.ReadStartMap());
            Assert.Equal(0, reader.BytesRead);
        }

        [Theory]
        [InlineData(CborConformanceLevel.Lax, new object[] { Map, 3, 3, 2, 2, 1, 1 }, "a3030302020101")]
        [InlineData(CborConformanceLevel.Strict, new object[] { Map, 3, 3, 2, 2, 1, 1 }, "a3030302020101")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { Map, 1, 1, 2, 2, 3, 3 }, "a3010102020303")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { Map, 1, 1, 2, 2, 3, 3 }, "a3010102020303")]
        // indefinite length string payload
        [InlineData(CborConformanceLevel.Lax, new object[] { Map, "b", 0, 2, 0, "a", 0, new object[] { "c", "" }, 0, 1, 0 }, "a5616200020061610082616360000100")]
        [InlineData(CborConformanceLevel.Strict, new object[] { Map, "b", 0, 2, 0, "a", 0, new object[] { "c", "" }, 0, 1, 0 }, "a5616200020061610082616360000100")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { Map, 1, 0, 2, 0, "a", 0, "b", 0, new object[] { "c", "" }, 0 }, "a5010002006161006162008261636000")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { Map, 1, 0, 2, 0, "a", 0, "b", 0, new object[] { "c", "" }, 0 }, "a5010002006161006162008261636000")]
        // CBOR sorting rules do not match canonical string sorting
        [InlineData(CborConformanceLevel.Lax, new object[] { Map, "aa", 0, "z", 0 }, "a262616100617a00")]
        [InlineData(CborConformanceLevel.Strict, new object[] { Map, "aa", 0, "z", 0 }, "a262616100617a00")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { Map, "z", 0, "aa", 0 }, "a2617a0062616100")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { Map, "z", 0, "aa", 0 }, "a2617a0062616100")]
        // Test case distinguishing between RFC7049 and CTAP2 sorting rules
        [InlineData(CborConformanceLevel.Lax, new object[] { Map, "", 0, 255, 0 }, "a2600018ff00")]
        [InlineData(CborConformanceLevel.Strict, new object[] { Map, "", 0, 255, 0 }, "a2600018ff00")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { Map, "", 0, 255, 0 }, "a2600018ff00")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { Map, 255, 0, "", 0 }, "a218ff006000")]
        public static void ReadMap_SimpleValues_ShouldAcceptKeysSortedAccordingToConformanceLevel(CborConformanceLevel level, object expectedValue, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            Helpers.VerifyValue(reader, expectedValue);
        }

        [Theory]
        [InlineData(CborConformanceLevel.Lax, new object[] { Map, -1, 0, new object[] { Map, 3, 3, 2, 2, 1, 1 }, 0, "a", 0, 256, 0, new object[] { Map, 2, 2, 1, 1 }, 0 }, "a52000a30303020201010061610019010000a20202010100")]
        [InlineData(CborConformanceLevel.Strict, new object[] { Map, -1, 0, new object[] { Map, 3, 3, 2, 2, 1, 1 }, 0, "a", 0, 256, 0, new object[] { Map, 2, 2, 1, 1 }, 0 }, "a52000a30303020201010061610019010000a20202010100")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { Map, -1, 0, "a", 0, 256, 0, new object[] { Map, 1, 1, 2, 2 }, 0, new object[] { Map, 1, 1, 2, 2, 3, 3 }, 0 }, "a5200061610019010000a20101020200a301010202030300")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { Map, 256, 0, -1, 0, "a", 0, new object[] { Map, 1, 1, 2, 2 }, 0, new object[] { Map, 1, 1, 2, 2, 3, 3 }, 0 }, "a5190100002000616100a20101020200a301010202030300")]
        public static void ReadMap_NestedValues_ShouldAcceptKeysSortedAccordingToConformanceLevel(CborConformanceLevel level, object expectedValue, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            Helpers.VerifyValue(reader, expectedValue);
        }

        [Theory]
        [InlineData(new object[] { Map, "a", 1, "a", 2 }, "a2616101616102")]
        public static void ReadMap_DuplicateKeys_ShouldSucceed(object[] values, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Helpers.VerifyMap(reader, values);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(CborConformanceLevel.Strict, 42, "a2182a01182a02")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, 42, "a2182a01182a02")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, 42, "a2182a01182a02")]
        [InlineData(CborConformanceLevel.Strict, "foobar", "a266666f6f6261720166666f6f62617202")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "foobar", "a266666f6f6261720166666f6f62617202")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "foobar", "a266666f6f6261720166666f6f62617202")]
        [InlineData(CborConformanceLevel.Strict, new object[] { new object[] { "x", "y" } }, "a28182617861790181826178617902")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { new object[] { "x", "y" } }, "a28182617861790181826178617902")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { new object[] { "x", "y" } }, "a28182617861790181826178617902")]
        public static void ReadMap_DuplicateKeys_StrictConformance_ShouldThrowFormatException(CborConformanceLevel level, object dupeKey, string hexEncoding)
        {
            var reader = new CborReader(hexEncoding.HexToByteArray(), level);
            reader.ReadStartMap();
            Helpers.VerifyValue(reader, dupeKey);
            reader.ReadInt32();

            int bytesRead = reader.BytesRead;
            int bytesRemaining = reader.BytesRemaining;
            CborReaderState state = reader.PeekState();

            Assert.Throws<FormatException>(() => Helpers.VerifyValue(reader, dupeKey));

            // ensure reader state is preserved
            Assert.Equal(bytesRead, reader.BytesRead);
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
            Assert.Equal(state, reader.PeekState());
        }

        [Theory]
        [InlineData(CborConformanceLevel.Lax, new object[] { 1, 2, 3, 0 }, "a40101020203030000")]
        [InlineData(CborConformanceLevel.Strict, new object[] { 1, 2, 3, 0 }, "a40101020203030000")]
        [InlineData(CborConformanceLevel.Lax, new object[] { 1, "", 25, "a", 2 }, "a5010060001819006161000200")]
        [InlineData(CborConformanceLevel.Strict, new object[] { 1, 25, "", "a", 2 }, "a5010018190060006161000200")]
        public static void ReadMap_UnsortedKeys_ConformanceNotRequiringSortedKeys_ShouldSucceed(CborConformanceLevel level, object[] keySequence, string hexEncoding)
        {
            var reader = new CborReader(hexEncoding.HexToByteArray(), level);
            reader.ReadStartMap();
            foreach (object key in keySequence)
            {
                Helpers.VerifyValue(reader, key); // verify key
                reader.ReadInt32(); // value is always an integer
            }

            reader.ReadEndMap();
        }

        [Theory]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { 1, 2, 3, 0 }, "a40101020203030000")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { 1, 2, 3, 0 }, "a40101020203030000")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { 1, "", 25, "a", 2 }, "a5010060001819006161000200")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { 1, 25, "", "a", 2 }, "a5010018190060006161000200")]
        public static void ReadMap_UnsortedKeys_ConformanceRequiringSortedKeys_ShouldThrowFormatException(CborConformanceLevel level, object[] keySequence, string hexEncoding)
        {
            var reader = new CborReader(hexEncoding.HexToByteArray(), level);
            reader.ReadStartMap();
            foreach (object key in keySequence.SkipLast(1))
            {
                Helpers.VerifyValue(reader, key); // verify key
                reader.ReadInt32(); // value is always an integer
            }

            int bytesRead = reader.BytesRead;
            int bytesRemaining = reader.BytesRemaining;
            CborReaderState state = reader.PeekState();

            // the final element violates sorting invariant
            Assert.Throws<FormatException>(() => Helpers.VerifyValue(reader, keySequence.Last()));

            // ensure reader state is preserved
            Assert.Equal(bytesRead, reader.BytesRead);
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
            Assert.Equal(state, reader.PeekState());
        }

        [Theory]
        [InlineData("a0", 0)]
        [InlineData("a10102", 1)]
        [InlineData("a3010203040506", 3)]
        public static void ReadMap_DefiniteLengthExceeded_ShouldThrowInvalidOperationException(string hexEncoding, int expectedLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            int? length = reader.ReadStartMap();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 0; i < expectedLength; i++)
            {
                reader.ReadInt64(); // key
                reader.ReadInt64(); // value
            }

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<InvalidOperationException>(() => reader.ReadInt64());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("a101a10101", 1)]
        [InlineData("a301a1010102a1020203a10303", 3)]
        public static void ReadMap_DefiniteLengthExceeded_WithNestedData_ShouldThrowInvalidOperationException(string hexEncoding, int expectedLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            int? length = reader.ReadStartMap();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 0; i < expectedLength; i++)
            {
                reader.ReadInt64(); // key

                // value
                int? nestedLength = reader.ReadStartMap();
                Assert.Equal(1, (int)nestedLength!.Value);
                reader.ReadInt64();
                reader.ReadInt64();
                reader.ReadEndMap();
            }

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<InvalidOperationException>(() => reader.ReadInt64());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("a10101", 1)]
        [InlineData("a3010203040506", 3)]
        public static void ReadEndMap_DefiniteLengthNotMet_ShouldThrowInvalidOperationException(string hexEncoding, int expectedLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            int? length = reader.ReadStartMap();
            Assert.Equal(expectedLength, length!.Value);

            for (int i = 1; i < expectedLength; i++)
            {
                reader.ReadInt64(); // key
                reader.ReadInt64(); // value
            }

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<InvalidOperationException>(() => reader.ReadEndMap());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("a101a10101", 1)]
        [InlineData("a301a1010102a10202a3a10303", 3)]
        public static void ReadEndMap_DefiniteLengthNotMet_WithNestedData_ShouldThrowInvalidOperationException(string hexEncoding, int expectedLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            int? length = reader.ReadStartMap();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 1; i < expectedLength; i++)
            {
                reader.ReadInt64(); // key

                int? nestedLength = reader.ReadStartMap();
                Assert.Equal(1, nestedLength!.Value);
                reader.ReadInt64();
                reader.ReadInt64();
                reader.ReadEndMap();
            }

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<InvalidOperationException>(() => reader.ReadEndMap());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
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

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<InvalidOperationException>(() => reader.ReadEndMap());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("a2011907e4", 2, 1)]
        [InlineData("a6011a01344224031a01344224", 6, 2)]
        public static void ReadMap_IncorrectDefiniteLength_ShouldThrowFormatException(string hexEncoding, int expectedLength, int actualLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            int? length = reader.ReadStartMap();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 0; i < actualLength; i++)
            {
                reader.ReadInt64(); // key
                reader.ReadInt64(); // value
            }

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<FormatException>(() => reader.ReadInt64());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
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
            while (reader.PeekState() == CborReaderState.UnsignedInteger)
            {
                reader.ReadInt64();
            }

            Assert.Equal(CborReaderState.EndOfData, reader.PeekState());
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

            int bytesRemaining = reader.BytesRemaining;

            Assert.Equal(CborReaderState.UnsignedInteger, reader.PeekState());
            Assert.Throws<InvalidOperationException>(() => reader.ReadEndMap());

            Assert.Equal(bytesRemaining, reader.BytesRemaining);
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

            int bytesRemaining = reader.BytesRemaining;

            Assert.Equal(CborReaderState.FormatError, reader.PeekState()); // don't want this to fail
            Assert.Throws<FormatException>(() => reader.ReadEndMap());

            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("a201811907e4", 2, 1)]
        [InlineData("a61907e4811907e402811907e4", 6, 2)]
        public static void ReadMap_IncorrectDefiniteLength_NestedValues_ShouldThrowFormatException(string hexEncoding, int expectedLength, int actualLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            int? length = reader.ReadStartMap();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 0; i < actualLength; i++)
            {
                reader.ReadInt64(); // key

                int? innerLength = reader.ReadStartArray();
                Assert.Equal(1, innerLength!.Value);
                reader.ReadInt64();
                reader.ReadEndArray();
            }

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<FormatException>(() => reader.ReadInt64());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadStartMap_EmptyBuffer_ShouldThrowFormatException()
        {
            byte[] encoding = Array.Empty<byte>();
            var reader = new CborReader(encoding);

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<FormatException>(() => reader.ReadStartMap());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
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
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.ReadStartMap());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
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
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadStartMap());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("b1")]
        [InlineData("b20101")]
        [InlineData("bb8000000000000000")] // long.MaxValue + 1
        [InlineData("bbffffffffffffffff")] // ulong.MaxValue
        public static void ReadStartMap_BufferTooSmall_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadStartMap());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }
    }
}
