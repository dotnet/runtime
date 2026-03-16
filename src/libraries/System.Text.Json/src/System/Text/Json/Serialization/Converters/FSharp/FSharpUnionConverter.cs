// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    // Converter for F# discriminated unions.
    // Fieldless cases are serialized as JSON strings.
    // Cases with fields are serialized as JSON objects with a type discriminator and named field properties.
    // The discriminator property name defaults to "$type" but can be customized via [JsonPolymorphic].
    internal sealed class FSharpUnionConverter<T> : JsonConverter<T>
    {
        private readonly CaseInfo[] _casesByTag;
        private readonly Dictionary<string, CaseInfo> _casesByName;
        private readonly Dictionary<string, CaseInfo>? _casesByNameCaseInsensitive;
        private readonly Func<object, int> _tagReader;
        private readonly string _typeDiscriminatorPropertyName;
        private readonly JsonEncodedText _typeDiscriminatorPropertyNameEncoded;

        [RequiresUnreferencedCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        [RequiresDynamicCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        public FSharpUnionConverter(
            FSharpCoreReflectionProxy.FSharpUnionCaseInfo[] unionCases,
            Func<object, int> tagReader,
            JsonSerializerOptions options,
            string typeDiscriminatorPropertyName)
        {
            _tagReader = tagReader;
            _typeDiscriminatorPropertyName = typeDiscriminatorPropertyName;
            _typeDiscriminatorPropertyNameEncoded = JsonEncodedText.Encode(typeDiscriminatorPropertyName, options.Encoder);

            int maxTag = 0;
            foreach (FSharpCoreReflectionProxy.FSharpUnionCaseInfo uc in unionCases)
            {
                if (uc.Tag > maxTag)
                {
                    maxTag = uc.Tag;
                }
            }

            _casesByTag = new CaseInfo[maxTag + 1];
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
                    if (uc.Fields.Length > 64)
                    {
                        ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(typeof(T));
                    }

                    fields = new CaseFieldInfo[uc.Fields.Length];
                    for (int i = 0; i < uc.Fields.Length; i++)
                    {
                        PropertyInfo prop = uc.Fields[i];
                        string fieldName = namingPolicy?.ConvertName(prop.Name) ?? prop.Name;
                        JsonEncodedText encodedFieldName = JsonEncodedText.Encode(fieldName, options.Encoder);

                        fields[i] = new CaseFieldInfo(fieldName, encodedFieldName, prop.PropertyType, options);
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
                        defaultFieldValues[i] = fieldType.IsValueType ? RuntimeHelpers.GetUninitializedObject(fieldType) : null!;
                    }
                }

                var caseInfo = new CaseInfo(
                    discriminatorName,
                    encodedDiscriminatorName,
                    uc.Tag,
                    uc.IsFieldless,
                    fields,
                    defaultFieldValues,
                    fields is not null ? BuildFieldIndexMap(fields, StringComparer.Ordinal) : null,
                    fields is not null && options.PropertyNameCaseInsensitive ? BuildFieldIndexMap(fields, StringComparer.OrdinalIgnoreCase) : null,
                    uc.FieldReader,
                    uc.Constructor,
                    typeof(T));

                _casesByTag[uc.Tag] = caseInfo;

                if (!_casesByName.TryAdd(discriminatorName, caseInfo))
                {
                    throw new InvalidOperationException(SR.Format(SR.FSharpUnionDuplicateCaseName, typeof(T), discriminatorName));
                }

                caseInsensitiveMap?.TryAdd(discriminatorName, caseInfo);
            }

            _casesByNameCaseInsensitive = caseInsensitiveMap;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return ReadFromString(ref reader, options);
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                return ReadFromObject(ref reader, options);
            }

            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(typeof(T));
            return default!;
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

        private T ReadFromObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.StartObject);

            // Checkpoint at StartObject for potential out-of-order restore.
            Utf8JsonReader startCheckpoint = reader;

            // Read the first property.
            reader.Read();

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(typeof(T));
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                ThrowHelper.ThrowJsonException();
            }

            CaseInfo caseInfo;

            if (reader.ValueTextEquals(_typeDiscriminatorPropertyName))
            {
                // Fast path: discriminator is the first property.
                reader.Read();
                string? caseName = reader.GetString();
                if (caseName is null)
                {
                    ThrowHelper.ThrowJsonException();
                }

                caseInfo = LookupCaseByName(caseName);
            }
            else if (options.AllowOutOfOrderMetadataProperties)
            {
                // Slow path: scan ahead to find the discriminator.
                // The converter uses ConverterStrategy.Value which ensures the entire
                // JSON value is buffered before Read() is called, enabling checkpoint restore.

                // Skip the first property value.
                if (!reader.Read())
                {
                    ThrowHelper.ThrowJsonException();
                }

                reader.TrySkip();

                string? caseName = null;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        ThrowHelper.ThrowJsonException();
                    }

                    bool isDiscriminator = reader.ValueTextEquals(_typeDiscriminatorPropertyName);
                    reader.Read();

                    if (isDiscriminator)
                    {
                        caseName = reader.GetString();
                        break;
                    }

                    reader.TrySkip();
                }

                if (caseName is null)
                {
                    ThrowHelper.ThrowJsonException();
                }

                caseInfo = LookupCaseByName(caseName);

                // Restore reader to StartObject so field-reading loop processes all properties.
                reader = startCheckpoint;
            }
            else
            {
                ThrowHelper.ThrowJsonException();
                return default!;
            }

            if (caseInfo.IsFieldless)
            {
                // Skip to end of object.
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        reader.Read();
                        reader.TrySkip();
                    }
                }

                return (T)caseInfo.Constructor(Array.Empty<object>());
            }

            // Read fields.
            object[] fieldValues = (object[])caseInfo.DefaultFieldValues!.Clone();
            long populatedFields = 0;

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

                // Skip the discriminator property when encountered during field reading.
                if (reader.ValueTextEquals(_typeDiscriminatorPropertyName))
                {
                    reader.Read();
                    reader.TrySkip();
                    continue;
                }

                string? fieldName = reader.GetString();
                reader.Read();

                if (fieldName is null || !TryGetFieldIndex(fieldName, caseInfo, out int fieldIndex))
                {
                    // Unknown property — skip it.
                    reader.TrySkip();
                    continue;
                }

                CaseFieldInfo field = caseInfo.Fields![fieldIndex];
                object? fieldValue = field.Converter.ReadAsObject(ref reader, field.FieldType, options);
                fieldValues[fieldIndex] = fieldValue!;
                populatedFields |= 1L << fieldIndex;
            }

            // Validate required fields when RespectRequiredConstructorParameters is enabled.
            if (options.RespectRequiredConstructorParameters)
            {
                long allFieldsMask = caseInfo.Fields!.Length == 64 ? ~0L : (1L << caseInfo.Fields.Length) - 1;
                if (populatedFields != allFieldsMask)
                {
                    ThrowForMissingRequiredFields(caseInfo, populatedFields);
                }
            }

            return (T)caseInfo.Constructor(fieldValues);
        }

        private static void ThrowForMissingRequiredFields(CaseInfo caseInfo, long populatedFields)
        {
            var builder = new System.Text.StringBuilder();
            bool first = true;
            for (int i = 0; i < caseInfo.Fields!.Length; i++)
            {
                if ((populatedFields & (1L << i)) == 0)
                {
                    if (!first)
                    {
                        builder.Append(", ");
                    }

                    builder.Append('\'');
                    builder.Append(caseInfo.Fields[i].FieldName);
                    builder.Append('\'');
                    first = false;
                }
            }

            throw new JsonException(SR.Format(SR.JsonRequiredPropertiesMissing, caseInfo.DeclaringType, builder.ToString()));
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            int tag = _tagReader(value);
            CaseInfo caseInfo = _casesByTag[tag];

            if (caseInfo.IsFieldless)
            {
                writer.WriteStringValue(caseInfo.EncodedDiscriminatorName);
                return;
            }

            writer.WriteStartObject();
            writer.WriteString(_typeDiscriminatorPropertyNameEncoded, caseInfo.EncodedDiscriminatorName);

            object[] fieldValues = caseInfo.FieldReader(value);
            Debug.Assert(fieldValues.Length == caseInfo.Fields!.Length);

            for (int i = 0; i < caseInfo.Fields.Length; i++)
            {
                CaseFieldInfo field = caseInfo.Fields[i];
                writer.WritePropertyName(field.EncodedFieldName);
                field.Converter.WriteAsObject(writer, fieldValues[i], options);
            }

            writer.WriteEndObject();
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

        private static Dictionary<string, int> BuildFieldIndexMap(CaseFieldInfo[] fields, StringComparer comparer)
        {
            var map = new Dictionary<string, int>(fields.Length, comparer);
            for (int i = 0; i < fields.Length; i++)
            {
                map.TryAdd(fields[i].FieldName, i);
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

            public CaseFieldInfo(
                string fieldName,
                JsonEncodedText encodedFieldName,
                Type fieldType,
                JsonSerializerOptions options)
            {
                FieldName = fieldName;
                EncodedFieldName = encodedFieldName;
                FieldType = fieldType;
                _options = options;
            }

            public string FieldName { get; }
            public JsonEncodedText EncodedFieldName { get; }
            public Type FieldType { get; }
            public JsonConverter Converter => _converter ??= _options.GetConverterInternal(FieldType);
        }
    }
}
