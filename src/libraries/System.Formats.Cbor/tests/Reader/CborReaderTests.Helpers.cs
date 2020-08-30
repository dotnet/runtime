// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Numerics;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborReaderTests
    {
        internal static class Helpers
        {
            public static void VerifyValue(CborReader reader, object expectedValue, bool expectDefiniteLengthCollections = true)
            {
                switch (expectedValue)
                {
                    case null:
                        Assert.Equal(CborReaderState.Null, reader.PeekState());
                        reader.ReadNull();
                        break;

                    case bool expected:
                        Assert.Equal(CborReaderState.Boolean, reader.PeekState());
                        bool b = reader.ReadBoolean();
                        Assert.Equal(expected, b);
                        break;

                    case int expected:
                        VerifyPeekInteger(reader, isUnsignedInteger: expected >= 0);
                        int i = reader.ReadInt32();
                        Assert.Equal(expected, i);
                        break;

                    case long expected:
                        VerifyPeekInteger(reader, isUnsignedInteger: expected >= 0);
                        long l = reader.ReadInt64();
                        Assert.Equal(expected, l);
                        break;

                    case ulong expected:
                        VerifyPeekInteger(reader, isUnsignedInteger: true);
                        ulong u = reader.ReadUInt64();
                        Assert.Equal(expected, u);
                        break;

                    case float expected:
                        Assert.Equal(CborReaderState.SinglePrecisionFloat, reader.PeekState());
                        float f = reader.ReadSingle();
                        Assert.Equal(expected, f);
                        break;

                    case double expected:
                        Assert.Equal(CborReaderState.DoublePrecisionFloat, reader.PeekState());
                        double d = reader.ReadDouble();
                        Assert.Equal(expected, d);
                        break;

                    case decimal expected:
                        Assert.Equal(CborReaderState.Tag, reader.PeekState());
                        decimal dec = reader.ReadDecimal();
                        Assert.Equal(expected, dec);
                        break;

                    case BigInteger expected:
                        Assert.Equal(CborReaderState.Tag, reader.PeekState());
                        BigInteger bigint = reader.ReadBigInteger();
                        Assert.Equal(expected, bigint);
                        break;

                    case DateTimeOffset expected:
                        Assert.Equal(CborReaderState.Tag, reader.PeekState());
                        DateTimeOffset dto = reader.ReadDateTimeOffset();
                        Assert.Equal(expected, dto);
                        break;

                    case string expected:
                        Assert.Equal(CborReaderState.TextString, reader.PeekState());
                        string s = reader.ReadTextString();
                        Assert.Equal(expected, s);
                        break;

                    case byte[] expected:
                        Assert.Equal(CborReaderState.ByteString, reader.PeekState());
                        byte[] bytes = reader.ReadByteString();
                        Assert.Equal(expected.ByteArrayToHex(), bytes.ByteArrayToHex());
                        break;

                    case string[] expectedChunks when CborWriterTests.Helpers.IsIndefiniteLengthByteString(expectedChunks):
                        byte[][] expectedByteChunks = expectedChunks.Skip(1).Select(ch => ch.HexToByteArray()).ToArray();
                        VerifyValue(reader, expectedByteChunks, expectDefiniteLengthCollections);
                        break;

                    case string[] expectedChunks:
                        Assert.Equal(CborReaderState.StartIndefiniteLengthTextString, reader.PeekState());
                        reader.ReadStartIndefiniteLengthTextString();
                        foreach(string expectedChunk in expectedChunks)
                        {
                            Assert.Equal(CborReaderState.TextString, reader.PeekState());
                            string chunk = reader.ReadTextString();
                            Assert.Equal(expectedChunk, chunk);
                        }
                        Assert.Equal(CborReaderState.EndIndefiniteLengthTextString, reader.PeekState());
                        reader.ReadEndIndefiniteLengthTextString();
                        break;

                    case byte[][] expectedChunks:
                        Assert.Equal(CborReaderState.StartIndefiniteLengthByteString, reader.PeekState());
                        reader.ReadStartIndefiniteLengthByteString();
                        foreach (byte[] expectedChunk in expectedChunks)
                        {
                            Assert.Equal(CborReaderState.ByteString, reader.PeekState());
                            byte[] chunk = reader.ReadByteString();
                            Assert.Equal(expectedChunk.ByteArrayToHex(), chunk.ByteArrayToHex());
                        }
                        Assert.Equal(CborReaderState.EndIndefiniteLengthByteString, reader.PeekState());
                        reader.ReadEndIndefiniteLengthByteString();
                        break;

                    case object[] nested when CborWriterTests.Helpers.IsCborMapRepresentation(nested):
                        VerifyMap(reader, nested, expectDefiniteLengthCollections);
                        break;

                    case object[] nested when CborWriterTests.Helpers.IsEncodedValueRepresentation(nested):
                        string expectedHexEncoding = (string)nested[1];
                        string actualHexEncoding = reader.ReadEncodedValue().ByteArrayToHex();
                        Assert.Equal(expectedHexEncoding, actualHexEncoding);
                        break;

                    case object[] nested when CborWriterTests.Helpers.IsTaggedValueRepresentation(nested):
                        CborTag expectedTag = (CborTag)nested[0];
                        object expectedNestedValue = nested[1];
                        Assert.Equal(CborReaderState.Tag, reader.PeekState());
                        Assert.Equal(expectedTag, reader.ReadTag());
                        VerifyValue(reader, expectedNestedValue, expectDefiniteLengthCollections);
                        break;

                    case object[] nested:
                        VerifyArray(reader, nested, expectDefiniteLengthCollections);
                        break;

                    default:
                        throw new ArgumentException($"Unrecognized argument type {expectedValue.GetType()}");
                }

                static void VerifyPeekInteger(CborReader reader, bool isUnsignedInteger)
                {
                    CborReaderState expectedState = isUnsignedInteger ? CborReaderState.UnsignedInteger : CborReaderState.NegativeInteger;
                    Assert.Equal(expectedState, reader.PeekState());
                }
            }

            public static void VerifyArray(CborReader reader, object[] expectedValues, bool expectDefiniteLengthCollections = true)
            {
                Assert.Equal(CborReaderState.StartArray, reader.PeekState());

                int? length = reader.ReadStartArray();

                if (expectDefiniteLengthCollections)
                {
                    Assert.NotNull(length);
                    Assert.Equal(expectedValues.Length, (int)length!.Value);
                }
                else
                {
                    Assert.Null(length);
                }

                foreach (object value in expectedValues)
                {
                    VerifyValue(reader, value);
                }

                Assert.Equal(CborReaderState.EndArray, reader.PeekState());
                reader.ReadEndArray();
            }

            public static void VerifyMap(CborReader reader, object[] expectedValues, bool expectDefiniteLengthCollections = true)
            {
                if (!CborWriterTests.Helpers.IsCborMapRepresentation(expectedValues))
                {
                    throw new ArgumentException($"cbor map expected values missing '{CborWriterTests.Helpers.MapPrefixIdentifier}' prefix.");
                }

                Assert.Equal(CborReaderState.StartMap, reader.PeekState());

                int? length = reader.ReadStartMap();

                if (expectDefiniteLengthCollections)
                {
                    Assert.NotNull(length);
                    Assert.Equal((expectedValues.Length - 1) / 2, (int)length!.Value);
                }
                else
                {
                    Assert.Null(length);
                }

                foreach (object value in expectedValues.Skip(1))
                {
                    VerifyValue(reader, value, expectDefiniteLengthCollections);
                }

                Assert.Equal(CborReaderState.EndMap, reader.PeekState());
                reader.ReadEndMap();
            }
        }

        public static string[] SampleCborValues =>
            new[]
            {
                // numeric values
                "01",
                "37",
                "3818",
                "1818",
                "190100",
                "390100",
                "1a000f4240",
                "3affffffff",
                // byte strings
                "40",
                "4401020304",
                "5f41ab40ff",
                // text strings
                "60",
                "6161",
                "6449455446",
                "7f62616260ff",
                // Arrays
                "80",
                "840120604107",
                "8301820203820405",
                "9f182aff",
                // Maps
                "a0",
                "a201020304",
                "a1a1617802182a",
                "bf01020304ff",
                // tagged values
                "c202",
                "d82076687474703a2f2f7777772e6578616d706c652e636f6d",
                // simple values
                "f4",
                "f6",
                // floating point encodings
                "fa47c35000",
            };

        public static string[] InvalidCborValues =>
            new[]
            {
                "",
                // numeric types with missing bytes
                "18",
                "19ff",
                "1affffff",
                "1bffffffffffffff",
                "38",
                "39ff",
                "3affffff",
                "3bffffffffffffff",
                // definite-length strings with missing bytes
                "41",
                "4201",
                "61",
                "6261",
                // indefinite-length strings with missing break byte
                "5f41ab40",
                "7f62616260",
                // definite-length arrays with missing elements
                "81",
                "8201",
                // definite-length maps with missing fields
                "a1",
                "a20102",
                // maps with odd number of elements
                "a101",
                "a2010203",
                "bf01ff",
                // indefinite-length collections with missing break byte
                "9f",
                "9f01",
                "bf",
                "bf0102",
                // tags missing data
                "d8",
                "d9ff",
                "daffffff",
                "daffffffffff",
                // valid tag not followed by value
                "c2",
                // floats missing data
                "f9ff",
                "faffffff",
                "fbffffffffffffff",
                // two-byte simple value missing data
                "f8",
            };
    }
}
