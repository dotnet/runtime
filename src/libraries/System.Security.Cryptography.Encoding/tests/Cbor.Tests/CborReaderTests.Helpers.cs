// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborReaderTests
    {
        internal static class Helpers
        {
            public static void VerifyValue(CborReader reader, object expectedValue, bool expectDefiniteLengthCollections = true)
            {
                switch (expectedValue)
                {
                    case int expected:
                        VerifyPeekInteger(reader, isUnsignedInteger: expected >= 0);
                        long i = reader.ReadInt64();
                        Assert.Equal(expected, (int)i);
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
                    case string expected:
                        Assert.Equal(CborReaderState.TextString, reader.Peek());
                        string s = reader.ReadTextString();
                        Assert.Equal(expected, s);
                        break;
                    case byte[] expected:
                        Assert.Equal(CborReaderState.ByteString, reader.Peek());
                        byte[] b = reader.ReadByteString();
                        Assert.Equal(expected.ByteArrayToHex(), b.ByteArrayToHex());
                        break;
                    case string[] expectedChunks:
                        Assert.Equal(CborReaderState.StartTextString, reader.Peek());
                        reader.ReadStartTextStringIndefiniteLength();
                        foreach(string expectedChunk in expectedChunks)
                        {
                            Assert.Equal(CborReaderState.TextString, reader.Peek());
                            string chunk = reader.ReadTextString();
                            Assert.Equal(expectedChunk, chunk);
                        }
                        Assert.Equal(CborReaderState.EndTextString, reader.Peek());
                        reader.ReadEndTextStringIndefiniteLength();
                        break;
                    case byte[][] expectedChunks:
                        Assert.Equal(CborReaderState.StartByteString, reader.Peek());
                        reader.ReadStartByteStringIndefiniteLength();
                        foreach (byte[] expectedChunk in expectedChunks)
                        {
                            Assert.Equal(CborReaderState.ByteString, reader.Peek());
                            byte[] chunk = reader.ReadByteString();
                            Assert.Equal(expectedChunk.ByteArrayToHex(), chunk.ByteArrayToHex());
                        }
                        Assert.Equal(CborReaderState.EndByteString, reader.Peek());
                        reader.ReadEndByteStringIndefiniteLength();
                        break;

                    case object[] nested when CborWriterTests.Helpers.IsCborMapRepresentation(nested):
                        VerifyMap(reader, nested, expectDefiniteLengthCollections);
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
                    Assert.Equal(expectedState, reader.Peek());
                }
            }

            public static void VerifyArray(CborReader reader, object[] expectedValues, bool expectDefiniteLengthCollections = true)
            {
                Assert.Equal(CborReaderState.StartArray, reader.Peek());

                ulong? length = reader.ReadStartArray();

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

                Assert.Equal(CborReaderState.EndArray, reader.Peek());
                reader.ReadEndArray();
            }

            public static void VerifyMap(CborReader reader, object[] expectedValues, bool expectDefiniteLengthCollections = true)
            {
                if (!CborWriterTests.Helpers.IsCborMapRepresentation(expectedValues))
                {
                    throw new ArgumentException($"cbor map expected values missing '{CborWriterTests.Helpers.MapPrefixIdentifier}' prefix.");
                }

                Assert.Equal(CborReaderState.StartMap, reader.Peek());

                ulong? length = reader.ReadStartMap();

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
                    VerifyValue(reader, value);
                }

                Assert.Equal(CborReaderState.EndMap, reader.Peek());
                reader.ReadEndMap();
            }
        }
    }
}
