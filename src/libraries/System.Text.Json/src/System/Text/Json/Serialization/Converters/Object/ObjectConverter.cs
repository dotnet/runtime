// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ObjectConverter : JsonConverter<object?>
    {
        internal override ConverterStrategy ConverterStrategy => ConverterStrategy.Object;

        public ObjectConverter()
        {
            CanBePolymorphic = true;
            // JsonElement/JsonNode parsing does not support async; force read ahead for now.
            RequiresReadAhead = true;
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (options.UnknownTypeHandling == JsonUnknownTypeHandling.JsonElement)
            {
                return JsonElement.ParseValue(ref reader);
            }

            Debug.Assert(options.UnknownTypeHandling == JsonUnknownTypeHandling.JsonNode);
            return JsonNodeConverter.Instance.Read(ref reader, typeToConvert, options);
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            Debug.Assert(value?.GetType() == typeof(object));
            writer.WriteStartObject();
            writer.WriteEndObject();
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out object? value)
        {
            object? referenceValue;

            if (options.UnknownTypeHandling == JsonUnknownTypeHandling.JsonElement)
            {
                JsonElement element = JsonElement.ParseValue(ref reader);

                // Edge case where we want to lookup for a reference when parsing into typeof(object)
                if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve &&
                    JsonSerializer.TryHandleReferenceFromJsonElement(ref reader, ref state, element, out referenceValue))
                {
                    value = referenceValue;
                }
                else
                {
                    value = element;
                }

                return true;
            }

            Debug.Assert(options.UnknownTypeHandling == JsonUnknownTypeHandling.JsonNode);

            JsonNode node = JsonNodeConverter.Instance.Read(ref reader, typeToConvert, options)!;

            if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve &&
                JsonSerializer.TryHandleReferenceFromJsonNode(ref reader, ref state, node, out referenceValue))
            {
                value = referenceValue;
            }
            else
            {
                value = node;
            }

            return true;
        }

        internal override object ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(TypeToConvert, this);
            return null!;
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            // This converter does not handle nulls.
            Debug.Assert(value != null);

            Type runtimeType = value.GetType();
            JsonConverter runtimeConverter = options.GetConverterInternal(runtimeType);
            if (runtimeConverter == this)
            {
                ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(runtimeType, this);
            }

            runtimeConverter.WriteAsPropertyNameCoreAsObject(writer, value, options, isWritingExtensionDataProperty);
        }
    }
}
