// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
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
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(type);

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
            ArgumentNullException.ThrowIfNull(typeInfo);

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
            JsonTypeInfo? parentPolymorphicTypeInfo = null,
            bool parentPolymorphicTypeContainsTypesWithoutDiscriminator = false,
            bool parentPolymorphicTypeIsNonNullable = false,
            KeyValuePair<string, JsonSchema>? typeDiscriminator = null,
            bool cacheResult = true)
        {
            Debug.Assert(typeInfo.IsConfigured);

            JsonSchemaExporterContext exporterContext = state.CreateContext(typeInfo, propertyInfo, parentPolymorphicTypeInfo);

            if (cacheResult && typeInfo.Kind is not JsonTypeInfoKind.None &&
                state.TryGetExistingJsonPointer(exporterContext, out string? existingJsonPointer))
            {
                // The schema context has already been generated in the schema document, return a reference to it.
                return CompleteSchema(ref state, new JsonSchema { Ref = existingJsonPointer });
            }

            JsonConverter effectiveConverter = customConverter ?? typeInfo.Converter;
            JsonNumberHandling effectiveNumberHandling = customNumberHandling ?? typeInfo.NumberHandling ?? typeInfo.Options.NumberHandling;
            if (effectiveConverter.GetSchema(effectiveNumberHandling) is { } schema)
            {
                // A schema has been provided by the converter.
                return CompleteSchema(ref state, schema);
            }

            if (parentPolymorphicTypeInfo is null && typeInfo.PolymorphismOptions is { DerivedTypes.Count: > 0 } polyOptions)
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
                        parentPolymorphicTypeInfo: typeInfo,
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

                if (elementConverter.IsIeeeFloatingPointConverter &&
                    (effectiveNumberHandling & JsonNumberHandling.AllowNamedFloatingPointLiterals) != 0)
                {
                    // IEEE floating-point types with AllowNamedFloatingPointLiterals generate an anyOf schema.
                    // Fold null into the numeric branch to preserve nullability for nullable wrappers.
                    Debug.Assert(schema.AnyOf is not null, "IEEE floating-point types with AllowNamedFloatingPointLiterals should generate an anyOf schema.");

                    List<JsonSchema> anyOf = schema.AnyOf;
                    Debug.Assert(anyOf.Exists(b => (b.Type & JsonSchemaType.Number) != 0),
                        "IEEE floating-point anyOf schema should have a numeric branch.");

                    foreach (JsonSchema branch in anyOf)
                    {
                        if ((branch.Type & JsonSchemaType.Number) != 0)
                        {
                            branch.Type |= JsonSchemaType.Null;
                            break;
                        }
                    }
                }
                else if (schema.Enum != null)
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

                    JsonUnmappedMemberHandling effectiveUnmappedMemberHandling = typeInfo.UnmappedMemberHandling ?? typeInfo.Options.UnmappedMemberHandling;
                    if (effectiveUnmappedMemberHandling is JsonUnmappedMemberHandling.Disallow)
                    {
                        additionalProperties = JsonSchema.CreateFalseSchema();
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
                            JsonSchema.EnsureMutable(ref propertySchema);
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

                case JsonTypeInfoKind.Union:
                    if (typeInfo.UnionCases is { Count: > 0 } unionCases)
                    {
                        JsonSchemaType unionSchemaType = JsonSchemaType.Any;
                        List<JsonSchema>? unionAnyOf = new(unionCases.Count);

                        state.PushSchemaNode(JsonSchema.AnyOfPropertyName);

                        foreach (JsonUnionCaseInfo caseInfo in unionCases)
                        {
                            JsonTypeInfo caseTypeInfo = typeInfo.Options.GetTypeInfoInternal(caseInfo.CaseType);

                            state.PushSchemaNode(unionAnyOf.Count.ToString(CultureInfo.InvariantCulture));
                            JsonSchema caseSchema = MapJsonSchemaCore(ref state, caseTypeInfo, cacheResult: false);
                            state.PopSchemaNode();

                            if (caseInfo.IsNullable)
                            {
                                caseSchema.Type |= JsonSchemaType.Null;
                            }

                            if (unionAnyOf.Count == 0)
                            {
                                unionSchemaType = caseSchema.Type;
                            }
                            else if (unionSchemaType != caseSchema.Type)
                            {
                                unionSchemaType = JsonSchemaType.Any;
                            }

                            unionAnyOf.Add(caseSchema);
                        }

                        state.PopSchemaNode();

                        if (unionSchemaType is not JsonSchemaType.Any)
                        {
                            foreach (JsonSchema caseSchema in unionAnyOf)
                            {
                                caseSchema.Type = JsonSchemaType.Any;

                                if (caseSchema.KeywordCount == 0)
                                {
                                    unionAnyOf = null;
                                    break;
                                }
                            }
                        }

                        return CompleteSchema(ref state, new()
                        {
                            Type = unionSchemaType,
                            AnyOf = unionAnyOf,
                        });
                    }

                    return CompleteSchema(ref state, JsonSchema.CreateTrueSchema());

                default:
                    Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.None);
                    // Return a `true` schema for types with user-defined converters.
                    return CompleteSchema(ref state, JsonSchema.CreateTrueSchema());
            }

            JsonSchema CompleteSchema(ref GenerationState state, JsonSchema schema)
            {
                if (schema.Ref is null)
                {
                    if (IsNullableSchema(state.ExporterOptions))
                    {
                        schema.MakeNullable();
                    }

                    bool IsNullableSchema(JsonSchemaExporterOptions options)
                    {
                        // A schema is marked as nullable if either:
                        // 1. We have a schema for a property where either the getter or setter are marked as nullable.
                        // 2. We have a schema for a Nullable<T> type.
                        // 3. We have a schema for a reference type, unless we're explicitly treating null-oblivious types as non-nullable.

                        if (propertyInfo is not null)
                        {
                            return propertyInfo.IsGetNullable || propertyInfo.IsSetNullable;
                        }

                        if (typeInfo.IsNullable)
                        {
                            return true;
                        }

                        return !typeInfo.Type.IsValueType && !parentPolymorphicTypeIsNonNullable && !options.TreatNullObliviousAsNonNullable;
                    }
                }

                if (state.ExporterOptions.TransformSchemaNode != null)
                {
                    // Prime the schema for invocation by the JsonNode transformer.
                    schema.ExporterContext = exporterContext;
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
            private readonly Dictionary<(JsonTypeInfo, JsonPropertyInfo?), string[]> _generated = new();

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
            /// Registers the current schema node generation context; if it has already been generated return a JSON pointer to its location.
            /// </summary>
            public bool TryGetExistingJsonPointer(in JsonSchemaExporterContext context, [NotNullWhen(true)] out string? existingJsonPointer)
            {
                (JsonTypeInfo TypeInfo, JsonPropertyInfo? PropertyInfo) key = (context.TypeInfo, context.PropertyInfo);
#if NET
                ref string[]? pathToSchema = ref CollectionsMarshal.GetValueRefOrAddDefault(_generated, key, out bool exists);
#else
                bool exists = _generated.TryGetValue(key, out string[]? pathToSchema);
#endif
                if (exists)
                {
                    existingJsonPointer = FormatJsonPointer(pathToSchema);
                    return true;
                }
#if NET
                pathToSchema = context._path;
#else
                _generated[key] = context._path;
#endif
                existingJsonPointer = null;
                return false;
            }

            public JsonSchemaExporterContext CreateContext(JsonTypeInfo typeInfo, JsonPropertyInfo? propertyInfo, JsonTypeInfo? baseTypeInfo)
            {
                return new JsonSchemaExporterContext(typeInfo, propertyInfo, baseTypeInfo, [.. _currentPath]);
            }

            private static string FormatJsonPointer(ReadOnlySpan<string> path)
            {
                if (path.IsEmpty)
                {
                    return "#";
                }

                using ValueStringBuilder sb = new(initialCapacity: path.Length * 10);
                sb.Append('#');

                foreach (string segment in path)
                {
                    sb.Append('/');
                    AppendEscapedReferenceToken(ref sb, segment);
                }

                return sb.ToString();
            }

            private static void AppendEscapedReferenceToken(ref ValueStringBuilder sb, string segment)
            {
                for (int i = 0; i < segment.Length; i++)
                {
                    char c = segment[i];
                    switch (c)
                    {
                        // Per RFC 6901 the characters '~' and '/' are escaped as '~0' and '~1'.
                        case '~':
                            sb.Append("~0");
                            break;
                        case '/':
                            sb.Append("~1");
                            break;
                        default:
                            if (IsUnescapedFragmentChar(c))
                            {
                                sb.Append(c);
                            }
                            else
                            {
                                // Per RFC 6901 section 6 the JSON Pointer is embedded in a URI fragment,
                                // so any characters outside the RFC 3986 'fragment' production are
                                // percent-encoded using their UTF-8 octets.
                                int codePoint = c;
                                if (char.IsHighSurrogate(c) && i + 1 < segment.Length && char.IsLowSurrogate(segment[i + 1]))
                                {
                                    codePoint = char.ConvertToUtf32(c, segment[i + 1]);
                                    i++;
                                }

                                AppendPercentEncoded(ref sb, codePoint);
                            }

                            break;
                    }
                }
            }

            private static void AppendPercentEncoded(ref ValueStringBuilder sb, int codePoint)
            {
                Span<byte> utf8Bytes = stackalloc byte[4];
                int byteCount;

                if (codePoint <= 0x7F)
                {
                    utf8Bytes[0] = (byte)codePoint;
                    byteCount = 1;
                }
                else if (codePoint <= 0x7FF)
                {
                    utf8Bytes[0] = (byte)(0xC0 | (codePoint >> 6));
                    utf8Bytes[1] = (byte)(0x80 | (codePoint & 0x3F));
                    byteCount = 2;
                }
                else if (codePoint <= 0xFFFF)
                {
                    utf8Bytes[0] = (byte)(0xE0 | (codePoint >> 12));
                    utf8Bytes[1] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
                    utf8Bytes[2] = (byte)(0x80 | (codePoint & 0x3F));
                    byteCount = 3;
                }
                else
                {
                    utf8Bytes[0] = (byte)(0xF0 | (codePoint >> 18));
                    utf8Bytes[1] = (byte)(0x80 | ((codePoint >> 12) & 0x3F));
                    utf8Bytes[2] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
                    utf8Bytes[3] = (byte)(0x80 | (codePoint & 0x3F));
                    byteCount = 4;
                }

                const string HexDigits = "0123456789ABCDEF";
                for (int i = 0; i < byteCount; i++)
                {
                    byte b = utf8Bytes[i];
                    sb.Append('%');
                    sb.Append(HexDigits[b >> 4]);
                    sb.Append(HexDigits[b & 0xF]);
                }
            }

            private static bool IsUnescapedFragmentChar(char c)
            {
                // Characters permitted unescaped by the RFC 3986 'fragment' production:
                //   fragment    = *( pchar / "/" / "?" )
                //   pchar       = unreserved / sub-delims / ":" / "@"
                //   unreserved  = ALPHA / DIGIT / "-" / "." / "_" / "~"
                //   sub-delims  = "!" / "$" / "&" / "'" / "(" / ")" / "*" / "+" / "," / ";" / "="
                // '/' is handled separately as the '~1' escape, so it is intentionally excluded here.
                if ((uint)(c - 'A') <= 'Z' - 'A' || (uint)(c - 'a') <= 'z' - 'a' || (uint)(c - '0') <= '9' - '0')
                {
                    return true;
                }

                switch (c)
                {
                    case '-':
                    case '.':
                    case '_':
                    case '~':
                    case '!':
                    case '$':
                    case '&':
                    case '\'':
                    case '(':
                    case ')':
                    case '*':
                    case '+':
                    case ',':
                    case ';':
                    case '=':
                    case ':':
                    case '@':
                    case '?':
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
