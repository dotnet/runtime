// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Default base class implementation of <cref>JsonDictionaryConverter{TCollection}</cref> .
    /// </summary>
    internal abstract class DictionaryDefaultConverter<TCollection, TKey, TValue>
        : JsonDictionaryConverter<TCollection> where TKey : notnull
    {
        /// <summary>
        /// When overridden, adds the value to the collection.
        /// </summary>
        protected abstract void Add(TKey key, TValue value, JsonSerializerOptions options, ref ReadStack state);

        /// <summary>
        /// When overridden, converts the temporary collection held in state.Current.ReturnValue to the final collection.
        /// This is used with immutable collections.
        /// </summary>
        protected virtual void ConvertCollection(ref ReadStack state, JsonSerializerOptions options) { }

        /// <summary>
        /// When overridden, create the collection. It may be a temporary collection or the final collection.
        /// </summary>
        protected virtual void CreateCollection(ref ReadStack state) { }

        internal override Type ElementType => typeof(TValue);

        internal override Type KeyType => typeof(TKey);

        protected static JsonConverter<TValue> GetElementConverter(ref ReadStack state)
        {
            JsonConverter<TValue> converter = (JsonConverter<TValue>)state.Current.JsonClassInfo.ElementClassInfo!.PolicyProperty!.ConverterBase;
            Debug.Assert(converter != null); // It should not be possible to have a null converter at this point.

            return converter;
        }

        protected string GetKeyName(string key, ref WriteStack state, JsonSerializerOptions options)
        {
            if (options.DictionaryKeyPolicy != null && !state.Current.IgnoreDictionaryKeyPolicy)
            {
                key = options.DictionaryKeyPolicy.ConvertName(key);

                if (key == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerDictionaryKeyNull(options.DictionaryKeyPolicy.GetType());
                }
            }

            return key;
        }

        protected static JsonConverter<TValue> GetValueConverter(ref WriteStack state)
        {
            JsonConverter<TValue> converter = (JsonConverter<TValue>)state.Current.DeclaredJsonPropertyInfo!.ConverterBase;
            Debug.Assert(converter != null); // It should not be possible to have a null converter at this point.

            return converter;
        }

        protected static KeyConverter<TKey> GetKeyConverter(JsonClassInfo classInfo) => (KeyConverter<TKey>)classInfo.KeyConverter;

        internal sealed override bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            ref ReadStack state,
            [MaybeNullWhen(false)] out TCollection value)
        {
            // Get the key converter at the very beginning; will throw NSE if there is no converter for TKey.
            // This is performed at the very beginning to avoid processing unsupported types.
            KeyConverter<TKey> keyConverter = GetKeyConverter(state.Current.JsonClassInfo);

            bool shouldReadPreservedReferences = options.ReferenceHandling.ShouldReadPreservedReferences();

            if (!state.SupportContinuation && !shouldReadPreservedReferences)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                CreateCollection(ref state);

                JsonConverter<TValue> elementConverter = GetElementConverter(ref state);
                if (elementConverter.CanUseDirectReadOrWrite)
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

                        if (reader.TokenType != JsonTokenType.PropertyName)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                        }

                        state.Current.JsonPropertyNameAsString = reader.GetString();
                        keyConverter.OnTryRead(ref reader, keyConverter.TypeToConvert, options, ref state, out TKey key);

                        // Read the value and add.
                        reader.ReadWithVerify();
                        TValue element = elementConverter.Read(ref reader, typeof(TValue), options);
                        Add(key, element, options, ref state);
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

                        if (reader.TokenType != JsonTokenType.PropertyName)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                        }

                        state.Current.JsonPropertyNameAsString = reader.GetString()!;
                        keyConverter.OnTryRead(ref reader, keyConverter.TypeToConvert, options, ref state, out TKey key);

                        reader.ReadWithVerify();

                        // Get the value from the converter and add it.
                        elementConverter.TryRead(ref reader, typeof(TValue), options, ref state, out TValue element);
                        Add(key, element, options, ref state);
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
                if (shouldReadPreservedReferences && state.Current.ObjectState < StackFrameObjectState.MetadataPropertyValue)
                {
                    if (JsonSerializer.ResolveMetadata(this, ref reader, ref state))
                    {
                        if (state.Current.ObjectState == StackFrameObjectState.MetadataRefPropertyEndObject)
                        {
                            value = (TCollection)state.Current.ReturnValue!;
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
                    CreateCollection(ref state);

                    if (state.Current.MetadataId != null)
                    {
                        Debug.Assert(CanHaveIdMetadata);

                        value = (TCollection)state.Current.ReturnValue!;
                        if (!state.ReferenceResolver.AddReferenceOnDeserialize(state.Current.MetadataId, value))
                        {
                            ThrowHelper.ThrowJsonException_MetadataDuplicateIdFound(state.Current.MetadataId, ref state);
                        }
                    }

                    state.Current.ObjectState = StackFrameObjectState.CreatedObject;
                }

                // Process all elements.
                JsonConverter<TValue> elementConverter = GetElementConverter(ref state);
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
                    if (state.Current.PropertyState < StackFramePropertyState.Name)
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.PropertyName)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                        }

                        state.Current.PropertyState = StackFramePropertyState.Name;

                        ReadOnlySpan<byte> propertyName = reader.GetSpan();
                        // Verify property doesn't contain metadata.
                        if (shouldReadPreservedReferences)
                        {
                            if (propertyName.Length > 0 && propertyName[0] == '$')
                            {
                                ThrowHelper.ThrowUnexpectedMetadataException(propertyName, ref reader, ref state);
                            }
                        }
                        state.Current.DictionaryKeyName = propertyName.ToArray();
                        state.Current.JsonPropertyNameAsString = reader.GetString();
                    }

                    if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
                    {
                        state.Current.PropertyState = StackFramePropertyState.ReadValue;

                        if (!SingleValueReadWithReadAhead(elementConverter.ClassType, ref reader, ref state))
                        {
                            value = default;
                            return false;
                        }
                    }

                    if (state.Current.PropertyState < StackFramePropertyState.TryRead)
                    {
                        keyConverter.OnTryRead(ref reader, keyConverter.TypeToConvert, options, ref state, out TKey key);

                        // Get the value from the converter and add it.
                        bool success = elementConverter.TryRead(ref reader, typeof(TValue), options, ref state, out TValue element);
                        if (!success)
                        {
                            value = default;
                            return false;
                        }

                        Add(key, element, options, ref state);
                        state.Current.EndElement();
                    }
                }
            }

            ConvertCollection(ref state, options);
            value = (TCollection)state.Current.ReturnValue!;
            return true;
        }

        internal sealed override bool OnTryWrite(
            Utf8JsonWriter writer,
            TCollection dictionary,
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

                if (options.ReferenceHandling.ShouldWritePreservedReferences())
                {
                    if (JsonSerializer.WriteReferenceForObject(this, dictionary, ref state, writer) == MetadataPropertyName.Ref)
                    {
                        return true;
                    }
                }

                state.Current.DeclaredJsonPropertyInfo = state.Current.JsonClassInfo.ElementClassInfo!.PolicyProperty!;
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
