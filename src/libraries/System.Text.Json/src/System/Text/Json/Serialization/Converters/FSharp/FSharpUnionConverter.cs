// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
#if !NET
using System.Runtime.Serialization;
#endif
using System.Text.Json.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    // Converter for F# discriminated unions.
    // Fieldless cases are serialized as JSON strings.
    // Cases with fields are serialized as JSON objects with a type discriminator and named field properties.
    // The discriminator property name defaults to "$type" but can be customized via [JsonPolymorphic].
    internal sealed class FSharpUnionConverter<T> : JsonConverter<T>
    {
        internal override bool CanHaveMetadata => true;
        private readonly CaseInfo[] _casesByTag;
        private readonly Dictionary<string, CaseInfo> _casesByName;
        private readonly Dictionary<string, CaseInfo>? _casesByNameCaseInsensitive;
        private readonly Func<object, int> _tagReader;
        private readonly string _typeDiscriminatorPropertyName;
        private readonly JsonEncodedText _typeDiscriminatorPropertyNameEncoded;
        private readonly JsonUnmappedMemberHandling _effectiveUnmappedMemberHandling;

        [RequiresUnreferencedCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        [RequiresDynamicCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        public FSharpUnionConverter(
            FSharpCoreReflectionProxy.FSharpUnionCaseInfo[] unionCases,
            Func<object, int> tagReader,
            JsonSerializerOptions options,
            string typeDiscriminatorPropertyName)
        {
            ConverterStrategy = ConverterStrategy.Object;
            SupportsMultipleTokenTypes = true;
            RequiresReadAhead = true;
            _tagReader = tagReader;
            _typeDiscriminatorPropertyName = typeDiscriminatorPropertyName;
            _typeDiscriminatorPropertyNameEncoded = JsonEncodedText.Encode(typeDiscriminatorPropertyName, options.Encoder);
            _effectiveUnmappedMemberHandling = typeof(T).GetUniqueCustomAttribute<JsonUnmappedMemberHandlingAttribute>(inherit: false)?.UnmappedMemberHandling
                ?? options.UnmappedMemberHandling;

            _casesByTag = new CaseInfo[unionCases.Length];
            _casesByName = new Dictionary<string, CaseInfo>(unionCases.Length, StringComparer.Ordinal);

            Dictionary<string, CaseInfo>? caseInsensitiveMap = options.PropertyNameCaseInsensitive
                ? new Dictionary<string, CaseInfo>(unionCases.Length, StringComparer.OrdinalIgnoreCase)
                : null;

            JsonNamingPolicy? namingPolicy = options.PropertyNamingPolicy;

            foreach (FSharpCoreReflectionProxy.FSharpUnionCaseInfo uc in unionCases)
            {
                // Case name resolution: JsonPropertyNameAttribute > PropertyNamingPolicy > raw name
                string discriminatorName = uc.JsonPropertyName
                    ?? namingPolicy?.ConvertName(uc.Name)
                    ?? uc.Name;

                JsonEncodedText encodedDiscriminatorName = JsonEncodedText.Encode(discriminatorName, options.Encoder);

                // Build field info for cases with fields
                CaseFieldInfo[]? fields = null;
                if (!uc.IsFieldless)
                {
                    fields = new CaseFieldInfo[uc.Fields.Length];
                    for (int i = 0; i < uc.Fields.Length; i++)
                    {
                        PropertyInfo prop = uc.Fields[i];
                        string fieldName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                            ?? namingPolicy?.ConvertName(prop.Name)
                            ?? prop.Name;
                        JsonEncodedText encodedFieldName = JsonEncodedText.Encode(fieldName, options.Encoder);

                        fields[i] = new CaseFieldInfo(fieldName, encodedFieldName, prop, options);
                    }

                    // Validate that no field name conflicts with the discriminator property name.
                    StringComparison conflictComparison = options.PropertyNameCaseInsensitive
                        ? StringComparison.OrdinalIgnoreCase
                        : StringComparison.Ordinal;

                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (fields[i].FieldName.Equals(typeDiscriminatorPropertyName, conflictComparison))
                        {
                            throw new InvalidOperationException(SR.Format(SR.FSharpUnionFieldConflictsWithDiscriminator, typeof(T), fields[i].FieldName, typeDiscriminatorPropertyName));
                        }
                    }
                }

                // Build default field values for deserialization with missing fields.
                object[]? defaultFieldValues = null;
                if (fields is not null)
                {
                    defaultFieldValues = new object[fields.Length];
                    for (int i = 0; i < fields.Length; i++)
                    {
                        Type fieldType = fields[i].FieldType;
                        defaultFieldValues[i] = fieldType.IsValueType ?
#if NET
                            RuntimeHelpers.GetUninitializedObject(fieldType) :
#else
                            FormatterServices.GetUninitializedObject(fieldType) :
#endif
                            null!;
                    }
                }

                var caseInfo = new CaseInfo(
                    discriminatorName,
                    encodedDiscriminatorName,
                    uc.Tag,
                    uc.IsFieldless,
                    fields,
                    defaultFieldValues,
                    fields is not null ? BuildFieldIndexMap(fields, StringComparer.Ordinal, typeof(T), discriminatorName) : null,
                    fields is not null && options.PropertyNameCaseInsensitive ? BuildFieldIndexMap(fields, StringComparer.OrdinalIgnoreCase, typeof(T), discriminatorName) : null,
                    uc.FieldReader,
                    uc.Constructor,
                    typeof(T));

                _casesByTag[uc.Tag] = caseInfo;

                if (!_casesByName.TryAdd(discriminatorName, caseInfo))
                {
                    throw new InvalidOperationException(SR.Format(SR.FSharpUnionDuplicateCaseName, typeof(T), discriminatorName));
                }

                if (caseInsensitiveMap is not null && !caseInsensitiveMap.TryAdd(discriminatorName, caseInfo))
                {
                    throw new InvalidOperationException(SR.Format(SR.FSharpUnionDuplicateCaseName, typeof(T), discriminatorName));
                }
            }

            _casesByNameCaseInsensitive = caseInsensitiveMap;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Fallback for direct converter invocations. The normal pipeline
            // uses TryRead -> OnTryRead which forwards state automatically.
            ReadStack state = default;
            JsonTypeInfo jsonTypeInfo = options.GetTypeInfoInternal(typeToConvert);
            state.Initialize(jsonTypeInfo);
            state.Push();

            OnTryRead(ref reader, typeToConvert, options, ref state, out T? value);
            return value!;
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out T? value)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                value = ReadFromString(ref reader, options);
                return true;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                value = ReadFromObject(ref reader, options, ref state);
                return true;
            }

            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(typeof(T));
            value = default;
            return true;
        }

        private T ReadFromString(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            string? caseName = reader.GetString();
            if (caseName is null)
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(typeof(T));
            }

            CaseInfo caseInfo = LookupCaseByName(caseName!);

            if (caseInfo.IsFieldless)
            {
                return (T)caseInfo.Constructor(Array.Empty<object>());
            }

            // String form for a case with fields: only allowed when fields are not required.
            if (options.RespectRequiredConstructorParameters)
            {
                ThrowHelper.ThrowJsonException();
            }

            return (T)caseInfo.Constructor((object[])caseInfo.DefaultFieldValues!.Clone());
        }

        private T ReadFromObject(ref Utf8JsonReader reader, JsonSerializerOptions options, scoped ref ReadStack state)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.StartObject);

            // This converter opts into read-ahead buffering (via RequiresReadAhead),
            // so by the time ReadFromObject is called the entire JSON value has been
            // buffered and we can safely scan ahead for the discriminator and restore the reader position.
            Utf8JsonReader checkpoint = reader;

            bool preserveReferences = !typeof(T).IsValueType &&
                options.ReferenceHandlingStrategy == JsonKnownReferenceHandler.Preserve;

            // Scan for the type discriminator property and $ref metadata.
            string? caseName = null;
            string? refId = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    ThrowHelper.ThrowJsonException();
                }

                MetadataPropertyName metadata = JsonSerializer.GetMetadataPropertyName(reader.ValueSpan, resolver: null);

                if (metadata is MetadataPropertyName.Ref)
                {
                    if (!preserveReferences)
                    {
                        goto DefaultProperty;
                    }

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        ThrowHelper.ThrowJsonException();
                    }

                    refId = reader.GetString();
                    break;
                }

                // Match discriminator: either $type (detected by GetMetadataPropertyName)
                // or a custom name (e.g. "kind") via direct comparison.
                if (metadata is MetadataPropertyName.Type || reader.ValueTextEquals(_typeDiscriminatorPropertyName))
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        ThrowHelper.ThrowJsonException();
                    }

                    caseName = reader.GetString();
                    break;
                }

            DefaultProperty:
                reader.Read();
                reader.TrySkip();
            }

            if (refId is not null)
            {
                // $ref node — resolve to a previously registered object.
                // Validate that no other properties are present (matching metadata pipeline behavior).
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
                }

                return (T)state.ReferenceResolver.ResolveReference(refId);
            }

            if (caseName is null)
            {
                throw new JsonException(SR.Format(SR.FSharpUnionMissingDiscriminatorProperty, _typeDiscriminatorPropertyName, typeof(T)));
            }

            CaseInfo caseInfo = LookupCaseByName(caseName);

            // Restore reader to re-read all properties for field population.
            reader = checkpoint;
            bool discriminatorSeen = false;
            string? referenceId = null;

            if (caseInfo.IsFieldless)
            {
                // Skip to end of object, validating unmapped members and duplicate properties.
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        MetadataPropertyName metadata = JsonSerializer.GetMetadataPropertyName(reader.ValueSpan, resolver: null);

                        if (metadata is MetadataPropertyName.Type || reader.ValueTextEquals(_typeDiscriminatorPropertyName))
                        {
                            if (discriminatorSeen)
                            {
                                ThrowHelper.ThrowJsonException_DuplicatePropertyNotAllowed(_typeDiscriminatorPropertyName);
                            }

                            discriminatorSeen = true;
                        }
                        else if (metadata is MetadataPropertyName.Id && preserveReferences)
                        {
                            reader.Read();
                            if (reader.TokenType != JsonTokenType.String)
                            {
                                ThrowHelper.ThrowJsonException();
                            }

                            referenceId = reader.GetString();
                            continue;
                        }
                        else if (_effectiveUnmappedMemberHandling is JsonUnmappedMemberHandling.Disallow)
                        {
                            ThrowHelper.ThrowJsonException_UnmappedJsonProperty(typeof(T), reader.GetString()!);
                        }

                        reader.Read();
                        reader.TrySkip();
                    }
                }

                T fieldlessValue = (T)caseInfo.Constructor(Array.Empty<object>());

                if (referenceId is not null)
                {
                    state.ReferenceResolver.AddReference(referenceId, fieldlessValue);
                }

                return fieldlessValue;
            }

            // Read fields.
            object[] fieldValues = (object[])caseInfo.DefaultFieldValues!.Clone();
            bool trackPopulated = options.RespectRequiredConstructorParameters || !options.AllowDuplicateProperties;
            BitArray? populatedFields = trackPopulated ? new BitArray(caseInfo.Fields!.Length) : null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    ThrowHelper.ThrowJsonException();
                }

                MetadataPropertyName metadata = JsonSerializer.GetMetadataPropertyName(reader.ValueSpan, resolver: null);

                // Skip the discriminator property during field reading.
                if (metadata is MetadataPropertyName.Type || reader.ValueTextEquals(_typeDiscriminatorPropertyName))
                {
                    if (discriminatorSeen)
                    {
                        ThrowHelper.ThrowJsonException_DuplicatePropertyNotAllowed(_typeDiscriminatorPropertyName);
                    }

                    discriminatorSeen = true;
                    reader.Read();
                    reader.TrySkip();
                    continue;
                }

                // Capture $id metadata for reference registration.
                if (metadata is MetadataPropertyName.Id && preserveReferences)
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        ThrowHelper.ThrowJsonException();
                    }

                    referenceId = reader.GetString();
                    continue;
                }

                string? fieldName = reader.GetString();
                reader.Read();

                if (fieldName is null || !TryGetFieldIndex(fieldName, caseInfo, out int fieldIndex))
                {
                    if (_effectiveUnmappedMemberHandling is JsonUnmappedMemberHandling.Disallow)
                    {
                        ThrowHelper.ThrowJsonException_UnmappedJsonProperty(typeof(T), fieldName ?? string.Empty);
                    }

                    reader.TrySkip();
                    continue;
                }

                if (populatedFields is not null)
                {
                    if (!options.AllowDuplicateProperties && populatedFields[fieldIndex])
                    {
                        ThrowHelper.ThrowJsonException_DuplicatePropertyNotAllowed(fieldName!);
                    }

                    populatedFields[fieldIndex] = true;
                }

                CaseFieldInfo field = caseInfo.Fields![fieldIndex];
                state.Current.JsonPropertyInfo = field.PropertyInfoForTypeInfo;
                state.Current.NumberHandling = field.NumberHandling;
                field.Converter.TryReadAsObject(ref reader, field.FieldType, options, ref state, out object? fieldValue);
                fieldValues[fieldIndex] = fieldValue!;
            }

            // Validate required fields when RespectRequiredConstructorParameters is enabled.
            if (options.RespectRequiredConstructorParameters && populatedFields is not null && !populatedFields.HasAllSet())
            {
                ThrowForMissingRequiredFields(caseInfo, populatedFields);
            }

            T result = (T)caseInfo.Constructor(fieldValues);

            if (referenceId is not null)
            {
                state.ReferenceResolver.AddReference(referenceId, result);
            }

            return result;
        }

        private static void ThrowForMissingRequiredFields(CaseInfo caseInfo, BitArray populatedFields)
        {
            const int CutOffLength = 60;
            var builder = new System.Text.StringBuilder();
            bool first = true;
            for (int i = 0; i < caseInfo.Fields!.Length; i++)
            {
                if (!populatedFields[i])
                {
                    if (!first)
                    {
                        builder.Append(CultureInfo.CurrentUICulture.TextInfo.ListSeparator);
                        builder.Append(' ');
                    }

                    builder.Append('\'');
                    builder.Append(caseInfo.Fields[i].FieldName);
                    builder.Append('\'');
                    first = false;

                    if (builder.Length >= CutOffLength)
                    {
                        break;
                    }
                }
            }

            throw new JsonException(SR.Format(SR.JsonRequiredPropertiesMissing, caseInfo.DeclaringType, builder.ToString()));
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            // Fallback for direct converter invocations. The normal pipeline
            // uses TryWrite -> OnTryWrite which forwards state automatically.
            WriteStack state = default;
            JsonTypeInfo typeInfo = options.GetTypeInfoInternal(typeof(T));
            state.Initialize(typeInfo);
            state.Push();

            try
            {
                OnTryWrite(writer, value, options, ref state);
            }
            catch
            {
                state.DisposePendingDisposablesOnException();
                throw;
            }
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
            int tag = _tagReader(value!);
            CaseInfo caseInfo = _casesByTag[tag];

            // Fieldless cases serialize as strings when reference tracking is not active.
            if (caseInfo.IsFieldless && state.NewReferenceId is null)
            {
                writer.WriteStringValue(caseInfo.EncodedDiscriminatorName);
                return true;
            }

            writer.WriteStartObject();

            // Write $id metadata if a new reference was registered by TryWrite.
            if (state.NewReferenceId is not null)
            {
                writer.WriteString(JsonSerializer.s_metadataId, state.NewReferenceId);
                state.NewReferenceId = null;
            }

            writer.WriteString(_typeDiscriminatorPropertyNameEncoded, caseInfo.EncodedDiscriminatorName);

            if (!caseInfo.IsFieldless)
            {
                object[] fieldValues = caseInfo.FieldReader(value!);
                Debug.Assert(fieldValues.Length == caseInfo.Fields!.Length);

                for (int i = 0; i < caseInfo.Fields.Length; i++)
                {
                    CaseFieldInfo field = caseInfo.Fields[i];
                    writer.WritePropertyName(field.EncodedFieldName);
                    state.Current.JsonPropertyInfo = field.PropertyInfoForTypeInfo;
                    state.Current.NumberHandling = field.NumberHandling;
                    field.Converter.TryWriteAsObject(writer, fieldValues[i], options, ref state);
                }
            }

            writer.WriteEndObject();
            return true;
        }

        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            // Clear any polymorphism options that PopulatePolymorphismMetadata() may have set
            // from a [JsonPolymorphic] attribute. The F# union converter handles the type
            // discriminator internally and does not use the standard polymorphism pipeline.
            jsonTypeInfo.PolymorphismOptions = null;
        }

        private CaseInfo LookupCaseByName(string caseName)
        {
            if (_casesByName.TryGetValue(caseName, out CaseInfo? caseInfo))
            {
                return caseInfo;
            }

            if (_casesByNameCaseInsensitive?.TryGetValue(caseName, out caseInfo) == true)
            {
                return caseInfo;
            }

            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(typeof(T));
            return default!;
        }

        private static bool TryGetFieldIndex(
            string fieldName,
            CaseInfo caseInfo,
            out int fieldIndex)
        {
            if (caseInfo.FieldIndexMap!.TryGetValue(fieldName, out fieldIndex))
            {
                return true;
            }

            if (caseInfo.FieldIndexMapCaseInsensitive?.TryGetValue(fieldName, out fieldIndex) == true)
            {
                return true;
            }

            fieldIndex = -1;
            return false;
        }

        private static Dictionary<string, int> BuildFieldIndexMap(CaseFieldInfo[] fields, StringComparer comparer, Type declaringType, string caseName)
        {
            var map = new Dictionary<string, int>(fields.Length, comparer);
            for (int i = 0; i < fields.Length; i++)
            {
                if (!map.TryAdd(fields[i].FieldName, i))
                {
                    throw new InvalidOperationException(SR.Format(SR.FSharpUnionDuplicateFieldName, declaringType, caseName, fields[i].FieldName));
                }
            }

            return map;
        }

        private sealed class CaseInfo
        {
            public CaseInfo(
                string discriminatorName,
                JsonEncodedText encodedDiscriminatorName,
                int tag,
                bool isFieldless,
                CaseFieldInfo[]? fields,
                object[]? defaultFieldValues,
                Dictionary<string, int>? fieldIndexMap,
                Dictionary<string, int>? fieldIndexMapCaseInsensitive,
                Func<object, object[]> fieldReader,
                Func<object[], object> constructor,
                Type declaringType)
            {
                DiscriminatorName = discriminatorName;
                EncodedDiscriminatorName = encodedDiscriminatorName;
                Tag = tag;
                IsFieldless = isFieldless;
                Fields = fields;
                DefaultFieldValues = defaultFieldValues;
                FieldIndexMap = fieldIndexMap;
                FieldIndexMapCaseInsensitive = fieldIndexMapCaseInsensitive;
                FieldReader = fieldReader;
                Constructor = constructor;
                DeclaringType = declaringType;
            }

            public string DiscriminatorName { get; }
            public JsonEncodedText EncodedDiscriminatorName { get; }
            public int Tag { get; }
            public bool IsFieldless { get; }
            public CaseFieldInfo[]? Fields { get; }
            public object[]? DefaultFieldValues { get; }
            public Dictionary<string, int>? FieldIndexMap { get; }
            public Dictionary<string, int>? FieldIndexMapCaseInsensitive { get; }
            public Func<object, object[]> FieldReader { get; }
            public Func<object[], object> Constructor { get; }
            public Type DeclaringType { get; }
        }

        private sealed class CaseFieldInfo
        {
            private readonly JsonSerializerOptions _options;
            private JsonConverter? _converter;
            private JsonPropertyInfo? _propertyInfoForTypeInfo;

            [RequiresUnreferencedCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
            [RequiresDynamicCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
            public CaseFieldInfo(
                string fieldName,
                JsonEncodedText encodedFieldName,
                PropertyInfo propertyInfo,
                JsonSerializerOptions options)
            {
                FieldName = fieldName;
                EncodedFieldName = encodedFieldName;
                FieldType = propertyInfo.PropertyType;
                _options = options;

                // Honor [JsonConverter] on the field PropertyInfo.
                JsonConverterAttribute? converterAttr = propertyInfo.GetCustomAttribute<JsonConverterAttribute>(inherit: false);
                if (converterAttr is not null)
                {
                    _converter = ResolveCustomConverter(converterAttr, FieldType, propertyInfo, options);
                }

                // Honor [JsonNumberHandling] on the field PropertyInfo.
                NumberHandling = propertyInfo.GetCustomAttribute<JsonNumberHandlingAttribute>(inherit: false)?.Handling;
            }

            public string FieldName { get; }
            public JsonEncodedText EncodedFieldName { get; }
            public Type FieldType { get; }
            public JsonNumberHandling? NumberHandling { get; }
            public JsonConverter Converter => _converter ??= _options.GetConverterInternal(FieldType);
            public JsonPropertyInfo PropertyInfoForTypeInfo => _propertyInfoForTypeInfo ??= _options.GetTypeInfoInternal(FieldType).PropertyInfoForTypeInfo;

            [RequiresUnreferencedCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
            [RequiresDynamicCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
            private static JsonConverter ResolveCustomConverter(JsonConverterAttribute converterAttribute, Type fieldType, PropertyInfo propertyInfo, JsonSerializerOptions options)
            {
                Type? converterType = converterAttribute.ConverterType;
                JsonConverter? converter;

                if (converterType is null)
                {
                    converter = converterAttribute.CreateConverter(fieldType);
                    if (converter is null)
                    {
                        ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(propertyInfo.DeclaringType!, propertyInfo, fieldType);
                    }
                }
                else
                {
                    ConstructorInfo? ctor = converterType.GetConstructor(Type.EmptyTypes);
                    if (!typeof(JsonConverter).IsAssignableFrom(converterType) || ctor is null || !ctor.IsPublic)
                    {
                        ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeInvalid(propertyInfo.DeclaringType!, propertyInfo);
                    }

                    converter = (JsonConverter)Activator.CreateInstance(converterType)!;
                }

                if (!converter!.CanConvert(fieldType))
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(propertyInfo.DeclaringType!, propertyInfo, fieldType);
                }

                if (converter is JsonConverterFactory factory)
                {
                    converter = factory.GetConverterInternal(fieldType, options);
                }

                return converter!;
            }
        }
    }
}
