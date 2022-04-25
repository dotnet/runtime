// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Base class for dictionary converters such as IDictionary, Hashtable, Dictionary{,} IDictionary{,} and SortedList.
    /// </summary>
    internal abstract class JsonDictionaryConverter<TDictionary> : JsonResumableConverter<TDictionary>
    {
        internal sealed override ConverterStrategy ConverterStrategy => ConverterStrategy.Dictionary;

        protected internal abstract bool OnWriteResume(Utf8JsonWriter writer, TDictionary dictionary, JsonSerializerOptions options, ref WriteStack state);
    }

    /// <summary>
    /// Base class for dictionary converters such as IDictionary, Hashtable, Dictionary{,} IDictionary{,} and SortedList.
    /// </summary>
    internal abstract class JsonDictionaryConverter<TDictionary, TKey, TValue> : JsonDictionaryConverter<TDictionary>
        where TKey : notnull
    {
        /// <summary>
        /// When overridden, adds the value to the collection.
        /// </summary>
        protected abstract void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state);

        /// <summary>
        /// When overridden, converts the temporary collection held in state.Current.ReturnValue to the final collection.
        /// This is used with immutable collections.
        /// </summary>
        protected virtual void ConvertCollection(ref ReadStack state, JsonSerializerOptions options) { }

        /// <summary>
        /// When overridden, create the collection. It may be a temporary collection or the final collection.
        /// </summary>
        protected virtual void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state)
        {
            JsonTypeInfo typeInfo = state.Current.JsonTypeInfo;

            if (typeInfo.CreateObject is null)
            {
                // The contract model was not able to produce a default constructor for two possible reasons:
                // 1. Either the declared collection type is abstract and cannot be instantiated.
                // 2. The collection type does not specify a default constructor.
                if (TypeToConvert.IsAbstract || TypeToConvert.IsInterface)
                {
                    ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
                }
                else
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(TypeToConvert, ref reader, ref state);
                }
            }

            state.Current.ReturnValue = typeInfo.CreateObject()!;
            Debug.Assert(state.Current.ReturnValue is TDictionary);
        }

        internal override Type ElementType => typeof(TValue);

        internal override Type KeyType => typeof(TKey);


        protected JsonConverter<TKey>? _keyConverter;
        protected JsonConverter<TValue>? _valueConverter;

        protected static JsonConverter<T> GetConverter<T>(JsonTypeInfo typeInfo)
        {
            JsonConverter<T> converter = (JsonConverter<T>)typeInfo.PropertyInfoForTypeInfo.ConverterBase;
            Debug.Assert(converter != null); // It should not be possible to have a null converter at this point.

            return converter;
        }

        internal sealed override bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            ref ReadStack state,
            [MaybeNullWhen(false)] out TDictionary value)
        {
            JsonTypeInfo keyTypeInfo = state.Current.JsonTypeInfo.KeyTypeInfo!;
            JsonTypeInfo elementTypeInfo = state.Current.JsonTypeInfo.ElementTypeInfo!;

            if (!state.SupportContinuation && !state.Current.CanContainMetadata)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                CreateCollection(ref reader, ref state);

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
                        Add(key, element!, options, ref state);
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
                        Add(key, element!, options, ref state);
                    }
                }
            }
            else
            {
                // Slower path that supports continuation and reading metadata.
                JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;

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
                    ResolvePolymorphicConverter(jsonTypeInfo, options, ref state) is JsonConverter polymorphicConverter)
                {
                    Debug.Assert(!IsValueType);
                    bool success = polymorphicConverter.OnTryReadAsObject(ref reader, options, ref state, out object? objectResult);
                    value = (TDictionary)objectResult!;
                    state.ExitPolymorphicConverter(success);
                    return success;
                }

                // Create the dictionary.
                if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                {
                    if (state.Current.CanContainMetadata)
                    {
                        JsonSerializer.ValidateMetadataForObjectConverter(this, ref reader, ref state);
                    }

                    CreateCollection(ref reader, ref state);

                    if (state.Current.MetadataPropertyNames.HasFlag(MetadataPropertyName.Id))
                    {
                        Debug.Assert(state.ReferenceId != null);
                        Debug.Assert(options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve);
                        Debug.Assert(state.Current.ReturnValue is TDictionary);
                        state.ReferenceResolver.AddReference(state.ReferenceId, state.Current.ReturnValue);
                        state.ReferenceId = null;
                    }

                    state.Current.ObjectState = StackFrameObjectState.CreatedObject;
                }

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
                            value = default;
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
                            value = default;
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
                            value = default;
                            return false;
                        }

                        Add(key, element!, options, ref state);
                        state.Current.EndElement();
                    }
                }
            }

            ConvertCollection(ref state, options);
            value = (TDictionary)state.Current.ReturnValue!;
            return true;

            static TKey ReadDictionaryKey(JsonConverter<TKey> keyConverter, ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
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
