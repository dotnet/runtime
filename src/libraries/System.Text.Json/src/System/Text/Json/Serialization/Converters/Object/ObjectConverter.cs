// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal abstract class ObjectConverter : JsonConverter<object?>
    {
        private protected override ConverterStrategy GetDefaultConverterStrategy() => ConverterStrategy.Object;

        public ObjectConverter()
        {
            CanBePolymorphic = true;
        }

        public sealed override object ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(Type, this);
            return null!;
        }

        internal sealed override object ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(Type, this);
            return null!;
        }

        public sealed override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WriteEndObject();
        }

        public sealed override void WriteAsPropertyName(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            WriteAsPropertyNameCore(writer, value, options, isWritingExtensionDataProperty: false);
        }

        internal sealed override void WriteAsPropertyNameCore(Utf8JsonWriter writer, object value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(value));
            }

            Type runtimeType = value.GetType();
            if (runtimeType == Type)
            {
                ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(runtimeType, this);
            }

            JsonConverter runtimeConverter = options.GetConverterInternal(runtimeType);
            runtimeConverter.WriteAsPropertyNameCoreAsObject(writer, value, options, isWritingExtensionDataProperty);
        }
    }

    /// <summary>
    /// Defines an object converter that only supports (polymorphic) serialization but not deserialization.
    /// This is done to avoid rooting dependencies to JsonNode/JsonElement necessary to drive object deserialization.
    /// Source generator users need to explicitly declare support for object so that the derived converter gets used.
    /// </summary>
    internal sealed class SlimObjectConverter : ObjectConverter
    {
        // Keep track of the originating resolver so that the converter surfaces
        // an accurate error message whenever deserialization is attempted.
        private readonly IJsonTypeInfoResolver _originatingResolver;

        public SlimObjectConverter(IJsonTypeInfoResolver originatingResolver)
            => _originatingResolver = originatingResolver;

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ThrowHelper.ThrowNotSupportedException_NoMetadataForType(typeToConvert, _originatingResolver);
            return null;
        }
    }

    /// <summary>
    /// Defines an object converter that supports deserialization via JsonElement/JsonNode representations.
    /// Used as the default in reflection or if object is declared in the JsonSerializerContext type graph.
    /// </summary>
    internal sealed class DefaultObjectConverter : ObjectConverter
    {
        public DefaultObjectConverter()
        {
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

            JsonNode? node = JsonNodeConverter.Instance.Read(ref reader, typeToConvert, options);

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

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => JsonSchema.True;
    }
}
