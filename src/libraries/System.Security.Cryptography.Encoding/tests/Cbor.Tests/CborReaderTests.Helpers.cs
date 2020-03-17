// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborReaderTests
    {
        internal static class Helpers
        {
            public static void VerifyValue(CborReader reader, object expectedValue)
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
                        Assert.Equal(expected, b);
                        break;
                    case object[] nested when CborWriterTests.Helpers.IsCborMapRepresentation(nested):
                        VerifyMap(reader, nested);
                        break;
                    case object[] nested:
                        VerifyArray(reader, nested);
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

            public static void VerifyArray(CborReader reader, params object[] expectedValues)
            {
                Assert.Equal(CborReaderState.StartArray, reader.Peek());

                ulong? length = reader.ReadStartArray();

                Assert.NotNull(length);
                Assert.Equal(expectedValues.Length, (int)length!.Value);

                foreach (object value in expectedValues)
                {
                    VerifyValue(reader, value);
                }

                Assert.Equal(CborReaderState.EndArray, reader.Peek());
                reader.ReadEndArray();
            }

            public static void VerifyMap(CborReader reader, params object[] expectedValues)
            {
                if (!CborWriterTests.Helpers.IsCborMapRepresentation(expectedValues))
                {
                    throw new ArgumentException($"cbor map expected values missing '{CborWriterTests.Helpers.MapPrefixIdentifier}' prefix.");
                }

                Assert.Equal(CborReaderState.StartMap, reader.Peek());
                ulong? length = reader.ReadStartMap();

                Assert.NotNull(length);
                Assert.Equal((expectedValues.Length - 1) / 2, (int)length!.Value);

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
