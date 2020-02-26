// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Default base class implementation of <cref>JsonObjectConverter{T}</cref>.
    /// </summary>
    internal sealed class ObjectDefaultConverter<T> : JsonObjectConverter<T>
    {
        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, [MaybeNullWhen(false)] out T value)
        {
            bool shouldReadPreservedReferences = options.ReferenceHandling.ShouldReadPreservedReferences();
            object obj;

            if (!state.SupportContinuation && !shouldReadPreservedReferences)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                if (state.Current.JsonClassInfo.CreateObject == null)
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeNoParameterlessConstructor(state.Current.JsonClassInfo.Type);
                }

                obj = state.Current.JsonClassInfo.CreateObject!()!;

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

                    if (tokenType != JsonTokenType.PropertyName)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                    }

                    JsonPropertyInfo jsonPropertyInfo = JsonSerializer.LookupProperty(
                        obj,
                        ref reader,
                        options,
                        ref state,
                        out bool useExtensionProperty);

                    // Skip the property if not found.
                    if (!jsonPropertyInfo.ShouldDeserialize)
                    {
                        reader.Skip();
                        state.Current.EndProperty();
                        continue;
                    }

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

                    // Ensure any exception thrown in the next read does not have a property in its JsonPath.
                    state.Current.EndProperty();
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
                if (state.Current.ObjectState < StackFrameObjectState.MetadataPropertyValue)
                {
                    if (shouldReadPreservedReferences)
                    {
                        if (JsonSerializer.ResolveMetadata(this, ref reader, ref state))
                        {
                            if (state.Current.ObjectState == StackFrameObjectState.MetadataRefPropertyEndObject)
                            {
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

                    state.Current.ObjectState = StackFrameObjectState.MetadataPropertyValue;
                }

                if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                {
                    if (state.Current.JsonClassInfo.CreateObject == null)
                    {
                        ThrowHelper.ThrowNotSupportedException_DeserializeNoParameterlessConstructor(state.Current.JsonClassInfo.Type);
                    }

                    obj = state.Current.JsonClassInfo.CreateObject!()!;
                    if (state.Current.MetadataId != null)
                    {
                        if (!state.ReferenceResolver.AddReferenceOnDeserialize(state.Current.MetadataId, obj))
                        {
                            ThrowHelper.ThrowJsonException_MetadataDuplicateIdFound(state.Current.MetadataId, ref state);
                        }
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
                            // We are done reading properties.
                            break;
                        }
                        else if (tokenType != JsonTokenType.PropertyName)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                        }

                        jsonPropertyInfo = JsonSerializer.LookupProperty(
                            obj,
                            ref reader,
                            options,
                            ref state,
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

                        // Returning false below will cause the read-ahead functionality to finish the read.
                        state.Current.PropertyState = StackFramePropertyState.ReadValue;

                        if (!state.Current.UseExtensionProperty)
                        {
                            if (!SingleValueReadWithReadAhead(jsonPropertyInfo.ConverterBase.ClassType, ref reader, ref state))
                            {
                                state.Current.ReturnValue = obj;
                                value = default;
                                return false;
                            }
                        }
                        else
                        {
                            // The actual converter is JsonElement, so force a read-ahead.
                            if (!SingleValueReadWithReadAhead(ClassType.Value, ref reader, ref state))
                            {
                                state.Current.ReturnValue = obj;
                                value = default;
                                return false;
                            }
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

            // Check if we are trying to build the sorted cache.
            if (state.Current.PropertyRefCache != null)
            {
                state.Current.JsonClassInfo.UpdateSortedPropertyCache(ref state.Current);
            }

            value = (T)obj;

            return true;
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
            // Minimize boxing for structs by only boxing once here
            object? objectValue = value;

            if (!state.SupportContinuation)
            {
                if (objectValue == null)
                {
                    writer.WriteNullValue();
                    return true;
                }

                writer.WriteStartObject();

                if (options.ReferenceHandling.ShouldWritePreservedReferences())
                {
                    if (JsonSerializer.WriteReferenceForObject(this, objectValue, ref state, writer) == MetadataPropertyName.Ref)
                    {
                        return true;
                    }
                }

                JsonPropertyInfo? dataExtensionProperty = state.Current.JsonClassInfo.DataExtensionProperty;

                int propertyCount = 0;
                JsonPropertyInfo[]? propertyCacheArray = state.Current.JsonClassInfo.PropertyCacheArray;
                if (propertyCacheArray != null)
                {
                    propertyCount = propertyCacheArray.Length;
                }

                for (int i = 0; i < propertyCount; i++)
                {
                    JsonPropertyInfo jsonPropertyInfo = propertyCacheArray![i];

                    // Remember the current property for JsonPath support if an exception is thrown.
                    state.Current.DeclaredJsonPropertyInfo = jsonPropertyInfo;

                    if (jsonPropertyInfo.ShouldSerialize)
                    {
                        if (jsonPropertyInfo == dataExtensionProperty)
                        {
                            if (!jsonPropertyInfo.GetMemberAndWriteJsonExtensionData(objectValue, ref state, writer))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!jsonPropertyInfo.GetMemberAndWriteJson(objectValue, ref state, writer))
                            {
                                Debug.Assert(jsonPropertyInfo.ConverterBase.ClassType != ClassType.Value);
                                return false;
                            }
                        }
                    }

                    state.Current.EndProperty();
                }

                writer.WriteEndObject();
                return true;
            }
            else
            {
                if (!state.Current.ProcessedStartToken)
                {
                    if (objectValue == null)
                    {
                        writer.WriteNullValue();
                        return true;
                    }

                    writer.WriteStartObject();

                    if (options.ReferenceHandling.ShouldWritePreservedReferences())
                    {
                        if (JsonSerializer.WriteReferenceForObject(this, objectValue, ref state, writer) == MetadataPropertyName.Ref)
                        {
                            return true;
                        }
                    }

                    state.Current.ProcessedStartToken = true;
                }

                JsonPropertyInfo? dataExtensionProperty = state.Current.JsonClassInfo.DataExtensionProperty;

                int propertyCount = 0;
                JsonPropertyInfo[]? propertyCacheArray = state.Current.JsonClassInfo.PropertyCacheArray;
                if (propertyCacheArray != null)
                {
                    propertyCount = propertyCacheArray.Length;
                }

                while (propertyCount > state.Current.EnumeratorIndex)
                {
                    JsonPropertyInfo jsonPropertyInfo = propertyCacheArray![state.Current.EnumeratorIndex];
                    state.Current.DeclaredJsonPropertyInfo = jsonPropertyInfo;

                    if (jsonPropertyInfo.ShouldSerialize)
                    {
                        if (jsonPropertyInfo == dataExtensionProperty)
                        {
                            if (!jsonPropertyInfo.GetMemberAndWriteJsonExtensionData(objectValue!, ref state, writer))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!jsonPropertyInfo.GetMemberAndWriteJson(objectValue!, ref state, writer))
                            {
                                Debug.Assert(jsonPropertyInfo.ConverterBase.ClassType != ClassType.Value);
                                return false;
                            }
                        }
                    }

                    state.Current.EndProperty();
                    state.Current.EnumeratorIndex++;

                    if (ShouldFlush(writer, ref state))
                    {
                        return false;
                    }
                }

                if (!state.Current.ProcessedEndToken)
                {
                    state.Current.ProcessedEndToken = true;
                    writer.WriteEndObject();
                }

                return true;
            }
        }
    }
}
