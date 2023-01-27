// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Base class for all collections. Collections are assumed to implement <see cref="IEnumerable{T}"/>
    /// or a variant thereof e.g. <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    internal abstract class JsonCollectionConverter<TCollection, TElement, IntermediateType> : JsonAdvancedConverter<TCollection, IntermediateType>
    {
        private protected sealed override ConverterStrategy GetDefaultConverterStrategy() => ConverterStrategy.Enumerable;
        internal override Type ElementType => typeof(TElement);

        private protected abstract void Add(ref IntermediateType collection, in TElement value, JsonTypeInfo collectionTypeInfo);

        protected static JsonConverter<TElement> GetElementConverter(JsonTypeInfo elementTypeInfo)
        {
            return ((JsonTypeInfo<TElement>)elementTypeInfo).EffectiveConverter;
        }

        protected static JsonConverter<TElement> GetElementConverter(ref WriteStack state)
        {
            Debug.Assert(state.Current.JsonPropertyInfo != null);
            return (JsonConverter<TElement>)state.Current.JsonPropertyInfo.EffectiveConverter;
        }

        internal override bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            scoped ref ReadStack state,
            [MaybeNullWhen(false)] out TCollection value)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
            JsonTypeInfo elementTypeInfo = jsonTypeInfo.ElementTypeInfo!;
            IntermediateType? obj;

            if (!state.SupportContinuation && !state.Current.CanContainMetadata)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartArray)
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
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        state.Current.ObjectState = StackFrameObjectState.ReadMetadata;
                    }
                    else if (state.Current.CanContainMetadata)
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

                    state.Current.JsonPropertyInfo = elementTypeInfo.PropertyInfoForTypeInfo;
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
                        value = JsonSerializer.ResolveReferenceId<TCollection>(ref state);
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
                    value = (TCollection)objectResult!;
                    state.ExitPolymorphicConverter(success);
                    return success;
                }

                if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                {
                    if (state.Current.CanContainMetadata)
                    {
                        JsonSerializer.ValidateMetadataForArrayConverter(this, ref reader, ref state);
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
                    obj = (IntermediateType?)state.Current.ReturnValue;
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
            JsonTypeInfo elementTypeInfo = jsonTypeInfo.ElementTypeInfo!;

            // needed for exception path to be reported correctly
            state.Current.ReturnValue = obj;

            if (!state.SupportContinuation && !state.Current.CanContainMetadata)
            {
                state.Current.JsonPropertyInfo = elementTypeInfo.PropertyInfoForTypeInfo;
                JsonConverter<TElement> elementConverter = GetElementConverter(elementTypeInfo);
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
                        TElement? element = elementConverter.Read(ref reader, elementConverter.TypeToConvert, options);
                        Add(ref obj, element!, jsonTypeInfo);

                        // needed for exception path to be reported correctly for value types
                        state.Current.ReturnValue = obj;
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
                        elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out TElement? element);
                        Add(ref obj, element!, jsonTypeInfo);

                        // needed for exception path to be reported correctly for value types
                        state.Current.ReturnValue = obj;
                    }
                }
            }
            else
            {
                if (state.Current.ObjectState < StackFrameObjectState.ReadElements)
                {
                    JsonConverter<TElement> elementConverter = GetElementConverter(elementTypeInfo);

                    // Process all elements.
                    while (true)
                    {
                        if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
                        {
                            state.Current.PropertyState = StackFramePropertyState.ReadValue;

                            if (!SingleValueReadWithReadAhead(elementConverter.RequiresReadAhead, ref reader, ref state))
                            {
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
                            if (!elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out TElement? element))
                            {
                                return false;
                            }

                            Add(ref obj, element!, jsonTypeInfo);

                            // No need to set PropertyState to TryRead since we're done with this element now.
                            state.Current.EndElement();
                        }
                    }

                    state.Current.ObjectState = StackFrameObjectState.ReadElements;
                }

                if (state.Current.ObjectState < StackFrameObjectState.EndToken)
                {
                    state.Current.ObjectState = StackFrameObjectState.EndToken;

                    // Array payload is nested inside a $values metadata property.
                    if (state.Current.MetadataPropertyNames.HasFlag(MetadataPropertyName.Values))
                    {
                        if (!reader.Read())
                        {
                            return false;
                        }
                    }
                }

                if (state.Current.ObjectState < StackFrameObjectState.EndTokenValidation)
                {
                    // Array payload is nested inside a $values metadata property.
                    if (state.Current.MetadataPropertyNames.HasFlag(MetadataPropertyName.Values))
                    {
                        if (reader.TokenType != JsonTokenType.EndObject)
                        {
                            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
                            ThrowHelper.ThrowJsonException_MetadataInvalidPropertyInArrayMetadata(ref state, TypeToConvert, reader);
                        }
                    }
                }
            }

            return true;
        }

        internal override bool OnTryWrite(
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

                    if (state.CurrentContainsMetadata && CanHaveMetadata)
                    {
                        state.Current.MetadataPropertyName = JsonSerializer.WriteMetadataForCollection(this, ref state, writer);
                    }

                    // Writing the start of the array must happen after any metadata
                    writer.WriteStartArray();
                    state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;
                }

                success = OnWriteResume(writer, value, options, ref state);
                if (success)
                {
                    if (!state.Current.ProcessedEndToken)
                    {
                        state.Current.ProcessedEndToken = true;
                        writer.WriteEndArray();

                        if (state.Current.MetadataPropertyName != 0)
                        {
                            // Write the EndObject for $values.
                            writer.WriteEndObject();
                        }
                    }
                }
            }

            return success;
        }
    }
}
