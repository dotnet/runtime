// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborWriterTests
    {
        // Data points taken from https://tools.ietf.org/html/rfc7049#appendix-A
        // Additional pairs generated using http://cbor.me/

        [Theory]
        [InlineData(new object[] { }, "80")]
        [InlineData(new object[] { 42 }, "81182a")]
        [InlineData(new object[] { 1, 2, 3 }, "83010203")]
        [InlineData(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 }, "98190102030405060708090a0b0c0d0e0f101112131415161718181819")]
        [InlineData(new object[] { 1, -1, "", new byte[] { 7 } }, "840120604107")]
        [InlineData(new object[] { "lorem", "ipsum", "dolor" }, "83656c6f72656d65697073756d65646f6c6f72")]
        [InlineData(new object?[] { false, null, float.NaN, double.PositiveInfinity }, "84f4f6f97e00f97c00")]
        public static void WriteArray_SimpleValues_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            var writer = new CborWriter();
            Helpers.WriteArray(writer, values);
            byte[] actualEncoding = writer.Encode();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(new object[] { new object[] { } }, "8180")]
        [InlineData(new object[] { 1, new object[] { 2, 3 }, new object[] { 4, 5 } }, "8301820203820405")]
        [InlineData(new object[] { "", new object[] { new object[] { }, new object[] { 1, new byte[] { 10 } } } }, "826082808201410a")]
        public static void WriteArray_NestedValues_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            var writer = new CborWriter();
            Helpers.WriteArray(writer, values);
            byte[] actualEncoding = writer.Encode();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(new object[] { }, "9fff")]
        [InlineData(new object[] { 42 }, "9f182aff")]
        [InlineData(new object[] { 1, 2, 3 }, "9f010203ff")]
        [InlineData(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 }, "9f0102030405060708090a0b0c0d0e0f101112131415161718181819ff")]
        [InlineData(new object[] { 1, -1, "", new byte[] { 7 } }, "9f0120604107ff")]
        [InlineData(new object[] { "lorem", "ipsum", "dolor" }, "9f656c6f72656d65697073756d65646f6c6f72ff")]
        [InlineData(new object?[] { false, null, float.NaN, double.PositiveInfinity }, "9ff4f6f97e00f97c00ff")]
        public static void WriteArray_IndefiniteLength_NoPatching_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();

            var writer = new CborWriter(convertIndefiniteLengthEncodings: false);
            Helpers.WriteArray(writer, values, useDefiniteLengthCollections: false);

            byte[] actualEncoding = writer.Encode();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(new object[] { new object[] { } }, "9f9fffff")]
        [InlineData(new object[] { 1, new object[] { 2, 3 }, new object[] { 4, 5 } }, "9f019f0203ff9f0405ffff")]
        [InlineData(new object[] { "", new object[] { new object[] { }, new object[] { 1, new byte[] { 10 } } } }, "9f609f9fff9f01410affffff")]
        public static void WriteArray_IndefiniteLength_NoPatching_NestedValues_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();

            var writer = new CborWriter(convertIndefiniteLengthEncodings: false);
            Helpers.WriteArray(writer, values, useDefiniteLengthCollections: false);

            byte[] actualEncoding = writer.Encode();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(new object[] { }, "80")]
        [InlineData(new object[] { 42 }, "81182a")]
        [InlineData(new object[] { 1, 2, 3 }, "83010203")]
        [InlineData(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 }, "98190102030405060708090a0b0c0d0e0f101112131415161718181819")]
        [InlineData(new object[] { 1, -1, "", new byte[] { 7 } }, "840120604107")]
        [InlineData(new object[] { "lorem", "ipsum", "dolor" }, "83656c6f72656d65697073756d65646f6c6f72")]
        [InlineData(new object?[] { false, null, float.NaN, double.PositiveInfinity }, "84f4f6f97e00f97c00")]
        public static void WriteArray_IndefiniteLength_WithPatching_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();

            var writer = new CborWriter(convertIndefiniteLengthEncodings: true);
            Helpers.WriteArray(writer, values, useDefiniteLengthCollections: false);

            byte[] actualEncoding = writer.Encode();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(new object[] { new object[] { } }, "8180")]
        [InlineData(new object[] { 1, new object[] { 2, 3 }, new object[] { 4, 5 } }, "8301820203820405")]
        [InlineData(new object[] { "", new object[] { new object[] { }, new object[] { 1, new byte[] { 10 } } } }, "826082808201410a")]
        public static void WriteArray_IndefiniteLength_WithPatching_NestedValues_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();

            var writer = new CborWriter(convertIndefiniteLengthEncodings: true);
            Helpers.WriteArray(writer, values, useDefiniteLengthCollections: false);

            byte[] actualEncoding = writer.Encode();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public static void WriteArray_DefiniteLengthExceeded_ShouldThrowInvalidOperationException(int definiteLength)
        {
            var writer = new CborWriter();
            writer.WriteStartArray(definiteLength);
            for (int i = 0; i < definiteLength; i++)
            {
                writer.WriteInt64(i);
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteInt64(0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public static void WriteArray_DefiniteLengthExceeded_WithNestedData_ShouldThrowInvalidOperationException(int definiteLength)
        {
            var writer = new CborWriter();
            writer.WriteStartArray(definiteLength);
            for (int i = 0; i < definiteLength; i++)
            {
                writer.WriteStartArray(definiteLength: 1);
                writer.WriteInt64(i);
                writer.WriteEndArray();
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteInt64(0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public static void WriteEndArray_DefiniteLengthNotMet_ShouldThrowInvalidOperationException(int definiteLength)
        {
            var writer = new CborWriter();
            writer.WriteStartArray(definiteLength);
            for (int i = 1; i < definiteLength; i++)
            {
                writer.WriteInt64(i);
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteEndArray());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public static void WriteEndArray_DefiniteLengthNotMet_WithNestedData_ShouldThrowInvalidOperationException(int definiteLength)
        {
            var writer = new CborWriter();
            writer.WriteStartArray(definiteLength);
            for (int i = 1; i < definiteLength; i++)
            {
                writer.WriteStartArray(definiteLength: 1);
                writer.WriteInt64(i);
                writer.WriteEndArray();
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteEndArray());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public static void WriteEndArray_ImbalancedCall_ShouldThrowInvalidOperationException(int depth)
        {
            var writer = new CborWriter();
            for (int i = 0; i < depth; i++)
            {
                writer.WriteStartMap(1);
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteEndArray());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public static void WriteEndArray_AfterStartMap_ShouldThrowInvalidOperationException(int depth)
        {
            var writer = new CborWriter();

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

            writer.WriteStartMap(definiteLength: 0);
            Assert.Throws<InvalidOperationException>(() => writer.WriteEndArray());
        }

        [Theory]
        [InlineData(CborConformanceMode.Canonical)]
        [InlineData(CborConformanceMode.Ctap2Canonical)]
        public static void WriteStartArray_IndefiniteLength_NoPatching_UnsupportedConformance_ShouldThrowInvalidOperationException(CborConformanceMode conformanceMode)
        {
            var writer = new CborWriter(conformanceMode, convertIndefiniteLengthEncodings: false);
            Assert.Throws<InvalidOperationException>(() => writer.WriteStartArray(null));
        }
    }
}
