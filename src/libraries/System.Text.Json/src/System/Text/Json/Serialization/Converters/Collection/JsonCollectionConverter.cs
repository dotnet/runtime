// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Base class for all collections. Collections are assumed to implement <see cref="IEnumerable{T}"/>
    /// or a variant thereof e.g. <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    internal abstract class JsonCollectionConverter<TCollection, TElement> : JsonResumableConverter<TCollection>
    {
        internal sealed override ConverterStrategy ConverterStrategy => ConverterStrategy.Enumerable;
        internal override Type ElementType => typeof(TElement);

        protected abstract void Add(in TElement value, ref ReadStack state);

        /// <summary>
        /// When overridden, create the collection. It may be a temporary collection or the final collection.
        /// </summary>
        protected virtual void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
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
            Debug.Assert(state.Current.ReturnValue is TCollection);
        }

        protected virtual void ConvertCollection(ref ReadStack state, JsonSerializerOptions options) { }

        protected static JsonConverter<TElement> GetElementConverter(JsonTypeInfo elementTypeInfo)
        {
            JsonConverter<TElement> converter = (JsonConverter<TElement>)elementTypeInfo.PropertyInfoForTypeInfo.ConverterBase;
            Debug.Assert(converter != null); // It should not be possible to have a null converter at this point.

            return converter;
        }

        protected static JsonConverter<TElement> GetElementConverter(ref WriteStack state)
        {
            JsonConverter<TElement> converter = (JsonConverter<TElement>)state.Current.JsonPropertyInfo!.ConverterBase;
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
            JsonTypeInfo elementTypeInfo = state.Current.JsonTypeInfo.ElementTypeInfo!;

            if (state.UseFastPath)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                CreateCollection(ref reader, ref state, options);

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
                        elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out TElement? element);
                        Add(element!, ref state);
                    }
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
                    else if (state.CanContainMetadata)
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
                if (state.CanContainMetadata && state.Current.ObjectState < StackFrameObjectState.ReadMetadata)
                {
                    if (!JsonSerializer.TryReadMetadata(this, ref reader, ref state))
                    {
                        value = default;
                        return false;
                    }

                    state.Current.ObjectState = StackFrameObjectState.ReadMetadata;
                }

                if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                {
                    if (state.CanContainMetadata)
                    {
                        JsonSerializer.ValidateMetadataForArrayConverter(this, ref reader, ref state);
                    }

                    if (state.Current.MetadataPropertyNames == MetadataPropertyName.Ref)
                    {
                        value = JsonSerializer.ResolveReferenceId<TCollection>(ref state);
                        return true;
                    }

                    CreateCollection(ref reader, ref state, options);

                    if (state.Current.MetadataPropertyNames.HasFlag(MetadataPropertyName.Id))
                    {
                        Debug.Assert(state.ReferenceId != null);
                        Debug.Assert(options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve);
                        Debug.Assert(state.Current.ReturnValue is TCollection);
                        state.ReferenceResolver.AddReference(state.ReferenceId, state.Current.ReturnValue);
                        state.ReferenceId = null;
                    }

                    state.Current.ObjectState = StackFrameObjectState.CreatedObject;
                }

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
                            if (!elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out TElement? element))
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

                    // Array payload is nested inside a $values metadata property.
                    if (state.Current.MetadataPropertyNames.HasFlag(MetadataPropertyName.Values))
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
                    // Array payload is nested inside a $values metadata property.
                    if (state.Current.MetadataPropertyNames.HasFlag(MetadataPropertyName.Values))
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
                    if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve)
                    {
                        MetadataPropertyName metadata = JsonSerializer.WriteReferenceForCollection(this, ref state, writer);
                        Debug.Assert(metadata != MetadataPropertyName.Ref);
                        state.Current.MetadataPropertyName = metadata;
                    }
                    else
                    {
                        writer.WriteStartArray();
                    }

                    state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;
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
