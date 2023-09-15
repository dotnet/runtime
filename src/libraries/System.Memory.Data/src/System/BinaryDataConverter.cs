// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Serializes <see cref="BinaryData"/> instances as Base64 JSON strings.
    /// </summary>
    public sealed class BinaryDataJsonConverter : JsonConverter<BinaryData>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryDataJsonConverter"/>.
        /// </summary>
        public BinaryDataJsonConverter()
        { }

        /// <inheritdoc/>
        public override BinaryData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return BinaryData.FromBytes(reader.GetBytesFromBase64());
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, BinaryData value, JsonSerializerOptions options)
        {
            writer.WriteBase64StringValue(value.ToMemory().Span);
        }
    }
}
