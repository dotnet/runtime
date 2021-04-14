// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.Text.Json.Node
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
            var output = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(output, options == null ? default(JsonWriterOptions) : options.GetWriterOptions()))
            {
                WriteTo(writer, options);
            }
            return JsonHelpers.Utf8GetString(output.WrittenSpan);
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

            var options = new JsonWriterOptions { Indented = true };
            var output = new ArrayBufferWriter<byte>();

            using (var writer = new Utf8JsonWriter(output, options))
            {
                WriteTo(writer);
            }

            return JsonHelpers.Utf8GetString(output.WrittenSpan);
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
