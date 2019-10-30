// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonConverterChar : JsonConverter<char>
    {
        public override char Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString()[0];
        }

        public override void Write(Utf8JsonWriter writer, char value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(
#if BUILDING_INBOX_LIBRARY
                MemoryMarshal.CreateSpan(ref value, 1)
#else
                value.ToString()
#endif
                );
        }
    }
}
