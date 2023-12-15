// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Text.Json.Nodes
{
    public abstract partial class JsonNode
    {
        /// <summary>
        ///   Converts the current instance to string in JSON format.
        /// </summary>
        /// <param name="options">Options to control the serialization behavior.</param>
        /// <returns>JSON representation of current instance.</returns>
        public string ToJsonString(JsonSerializerOptions? options = null)
        {
            JsonWriterOptions writerOptions = default;
            int defaultBufferSize = JsonSerializerOptions.BufferSizeDefault;
            if (options is not null)
            {
                writerOptions = options.GetWriterOptions();
                defaultBufferSize = options.DefaultBufferSize;
            }

            Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(writerOptions, defaultBufferSize, out PooledByteBufferWriter output);
            try
            {
                WriteTo(writer, options);
                writer.Flush();
                return JsonHelpers.Utf8GetString(output.WrittenMemory.Span);
            }
            finally
            {
                Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, output);
            }
        }

        /// <summary>
        ///   Gets a string representation for the current value appropriate to the node type.
        /// </summary>
        /// <returns>A string representation for the current value appropriate to the node type.</returns>
        public override string ToString()
        {
            // Special case for string; don't quote it.
            if (this is JsonValue)
            {
                if (this is JsonValue<string> jsonString)
                {
                    return jsonString.Value;
                }

                if (this is JsonValue<JsonElement> jsonElement &&
                    jsonElement.Value.ValueKind == JsonValueKind.String)
                {
                    return jsonElement.Value.GetString()!;
                }
            }

            Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(new JsonWriterOptions { Indented = true }, JsonSerializerOptions.BufferSizeDefault, out PooledByteBufferWriter output);
            try
            {
                WriteTo(writer);
                writer.Flush();
                return JsonHelpers.Utf8GetString(output.WrittenMemory.Span);
            }
            finally
            {
                Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, output);
            }
        }

        /// <summary>
        ///   Write the <see cref="JsonNode"/> into the provided <see cref="Utf8JsonWriter"/> as JSON.
        /// </summary>
        /// <param name="writer">The <see cref="Utf8JsonWriter"/>.</param>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="writer"/> parameter is <see langword="null"/>.
        /// </exception>
        /// <param name="options">Options to control the serialization behavior.</param>
        public abstract void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null);
    }
}
