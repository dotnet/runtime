// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ObjectConverter : JsonConverter<object?>
    {
        private protected override ConverterStrategy GetDefaultConverterStrategy() => ConverterStrategy.Object;

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

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out object? value)
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

        public override object ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(TypeToConvert, this);
            return null!;
        }

        internal override object ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(TypeToConvert, this);
            return null!;
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(value));
            }

            WriteAsPropertyNameCore(writer, value, options, isWritingExtensionDataProperty: false);
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

    /// <summary>
    /// A placeholder ObjectConverter used for driving object root value
    /// serialization only and does not root JsonNode/JsonDocument.
    /// </summary>
    internal sealed class ObjectConverterSlim : JsonConverter<object?>
    {
        private protected override ConverterStrategy GetDefaultConverterStrategy() => ConverterStrategy.Object;

        public ObjectConverterSlim()
        {
            CanBePolymorphic = true;
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Fail("Converter should only be used to drive root-level object serialization.");
            return null;
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            Debug.Fail("Converter should only be used to drive root-level object serialization.");
        }
    }
}
