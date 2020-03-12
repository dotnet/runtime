// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborWriterTests
    {
        internal static class Helpers
        {
            public const string MapPrefixIdentifier = "_map";

            // Since we inject test data using attributes, meed to represent both arrays and maps using object arrays.
            // To distinguish between the two types, we prepend map representations using a string constant.
            public static bool IsCborMapRepresentation(object[] values)
            {
                return values.Length % 2 == 1 && values[0] is string s && s == MapPrefixIdentifier;
            }

            public static void WriteValue(CborWriter writer, object value)
            {
                switch (value)
                {
                    case int i: writer.WriteInt64(i); break;
                    case long i: writer.WriteInt64(i); break;
                    case ulong i: writer.WriteUInt64(i); break;
                    case string s: writer.WriteTextString(s); break;
                    case byte[] b: writer.WriteByteString(b); break;
                    case object[] nested when IsCborMapRepresentation(nested): WriteMap(writer, nested); break;
                    case object[] nested: WriteArray(writer, nested); break;
                    default: throw new ArgumentException($"Unrecognized argument type {value.GetType()}");
                };
            }

            public static void WriteArray(CborWriter writer, params object[] values)
            {
                writer.WriteStartArray(values.Length);
                foreach (object value in values)
                {
                    WriteValue(writer, value);
                }
                writer.WriteEndArray();
            }

            public static void WriteMap(CborWriter writer, params object[] keyValuePairs)
            {
                if (!IsCborMapRepresentation(keyValuePairs))
                {
                    throw new ArgumentException($"CBOR map representation must contain odd number of elements prepended with a '{MapPrefixIdentifier}' constant.");
                }

                writer.WriteStartMap(keyValuePairs.Length / 2);

                foreach (object value in keyValuePairs.Skip(1))
                {
                    WriteValue(writer, value);
                }

                writer.WriteEndMap();
            }
        }
    }
}
