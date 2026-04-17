// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class StringConverter : JsonPrimitiveConverter<string?>
    {
        // Use 1 MB segments as a performance tradeoff when writing strings larger than the threshold computed by
        // ComputeMaxSafeStringLength(writer): large enough to keep the number of WriteStringValueSegment calls low,
        // but small enough to avoid pushing extremely large spans through a single segmented write. This is not a
        // correctness or protocol limit; it can be tuned if profiling shows a better size for writer throughput/
        // allocation behavior.
        private const int ChunkSize = 1024 * 1024;

        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            // For performance, lift up the writer implementation.
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {

                ReadOnlySpan<char> remaining = value.AsSpan();
                if (remaining.Length < ComputeMaxSafeStringLength(writer))
                {
                    writer.WriteStringValue(remaining);
                }
                else
                {
                    WriteStringValueSegment(writer, remaining);
                }
            }
        }

        private static void WriteStringValueSegment(Utf8JsonWriter writer, ReadOnlySpan<char> value)
        {
            int chunkSize = ChunkSize;
            while (value.Length > chunkSize)
            {
                ReadOnlySpan<char> chunk = value.Slice(0, chunkSize);
                writer.WriteStringValueSegment(chunk, isFinalSegment: false);
                value = value.Slice(chunk.Length);
            }

            writer.WriteStringValueSegment(value, isFinalSegment: true);
        }

        private static int ComputeMaxSafeStringLength(Utf8JsonWriter writer)
        {
            int indentOverhead = writer.Options.Indented ? writer.CurrentDepth * writer.Options.IndentSize + writer.Options.NewLine.Length : 0;
            return (int.MaxValue / (JsonConstants.MaxExpansionFactorWhileEscaping * JsonConstants.MaxExpansionFactorWhileTranscoding)) - (3 + indentOverhead);
        }

        internal override string ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
            return reader.GetString()!;
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, string value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (options.DictionaryKeyPolicy != null && !isWritingExtensionDataProperty)
            {
                value = options.DictionaryKeyPolicy.ConvertName(value);

                if (value == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_NamingPolicyReturnNull(options.DictionaryKeyPolicy);
                }
            }

            writer.WritePropertyName(value);
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => new() { Type = JsonSchemaType.String };
    }
}
