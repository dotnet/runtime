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
        protected virtual void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state) { }

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
            JsonTypeInfo elementTypeInfo = state.Current.JsonTypeInfo.ElementTypeInfo!;

            if (state.UseFastPath)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                CreateCollection(ref reader, ref state);

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

                        TKey key = ReadDictionaryKey(ref reader, ref state);

                        // Read the value and add.
                        reader.ReadWithVerify();
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

                        TKey key = ReadDictionaryKey(ref reader, ref state);

                        reader.ReadWithVerify();

                        // Get the value from the converter and add it.
                        _valueConverter.TryRead(ref reader, ElementType, options, ref state, out TValue? element);
                        Add(key, element!, options, ref state);
                    }
                }
            }
            else
            {
                // Slower path that supports continuation and preserved references.

                if (state.Current.ObjectState == StackFrameObjectState.None)
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                    }

                    state.Current.ObjectState = StackFrameObjectState.StartToken;
                }

                // Handle the metadata properties.
                bool preserveReferences = options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve;
                if (preserveReferences && state.Current.ObjectState < StackFrameObjectState.PropertyValue)
                {
                    if (JsonSerializer.ResolveMetadataForJsonObject<TDictionary>(ref reader, ref state, options))
                    {
                        if (state.Current.ObjectState == StackFrameObjectState.ReadRefEndObject)
                        {
                            // This will never throw since it was previously validated in ResolveMetadataForJsonObject.
                            value = (TDictionary)state.Current.ReturnValue!;
                            return true;
                        }
                    }
                    else
                    {
                        value = default;
                        return false;
                    }
                }

                // Create the dictionary.
                if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                {
                    CreateCollection(ref reader, ref state);
                    state.Current.ObjectState = StackFrameObjectState.CreatedObject;
                }

                // Process all elements.
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

                        if (preserveReferences)
                        {
                            ReadOnlySpan<byte> propertyName = reader.GetSpan();
                            if (propertyName.Length > 0 && propertyName[0] == '$')
                            {
                                ThrowHelper.ThrowUnexpectedMetadataException(propertyName, ref reader, ref state);
                            }
                        }

                        key = ReadDictionaryKey(ref reader, ref state);
                    }
                    else
                    {
                        // DictionaryKey is assigned before all return false cases, null value is unreachable
                        key = (TKey)state.Current.DictionaryKey!;
                    }

                    if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
                    {
                        state.Current.PropertyState = StackFramePropertyState.ReadValue;

                        if (!SingleValueReadWithReadAhead(_valueConverter.ConverterStrategy, ref reader, ref state))
                        {
                            state.Current.DictionaryKey = key;
                            value = default;
                            return false;
                        }
                    }

                    if (state.Current.PropertyState < StackFramePropertyState.TryRead)
                    {
                        // Get the value from the converter and add it.
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

            TKey ReadDictionaryKey(ref Utf8JsonReader reader, ref ReadStack state)
            {
                TKey key;
                string unescapedPropertyNameAsString;

                // Special case string to avoid calling GetString twice and save one allocation.
                if (KeyType == typeof(string))
                {
                    unescapedPropertyNameAsString = reader.GetString()!;
                    key = (TKey)(object)unescapedPropertyNameAsString;
                }
                else
                {
                    _keyConverter ??= GetConverter<TKey>(state.Current.JsonTypeInfo.KeyTypeInfo!);
                    key = _keyConverter.ReadAsPropertyName(ref reader, typeToConvert, options);
                    unescapedPropertyNameAsString = reader.GetString()!;
                }

                // Copy key name for JSON Path support in case of error.
                state.Current.JsonPropertyNameAsString = unescapedPropertyNameAsString;
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
                if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve)
                {
                    if (JsonSerializer.WriteReferenceForObject(this, dictionary, ref state, writer) == MetadataPropertyName.Ref)
                    {
                        return true;
                    }
                }

                state.Current.DeclaredJsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;
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

        internal sealed override void CreateInstanceForReferenceResolver(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
            => CreateCollection(ref reader, ref state);
    }
}
