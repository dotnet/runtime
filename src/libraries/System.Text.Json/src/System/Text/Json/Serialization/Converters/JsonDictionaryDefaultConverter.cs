// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Default base class implementation of <cref>JsonDictionaryConverter{TCollection}</cref> .
    /// </summary>
    internal abstract class JsonDictionaryDefaultConverter<TCollection, TValue>
        : JsonDictionaryConverter<TCollection>
    {
        /// <summary>
        /// When overridden, adds the value to the collection.
        /// </summary>
        protected abstract void Add(TValue value, JsonSerializerOptions options, ref ReadStack state);

        /// <summary>
        /// When overridden, converts the temporary collection held in state.ReturnValue to the final collection.
        /// This is used with immutable collections.
        /// </summary>
        protected virtual void ConvertCollection(ref ReadStack state, JsonSerializerOptions options) { }

        /// <summary>
        /// When overridden, create the collection. It may be a temporary collection or the final collection.
        /// </summary>
        protected virtual void CreateCollection(ref ReadStack state) { }

        internal override Type ElementType => typeof(TValue);

        protected static JsonConverter<TValue> GetElementConverter(ref ReadStack state)
        {
            JsonConverter<TValue>? converter = state.Current.JsonClassInfo.ElementClassInfo!.PolicyProperty!.ConverterBase as JsonConverter<TValue>;
            if (converter == null)
            {
                state.Current.JsonClassInfo.ElementClassInfo.PolicyProperty.ThrowCollectionNotSupportedException();
            }

            return converter!;
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

        protected JsonConverter<TValue> GetValueConverter(ref WriteStack state)
        {
            JsonConverter<TValue> converter = (JsonConverter<TValue>)state.Current.DeclaredJsonPropertyInfo.ConverterBase;
            if (converter == null)
            {
                state.Current.JsonClassInfo.ElementClassInfo!.PolicyProperty!.ThrowCollectionNotSupportedException();
            }

            return converter!;
        }

        internal override sealed bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            ref ReadStack state,
            out TCollection value)
        {
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
                    while (true)
                    {
                        // Read the key name.
                        reader.Read();

                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.PropertyName)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                        }

                        state.Current.KeyName = reader.GetString();

                        // Read the value and add.
                        reader.Read();
                        TValue element = elementConverter.Read(ref reader, typeof(TValue), options);
                        Add(element, options, ref state);
                    }
                }
                else
                {
                    while (true)
                    {
                        // Read the key name.
                        reader.Read();

                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.PropertyName)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                        }

                        state.Current.KeyName = reader.GetString();

                        // Read the value and add.
                        reader.Read();
                        elementConverter.TryRead(ref reader, typeof(TValue), options, ref state, out TValue element);
                        Add(element, options, ref state);
                    }
                }
            }
            else
            {
                if (state.Current.ObjectState < StackFrameObjectState.StartToken)
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                    }

                    state.Current.ObjectState = StackFrameObjectState.StartToken;
                }

                // Handle the metadata properties.
                if (shouldReadPreservedReferences && state.Current.ObjectState < StackFrameObjectState.MetataPropertyValue)
                {
                    if (this.ResolveMetadata(ref reader, ref state, out value))
                    {
                        if (state.Current.ObjectState == StackFrameObjectState.MetadataRefPropertyEndObject)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                {
                    CreateCollection(ref state);

                    if (state.Current.MetadataId != null)
                    {
                        if (!CanHaveMetadata)
                        {
                            ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(TypeToConvert);
                        }

                        value = (TCollection)state.Current.ReturnValue!;
                        if (!state.ReferenceResolver.AddReferenceOnDeserialize(state.Current.MetadataId, value))
                        {
                            // Reset so JsonPath throws exception with $id in it.
                            state.Current.MetadataPropertyName = MetadataPropertyName.Id;

                            ThrowHelper.ThrowJsonException_MetadataDuplicateIdFound(state.Current.MetadataId);
                        }
                    }

                    state.Current.ObjectState = StackFrameObjectState.CreatedObject;
                }

                JsonConverter<TValue> elementConverter = GetElementConverter(ref state);
                while (true)
                {
                    if (state.Current.PropertyState < StackFramePropertyState.ReadName)
                    {
                        state.Current.PropertyState = StackFramePropertyState.ReadName;

                        // Read the key name.
                        if (!reader.Read())
                        {
                            value = default!;
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

                        // Verify property doesn't contain metadata.
                        if (shouldReadPreservedReferences)
                        {
                            ReadOnlySpan<byte> propertyName = JsonSerializer.GetSpan(ref reader);
                            if (propertyName.Length > 0 && propertyName[0] == '$')
                            {
                                ThrowHelper.ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(propertyName, ref state, reader);
                            }
                        }

                        state.Current.KeyName = reader.GetString();
                    }

                    if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
                    {
                        state.Current.PropertyState = StackFramePropertyState.ReadValue;

                        if (!SingleValueReadWithReadAhead(elementConverter.ClassType, ref reader, ref state))
                        {
                            value = default!;
                            return false;
                        }
                    }

                    if (state.Current.PropertyState < StackFramePropertyState.TryRead)
                    {
                        // Read the value and add.
                        bool success = elementConverter.TryRead(ref reader, typeof(TValue), options, ref state, out TValue element);
                        if (!success)
                        {
                            value = default!;
                            return false;
                        }

                        Add(element, options, ref state);
                        state.Current.EndElement();
                    }
                }
            }

            ConvertCollection(ref state, options);
            value = (TCollection)state.Current.ReturnValue!;
            return true;
        }

        internal override sealed bool OnTryWrite(
            Utf8JsonWriter writer,
            TCollection dictionary,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            bool success;

            if (dictionary == null)
            {
                writer.WriteNullValue();
                success = true;
            }
            else
            {
                if (!state.Current.ProcessedStartToken)
                {
                    state.Current.ProcessedStartToken = true;
                    writer.WriteStartObject();

                    if (options.ReferenceHandling.ShouldWritePreservedReferences())
                    {
                        if (JsonSerializer.WriteReferenceForObject(this, dictionary, ref state, writer) == MetadataPropertyName.Ref)
                        {
                            writer.WriteEndObject();
                            return true;
                        }
                    }

                    state.Current.DeclaredJsonPropertyInfo = state.Current.JsonClassInfo.ElementClassInfo!.PolicyProperty!;
                }

                success = OnWriteResume(writer, dictionary, options, ref state);
                if (success)
                {
                    if (!state.Current.ProcessedEndToken)
                    {
                        state.Current.ProcessedEndToken = true;
                        writer.WriteEndObject();
                    }
                }
            }

            return success;
        }
    }
}
