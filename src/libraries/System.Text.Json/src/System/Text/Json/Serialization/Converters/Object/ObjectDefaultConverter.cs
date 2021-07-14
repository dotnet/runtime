// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Default base class implementation of <cref>JsonObjectConverter{T}</cref>.
    /// </summary>
    internal class ObjectDefaultConverter<T> : JsonObjectConverter<T> where T : notnull
    {
        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, [MaybeNullWhen(false)] out T value)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;

            object obj;

            if (state.UseFastPath)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                if (jsonTypeInfo.CreateObject == null)
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(jsonTypeInfo.Type, ref reader, ref state);
                }

                obj = jsonTypeInfo.CreateObject!()!;

                if (obj is IJsonOnDeserializing onDeserializing)
                {
                    onDeserializing.OnDeserializing();
                }

                // Process all properties.
                while (true)
                {
                    // Read the property name or EndObject.
                    reader.ReadWithVerify();

                    JsonTokenType tokenType = reader.TokenType;

                    if (tokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    // Read method would have thrown if otherwise.
                    Debug.Assert(tokenType == JsonTokenType.PropertyName);

                    ReadOnlySpan<byte> unescapedPropertyName = JsonSerializer.GetPropertyName(ref state, ref reader, options);
                    JsonPropertyInfo jsonPropertyInfo = JsonSerializer.LookupProperty(
                        obj,
                        unescapedPropertyName,
                        ref state,
                        options,
                        out bool useExtensionProperty);

                    ReadPropertyValue(obj, ref state, ref reader, jsonPropertyInfo, useExtensionProperty);
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
                if (state.Current.ObjectState < StackFrameObjectState.PropertyValue)
                {
                    if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve)
                    {
                        if (JsonSerializer.ResolveMetadataForJsonObject<T>(ref reader, ref state, options))
                        {
                            if (state.Current.ObjectState == StackFrameObjectState.ReadRefEndObject)
                            {
                                // This will never throw since it was previously validated in ResolveMetadataForJsonObject.
                                value = (T)state.Current.ReturnValue!;
                                return true;
                            }
                        }
                        else
                        {
                            value = default;
                            return false;
                        }
                    }
                }

                if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                {
                    if (jsonTypeInfo.CreateObject == null)
                    {
                        ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(jsonTypeInfo.Type, ref reader, ref state);
                    }

                    obj = jsonTypeInfo.CreateObject!()!;

                    if (obj is IJsonOnDeserializing onDeserializing)
                    {
                        onDeserializing.OnDeserializing();
                    }

                    state.Current.ReturnValue = obj;
                    state.Current.ObjectState = StackFrameObjectState.CreatedObject;
                }
                else
                {
                    obj = state.Current.ReturnValue!;
                    Debug.Assert(obj != null);
                }

                // Process all properties.
                while (true)
                {
                    // Determine the property.
                    if (state.Current.PropertyState == StackFramePropertyState.None)
                    {
                        state.Current.PropertyState = StackFramePropertyState.ReadName;

                        if (!reader.Read())
                        {
                            // The read-ahead functionality will do the Read().
                            state.Current.ReturnValue = obj;
                            value = default;
                            return false;
                        }
                    }

                    JsonPropertyInfo jsonPropertyInfo;

                    if (state.Current.PropertyState < StackFramePropertyState.Name)
                    {
                        state.Current.PropertyState = StackFramePropertyState.Name;

                        JsonTokenType tokenType = reader.TokenType;
                        if (tokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        // Read method would have thrown if otherwise.
                        Debug.Assert(tokenType == JsonTokenType.PropertyName);

                        ReadOnlySpan<byte> unescapedPropertyName = JsonSerializer.GetPropertyName(ref state, ref reader, options);
                        jsonPropertyInfo = JsonSerializer.LookupProperty(
                            obj,
                            unescapedPropertyName,
                            ref state,
                            options,
                            out bool useExtensionProperty);

                        state.Current.UseExtensionProperty = useExtensionProperty;
                    }
                    else
                    {
                        Debug.Assert(state.Current.JsonPropertyInfo != null);
                        jsonPropertyInfo = state.Current.JsonPropertyInfo!;
                    }

                    if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
                    {
                        if (!jsonPropertyInfo.ShouldDeserialize)
                        {
                            if (!reader.TrySkip())
                            {
                                state.Current.ReturnValue = obj;
                                value = default;
                                return false;
                            }

                            state.Current.EndProperty();
                            continue;
                        }

                        if (!ReadAheadPropertyValue(ref state, ref reader, jsonPropertyInfo))
                        {
                            state.Current.ReturnValue = obj;
                            value = default;
                            return false;
                        }
                    }

                    if (state.Current.PropertyState < StackFramePropertyState.TryRead)
                    {
                        // Obtain the CLR value from the JSON and set the member.
                        if (!state.Current.UseExtensionProperty)
                        {
                            if (!jsonPropertyInfo.ReadJsonAndSetMember(obj, ref state, ref reader))
                            {
                                state.Current.ReturnValue = obj;
                                value = default;
                                return false;
                            }
                        }
                        else
                        {
                            if (!jsonPropertyInfo.ReadJsonAndAddExtensionProperty(obj, ref state, ref reader))
                            {
                                // No need to set 'value' here since JsonElement must be read in full.
                                state.Current.ReturnValue = obj;
                                value = default;
                                return false;
                            }
                        }

                        state.Current.EndProperty();
                    }
                }
            }

            if (obj is IJsonOnDeserialized onDeserialized)
            {
                onDeserialized.OnDeserialized();
            }

            // Unbox
            Debug.Assert(obj != null);
            value = (T)obj;

            // Check if we are trying to build the sorted cache.
            if (state.Current.PropertyRefCache != null)
            {
                jsonTypeInfo.UpdateSortedPropertyCache(ref state.Current);
            }

            return true;
        }

        internal sealed override bool OnTryWrite(
            Utf8JsonWriter writer,
            T value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;

            object obj = value; // box once

            if (!state.SupportContinuation)
            {
                writer.WriteStartObject();
                if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve)
                {
                    if (JsonSerializer.WriteReferenceForObject(this, obj, ref state, writer) == MetadataPropertyName.Ref)
                    {
                        return true;
                    }
                }

                if (obj is IJsonOnSerializing onSerializing)
                {
                    onSerializing.OnSerializing();
                }

                List<KeyValuePair<string, JsonPropertyInfo?>> properties = state.Current.JsonTypeInfo.PropertyCache!.List;
                for (int i = 0; i < properties.Count; i++)
                {
                    JsonPropertyInfo jsonPropertyInfo = properties[i].Value!;
                    if (jsonPropertyInfo.ShouldSerialize)
                    {
                        // Remember the current property for JsonPath support if an exception is thrown.
                        state.Current.DeclaredJsonPropertyInfo = jsonPropertyInfo;
                        state.Current.NumberHandling = jsonPropertyInfo.NumberHandling;

                        bool success = jsonPropertyInfo.GetMemberAndWriteJson(obj, ref state, writer);
                        // Converters only return 'false' when out of data which is not possible in fast path.
                        Debug.Assert(success);

                        state.Current.EndProperty();
                    }
                }

                // Write extension data after the normal properties.
                JsonPropertyInfo? dataExtensionProperty = state.Current.JsonTypeInfo.DataExtensionProperty;
                if (dataExtensionProperty?.ShouldSerialize == true)
                {
                    // Remember the current property for JsonPath support if an exception is thrown.
                    state.Current.DeclaredJsonPropertyInfo = dataExtensionProperty;
                    state.Current.NumberHandling = dataExtensionProperty.NumberHandling;

                    bool success = dataExtensionProperty.GetMemberAndWriteJsonExtensionData(obj, ref state, writer);
                    Debug.Assert(success);

                    state.Current.EndProperty();
                }

                writer.WriteEndObject();
            }
            else
            {
                if (!state.Current.ProcessedStartToken)
                {
                    writer.WriteStartObject();
                    if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve)
                    {
                        if (JsonSerializer.WriteReferenceForObject(this, obj, ref state, writer) == MetadataPropertyName.Ref)
                        {
                            return true;
                        }
                    }

                    if (obj is IJsonOnSerializing onSerializing)
                    {
                        onSerializing.OnSerializing();
                    }

                    state.Current.ProcessedStartToken = true;
                }

                List<KeyValuePair<string, JsonPropertyInfo?>>? propertyList = state.Current.JsonTypeInfo.PropertyCache!.List!;
                while (state.Current.EnumeratorIndex < propertyList.Count)
                {
                    JsonPropertyInfo? jsonPropertyInfo = propertyList![state.Current.EnumeratorIndex].Value;
                    Debug.Assert(jsonPropertyInfo != null);
                    if (jsonPropertyInfo.ShouldSerialize)
                    {
                        state.Current.DeclaredJsonPropertyInfo = jsonPropertyInfo;
                        state.Current.NumberHandling = jsonPropertyInfo.NumberHandling;

                        if (!jsonPropertyInfo.GetMemberAndWriteJson(obj!, ref state, writer))
                        {
                            Debug.Assert(jsonPropertyInfo.ConverterBase.ConverterStrategy != ConverterStrategy.Value ||
                                         jsonPropertyInfo.ConverterBase.TypeToConvert == JsonTypeInfo.ObjectType);

                            return false;
                        }

                        state.Current.EndProperty();
                        state.Current.EnumeratorIndex++;

                        if (ShouldFlush(writer, ref state))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        state.Current.EnumeratorIndex++;
                    }
                }

                // Write extension data after the normal properties.
                if (state.Current.EnumeratorIndex == propertyList.Count)
                {
                    JsonPropertyInfo? dataExtensionProperty = state.Current.JsonTypeInfo.DataExtensionProperty;
                    if (dataExtensionProperty?.ShouldSerialize == true)
                    {
                        // Remember the current property for JsonPath support if an exception is thrown.
                        state.Current.DeclaredJsonPropertyInfo = dataExtensionProperty;
                        state.Current.NumberHandling = dataExtensionProperty.NumberHandling;

                        if (!dataExtensionProperty.GetMemberAndWriteJsonExtensionData(obj, ref state, writer))
                        {
                            return false;
                        }

                        state.Current.EndProperty();
                        state.Current.EnumeratorIndex++;

                        if (ShouldFlush(writer, ref state))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        state.Current.EnumeratorIndex++;
                    }
                }

                if (!state.Current.ProcessedEndToken)
                {
                    state.Current.ProcessedEndToken = true;
                    writer.WriteEndObject();
                }
            }

            if (obj is IJsonOnSerialized onSerialized)
            {
                onSerialized.OnSerialized();
            }

            value = (T)obj; // unbox

            return true;
        }

        // AggressiveInlining since this method is only called from two locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ReadPropertyValue(
            object obj,
            ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonPropertyInfo jsonPropertyInfo,
            bool useExtensionProperty)
        {
            // Skip the property if not found.
            if (!jsonPropertyInfo.ShouldDeserialize)
            {
                reader.Skip();
            }
            else
            {
                // Set the property value.
                reader.ReadWithVerify();

                if (!useExtensionProperty)
                {
                    jsonPropertyInfo.ReadJsonAndSetMember(obj, ref state, ref reader);
                }
                else
                {
                    jsonPropertyInfo.ReadJsonAndAddExtensionProperty(obj, ref state, ref reader);
                }
            }

            // Ensure any exception thrown in the next read does not have a property in its JsonPath.
            state.Current.EndProperty();
        }

        protected bool ReadAheadPropertyValue(ref ReadStack state, ref Utf8JsonReader reader, JsonPropertyInfo jsonPropertyInfo)
        {
            // Returning false below will cause the read-ahead functionality to finish the read.
            state.Current.PropertyState = StackFramePropertyState.ReadValue;

            if (!state.Current.UseExtensionProperty)
            {
                if (!SingleValueReadWithReadAhead(jsonPropertyInfo.ConverterBase.ConverterStrategy, ref reader, ref state))
                {
                    return false;
                }
            }
            else
            {
                // The actual converter is JsonElement, so force a read-ahead.
                if (!SingleValueReadWithReadAhead(ConverterStrategy.Value, ref reader, ref state))
                {
                    return false;
                }
            }

            return true;
        }

        internal sealed override void CreateInstanceForReferenceResolver(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
        {
            if (state.Current.JsonTypeInfo.CreateObject == null)
            {
                ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(state.Current.JsonTypeInfo.Type, ref reader, ref state);
            }

            object obj = state.Current.JsonTypeInfo.CreateObject!()!;
            state.Current.ReturnValue = obj;

            if (obj is IJsonOnDeserializing onDeserializing)
            {
                onDeserializing.OnDeserializing();
            }
        }
    }
}
