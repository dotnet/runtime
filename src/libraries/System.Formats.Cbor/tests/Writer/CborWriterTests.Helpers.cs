// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Numerics;
using Test.Cryptography;

namespace System.Formats.Cbor.Tests
{
    public partial class CborWriterTests
    {
        internal static class Helpers
        {
            public const string MapPrefixIdentifier = "_map";

            public const string EncodedPrefixIdentifier = "_encodedValue";

            public const string HexByteStringIdentifier = "_hex";

            // Since we inject test data using attributes, meed to represent both arrays and maps using object arrays.
            // To distinguish between the two types, we prepend map representations using a string constant.
            public static bool IsCborMapRepresentation(object[] values)
            {
                return values.Length % 2 == 1 && values[0] is string s && s == MapPrefixIdentifier;
            }

            public static bool IsEncodedValueRepresentation(object[] values)
            {
                return values.Length == 2 &&
                       values[0] is string s && s == EncodedPrefixIdentifier &&
                       values[1] is string;
            }

            public static bool IsTaggedValueRepresentation(object[] values)
            {
                return values.Length == 2 && values[0] is CborTag;
            }

            public static bool IsIndefiniteLengthByteString(string[] values)
            {
                return values.Length % 2 == 1 && values[0] == HexByteStringIdentifier;
            }

            public static void WriteValue(CborWriter writer, object value, bool useDefiniteLengthCollections = true)
            {
                switch (value)
                {
                    case null: writer.WriteNull(); break;
                    case bool b: writer.WriteBoolean(b); break;
                    case int i: writer.WriteInt32(i); break;
                    case long i: writer.WriteInt64(i); break;
                    case ulong i: writer.WriteUInt64(i); break;
                    case float f: writer.WriteSingle(f); break;
                    case double d: writer.WriteDouble(d); break;
                    case decimal d: writer.WriteDecimal(d); break;
                    case string s: writer.WriteTextString(s); break;
                    case BigInteger i: writer.WriteBigInteger(i); break;
                    case DateTimeOffset d: writer.WriteDateTimeOffset(d); break;
                    case byte[] b: writer.WriteByteString(b); break;
                    case byte[][] chunks: WriteChunkedByteString(writer, chunks); break;
                    case string[] chunks when IsIndefiniteLengthByteString(chunks):
                        byte[][] byteChunks = chunks.Skip(1).Select(ch => ch.HexToByteArray()).ToArray();
                        WriteChunkedByteString(writer, byteChunks);
                        break;

                    case string[] chunks: WriteChunkedTextString(writer, chunks); break;
                    case object[] nested when IsCborMapRepresentation(nested): WriteMap(writer, nested, useDefiniteLengthCollections); break;
                    case object[] nested when IsEncodedValueRepresentation(nested):
                        byte[] encodedValue = ((string)nested[1]).HexToByteArray();
                        writer.WriteEncodedValue(encodedValue);
                        break;

                    case object[] nested when IsTaggedValueRepresentation(nested):
                        writer.WriteTag((CborTag)nested[0]);
                        WriteValue(writer, nested[1]);
                        break;

                    case object[] nested: WriteArray(writer, nested, useDefiniteLengthCollections); break;
                    default: throw new ArgumentException($"Unrecognized argument type {value.GetType()}");
                };
            }

            public static void WriteArray(CborWriter writer, object[] values, bool useDefiniteLengthCollections = true)
            {
                if (useDefiniteLengthCollections)
                {
                    writer.WriteStartArray(values.Length);
                }
                else
                {
                    writer.WriteStartArray(null);
                }

                foreach (object value in values)
                {
                    WriteValue(writer, value, useDefiniteLengthCollections);
                }

                writer.WriteEndArray();
            }

            public static void WriteMap(CborWriter writer, object[] keyValuePairs, bool useDefiniteLengthCollections = true)
            {
                if (!IsCborMapRepresentation(keyValuePairs))
                {
                    throw new ArgumentException($"CBOR map representation must contain odd number of elements prepended with a '{MapPrefixIdentifier}' constant.");
                }

                if (useDefiniteLengthCollections)
                {
                    writer.WriteStartMap(keyValuePairs.Length / 2);
                }
                else
                {
                    writer.WriteStartMap(null);
                }

                foreach (object value in keyValuePairs.Skip(1))
                {
                    WriteValue(writer, value, useDefiniteLengthCollections);
                }

                writer.WriteEndMap();
            }

            public static void WriteChunkedByteString(CborWriter writer, byte[][] chunks)
            {
                writer.WriteStartIndefiniteLengthByteString();
                foreach (byte[] chunk in chunks)
                {
                    writer.WriteByteString(chunk);
                }
                writer.WriteEndIndefiniteLengthByteString();
            }

            public static void WriteChunkedTextString(CborWriter writer, string[] chunks)
            {
                writer.WriteStartIndefiniteLengthTextString();
                foreach (string chunk in chunks)
                {
                    writer.WriteTextString(chunk);
                }
                writer.WriteEndIndefiniteLengthTextString();
            }

            public static void ExecOperation(CborWriter writer, string op)
            {
                switch (op)
                {
                    case nameof(writer.WriteInt64): writer.WriteInt64(42); break;
                    case nameof(writer.WriteByteString): writer.WriteByteString(Array.Empty<byte>()); break;
                    case nameof(writer.WriteTextString): writer.WriteTextString(""); break;
                    case nameof(writer.WriteStartIndefiniteLengthTextString): writer.WriteStartIndefiniteLengthTextString(); break;
                    case nameof(writer.WriteStartIndefiniteLengthByteString): writer.WriteStartIndefiniteLengthByteString(); break;
                    case nameof(writer.WriteStartArray): writer.WriteStartArray(null); break;
                    case nameof(writer.WriteStartMap): writer.WriteStartMap(null); break;
                    case nameof(writer.WriteEndIndefiniteLengthByteString): writer.WriteEndIndefiniteLengthByteString(); break;
                    case nameof(writer.WriteEndIndefiniteLengthTextString): writer.WriteEndIndefiniteLengthTextString(); break;
                    case nameof(writer.WriteEndArray): writer.WriteEndArray(); break;
                    case nameof(writer.WriteEndMap): writer.WriteEndMap(); break;
                    default: throw new Exception($"Unrecognized CborWriter operation name {op}");
                }
            }
        }
    }
}
