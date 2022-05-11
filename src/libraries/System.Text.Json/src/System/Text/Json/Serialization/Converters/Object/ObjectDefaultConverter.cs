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
        internal override bool CanHaveMetadata => true;

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, [MaybeNullWhen(false)] out T value)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;

            object obj;

            if (!state.SupportContinuation && !state.Current.CanContainMetadata)
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
                        value = JsonSerializer.ResolveReferenceId<T>(ref state);
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
                    value = (T)objectResult!;
                    state.ExitPolymorphicConverter(success);
                    return success;
                }

                if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                {
                    if (state.Current.CanContainMetadata)
                    {
                        JsonSerializer.ValidateMetadataForObjectConverter(this, ref reader, ref state);
                    }

                    if (state.Current.MetadataPropertyNames == MetadataPropertyName.Ref)
                    {
                        value = JsonSerializer.ResolveReferenceId<T>(ref state);
                        return true;
                    }

                    if (jsonTypeInfo.CreateObject == null)
                    {
                        ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(jsonTypeInfo.Type, ref reader, ref state);
                    }

                    obj = jsonTypeInfo.CreateObject!()!;

                    if (state.Current.MetadataPropertyNames.HasFlag(MetadataPropertyName.Id))
                    {
                        Debug.Assert(state.ReferenceId != null);
                        Debug.Assert(options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve);
                        state.ReferenceResolver.AddReference(state.ReferenceId, obj);
                        state.ReferenceId = null;
                    }

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

                if (state.CurrentContainsMetadata && CanHaveMetadata)
                {
                    JsonSerializer.WriteMetadataForObject(this, ref state, writer);
                }

                if (obj is IJsonOnSerializing onSerializing)
                {
                    onSerializing.OnSerializing();
                }

                List<KeyValuePair<string, JsonPropertyInfo?>> properties = jsonTypeInfo.PropertyCache!.List;
                for (int i = 0; i < properties.Count; i++)
                {
                    JsonPropertyInfo jsonPropertyInfo = properties[i].Value!;
                    if (jsonPropertyInfo.ShouldSerialize)
                    {
                        // Remember the current property for JsonPath support if an exception is thrown.
                        state.Current.JsonPropertyInfo = jsonPropertyInfo;
                        state.Current.NumberHandling = jsonPropertyInfo.EffectiveNumberHandling;

                        bool success = jsonPropertyInfo.GetMemberAndWriteJson(obj, ref state, writer);
                        // Converters only return 'false' when out of data which is not possible in fast path.
                        Debug.Assert(success);

                        state.Current.EndProperty();
                    }
                }

                // Write extension data after the normal properties.
                JsonPropertyInfo? dataExtensionProperty = jsonTypeInfo.DataExtensionProperty;
                if (dataExtensionProperty?.ShouldSerialize == true)
                {
                    // Remember the current property for JsonPath support if an exception is thrown.
                    state.Current.JsonPropertyInfo = dataExtensionProperty;
                    state.Current.NumberHandling = dataExtensionProperty.EffectiveNumberHandling;

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

                    if (state.CurrentContainsMetadata && CanHaveMetadata)
                    {
                        JsonSerializer.WriteMetadataForObject(this, ref state, writer);
                    }

                    if (obj is IJsonOnSerializing onSerializing)
                    {
                        onSerializing.OnSerializing();
                    }

                    state.Current.ProcessedStartToken = true;
                }

                List<KeyValuePair<string, JsonPropertyInfo?>>? propertyList = jsonTypeInfo.PropertyCache!.List!;
                while (state.Current.EnumeratorIndex < propertyList.Count)
                {
                    JsonPropertyInfo? jsonPropertyInfo = propertyList![state.Current.EnumeratorIndex].Value;
                    Debug.Assert(jsonPropertyInfo != null);
                    if (jsonPropertyInfo.ShouldSerialize)
                    {
                        state.Current.JsonPropertyInfo = jsonPropertyInfo;
                        state.Current.NumberHandling = jsonPropertyInfo.EffectiveNumberHandling;

                        if (!jsonPropertyInfo.GetMemberAndWriteJson(obj!, ref state, writer))
                        {
                            Debug.Assert(jsonPropertyInfo.ConverterBase.ConverterStrategy != ConverterStrategy.Value);
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
                    JsonPropertyInfo? dataExtensionProperty = jsonTypeInfo.DataExtensionProperty;
                    if (dataExtensionProperty?.ShouldSerialize == true)
                    {
                        // Remember the current property for JsonPath support if an exception is thrown.
                        state.Current.JsonPropertyInfo = dataExtensionProperty;
                        state.Current.NumberHandling = dataExtensionProperty.EffectiveNumberHandling;

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

            return true;
        }

        // AggressiveInlining since this method is only called from two locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ReadPropertyValue(
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

        protected static bool ReadAheadPropertyValue(ref ReadStack state, ref Utf8JsonReader reader, JsonPropertyInfo jsonPropertyInfo)
        {
            // Returning false below will cause the read-ahead functionality to finish the read.
            state.Current.PropertyState = StackFramePropertyState.ReadValue;

            if (!state.Current.UseExtensionProperty)
            {
                if (!SingleValueReadWithReadAhead(jsonPropertyInfo.ConverterBase.RequiresReadAhead, ref reader, ref state))
                {
                    return false;
                }
            }
            else
            {
                // The actual converter is JsonElement, so force a read-ahead.
                if (!SingleValueReadWithReadAhead(requiresReadAhead: true, ref reader, ref state))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
