// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Text.Json.Nodes
{
    public abstract partial class JsonNode
    {
        /// <summary>Lazily-initialized options that set the <see cref="JsonSerializerOptions.MaxDepth"/> to match the <see cref="JsonWriterOptions"/> default.</summary>
        private static JsonSerializerOptions? s_unindentedToStringOptions;

        /// <summary>
        /// Lazily-initialized options that set the <see cref="JsonSerializerOptions.MaxDepth"/> to match the <see cref="JsonWriterOptions"/> default
        /// and that sets <see cref="JsonSerializerOptions.WriteIndented"/> to true.
        /// </summary>
        private static JsonSerializerOptions? s_indentedToStringOptions;

        /// <summary>
        ///   Converts the current instance to string in JSON format.
        /// </summary>
        /// <param name="options">Options to control the serialization behavior.</param>
        /// <returns>JSON representation of current instance.</returns>
        public string ToJsonString(JsonSerializerOptions? options = null)
        {
            options ??=
                s_unindentedToStringOptions ??
                Interlocked.CompareExchange(ref s_unindentedToStringOptions, new JsonSerializerOptions { MaxDepth = JsonWriterOptions.DefaultMaxDepth }, null) ??
                s_unindentedToStringOptions;
            return JsonSerializer.Serialize(this, options);
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

#pragma warning disable CA1869 // TODO https://github.com/dotnet/roslyn-analyzers/issues/6957
            JsonSerializerOptions options =
                s_indentedToStringOptions ??
                Interlocked.CompareExchange(ref s_indentedToStringOptions, new JsonSerializerOptions { MaxDepth = JsonWriterOptions.DefaultMaxDepth, WriteIndented = true, }, null) ??
                s_indentedToStringOptions;
#pragma warning restore CA1869
            return JsonSerializer.Serialize(this, options);
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

        /// <summary>
        /// Creates a pooled buffer writer instance and serializes all contents to it.
        /// </summary>
        internal PooledByteBufferWriter WriteToPooledBuffer(
            JsonSerializerOptions? options = null,
            JsonWriterOptions writerOptions = default,
            int bufferSize = JsonSerializerOptions.BufferSizeDefault)
        {
            var bufferWriter = new PooledByteBufferWriter(bufferSize);
            using var writer = new Utf8JsonWriter(bufferWriter, writerOptions);
            WriteTo(writer, options);
            return bufferWriter;
        }
    }
}
