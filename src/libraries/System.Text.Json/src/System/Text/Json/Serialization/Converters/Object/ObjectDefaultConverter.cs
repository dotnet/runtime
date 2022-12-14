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
        internal override bool SupportsCreateObjectDelegate => true;

        internal sealed override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, [MaybeNullWhen(false)] out T? value)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
            object? obj;

            if (!state.SupportContinuation && !state.Current.CanContainMetadata)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartObject)
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
                    ResolvePolymorphicConverter(jsonTypeInfo, ref state) is JsonConverter polymorphicConverter)
                {
                    Debug.Assert(!IsValueType);
                    bool success = polymorphicConverter.OnTryReadAsObject(ref reader, polymorphicConverter.TypeToConvert, options, ref state, out object? objectResult);
                    value = (T)objectResult!;
                    state.ExitPolymorphicConverter(success);
                    return success;
                }

                // Handle metadata post polymorphic dispatch
                if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                {
                    // TODO: should we have this here?
                    //if (state.Current.CanContainMetadata)
                    //{
                    //    JsonSerializer.ValidateMetadataForObjectConverter(ref state);
                    //}

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
                    obj = state.Current.ReturnValue!;
                    Debug.Assert(obj is T);
                }

                if (!TryPopulate(ref reader, options, ref state, ref obj))
                {
                    value = default;
                    return false;
                }
            }

            return TryConvert(ref reader, jsonTypeInfo, ref state, obj, out value);
        }

        private protected override bool TryConvert(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, object obj, out T value)
        {
            // Check if we are trying to build the sorted cache.
            if (state.Current.PropertyRefCache != null)
            {
                jsonTypeInfo.UpdateSortedPropertyCache(ref state.Current);
            }

            value = (T)obj;
            return true;
        }

        private protected override bool TryPopulate(ref Utf8JsonReader reader, JsonSerializerOptions options, scoped ref ReadStack state, ref object obj)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;

            state.Current.EnsureInitializedRequiredPropertiesValidationState(jsonTypeInfo);
            jsonTypeInfo.OnDeserializing?.Invoke(obj);

            if (TryPopulateCore(ref reader, options, ref state, obj))
            {
                jsonTypeInfo.OnDeserialized?.Invoke(obj);
                state.Current.ValidateAllRequiredPropertiesAreRead(jsonTypeInfo);
                return true;
            }

            return false;
        }

        private protected virtual bool TryPopulateCore(ref Utf8JsonReader reader, JsonSerializerOptions options, scoped ref ReadStack state, object obj)
        {
            if (!state.SupportContinuation && !state.Current.CanContainMetadata)
            {
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

                    ReadOnlySpan<byte> unescapedPropertyName = JsonSerializer.GetPropertyName(ref state, ref reader);
                    JsonPropertyInfo jsonPropertyInfo = JsonSerializer.LookupProperty(
                        obj,
                        unescapedPropertyName,
                        ref state,
                        options,
                        out bool useExtensionProperty);

                    ReadPropertyValue(obj, ref state, ref reader, jsonPropertyInfo, useExtensionProperty);
                }

                return true;
            }
            else
            {
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

                        ReadOnlySpan<byte> unescapedPropertyName = JsonSerializer.GetPropertyName(ref state, ref reader);
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
                        if (!jsonPropertyInfo.CanDeserialize)
                        {
                            if (!reader.TrySkip())
                            {
                                state.Current.ReturnValue = obj;
                                return false;
                            }

                            state.Current.EndProperty();
                            continue;
                        }

                        if (!ReadAheadPropertyValue(ref state, ref reader, jsonPropertyInfo))
                        {
                            state.Current.ReturnValue = obj;
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
                                return false;
                            }
                        }
                        else
                        {
                            if (!jsonPropertyInfo.ReadJsonAndAddExtensionProperty(obj, ref state, ref reader))
                            {
                                // No need to set 'value' here since JsonElement must be read in full.
                                state.Current.ReturnValue = obj;
                                return false;
                            }
                        }

                        state.Current.EndProperty();
                    }
                }
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

                jsonTypeInfo.OnSerializing?.Invoke(obj);

                List<KeyValuePair<string, JsonPropertyInfo>> properties = jsonTypeInfo.PropertyCache!.List;
                for (int i = 0; i < properties.Count; i++)
                {
                    JsonPropertyInfo jsonPropertyInfo = properties[i].Value;
                    if (jsonPropertyInfo.CanSerialize)
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
                JsonPropertyInfo? extensionDataProperty = jsonTypeInfo.ExtensionDataProperty;
                if (extensionDataProperty?.CanSerialize == true)
                {
                    // Remember the current property for JsonPath support if an exception is thrown.
                    state.Current.JsonPropertyInfo = extensionDataProperty;
                    state.Current.NumberHandling = extensionDataProperty.EffectiveNumberHandling;

                    bool success = extensionDataProperty.GetMemberAndWriteJsonExtensionData(obj, ref state, writer);
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

                    jsonTypeInfo.OnSerializing?.Invoke(obj);

                    state.Current.ProcessedStartToken = true;
                }

                List<KeyValuePair<string, JsonPropertyInfo>> propertyList = jsonTypeInfo.PropertyCache!.List;
                while (state.Current.EnumeratorIndex < propertyList.Count)
                {
                    JsonPropertyInfo jsonPropertyInfo = propertyList[state.Current.EnumeratorIndex].Value;
                    if (jsonPropertyInfo.CanSerialize)
                    {
                        state.Current.JsonPropertyInfo = jsonPropertyInfo;
                        state.Current.NumberHandling = jsonPropertyInfo.EffectiveNumberHandling;

                        if (!jsonPropertyInfo.GetMemberAndWriteJson(obj!, ref state, writer))
                        {
                            Debug.Assert(jsonPropertyInfo.EffectiveConverter.ConverterStrategy != ConverterStrategy.Value);
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
                    JsonPropertyInfo? extensionDataProperty = jsonTypeInfo.ExtensionDataProperty;
                    if (extensionDataProperty?.CanSerialize == true)
                    {
                        // Remember the current property for JsonPath support if an exception is thrown.
                        state.Current.JsonPropertyInfo = extensionDataProperty;
                        state.Current.NumberHandling = extensionDataProperty.EffectiveNumberHandling;

                        if (!extensionDataProperty.GetMemberAndWriteJsonExtensionData(obj, ref state, writer))
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

            jsonTypeInfo.OnSerialized?.Invoke(obj);

            return true;
        }

        // AggressiveInlining since this method is only called from two locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ReadPropertyValue(
            object obj,
            scoped ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonPropertyInfo jsonPropertyInfo,
            bool useExtensionProperty)
        {
            // Skip the property if not found.
            if (!jsonPropertyInfo.CanDeserialize)
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

        protected static bool ReadAheadPropertyValue(scoped ref ReadStack state, ref Utf8JsonReader reader, JsonPropertyInfo jsonPropertyInfo)
        {
            // Returning false below will cause the read-ahead functionality to finish the read.
            state.Current.PropertyState = StackFramePropertyState.ReadValue;

            if (!state.Current.UseExtensionProperty)
            {
                if (!SingleValueReadWithReadAhead(jsonPropertyInfo.EffectiveConverter.RequiresReadAhead, ref reader, ref state))
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
