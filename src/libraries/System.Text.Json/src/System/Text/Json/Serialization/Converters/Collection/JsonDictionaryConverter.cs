// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Base class for dictionary converters such as IDictionary, Hashtable, Dictionary{,} IDictionary{,} and SortedList.
    /// </summary>
    internal abstract class JsonDictionaryConverter<TDictionary, IntermediateType> : JsonAdvancedConverter<TDictionary, IntermediateType>
    {
        internal override bool SupportsCreateObjectDelegate => true;
        private protected sealed override ConverterStrategy GetDefaultConverterStrategy() => ConverterStrategy.Dictionary;

        internal override bool IsJsonDictionaryConverter => true;
        internal override bool OnWriteResume(Utf8JsonWriter writer, TDictionary dictionary, JsonSerializerOptions options, ref WriteStack state)
            => OnWriteResumeCore(writer, dictionary, options, ref state);

        private protected abstract bool OnWriteResumeCore(Utf8JsonWriter writer, TDictionary dictionary, JsonSerializerOptions options, ref WriteStack state);
    }

    /// <summary>
    /// Base class for dictionary converters such as IDictionary, Hashtable, Dictionary{,} IDictionary{,} and SortedList.
    /// </summary>
    internal abstract class JsonDictionaryConverter<TDictionary, TKey, TValue, IntermediateType> : JsonDictionaryConverter<TDictionary, IntermediateType>
        where TKey : notnull
    {
        /// <summary>
        /// When overridden, adds the value to the collection.
        /// </summary>
        protected abstract void Add(ref IntermediateType collection, TKey key, in TValue value, JsonSerializerOptions options);

        internal override Type ElementType => typeof(TValue);

        internal override Type KeyType => typeof(TKey);


        protected JsonConverter<TKey>? _keyConverter;
        protected JsonConverter<TValue>? _valueConverter;

        protected static JsonConverter<T> GetConverter<T>(JsonTypeInfo typeInfo)
        {
            return ((JsonTypeInfo<T>)typeInfo).EffectiveConverter;
        }

        internal sealed override bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            scoped ref ReadStack state,
            [MaybeNullWhen(false)] out TDictionary value)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
            IntermediateType? obj;

            if (!state.SupportContinuation && !state.Current.CanContainMetadata)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                if (!TryGetOrCreateObject(ref reader, jsonTypeInfo, ref state, out obj))
                {
                    Debug.Fail("TryGetOrCreateObject returned false when continuation is disabled");
                    value = default;
                    return false;
                }

                if (!TryPopulate(ref reader, options, ref state, ref obj))
                {
                    Debug.Fail("TryPopulate returned false when continuation is disabled");
                    value = default;
                    return false;
                }
            }
            else
            {
                // Slower path that supports continuation and reading metadata.
                if (state.Current.ObjectState == StackFrameObjectState.None)
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                    }

                    state.Current.ObjectState = StackFrameObjectState.StartToken;
                }

                // Handle the metadata properties.
                if (state.Current.CanContainMetadata && state.Current.ObjectState < StackFrameObjectState.ReadMetadata)
                {
                    if (!JsonSerializer.TryReadMetadata(this, jsonTypeInfo, ref reader, ref state))
                    {
                        value = default;
                        return false;
                    }

                    if (state.Current.MetadataPropertyNames == MetadataPropertyName.Ref)
                    {
                        value = JsonSerializer.ResolveReferenceId<TDictionary>(ref state);
                        return true;
                    }

                    state.Current.ObjectState = StackFrameObjectState.ReadMetadata;
                }

                // Dispatch to any polymorphic converters: should always be entered regardless of ObjectState progress
                if (state.Current.MetadataPropertyNames.HasFlag(MetadataPropertyName.Type) &&
                    state.Current.PolymorphicSerializationState != PolymorphicSerializationState.PolymorphicReEntryStarted &&
                    ResolvePolymorphicConverter(jsonTypeInfo, ref state) is JsonConverter polymorphicConverter)
                {
                    Debug.Assert(!IsValueType);
                    bool success = polymorphicConverter.OnTryReadAsObject(ref reader, polymorphicConverter.TypeToConvert, options, ref state, out object? objectResult);
                    value = (TDictionary)objectResult!;
                    state.ExitPolymorphicConverter(success);
                    return success;
                }

                // Create the dictionary.
                if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                {
                    if (state.Current.CanContainMetadata)
                    {
                        JsonSerializer.ValidateMetadataForObjectConverter(ref state);
                    }

                    if (!TryGetOrCreateObject(ref reader, jsonTypeInfo, ref state, out obj))
                    {
                        value = default;
                        return false;
                    }

                    state.Current.ReturnValue = obj;
                    state.Current.ObjectState = StackFrameObjectState.CreatedObject;
                }
                else
                {
                    obj = (IntermediateType?)state.Current.ReturnValue!;
                    Debug.Assert(obj != null);
                }

                if (!TryPopulate(ref reader, options, ref state, ref obj))
                {
                    value = default;
                    return false;
                }
            }

            return TryConvert(ref reader, jsonTypeInfo, ref state, obj, out value);
        }

        private protected sealed override bool TryPopulate(ref Utf8JsonReader reader, JsonSerializerOptions options, scoped ref ReadStack state, ref IntermediateType obj)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
            JsonTypeInfo keyTypeInfo = jsonTypeInfo.KeyTypeInfo!;
            JsonTypeInfo elementTypeInfo = jsonTypeInfo.ElementTypeInfo!;

            // needed for exception path to be reported correctly
            state.Current.ReturnValue = obj;

            if (!state.SupportContinuation && !state.Current.CanContainMetadata)
            {
                _keyConverter ??= GetConverter<TKey>(keyTypeInfo);
                _valueConverter ??= GetConverter<TValue>(elementTypeInfo);

                if (_valueConverter.CanUseDirectReadOrWrite && state.Current.NumberHandling == null)
                {
                    // Process all elements.
                    while (true)
                    {
                        // Read the key name.
                        reader.ReadWithVerify();

                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        // Read method would have thrown if otherwise.
                        Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

                        state.Current.JsonPropertyInfo = keyTypeInfo.PropertyInfoForTypeInfo;
                        TKey key = ReadDictionaryKey(_keyConverter, ref reader, ref state, options);

                        // Read the value and add.
                        reader.ReadWithVerify();
                        state.Current.JsonPropertyInfo = elementTypeInfo.PropertyInfoForTypeInfo;
                        TValue? element = _valueConverter.Read(ref reader, ElementType, options);
                        Add(ref obj, key, element!, options);

                        // needed for exception path to be reported correctly for value types
                        state.Current.ReturnValue = obj;
                    }
                }
                else
                {
                    // Process all elements.
                    while (true)
                    {
                        // Read the key name.
                        reader.ReadWithVerify();

                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        // Read method would have thrown if otherwise.
                        Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

                        state.Current.JsonPropertyInfo = keyTypeInfo.PropertyInfoForTypeInfo;
                        TKey key = ReadDictionaryKey(_keyConverter, ref reader, ref state, options);

                        reader.ReadWithVerify();

                        // Get the value from the converter and add it.
                        state.Current.JsonPropertyInfo = elementTypeInfo.PropertyInfoForTypeInfo;
                        _valueConverter.TryRead(ref reader, ElementType, options, ref state, out TValue? element);
                        Add(ref obj, key, element!, options);

                        // needed for exception path to be reported correctly for value types
                        state.Current.ReturnValue = obj;
                    }
                }
            }
            else
            {
                // Process all elements.
                _keyConverter ??= GetConverter<TKey>(keyTypeInfo);
                _valueConverter ??= GetConverter<TValue>(elementTypeInfo);
                while (true)
                {
                    if (state.Current.PropertyState == StackFramePropertyState.None)
                    {
                        state.Current.PropertyState = StackFramePropertyState.ReadName;

                        // Read the key name.
                        if (!reader.Read())
                        {
                            return false;
                        }
                    }

                    // Determine the property.
                    TKey key;
                    if (state.Current.PropertyState < StackFramePropertyState.Name)
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        // Read method would have thrown if otherwise.
                        Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

                        state.Current.PropertyState = StackFramePropertyState.Name;

                        if (state.Current.CanContainMetadata)
                        {
                            ReadOnlySpan<byte> propertyName = reader.GetSpan();
                            if (JsonSerializer.IsMetadataPropertyName(propertyName, state.Current.BaseJsonTypeInfo.PolymorphicTypeResolver))
                            {
                                ThrowHelper.ThrowUnexpectedMetadataException(propertyName, ref reader, ref state);
                            }
                        }

                        state.Current.JsonPropertyInfo = keyTypeInfo.PropertyInfoForTypeInfo;
                        key = ReadDictionaryKey(_keyConverter, ref reader, ref state, options);
                    }
                    else
                    {
                        // DictionaryKey is assigned before all return false cases, null value is unreachable
                        key = (TKey)state.Current.DictionaryKey!;
                    }

                    if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
                    {
                        state.Current.PropertyState = StackFramePropertyState.ReadValue;

                        if (!SingleValueReadWithReadAhead(_valueConverter.RequiresReadAhead, ref reader, ref state))
                        {
                            state.Current.DictionaryKey = key;
                            return false;
                        }
                    }

                    if (state.Current.PropertyState < StackFramePropertyState.TryRead)
                    {
                        // Get the value from the converter and add it.
                        state.Current.JsonPropertyInfo = elementTypeInfo.PropertyInfoForTypeInfo;
                        bool success = _valueConverter.TryRead(ref reader, typeof(TValue), options, ref state, out TValue? element);
                        if (!success)
                        {
                            state.Current.DictionaryKey = key;
                            return false;
                        }

                        Add(ref obj, key, element!, options);
                        state.Current.ReturnValue = obj;
                        state.Current.EndElement();
                    }
                }
            }

            return true;
        }

        private static TKey ReadDictionaryKey(JsonConverter<TKey> keyConverter, ref Utf8JsonReader reader, scoped ref ReadStack state, JsonSerializerOptions options)
        {
            TKey key;
            string unescapedPropertyNameAsString = reader.GetString()!;
            state.Current.JsonPropertyNameAsString = unescapedPropertyNameAsString; // Copy key name for JSON Path support in case of error.

            // Special case string to avoid calling GetString twice and save one allocation.
            if (keyConverter.IsInternalConverter && keyConverter.TypeToConvert == typeof(string))
            {
                key = (TKey)(object)unescapedPropertyNameAsString;
            }
            else
            {
                key = keyConverter.ReadAsPropertyNameCore(ref reader, keyConverter.TypeToConvert, options);
            }

            return key;
        }

        internal sealed override bool OnTryWrite(
            Utf8JsonWriter writer,
            TDictionary dictionary,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            if (dictionary == null)
            {
                writer.WriteNullValue();
                return true;
            }

            if (!state.Current.ProcessedStartToken)
            {
                state.Current.ProcessedStartToken = true;
                writer.WriteStartObject();

                if (state.CurrentContainsMetadata && CanHaveMetadata)
                {
                    JsonSerializer.WriteMetadataForObject(this, ref state, writer);
                }

                state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;
            }

            bool success = OnWriteResume(writer, dictionary, options, ref state);
            if (success)
            {
                if (!state.Current.ProcessedEndToken)
                {
                    state.Current.ProcessedEndToken = true;
                    writer.WriteEndObject();
                }
            }

            return success;
        }
    }
}
