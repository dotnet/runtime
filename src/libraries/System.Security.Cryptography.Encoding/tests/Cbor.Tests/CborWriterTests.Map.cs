// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using Test.Cryptography;
using Xunit;
//using static W = System.Formats.Cbor.Tests.CborWriterHelpers;

namespace System.Formats.Cbor.Tests
{
    public partial class CborWriterTests
    {
        // Data points taken from https://tools.ietf.org/html/rfc7049#appendix-A
        // Additional pairs generated using http://cbor.me/

        public const string Map = Helpers.MapPrefixIdentifier;
        public const string Hex = Helpers.HexByteStringIdentifier;

        [Theory]
        [InlineData(new object[] { Map }, "a0")]
        [InlineData(new object[] { Map, 1, 2, 3, 4 }, "a201020304")]
        [InlineData(new object[] { Map, "a", "A", "b", "B", "c", "C", "d", "D", "e", "E" }, "a56161614161626142616361436164614461656145")]
        [InlineData(new object[] { Map, "a", "A", -1, 2, new byte[] { }, new byte[] { 1 } }, "a3616161412002404101")]
        public static void WriteMap_SimpleValues_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            using var writer = new CborWriter();
            Helpers.WriteMap(writer, values);
            byte[] actualEncoding = writer.GetEncoding();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(new object[] { Map, "a", 1, "b", new object[] { Map, 2, 3 } }, "a26161016162a10203")]
        [InlineData(new object[] { Map, "a", new object[] { Map, 2, 3 }, "b", new object[] { Map, "x", -1, "y", new object[] { Map, "z", 0 } } }, "a26161a102036162a26178206179a1617a00")]
        [InlineData(new object[] { Map, new object[] { Map, "x", 2 }, 42 }, "a1a1617802182a")] // using maps as keys
        public static void WriteMap_NestedValues_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            using var writer = new CborWriter();
            Helpers.WriteMap(writer, values);
            byte[] actualEncoding = writer.GetEncoding();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(new object[] { Map }, "bfff")]
        [InlineData(new object[] { Map, 1, 2, 3, 4 }, "bf01020304ff")]
        [InlineData(new object[] { Map, "a", "A", "b", "B", "c", "C", "d", "D", "e", "E" }, "bf6161614161626142616361436164614461656145ff")]
        [InlineData(new object[] { Map, "a", "A", -1, 2, new byte[] { }, new byte[] { 1 } }, "bf616161412002404101ff")]
        public static void WriteMap_IndefiniteLength_NoPatching_SimpleValues_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            using var writer = new CborWriter(encodeIndefiniteLengths: true);
            Helpers.WriteMap(writer, values, useDefiniteLengthCollections: false);
            byte[] actualEncoding = writer.GetEncoding();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(new object[] { Map, "a", 1, "b", new object[] { Map, 2, 3 } }, "bf6161016162bf0203ffff")]
        [InlineData(new object[] { Map, "a", new object[] { Map, 2, 3 }, "b", new object[] { Map, "x", -1, "y", new object[] { Map, "z", 0 } } }, "bf6161bf0203ff6162bf6178206179bf617a00ffffff")]
        [InlineData(new object[] { Map, new object[] { Map, "x", 2 }, 42 }, "bfbf617802ff182aff")] // using maps as keys
        public static void WriteMap_IndefiniteLength_NoPatching_NestedValues_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            using var writer = new CborWriter(encodeIndefiniteLengths: true);
            Helpers.WriteMap(writer, values, useDefiniteLengthCollections: false);
            byte[] actualEncoding = writer.GetEncoding();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(new object[] { Map }, "a0")]
        [InlineData(new object[] { Map, 1, 2, 3, 4 }, "a201020304")]
        [InlineData(new object[] { Map, "a", "A", "b", "B", "c", "C", "d", "D", "e", "E" }, "a56161614161626142616361436164614461656145")]
        [InlineData(new object[] { Map, "a", "A", -1, 2, new byte[] { }, new byte[] { 1 } }, "a3616161412002404101")]
        public static void WriteMap_IndefiniteLength_WithPatching_SimpleValues_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            using var writer = new CborWriter();
            Helpers.WriteMap(writer, values, useDefiniteLengthCollections: false);
            byte[] actualEncoding = writer.GetEncoding();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(new object[] { Map, "a", 1, "b", new object[] { Map, 2, 3 } }, "a26161016162a10203")]
        [InlineData(new object[] { Map, "a", new object[] { Map, 2, 3 }, "b", new object[] { Map, "x", -1, "y", new object[] { Map, "z", 0 } } }, "a26161a102036162a26178206179a1617a00")]
        [InlineData(new object[] { Map, new object[] { Map, "x", 2 }, 42 }, "a1a1617802182a")] // using maps as keys
        public static void WriteMap_IndefiniteLength_WithPatching_NestedValues_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            using var writer = new CborWriter();
            Helpers.WriteMap(writer, values, useDefiniteLengthCollections: false);
            byte[] actualEncoding = writer.GetEncoding();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(new object[] { Map, 3, 4, 1, 2 }, "a201020304")]
        [InlineData(new object[] { Map, "d", "D", "e", "E", "a", "A", "b", "B", "c", "C" }, "a56161614161626142616361436164614461656145")]
        [InlineData(new object[] { Map, "a", "A", -1, 2, new byte[] { }, new byte[] { 1 } }, "a3200240410161616141")]
        [InlineData(new object[] { Map, new object[] { Map, 3, 4, 1, 2 }, 0, new object[] { 1, 2, 3 }, 0, new string[] { "a", "b" }, 0, new string[] { Hex, "ab", "" }, 00 }, "a441ab00626162008301020300a20102030400")]
        public static void WriteMap_IndefiniteLength_WithPatching_Ctap2Sorting_HappyPath(object[] values, string expectedHexEncoding)
    {
        byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
        using var writer = new CborWriter(CborConformanceLevel.Ctap2Canonical);
        Helpers.WriteMap(writer, values, useDefiniteLengthCollections: false);
        byte[] actualEncoding = writer.GetEncoding();
        AssertHelper.HexEqual(expectedEncoding, actualEncoding);
    }

    [Theory]
        [InlineData(new object[] { Map, "a", 1, "b", new object[] { 2, 3 } }, "a26161016162820203")]
        [InlineData(new object[] { Map, "a", new object[] { 2, 3, "b", new object[] { Map, "x", -1, "y", new object[] { "z", 0 } } } }, "a161618402036162a2617820617982617a00")]
        [InlineData(new object[] { "a", new object[] { Map, "b", "c" } }, "826161a161626163")]
        [InlineData(new object[] { Map, new object[] { 1 }, 42 }, "a18101182a")] // using arrays as keys
        public static void WriteMap_NestedListValues_HappyPath(object value, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            using var writer = new CborWriter();
            Helpers.WriteValue(writer, value);
            byte[] actualEncoding = writer.GetEncoding();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(CborConformanceLevel.Lax, new object[] { Map, 3, 3, 2, 2, 1, 1 }, "a3030302020101")]
        [InlineData(CborConformanceLevel.Strict, new object[] { Map, 3, 3, 2, 2, 1, 1 }, "a3030302020101")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { Map, 3, 3, 2, 2, 1, 1 }, "a3010102020303")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { Map, 3, 3, 2, 2, 1, 1 }, "a3010102020303")]
        // nested array payload
        [InlineData(CborConformanceLevel.Lax, new object[] { Map, "b", 0, 2, 0, "a", 0, new object[] { "c", "" }, 0, 1, 0 }, "a5616200020061610082616360000100")]
        [InlineData(CborConformanceLevel.Strict, new object[] { Map, "b", 0, 2, 0, "a", 0, new object[] { "c", "" }, 0, 1, 0 }, "a5616200020061610082616360000100")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { Map, "b", 0, 2, 0, "a", 0, new object[] { "c", "" }, 0, 1, 0 }, "a5010002006161006162008261636000")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { Map, "b", 0, 2, 0, "a", 0, new object[] { "c", "" }, 0, 1, 0 }, "a5010002006161006162008261636000")]
        // CBOR sorting rules do not match canonical string sorting
        [InlineData(CborConformanceLevel.Lax, new object[] { Map, "aa", 0, "z", 0 }, "a262616100617a00")]
        [InlineData(CborConformanceLevel.Strict, new object[] { Map, "aa", 0, "z", 0 }, "a262616100617a00")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { Map, "aa", 0, "z", 0 }, "a2617a0062616100")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { Map, "aa", 0, "z", 0 }, "a2617a0062616100")]
        // Test case distinguishing between RFC7049 and CTAP2 sorting rules
        [InlineData(CborConformanceLevel.Lax, new object[] { Map, "", 0, 255, 0 }, "a2600018ff00")]
        [InlineData(CborConformanceLevel.Strict, new object[] { Map, "", 0, 255, 0 }, "a2600018ff00")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { Map, "", 0, 255, 0 }, "a2600018ff00")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { Map, "", 0, 255, 0 }, "a218ff006000")]
        public static void WriteMap_SimpleValues_ShouldSortKeysAccordingToConformanceLevel(CborConformanceLevel level, object value, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            using var writer = new CborWriter(level);
            Helpers.WriteValue(writer, value);
            byte[] actualEncoding = writer.GetEncoding();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData("a52000a30303020201010061610019010000a20202010100", CborConformanceLevel.Lax)]
        [InlineData("a52000a30303020201010061610019010000a20202010100", CborConformanceLevel.Strict)]
        [InlineData("a5200061610019010000a20101020200a301010202030300", CborConformanceLevel.Rfc7049Canonical)]
        [InlineData("a5190100002000616100a20101020200a301010202030300", CborConformanceLevel.Ctap2Canonical)]
        public static void WriteMap_NestedValues_ShouldSortKeysAccordingToConformanceLevel(string expectedHexEncoding, CborConformanceLevel level)
        {
            object[] value = new object[] { Map, -1, 0, new object[] { Map, 3, 3, 2, 2, 1, 1 }, 0, "a", 0, 256, 0, new object[] { Map, 2, 2, 1, 1 }, 0 };
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            using var writer = new CborWriter(level);
            Helpers.WriteValue(writer, value);
            byte[] actualEncoding = writer.GetEncoding();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(new object[] { Map, "a", 1, "a", 2 }, "a2616101616102")]
        public static void WriteMap_DuplicateKeys_ShouldSucceed(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            using var writer = new CborWriter();
            Helpers.WriteMap(writer, values);
            byte[] actualEncoding = writer.GetEncoding();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(CborConformanceLevel.Strict, 42)]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, 42)]
        [InlineData(CborConformanceLevel.Ctap2Canonical, 42)]
        [InlineData(CborConformanceLevel.Strict, "foobar")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "foobar")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "foobar")]
        [InlineData(CborConformanceLevel.Strict, new object[] { new object[] { "x", "y" } })]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { new object[] { "x", "y" } })]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { new object[] { "x", "y" } })]
        public static void WriteMap_DuplicateKeys_StrictConformance_ShouldFail(CborConformanceLevel level, object dupeKey)
        {
            using var writer = new CborWriter(level);
            writer.WriteStartMap(2);
            Helpers.WriteValue(writer, dupeKey);
            writer.WriteInt32(0);
            Assert.Throws<InvalidOperationException>(() => Helpers.WriteValue(writer, dupeKey));
        }

        [Theory]
        [InlineData(CborConformanceLevel.Strict, 42)]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, 42)]
        [InlineData(CborConformanceLevel.Ctap2Canonical, 42)]
        [InlineData(CborConformanceLevel.Strict, "foobar")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "foobar")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "foobar")]
        [InlineData(CborConformanceLevel.Strict, new object[] { new object[] { "x", "y" } })]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, new object[] { new object[] { "x", "y" } })]
        [InlineData(CborConformanceLevel.Ctap2Canonical, new object[] { new object[] { "x", "y" } })]
        public static void WriteMap_DuplicateKeys_StrictConformance_ShouldBeRecoverableError(CborConformanceLevel level, object dupeKey)
        {
            byte[] expected = PerformWrite(attemptDuplicateWrite: false);
            byte[] actual = PerformWrite(attemptDuplicateWrite: true);
            Assert.Equal(expected.ByteArrayToHex(), actual.ByteArrayToHex());

            byte[] PerformWrite(bool attemptDuplicateWrite)
            {
                using var writer = new CborWriter(level);
                writer.WriteStartMap(2);
                Helpers.WriteValue(writer, dupeKey);
                writer.WriteInt32(0);

                if (attemptDuplicateWrite)
                {
                    Assert.Throws<InvalidOperationException>(() => Helpers.WriteValue(writer, dupeKey));
                }

                // wrap dupe key in an array to satisfy key sorting & uniqueness constraints
                Helpers.WriteValue(writer, new object[] { dupeKey }); 
                writer.WriteInt32(0);
                writer.WriteEndMap();

                return writer.GetEncoding();
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public static void WriteMap_DefiniteLengthExceeded_ShouldThrowInvalidOperationException(int definiteLength)
        {
            using var writer = new CborWriter();
            writer.WriteStartMap(definiteLength);
            for (int i = 0; i < definiteLength; i++)
            {
                writer.WriteTextString($"key_{i}");
                writer.WriteInt64(i);
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteInt64(0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public static void WriteMap_DefiniteLengthExceeded_WithNestedData_ShouldThrowInvalidOperationException(int definiteLength)
        {
            using var writer = new CborWriter();
            writer.WriteStartMap(definiteLength);
            for (int i = 0; i < definiteLength; i++)
            {
                writer.WriteTextString($"key_{i}");
                writer.WriteStartMap(definiteLength: 1);
                writer.WriteInt64(i);
                writer.WriteInt64(i);
                writer.WriteEndMap();
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteInt64(0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public static void EndWriteMap_DefiniteLengthNotMet_ShouldThrowInvalidOperationException(int definiteLength)
        {
            using var writer = new CborWriter();
            writer.WriteStartMap(definiteLength);
            for (int i = 1; i < definiteLength; i++)
            {
                writer.WriteTextString($"key_{i}");
                writer.WriteInt64(i);
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteEndMap());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public static void EndWriteMap_DefiniteLengthNotMet_WithNestedData_ShouldThrowInvalidOperationException(int definiteLength)
        {
            using var writer = new CborWriter();
            writer.WriteStartMap(definiteLength);
            for (int i = 1; i < definiteLength; i++)
            {
                writer.WriteTextString($"key_{i}");
                writer.WriteStartMap(definiteLength: 1);
                writer.WriteInt64(i);
                writer.WriteInt64(i);
                writer.WriteEndMap();
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteEndMap());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(10)]
        public static void EndWriteMap_IndefiniteLength_OddItems_ShouldThrowInvalidOperationException(int length)
        {
            using var writer = new CborWriter();
            writer.WriteStartMap();

            for (int i = 1; i < length; i++)
            {
                writer.WriteTextString($"key_{i}");
                writer.WriteInt64(i);
            }

            writer.WriteInt64(0);

            Assert.Throws<InvalidOperationException>(() => writer.WriteEndMap());
        }

        [Fact]
        public static void EndWriteMap_ImbalancedCall_ShouldThrowInvalidOperationException()
        {
            using var writer = new CborWriter();
            Assert.Throws<InvalidOperationException>(() => writer.WriteEndMap());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public static void WriteEndMap_ImbalancedCall_ShouldThrowInvalidOperationException(int depth)
        {
            using var writer = new CborWriter();
            for (int i = 0; i < depth; i++)
            {
                writer.WriteStartArray(1);
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteEndMap());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public static void WriteEndMap_AfterStartArray_ShouldThrowInvalidOperationException(int depth)
        {
            using var writer = new CborWriter();

            for (int i = 0; i < depth; i++)
            {
                if (i % 2 == 0)
                {
                    writer.WriteStartArray(1);
                }
                else
                {
                    writer.WriteStartMap(1);
                }
            }

            writer.WriteStartArray(definiteLength: 0);
            Assert.Throws<InvalidOperationException>(() => writer.WriteEndMap());
        }
    }
}
