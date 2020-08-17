// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        protected abstract void Add(in TElement value, ref ReadStack state);
        protected abstract void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options);
        protected virtual void ConvertCollection(ref ReadStack state, JsonSerializerOptions options) { }

        protected static JsonConverter<TElement> GetElementConverter(JsonClassInfo elementClassInfo)
        {
            JsonConverter<TElement> converter = (JsonConverter<TElement>)elementClassInfo.PropertyInfoForClassInfo.ConverterBase;
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
            JsonClassInfo elementClassInfo = state.Current.JsonClassInfo.ElementClassInfo!;

            if (state.UseFastPath)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                CreateCollection(ref reader, ref state, options);

                JsonConverter<TElement> elementConverter = GetElementConverter(elementClassInfo);
                if (elementConverter.CanUseDirectReadOrWrite && state.Current.NumberHandling == null)
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

                bool preserveReferences = options.ReferenceHandler != null;
                if (state.Current.ObjectState == StackFrameObjectState.None)
                {
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        state.Current.ObjectState = StackFrameObjectState.PropertyValue;
                    }
                    else if (preserveReferences)
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
                if (preserveReferences && state.Current.ObjectState < StackFrameObjectState.PropertyValue)
                {
                    if (JsonSerializer.ResolveMetadataForJsonArray(ref reader, ref state, options))
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
                    state.Current.JsonPropertyInfo = state.Current.JsonClassInfo.ElementClassInfo!.PropertyInfoForClassInfo;
                    state.Current.ObjectState = StackFrameObjectState.CreatedObject;
                }

                if (state.Current.ObjectState < StackFrameObjectState.ReadElements)
                {
                    JsonConverter<TElement> elementConverter = GetElementConverter(elementClassInfo);

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

                    // Read the EndObject token for an array with preserve semantics.
                    if (state.Current.ValidateEndTokenOnArray)
                    {
                        if (!reader.Read())
                        {
                            value = default;
                            return false;
                        }
                    }
                }

                if (state.Current.ObjectState < StackFrameObjectState.EndTokenValidation)
                {
                    if (state.Current.ValidateEndTokenOnArray)
                    {
                        if (reader.TokenType != JsonTokenType.EndObject)
                        {
                            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
                            ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(ref state, typeToConvert, reader);
                        }
                    }
                }
            }

            ConvertCollection(ref state, options);
            value = (TCollection)state.Current.ReturnValue!;
            return true;
        }

        internal sealed override bool OnTryWrite(
            Utf8JsonWriter writer,
            TCollection value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            bool success;

            if (value == null)
            {
                writer.WriteNullValue();
                success = true;
            }
            else
            {
                if (!state.Current.ProcessedStartToken)
                {
                    state.Current.ProcessedStartToken = true;

                    if (options.ReferenceHandler == null)
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

        internal sealed override void CreateInstanceForReferenceResolver(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
            => CreateCollection(ref reader, ref state, options);
    }
}
