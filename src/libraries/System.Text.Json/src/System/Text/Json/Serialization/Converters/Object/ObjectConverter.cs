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
            => ReadAsPropertyNameCore(ref reader, typeToConvert, options);

        internal sealed override object ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == Type)
            {
                ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(typeToConvert, this);
            }

            return GetConverterForKeyType(typeToConvert, options).ReadAsPropertyNameCoreAsObject(ref reader, typeToConvert, options)!;
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
            ArgumentNullException.ThrowIfNull(value);

            Type runtimeType = value.GetType();
            if (runtimeType == Type)
            {
                ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(runtimeType, this);
            }

            GetConverterForKeyType(runtimeType, options).WriteAsPropertyNameCoreAsObject(writer, value, options, isWritingExtensionDataProperty);
        }

        // Resolves the converter that should serialize a property name of the given key type.
        private JsonConverter GetConverterForKeyType(Type keyType, JsonSerializerOptions options)
        {
            JsonConverter converter = options.GetConverterInternal(keyType);

            // A converter declared for object delegates straight back into this fallback, which would
            // recurse until the stack overflows (dotnet/runtime#122835). Such a converter doesn't
            // override the property name methods -- if it did, it would have handled the key without
            // ever getting here -- so use the default converter for the key type instead of
            // dispatching through it again.
            JsonConverter sourceConverter = converter.SourceConverterForCastingConverter ?? converter;
            if (sourceConverter.Type == typeof(object))
            {
                if (!DefaultJsonTypeInfoResolver.TryGetDefaultSimpleConverter(keyType, out JsonConverter? defaultConverter))
                {
                    ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(keyType, this);
                }

                converter = defaultConverter;
            }

            return converter;
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
                return JsonElement.ParseValue(ref reader, options.AllowDuplicateProperties);
            }

            Debug.Assert(options.UnknownTypeHandling == JsonUnknownTypeHandling.JsonNode);
            return JsonNodeConverter.Instance.Read(ref reader, typeToConvert, options);
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out object? value)
        {
            object? referenceValue;

            if (options.UnknownTypeHandling == JsonUnknownTypeHandling.JsonElement)
            {
                JsonElement element = JsonElement.ParseValue(ref reader, options.AllowDuplicateProperties);

                // Edge case where we want to lookup for a reference when parsing into typeof(object)
                if (options.ReferenceHandlingStrategy == JsonKnownReferenceHandler.Preserve &&
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

            if (options.ReferenceHandlingStrategy == JsonKnownReferenceHandler.Preserve &&
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

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => JsonSchema.CreateTrueSchema();
    }
}
