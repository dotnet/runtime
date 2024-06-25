// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Schema
{
    /// <summary>
    /// Functionality for exporting JSON schema from serialization contracts defined in <see cref="JsonTypeInfo"/>.
    /// </summary>
    public static class JsonSchemaExporter
    {
        /// <summary>
        /// Gets the JSON schema for <paramref name="type"/> as a <see cref="JsonNode"/> document.
        /// </summary>
        /// <param name="options">The options declaring the contract for the type.</param>
        /// <param name="type">The type for which to resolve a schema.</param>
        /// <param name="exporterOptions">The options object governing the export operation.</param>
        /// <returns>A JSON object containing the schema for <paramref name="type"/>.</returns>
        public static JsonNode GetJsonSchemaAsNode(this JsonSerializerOptions options, Type type, JsonSchemaExporterOptions? exporterOptions = null)
        {
            if (options is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(options));
            }

            if (type is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(type));
            }

            ValidateOptions(options);
            JsonTypeInfo typeInfo = options.GetTypeInfoInternal(type);
            return typeInfo.GetJsonSchemaAsNode(exporterOptions);
        }

        /// <summary>
        /// Gets the JSON schema for <paramref name="typeInfo"/> as a <see cref="JsonNode"/> document.
        /// </summary>
        /// <param name="typeInfo">The contract from which to resolve the JSON schema.</param>
        /// <param name="exporterOptions">The options object governing the export operation.</param>
        /// <returns>A JSON object containing the schema for <paramref name="typeInfo"/>.</returns>
        public static JsonNode GetJsonSchemaAsNode(this JsonTypeInfo typeInfo, JsonSchemaExporterOptions? exporterOptions = null)
        {
            if (typeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(typeInfo));
            }

            ValidateOptions(typeInfo.Options);
            exporterOptions ??= JsonSchemaExporterOptions.Default;

            typeInfo.EnsureConfigured();
            GenerationState state = new(typeInfo.Options, exporterOptions);
            JsonSchema schema = MapJsonSchemaCore(ref state, typeInfo);
            return schema.ToJsonNode(exporterOptions);
        }

        private static JsonSchema MapJsonSchemaCore(
            ref GenerationState state,
            JsonTypeInfo typeInfo,
            JsonPropertyInfo? propertyInfo = null,
            JsonConverter? customConverter = null,
            JsonNumberHandling? customNumberHandling = null,
            Type? parentPolymorphicType = null,
            bool parentPolymorphicTypeContainsTypesWithoutDiscriminator = false,
            bool parentPolymorphicTypeIsNonNullable = false,
            KeyValuePair<string, JsonSchema>? typeDiscriminator = null,
            bool cacheResult = true)
        {
            Debug.Assert(typeInfo.IsConfigured);

            if (cacheResult && state.TryPushType(typeInfo, propertyInfo, out string? existingJsonPointer))
            {
                // We're generating the schema of a recursive type, return a reference pointing to the outermost schema.
                return CompleteSchema(ref state, new JsonSchema { Ref = existingJsonPointer });
            }

            JsonConverter effectiveConverter = customConverter ?? typeInfo.Converter;
            JsonNumberHandling effectiveNumberHandling = customNumberHandling ?? typeInfo.NumberHandling ?? typeInfo.Options.NumberHandling;
            if (effectiveConverter.GetSchema(effectiveNumberHandling) is { } schema)
            {
                // A schema has been provided by the converter.
                return CompleteSchema(ref state, schema);
            }

            if (parentPolymorphicType is null && typeInfo.PolymorphismOptions is { DerivedTypes.Count: > 0 } polyOptions)
            {
                // This is the base type of a polymorphic type hierarchy. The schema for this type
                // will include an "anyOf" property with the schemas for all derived types.
                string typeDiscriminatorKey = polyOptions.TypeDiscriminatorPropertyName;
                List<JsonDerivedType> derivedTypes = new(polyOptions.DerivedTypes);

                if (!typeInfo.Type.IsAbstract && !IsPolymorphicTypeThatSpecifiesItselfAsDerivedType(typeInfo))
                {
                    // For non-abstract base types that haven't been explicitly configured,
                    // add a trivial schema to the derived types since we should support it.
                    derivedTypes.Add(new JsonDerivedType(typeInfo.Type));
                }

                bool containsTypesWithoutDiscriminator = derivedTypes.Exists(static derivedTypes => derivedTypes.TypeDiscriminator is null);
                JsonSchemaType schemaType = JsonSchemaType.Any;
                List<JsonSchema>? anyOf = new(derivedTypes.Count);

                state.PushSchemaNode(JsonSchema.AnyOfPropertyName);

                foreach (JsonDerivedType derivedType in derivedTypes)
                {
                    Debug.Assert(derivedType.TypeDiscriminator is null or int or string);

                    KeyValuePair<string, JsonSchema>? derivedTypeDiscriminator = null;
                    if (derivedType.TypeDiscriminator is { } discriminatorValue)
                    {
                        JsonNode discriminatorNode = discriminatorValue switch
                        {
                            string stringId => (JsonNode)stringId,
                            _ => (JsonNode)(int)discriminatorValue,
                        };

                        JsonSchema discriminatorSchema = new() { Constant = discriminatorNode };
                        derivedTypeDiscriminator = new(typeDiscriminatorKey, discriminatorSchema);
                    }

                    JsonTypeInfo derivedTypeInfo = typeInfo.Options.GetTypeInfoInternal(derivedType.DerivedType);

                    state.PushSchemaNode(anyOf.Count.ToString(CultureInfo.InvariantCulture));
                    JsonSchema derivedSchema = MapJsonSchemaCore(
                        ref state,
                        derivedTypeInfo,
                        parentPolymorphicType: typeInfo.Type,
                        typeDiscriminator: derivedTypeDiscriminator,
                        parentPolymorphicTypeContainsTypesWithoutDiscriminator: containsTypesWithoutDiscriminator,
                        parentPolymorphicTypeIsNonNullable: propertyInfo is { IsGetNullable: false, IsSetNullable: false },
                        cacheResult: false);

                    state.PopSchemaNode();

                    // Determine if all derived schemas have the same type.
                    if (anyOf.Count == 0)
                    {
                        schemaType = derivedSchema.Type;
                    }
                    else if (schemaType != derivedSchema.Type)
                    {
                        schemaType = JsonSchemaType.Any;
                    }

                    anyOf.Add(derivedSchema);
                }

                state.PopSchemaNode();

                if (schemaType is not JsonSchemaType.Any)
                {
                    // If all derived types have the same schema type, we can simplify the schema
                    // by moving the type keyword to the base schema and removing it from the derived schemas.
                    foreach (JsonSchema derivedSchema in anyOf)
                    {
                        derivedSchema.Type = JsonSchemaType.Any;

                        if (derivedSchema.KeywordCount == 0)
                        {
                            // if removing the type results in an empty schema,
                            // remove the anyOf array entirely since it's always true.
                            anyOf = null;
                            break;
                        }
                    }
                }

                return CompleteSchema(ref state, new()
                {
                    Type = schemaType,
                    AnyOf = anyOf,
                    // If all derived types have a discriminator, we can require it in the base schema.
                    Required = containsTypesWithoutDiscriminator ? null : [typeDiscriminatorKey]
                });
            }

            if (effectiveConverter.NullableElementConverter is { } elementConverter)
            {
                JsonTypeInfo elementTypeInfo = typeInfo.Options.GetTypeInfo(elementConverter.Type!);
                schema = MapJsonSchemaCore(ref state, elementTypeInfo, customConverter: elementConverter, cacheResult: false);

                if (schema.Enum != null)
                {
                    Debug.Assert(elementTypeInfo.Type.IsEnum, "The enum keyword should only be populated by schemas for enum types.");
                    schema.Enum.Add(null); // Append null to the enum array.
                }

                return CompleteSchema(ref state, schema);
            }

            switch (typeInfo.Kind)
            {
                case JsonTypeInfoKind.Object:
                    List<KeyValuePair<string, JsonSchema>>? properties = null;
                    List<string>? required = null;
                    JsonSchema? additionalProperties = null;

                    if (typeInfo.UnmappedMemberHandling is JsonUnmappedMemberHandling.Disallow)
                    {
                        additionalProperties = JsonSchema.False;
                    }

                    if (typeDiscriminator is { } typeDiscriminatorPair)
                    {
                        (properties ??= []).Add(typeDiscriminatorPair);
                        if (parentPolymorphicTypeContainsTypesWithoutDiscriminator)
                        {
                            // Require the discriminator here since it's not common to all derived types.
                            (required ??= []).Add(typeDiscriminatorPair.Key);
                        }
                    }

                    state.PushSchemaNode(JsonSchema.PropertiesPropertyName);
                    foreach (JsonPropertyInfo property in typeInfo.Properties)
                    {
                        if (property is { Get: null, Set: null } or { IsExtensionData: true })
                        {
                            continue; // Skip JsonIgnored properties and extension data
                        }

                        state.PushSchemaNode(property.Name);
                        JsonSchema propertySchema = MapJsonSchemaCore(
                            ref state,
                            property.JsonTypeInfo,
                            propertyInfo: property,
                            customConverter: property.EffectiveConverter,
                            customNumberHandling: property.EffectiveNumberHandling);

                        state.PopSchemaNode();

                        if (property.AssociatedParameter is { HasDefaultValue: true } parameterInfo)
                        {
                            propertySchema.DefaultValue = JsonSerializer.SerializeToNode(parameterInfo.DefaultValue, property.JsonTypeInfo);
                            propertySchema.HasDefaultValue = true;
                        }

                        (properties ??= []).Add(new(property.Name, propertySchema));

                        // Mark as required if either the property is required or the associated constructor parameter is non-optional.
                        // While the latter implies the former in cases where the JsonSerializerOptions.RespectRequiredConstructorParameters
                        // setting has been enabled, for the case of the schema exporter we always mark non-optional constructor parameters as required.
                        if (property is { IsRequired: true } or { AssociatedParameter.IsRequiredParameter: true })
                        {
                            (required ??= []).Add(property.Name);
                        }
                    }

                    state.PopSchemaNode();
                    return CompleteSchema(ref state, new()
                    {
                        Type = JsonSchemaType.Object,
                        Properties = properties,
                        Required = required,
                        AdditionalProperties = additionalProperties,
                    });

                case JsonTypeInfoKind.Enumerable:
                    Debug.Assert(typeInfo.ElementTypeInfo != null);

                    if (typeDiscriminator is null)
                    {
                        state.PushSchemaNode(JsonSchema.ItemsPropertyName);
                        JsonSchema items = MapJsonSchemaCore(ref state, typeInfo.ElementTypeInfo, customNumberHandling: effectiveNumberHandling);
                        state.PopSchemaNode();

                        return CompleteSchema(ref state, new()
                        {
                            Type = JsonSchemaType.Array,
                            Items = items.IsTrue ? null : items,
                        });
                    }
                    else
                    {
                        // Polymorphic enumerable types are represented using a wrapping object:
                        // { "$type" : "discriminator", "$values" : [element1, element2, ...] }
                        // Which corresponds to the schema
                        // { "properties" : { "$type" : { "const" : "discriminator" }, "$values" : { "type" : "array", "items" : { ... } } } }
                        const string ValuesKeyword = JsonSerializer.ValuesPropertyName;

                        state.PushSchemaNode(JsonSchema.PropertiesPropertyName);
                        state.PushSchemaNode(ValuesKeyword);
                        state.PushSchemaNode(JsonSchema.ItemsPropertyName);

                        JsonSchema items = MapJsonSchemaCore(ref state, typeInfo.ElementTypeInfo, customNumberHandling: effectiveNumberHandling);

                        state.PopSchemaNode();
                        state.PopSchemaNode();
                        state.PopSchemaNode();

                        return CompleteSchema(ref state, new()
                        {
                            Type = JsonSchemaType.Object,
                            Properties =
                            [
                                typeDiscriminator.Value,
                                new(ValuesKeyword,
                                    new JsonSchema()
                                    {
                                        Type = JsonSchemaType.Array,
                                        Items = items.IsTrue ? null : items,
                                    }),
                            ],
                            Required = parentPolymorphicTypeContainsTypesWithoutDiscriminator ? [typeDiscriminator.Value.Key] : null,
                        });
                    }

                case JsonTypeInfoKind.Dictionary:
                    Debug.Assert(typeInfo.ElementTypeInfo != null);

                    List<KeyValuePair<string, JsonSchema>>? dictProps = null;
                    List<string>? dictRequired = null;

                    if (typeDiscriminator is { } dictDiscriminator)
                    {
                        dictProps = [dictDiscriminator];
                        if (parentPolymorphicTypeContainsTypesWithoutDiscriminator)
                        {
                            // Require the discriminator here since it's not common to all derived types.
                            dictRequired = [dictDiscriminator.Key];
                        }
                    }

                    state.PushSchemaNode(JsonSchema.AdditionalPropertiesPropertyName);
                    JsonSchema valueSchema = MapJsonSchemaCore(ref state, typeInfo.ElementTypeInfo, customNumberHandling: effectiveNumberHandling);
                    state.PopSchemaNode();

                    return CompleteSchema(ref state, new()
                    {
                        Type = JsonSchemaType.Object,
                        Properties = dictProps,
                        Required = dictRequired,
                        AdditionalProperties = valueSchema.IsTrue ? null : valueSchema,
                    });

                default:
                    Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.None);
                    // Return a `true` schema for types with user-defined converters.
                    return CompleteSchema(ref state, JsonSchema.True);
            }

            JsonSchema CompleteSchema(ref GenerationState state, JsonSchema schema)
            {
                if (schema.Ref is null)
                {
                    // A schema is marked as nullable if either
                    // 1. We have a schema for a property where either the getter or setter are marked as nullable.
                    // 2. We have a schema for a reference type, unless we're explicitly treating null-oblivious types as non-nullable.
                    bool isNullableSchema = propertyInfo != null
                        ? propertyInfo.IsGetNullable || propertyInfo.IsSetNullable
                        : typeInfo.CanBeNull && !parentPolymorphicTypeIsNonNullable && !state.ExporterOptions.TreatNullObliviousAsNonNullable;

                    if (isNullableSchema)
                    {
                        schema.MakeNullable();
                    }

                    if (cacheResult)
                    {
                        state.PopGeneratedType();
                    }
                }

                if (state.ExporterOptions.TransformSchemaNode != null)
                {
                    // Prime the schema for invocation by the JsonNode transformer.
                    schema.ExporterContext = state.CreateContext(typeInfo, propertyInfo);
                }

                return schema;
            }
        }

        private static void ValidateOptions(JsonSerializerOptions options)
        {
            if (options.ReferenceHandler == ReferenceHandler.Preserve)
            {
                ThrowHelper.ThrowNotSupportedException_JsonSchemaExporterDoesNotSupportReferenceHandlerPreserve();
            }

            options.MakeReadOnly();
        }

        private static bool IsPolymorphicTypeThatSpecifiesItselfAsDerivedType(JsonTypeInfo typeInfo)
        {
            Debug.Assert(typeInfo.PolymorphismOptions is not null);

            foreach (JsonDerivedType derivedType in typeInfo.PolymorphismOptions.DerivedTypes)
            {
                if (derivedType.DerivedType == typeInfo.Type)
                {
                    return true;
                }
            }

            return false;
        }

        private readonly ref struct GenerationState(JsonSerializerOptions options, JsonSchemaExporterOptions exporterOptions)
        {
            private readonly List<string> _currentPath = [];
            private readonly List<(JsonTypeInfo typeInfo, JsonPropertyInfo? propertyInfo, int depth)> _generationStack = [];

            public int CurrentDepth => _currentPath.Count;
            public JsonSerializerOptions Options { get; } = options;
            public JsonSchemaExporterOptions ExporterOptions { get; } = exporterOptions;

            public void PushSchemaNode(string nodeId)
            {
                if (CurrentDepth == Options.EffectiveMaxDepth)
                {
                    ThrowHelper.ThrowInvalidOperationException_JsonSchemaExporterDepthTooLarge();
                }

                _currentPath.Add(nodeId);
            }

            public void PopSchemaNode()
            {
                Debug.Assert(CurrentDepth > 0);
                _currentPath.RemoveAt(_currentPath.Count - 1);
            }

            /// <summary>
            /// Pushes the current type/property to the generation stack or returns a JSON pointer if the type is recursive.
            /// </summary>
            public bool TryPushType(JsonTypeInfo typeInfo, JsonPropertyInfo? propertyInfo, [NotNullWhen(true)] out string? existingJsonPointer)
            {
                foreach ((JsonTypeInfo otherTypeInfo, JsonPropertyInfo? otherPropertyInfo, int depth) in _generationStack)
                {
                    if (typeInfo == otherTypeInfo && propertyInfo == otherPropertyInfo)
                    {
                        existingJsonPointer = FormatJsonPointer(_currentPath, depth);
                        return true;
                    }
                }

                _generationStack.Add((typeInfo, propertyInfo, CurrentDepth));
                existingJsonPointer = null;
                return false;
            }

            public void PopGeneratedType()
            {
                Debug.Assert(_generationStack.Count > 0);
                _generationStack.RemoveAt(_generationStack.Count - 1);
            }

            public JsonSchemaExporterContext CreateContext(JsonTypeInfo typeInfo, JsonPropertyInfo? propertyInfo)
            {
                return new JsonSchemaExporterContext(typeInfo, propertyInfo, _currentPath.ToArray());
            }

            private static string FormatJsonPointer(List<string> currentPathList, int depth)
            {
                Debug.Assert(0 <= depth && depth < currentPathList.Count);

                if (depth == 0)
                {
                    return "#";
                }

                using ValueStringBuilder sb = new(initialCapacity: depth * 10);
                sb.Append('#');

                for (int i = 0; i < depth; i++)
                {
                    ReadOnlySpan<char> segment = currentPathList[i].AsSpan();
                    sb.Append('/');

                    do
                    {
                        // Per RFC 6901 the characters '~' and '/' must be escaped.
                        int pos = segment.IndexOfAny('~', '/');
                        if (pos < 0)
                        {
                            sb.Append(segment);
                            break;
                        }

                        sb.Append(segment.Slice(0, pos));

                        if (segment[pos] == '~')
                        {
                            sb.Append("~0");
                        }
                        else
                        {
                            Debug.Assert(segment[pos] == '/');
                            sb.Append("~1");
                        }

                        segment = segment.Slice(pos + 1);
                    }
                    while (!segment.IsEmpty);
                }

                return sb.ToString();
            }
        }
    }
}
