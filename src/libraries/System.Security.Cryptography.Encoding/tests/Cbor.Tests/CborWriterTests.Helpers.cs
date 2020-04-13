// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborWriterTests
    {
        internal static class Helpers
        {
            public const string MapPrefixIdentifier = "_map";

            public const string EncodedPrefixIdentifier = "_encodedValue";

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

            public static void WriteValue(CborWriter writer, object value, bool useDefiniteLengthCollections = true)
            {
                switch (value)
                {
                    case null: writer.WriteNull(); break;
                    case bool b: writer.WriteBoolean(b); break;
                    case int i: writer.WriteInt64(i); break;
                    case long i: writer.WriteInt64(i); break;
                    case ulong i: writer.WriteUInt64(i); break;
                    case float f: writer.WriteSingle(f); break;
                    case double d: writer.WriteDouble(d); break;
                    case string s: writer.WriteTextString(s); break;
                    case byte[] b: writer.WriteByteString(b); break;
                    case byte[][] chunks: WriteChunkedByteString(writer, chunks); break;
                    case string[] chunks: WriteChunkedTextString(writer, chunks); break;
                    case object[] nested when IsCborMapRepresentation(nested): WriteMap(writer, nested, useDefiniteLengthCollections); break;
                    case object[] nested when IsEncodedValueRepresentation(nested):
                        byte[] encodedValue = ((string)nested[1]).HexToByteArray();
                        writer.WriteEncodedValue(encodedValue);
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
                    writer.WriteStartArrayIndefiniteLength();
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
                    writer.WriteStartMapIndefiniteLength();
                }

                foreach (object value in keyValuePairs.Skip(1))
                {
                    WriteValue(writer, value, useDefiniteLengthCollections);
                }

                writer.WriteEndMap();
            }

            public static void WriteChunkedByteString(CborWriter writer, byte[][] chunks)
            {
                writer.WriteStartByteStringIndefiniteLength();
                foreach (byte[] chunk in chunks)
                {
                    writer.WriteByteString(chunk);
                }
                writer.WriteEndByteStringIndefiniteLength();
            }

            public static void WriteChunkedTextString(CborWriter writer, string[] chunks)
            {
                writer.WriteStartTextStringIndefiniteLength();
                foreach (string chunk in chunks)
                {
                    writer.WriteTextString(chunk);
                }
                writer.WriteEndTextStringIndefiniteLength();
            }

            public static void ExecOperation(CborWriter writer, string op)
            {
                switch (op)
                {
                    case nameof(writer.WriteInt64): writer.WriteInt64(42); break;
                    case nameof(writer.WriteByteString): writer.WriteByteString(Array.Empty<byte>()); break;
                    case nameof(writer.WriteTextString): writer.WriteTextString(""); break;
                    case nameof(writer.WriteStartTextStringIndefiniteLength): writer.WriteStartTextStringIndefiniteLength(); break;
                    case nameof(writer.WriteStartByteStringIndefiniteLength): writer.WriteStartByteStringIndefiniteLength(); break;
                    case nameof(writer.WriteStartArray): writer.WriteStartArrayIndefiniteLength(); break;
                    case nameof(writer.WriteStartMap): writer.WriteStartMapIndefiniteLength(); break;
                    case nameof(writer.WriteEndByteStringIndefiniteLength): writer.WriteEndByteStringIndefiniteLength(); break;
                    case nameof(writer.WriteEndTextStringIndefiniteLength): writer.WriteEndTextStringIndefiniteLength(); break;
                    case nameof(writer.WriteEndArray): writer.WriteEndArray(); break;
                    case nameof(writer.WriteEndMap): writer.WriteEndMap(); break;
                    default: throw new Exception($"Unrecognized CborWriter operation name {op}");
                }
            }
        }
    }
}
