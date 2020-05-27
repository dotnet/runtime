// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Default base class implementation of <cref>JsonIEnumerableConverter{TCollection, TElement}</cref>.
    /// </summary>
    internal abstract class IEnumerableDefaultConverter<TCollection, TElement>
        : JsonCollectionConverter<TCollection, TElement>
    {
        protected abstract void Add(TElement value, ref ReadStack state);
        protected abstract void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options);
        protected virtual void ConvertCollection(ref ReadStack state, JsonSerializerOptions options) { }

        protected static JsonConverter<TElement> GetElementConverter(ref ReadStack state)
        {
            JsonConverter<TElement> converter = (JsonConverter<TElement>)state.Current.JsonClassInfo.ElementClassInfo!.PropertyInfoForClassInfo.ConverterBase;
            Debug.Assert(converter != null); // It should not be possible to have a null converter at this point.

            return converter;
        }

        protected static JsonConverter<TElement> GetElementConverter(ref WriteStack state)
        {
            JsonConverter<TElement> converter = (JsonConverter<TElement>)state.Current.DeclaredJsonPropertyInfo!.ConverterBase;
            Debug.Assert(converter != null); // It should not be possible to have a null converter at this point.

            return converter;
        }

        internal override bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            ref ReadStack state,
            [MaybeNullWhen(false)] out TCollection value)
        {
            bool shouldReadPreservedReferences = options.ReferenceHandling.ShouldReadPreservedReferences();

            if (!state.SupportContinuation && !shouldReadPreservedReferences)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                CreateCollection(ref reader, ref state, options);

                JsonConverter<TElement> elementConverter = GetElementConverter(ref state);
                if (elementConverter.CanUseDirectReadOrWrite)
                {
                    // Fast path that avoids validation and extra indirection.
                    while (true)
                    {
                        reader.ReadWithVerify();
                        if (reader.TokenType == JsonTokenType.EndArray)
                        {
                            break;
                        }

                        // Obtain the CLR value from the JSON and apply to the object.
                        TElement element = elementConverter.Read(ref reader, elementConverter.TypeToConvert, options);
                        Add(element!, ref state);
                    }
                }
                else
                {
                    // Process all elements.
                    while (true)
                    {
                        reader.ReadWithVerify();
                        if (reader.TokenType == JsonTokenType.EndArray)
                        {
                            break;
                        }

                        // Get the value from the converter and add it.
                        elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out TElement element);
                        Add(element!, ref state);
                    }
                }
            }
            else
            {
                // Slower path that supports continuation and preserved references.

                if (state.Current.ObjectState == StackFrameObjectState.None)
                {
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        state.Current.ObjectState = StackFrameObjectState.PropertyValue;
                    }
                    else if (shouldReadPreservedReferences)
                    {
                        if (reader.TokenType != JsonTokenType.StartObject)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                        }

                        state.Current.ObjectState = StackFrameObjectState.StartToken;
                    }
                    else
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                    }
                }

                // Handle the metadata properties.
                if (shouldReadPreservedReferences && state.Current.ObjectState < StackFrameObjectState.PropertyValue)
                {
                    if (JsonSerializer.ResolveMetadata(this, ref reader, ref state))
                    {
                        if (state.Current.ObjectState == StackFrameObjectState.ReadRefEndObject)
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

                if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                {
                    CreateCollection(ref reader, ref state, options);

                    if (state.Current.MetadataId != null)
                    {
                        value = (TCollection)state.Current.ReturnValue!;
                        if (!state.ReferenceResolver.AddReferenceOnDeserialize(state.Current.MetadataId, value))
                        {
                            ThrowHelper.ThrowJsonException_MetadataDuplicateIdFound(state.Current.MetadataId, ref state);
                        }
                    }

                    state.Current.JsonPropertyInfo = state.Current.JsonClassInfo.ElementClassInfo!.PropertyInfoForClassInfo;
                    state.Current.ObjectState = StackFrameObjectState.CreatedObject;
                }

                if (state.Current.ObjectState < StackFrameObjectState.ReadElements)
                {
                    JsonConverter<TElement> elementConverter = GetElementConverter(ref state);

                    // Process all elements.
                    while (true)
                    {
                        if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
                        {
                            state.Current.PropertyState = StackFramePropertyState.ReadValue;

                            if (!SingleValueReadWithReadAhead(elementConverter.ClassType, ref reader, ref state))
                            {
                                value = default;
                                return false;
                            }
                        }

                        if (state.Current.PropertyState < StackFramePropertyState.ReadValueIsEnd)
                        {
                            if (reader.TokenType == JsonTokenType.EndArray)
                            {
                                break;
                            }

                            state.Current.PropertyState = StackFramePropertyState.ReadValueIsEnd;
                        }

                        if (state.Current.PropertyState < StackFramePropertyState.TryRead)
                        {
                            // Get the value from the converter and add it.
                            if (!elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out TElement element))
                            {
                                value = default;
                                return false;
                            }

                            Add(element!, ref state);

                            // No need to set PropertyState to TryRead since we're done with this element now.
                            state.Current.EndElement();
                        }
                    }

                    state.Current.ObjectState = StackFrameObjectState.ReadElements;
                }

                if (state.Current.ObjectState < StackFrameObjectState.EndToken)
                {
                    state.Current.ObjectState = StackFrameObjectState.EndToken;

                    // Read the EndObject for $values.
                    if (state.Current.MetadataId != null)
                    {
                        if (!reader.Read())
                        {
                            value = default;
                            return false;
                        }

                        if (reader.TokenType != JsonTokenType.EndObject)
                        {
                            if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                state.Current.JsonPropertyName = reader.GetSpan().ToArray();
                            }

                            ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(typeToConvert, reader);
                        }
                    }
                }

                if (state.Current.ObjectState < StackFrameObjectState.EndTokenValidation)
                {
                    if (state.Current.MetadataId != null)
                    {
                        if (reader.TokenType != JsonTokenType.EndObject)
                        {
                            ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(typeToConvert, reader);
                        }
                    }
                }
            }

            ConvertCollection(ref state, options);
            value = (TCollection)state.Current.ReturnValue!;
            return true;
        }

        internal sealed override bool OnTryWrite(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            bool success;

            if (value == null)
            {
                writer.WriteNullValue();
                success = true;
            }
            else
            {
                bool shouldWritePreservedReferences = options.ReferenceHandling.ShouldWritePreservedReferences();

                if (!state.Current.ProcessedStartToken)
                {
                    state.Current.ProcessedStartToken = true;

                    if (!shouldWritePreservedReferences)
                    {
                        writer.WriteStartArray();
                    }
                    else
                    {
                        MetadataPropertyName metadata = JsonSerializer.WriteReferenceForCollection(this, value, ref state, writer);
                        if (metadata == MetadataPropertyName.Ref)
                        {
                            return true;
                        }

                        state.Current.MetadataPropertyName = metadata;
                    }

                    state.Current.DeclaredJsonPropertyInfo = state.Current.JsonClassInfo.ElementClassInfo!.PropertyInfoForClassInfo;
                }

                success = OnWriteResume(writer, value, options, ref state);
                if (success)
                {
                    if (!state.Current.ProcessedEndToken)
                    {
                        state.Current.ProcessedEndToken = true;
                        writer.WriteEndArray();

                        if (state.Current.MetadataPropertyName == MetadataPropertyName.Id)
                        {
                            // Write the EndObject for $values.
                            writer.WriteEndObject();
                        }
                    }
                }
            }

            return success;
        }

        protected abstract bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state);
    }
}
