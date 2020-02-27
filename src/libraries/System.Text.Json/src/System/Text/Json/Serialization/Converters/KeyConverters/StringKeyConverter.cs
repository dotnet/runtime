// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class StringKeyConverter : KeyConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override string ReadKeyFromBytes(ReadOnlySpan<byte> bytes)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, string key, JsonSerializerOptions options)
            => writer.WritePropertyName(key);
    }
}
