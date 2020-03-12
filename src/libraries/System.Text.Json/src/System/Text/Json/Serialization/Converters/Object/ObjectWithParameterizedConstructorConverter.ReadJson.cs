// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal abstract partial class ObjectWithParameterizedConstructorConverter<T> : ObjectDefaultConverter<T> where T : notnull
    {
        /// <summary>
        /// This method does a full first pass of the JSON input; deserializes the ctor args and extension
        /// data; and keeps track of the positions of object members in the JSON in the reader.
        /// The properties will be deserialized after object construction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadConstructorArguments(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.StartObject);

            while (true)
            {
                // Read the property name or EndObject.
                reader.Read();

                JsonTokenType tokenType = reader.TokenType;
                if (tokenType == JsonTokenType.EndObject)
                {
                    return;
                }

                if (tokenType != JsonTokenType.PropertyName)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                ReadOnlySpan<byte> escapedPropertyName = reader.GetSpan();
                ReadOnlySpan<byte> unescapedPropertyName;

                if (reader._stringHasEscaping)
                {
                    int idx = escapedPropertyName.IndexOf(JsonConstants.BackSlash);
                    Debug.Assert(idx != -1);
                    unescapedPropertyName = JsonReaderHelper.GetUnescapedSpan(escapedPropertyName, idx);
                }
                else
                {
                    unescapedPropertyName = escapedPropertyName;
                }

                if (!TryLookupConstructorParameterFromFastCache(
                    ref state,
                    unescapedPropertyName,
                    options,
                    out JsonParameterInfo? jsonParameterInfo))
                {
                    if (state.Current.JsonClassInfo.TryGetPropertyFromFastCache(
                        unescapedPropertyName,
                        ref state.Current,
                        out JsonPropertyInfo? jsonPropertyInfo))
                    {
                        Debug.Assert(jsonPropertyInfo != null);

                        HandleProperty(ref state, ref reader, unescapedPropertyName, jsonPropertyInfo, options);

                        // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
                        state.Current.PropertyIndex++;

                        continue;
                    }
                    else if (!TryLookupConstructorParameterFromSlowCache(
                        ref state,
                        unescapedPropertyName,
                        options,
                        out string unescapedPropertyNameAsString,
                        out jsonParameterInfo))
                    {
                        jsonPropertyInfo = state.Current.JsonClassInfo.GetPropertyFromSlowCache(
                            unescapedPropertyName,
                            ref state.Current,
                            unescapedPropertyNameAsString);

                        Debug.Assert(jsonPropertyInfo != null);

                        HandleProperty(ref state, ref reader, unescapedPropertyName, jsonPropertyInfo, options, unescapedPropertyNameAsString);

                        // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
                        state.Current.PropertyIndex++;

                        continue;
                    }
                }

                // Set the property value.
                reader.Read();

                if (!(jsonParameterInfo!.ShouldDeserialize))
                {
                    reader.Skip();
                    state.Current.EndConstructorParameter();
                    continue;
                }

                ReadAndCacheConstructorArgument(ref state, ref reader, jsonParameterInfo, options);

                state.Current.EndConstructorParameter();
            }
        }

        /// <summary>
        /// This method does a full first pass of the JSON input; deserializes the ctor args and extension
        /// data; and keeps track of the positions of object members in the JSON in the reader; in a manner
        /// that supports continuation. The properties will be deserialized after object construction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ReadConstructorArgumentsWithContinuation(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            bool shouldReadPreservedReferences,
            JsonSerializerOptions options)
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
                        return false;
                    }
                }

                JsonParameterInfo? jsonParameterInfo = null;
                JsonPropertyInfo? jsonPropertyInfo = null;

                if (state.Current.PropertyState < StackFramePropertyState.Name)
                {
                    state.Current.PropertyState = StackFramePropertyState.Name;

                    JsonTokenType tokenType = reader.TokenType;
                    if (tokenType == JsonTokenType.EndObject)
                    {
                        // We are done with the first pass.
                        break;
                    }
                    else if (tokenType != JsonTokenType.PropertyName)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                    }

                    ReadOnlySpan<byte> escapedPropertyName = reader.GetSpan();
                    ReadOnlySpan<byte> unescapedPropertyName;

                    if (reader._stringHasEscaping)
                    {
                        int idx = escapedPropertyName.IndexOf(JsonConstants.BackSlash);
                        Debug.Assert(idx != -1);
                        unescapedPropertyName = JsonReaderHelper.GetUnescapedSpan(escapedPropertyName, idx);
                    }
                    else
                    {
                        unescapedPropertyName = escapedPropertyName;
                    }

                    if (shouldReadPreservedReferences)
                    {
                        if (escapedPropertyName.Length > 0 && escapedPropertyName[0] == '$')
                        {
                            ThrowHelper.ThrowUnexpectedMetadataException(escapedPropertyName, ref reader, ref state);
                        }
                    }

                    if (!TryLookupConstructorParameterFromFastCache(
                        ref state,
                        unescapedPropertyName,
                        options,
                        out jsonParameterInfo))
                    {
                        if (state.Current.JsonClassInfo.TryGetPropertyFromFastCache(
                            unescapedPropertyName,
                            ref state.Current,
                            out jsonPropertyInfo))
                        {
                            Debug.Assert(jsonPropertyInfo != null);

                            // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
                            state.Current.PropertyIndex++;

                            if (!HandleDataExtensionWithContinuation(ref state, ref reader, unescapedPropertyName, jsonPropertyInfo, options))
                            {
                                return false;
                            }
                        }
                        else if (!TryLookupConstructorParameterFromSlowCache(
                            ref state,
                            unescapedPropertyName,
                            options,
                            out string unescapedPropertyNameAsString,
                            out jsonParameterInfo))
                        {
                            jsonPropertyInfo = state.Current.JsonClassInfo.GetPropertyFromSlowCache(
                                unescapedPropertyName,
                                ref state.Current,
                                unescapedPropertyNameAsString);

                            Debug.Assert(jsonPropertyInfo != null);

                            // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
                            state.Current.PropertyIndex++;

                            if (!HandleDataExtensionWithContinuation(ref state, ref reader, unescapedPropertyName, jsonPropertyInfo, options, unescapedPropertyNameAsString))
                            {
                                return false;
                            }
                        }
                    }
                }
                else if (state.Current.CtorArgumentState.JsonParameterInfo != null)
                {
                    jsonParameterInfo = state.Current.CtorArgumentState.JsonParameterInfo;
                }
                else
                {
                    Debug.Assert(state.Current.JsonPropertyInfo != null);
                    jsonPropertyInfo = state.Current.JsonPropertyInfo!;
                }

                // Handle either constructor arg or property.
                if (jsonParameterInfo != null)
                {
                    if (!HandleConstructorArgumentWithContinuation(ref state, ref reader, jsonParameterInfo, options))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!HandlePropertyWithContinuation(ref state, ref reader, jsonPropertyInfo!))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandleConstructorArgumentWithContinuation(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonParameterInfo jsonParameterInfo,
            JsonSerializerOptions options)
        {
            if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
            {
                if (!jsonParameterInfo.ShouldDeserialize)
                {
                    if (!reader.TrySkip())
                    {
                        return false;
                    }

                    state.Current.EndConstructorParameter();
                    return true;
                }

                // Returning false below will cause the read-ahead functionality to finish the read.
                state.Current.PropertyState = StackFramePropertyState.ReadValue;

                if (!SingleValueReadWithReadAhead(jsonParameterInfo.ConverterBase.ClassType, ref reader, ref state))
                {
                    return false;
                }
            }

            if (state.Current.PropertyState < StackFramePropertyState.TryRead)
            {
                if (!ReadAndCacheConstructorArgument(ref state, ref reader, jsonParameterInfo, options))
                {
                    return false;
                }
            }

            state.Current.EndConstructorParameter();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandleDataExtensionWithContinuation(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            ReadOnlySpan<byte> unescapedPropertyName,
            JsonPropertyInfo jsonPropertyInfo,
            JsonSerializerOptions options,
            string? unescapedPropertyNameAsString = null)
        {
            bool useExtensionProperty;
            // Determine if we should use the extension property.
            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                JsonPropertyInfo? dataExtProperty = state.Current.JsonClassInfo.DataExtensionProperty;

                if (dataExtProperty != null)
                {
                    if (state.Current.CtorArgumentState.DataExtension == null)
                    {
                        if (dataExtProperty.RuntimeClassInfo.CreateObject == null)
                        {
                            ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(dataExtProperty.DeclaredPropertyType);
                        }

                        state.Current.CtorArgumentState.DataExtension = dataExtProperty.RuntimeClassInfo.CreateObject();
                    }

                    state.Current.JsonPropertyNameAsString = unescapedPropertyNameAsString ?? JsonHelpers.Utf8GetString(unescapedPropertyName);
                    jsonPropertyInfo = dataExtProperty;
                    useExtensionProperty = true;
                }
                else
                {
                    if (!reader.TrySkip())
                    {
                        return false;
                    }

                    state.Current.EndProperty();
                    return true;
                }
            }
            else
            {
                if (jsonPropertyInfo.JsonPropertyName == null)
                {
                    byte[] propertyNameArray = unescapedPropertyName.ToArray();
                    if (options.PropertyNameCaseInsensitive)
                    {
                        // Each payload can have a different name here; remember the value on the temporary stack.
                        state.Current.JsonPropertyName = propertyNameArray;
                    }
                    else
                    {
                        // Prevent future allocs by caching globally on the JsonPropertyInfo which is specific to a Type+PropertyName
                        // so it will match the incoming payload except when case insensitivity is enabled (which is handled above).
                        jsonPropertyInfo.JsonPropertyName = propertyNameArray;
                    }
                }

                useExtensionProperty = false;
            }

            state.Current.JsonPropertyInfo = jsonPropertyInfo;

            state.Current.UseExtensionProperty = useExtensionProperty;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleProperty(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            ReadOnlySpan<byte> unescapedPropertyName,
            JsonPropertyInfo jsonPropertyInfo,
            JsonSerializerOptions options,
            string? unescapedPropertyNameAsString = null)
        {
            bool useExtensionProperty;
            // Determine if we should use the extension property.
            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                JsonPropertyInfo? dataExtProperty = state.Current.JsonClassInfo.DataExtensionProperty;
                if (dataExtProperty != null)
                {
                    if (state.Current.CtorArgumentState.DataExtension == null)
                    {
                        if (dataExtProperty.RuntimeClassInfo.CreateObject == null)
                        {
                            ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(dataExtProperty.DeclaredPropertyType);
                        }

                        state.Current.CtorArgumentState.DataExtension = dataExtProperty.RuntimeClassInfo.CreateObject();
                    }

                    state.Current.JsonPropertyNameAsString = unescapedPropertyNameAsString ?? JsonHelpers.Utf8GetString(unescapedPropertyName);
                    jsonPropertyInfo = dataExtProperty;
                    state.Current.JsonPropertyInfo = jsonPropertyInfo;
                    useExtensionProperty = true;
                }
                else
                {
                    reader.Skip();
                    return;
                }
            }
            else
            {
                if (jsonPropertyInfo.JsonPropertyName == null && !options.PropertyNameCaseInsensitive)
                {
                    // Prevent future allocs by caching globally on the JsonPropertyInfo which is specific to a Type+PropertyName
                    // so it will match the incoming payload except when case insensitivity is enabled (which is handled above).
                    jsonPropertyInfo.JsonPropertyName = unescapedPropertyName.ToArray();
                }

                useExtensionProperty = false;
            }

            if (!jsonPropertyInfo.ShouldDeserialize)
            {
                reader.Skip();
                return;
            }

            if (!useExtensionProperty)
            {
                if (state.Current.CtorArgumentState.FoundProperties == null)
                {
                    state.Current.CtorArgumentState.FoundProperties =
                        ArrayPool<ValueTuple<
                            JsonPropertyInfo,
                            JsonReaderState,
                            long,
                            byte[]?>>.Shared.Rent(state.Current.JsonClassInfo.PropertyCache!.Count);
                }
                else if (state.Current.CtorArgumentState.FoundPropertyCount == state.Current.CtorArgumentState.FoundProperties!.Length)
                {
                    // Rare case where we can't fit all the JSON properties in the rented pool, we have to grow.
                    // This could happen if there are duplicate properties in the JSON.
                    var newCache = ArrayPool<ValueTuple<JsonPropertyInfo, JsonReaderState, long, byte[]?>>.Shared.Rent(
                        state.Current.CtorArgumentState.FoundProperties!.Length * 2);
                    state.Current.CtorArgumentState.FoundProperties!.CopyTo(newCache, 0);

                    ArrayPool<ValueTuple<JsonPropertyInfo, JsonReaderState, long, byte[]?>>.Shared.Return(
                        state.Current.CtorArgumentState.FoundProperties!, clearArray: true);
                    state.Current.CtorArgumentState.FoundProperties = newCache!;
                }

                byte[]? propertyNameArray;

                if (jsonPropertyInfo.JsonPropertyName == null)
                {
                    propertyNameArray = unescapedPropertyName.ToArray();
                }
                else
                {
                    propertyNameArray = null;
                }

                state.Current.CtorArgumentState.FoundProperties![state.Current.CtorArgumentState.FoundPropertyCount++] = (
                    jsonPropertyInfo,
                    reader.CurrentState,
                    reader.BytesConsumed,
                    propertyNameArray);

                reader.Skip();
            }
            else
            {
                reader.Read();

                if (state.Current.JsonClassInfo.DataExtensionIsObject)
                {
                    jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out object? extDataValue);
                    ((IDictionary<string, object>)state.Current.CtorArgumentState.DataExtension!)[state.Current.JsonPropertyNameAsString!] = extDataValue!;
                }
                else
                {
                    jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out JsonElement extDataValue);
                    ((IDictionary<string, JsonElement>)state.Current.CtorArgumentState.DataExtension!)[state.Current.JsonPropertyNameAsString!] = extDataValue!;
                }

                // Ensure any exception thrown in the next read does not have a property in its JsonPath.
                state.Current.EndProperty();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandlePropertyWithContinuation(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonPropertyInfo jsonPropertyInfo)
        {
            if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
            {
                if (!state.Current.UseExtensionProperty)
                {
                    if (!jsonPropertyInfo.ShouldDeserialize)
                    {
                        if (!reader.TrySkip())
                        {
                            return false;
                        }

                        state.Current.EndProperty();
                        return true;
                    }

                    state.Current.PropertyState = StackFramePropertyState.ReadValue;

                    if (state.Current.CtorArgumentState.FoundPropertiesAsync == null)
                    {
                        state.Current.CtorArgumentState.FoundPropertiesAsync =
                            ArrayPool<ValueTuple<
                                JsonPropertyInfo,
                                JsonReaderState,
                                byte[],
                                byte[]?>>.Shared.Rent(state.Current.JsonClassInfo.PropertyCache!.Count);
                    }
                    else if (state.Current.CtorArgumentState.FoundPropertyCount == state.Current.CtorArgumentState.FoundPropertiesAsync!.Length)
                    {
                        // Case where we can't fit all the JSON properties in the rented pool, we have to grow.
                        var newCache = ArrayPool<ValueTuple<JsonPropertyInfo, JsonReaderState, byte[], byte[]?>>.Shared.Rent(
                            state.Current.CtorArgumentState.FoundPropertiesAsync!.Length * 2);
                        state.Current.CtorArgumentState.FoundPropertiesAsync!.CopyTo(newCache, 0);

                        ArrayPool<ValueTuple<JsonPropertyInfo, JsonReaderState, byte[], byte[]?>>.Shared.Return(
                            state.Current.CtorArgumentState.FoundPropertiesAsync!, clearArray: true);
                        state.Current.CtorArgumentState.FoundPropertiesAsync = newCache!;
                    }

                    Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

                    state.Current.CtorArgumentState.ReaderState = reader.CurrentState;

                    // We want to cache all the bytes needed to create the value, so force a read-ahead.
                    if (!SingleValueReadWithReadAhead(ClassType.Value, ref reader, ref state))
                    {
                        return false;
                    }
                }
                else
                {
                    state.Current.PropertyState = StackFramePropertyState.ReadValue;

                    // The actual converter is JsonElement, so force a read-ahead.
                    if (!SingleValueReadWithReadAhead(ClassType.Value, ref reader, ref state))
                    {
                        return false;
                    }
                }
            }

            if (!state.Current.UseExtensionProperty)
            {
                if (state.Current.PropertyState < StackFramePropertyState.ReadValueIsEnd)
                {
                    state.Current.PropertyState = StackFramePropertyState.ReadValueIsEnd;

                    byte[]? propertyNameArray;

                    if (jsonPropertyInfo.JsonPropertyName == null)
                    {
                        Debug.Assert(state.Current.JsonPropertyName != null);
                        propertyNameArray = state.Current.JsonPropertyName;
                    }
                    else
                    {
                        propertyNameArray = null;
                    }

                    state.Current.CtorArgumentState.FoundPropertiesAsync![state.Current.CtorArgumentState.FoundPropertyCount++] = (
                            jsonPropertyInfo,
                            state.Current.CtorArgumentState.ReaderState,
                            reader.OriginalSpan.Slice(checked((int)reader.TokenStartIndex)).ToArray(),
                            propertyNameArray);


                    if (!reader.TrySkip())
                    {
                        return false;
                    }
                }

                if (state.Current.PropertyState < StackFramePropertyState.TryRead)
                {
                    state.Current.EndProperty();
                    return true;
                }
            }

            if (state.Current.PropertyState < StackFramePropertyState.TryRead)
            {
                // Obtain the CLR value from the JSON and set the member.
                if (state.Current.UseExtensionProperty)
                {
                    if (state.Current.JsonClassInfo.DataExtensionIsObject)
                    {
                        if (!jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out object? extDataValue))
                        {
                            return false;
                        }
                        ((IDictionary<string, object>)state.Current.CtorArgumentState.DataExtension!)[state.Current.JsonPropertyNameAsString!] = extDataValue!;
                    }
                    else
                    {
                        if (!jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out JsonElement extDataValue))
                        {
                            return false;
                        }
                        ((IDictionary<string, JsonElement>)state.Current.CtorArgumentState.DataExtension!)[state.Current.JsonPropertyNameAsString!] = extDataValue!;
                    }
                }
            }

            state.Current.EndProperty();
            return true;
        }

        /// <summary>
        /// Lookup the constructor parameter given its name in the reader.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryLookupConstructorParameterFromFastCache(
            ref ReadStack state,
            ReadOnlySpan<byte> unescapedPropertyName,
            JsonSerializerOptions options,
            out JsonParameterInfo? jsonParameterInfo)
        {
            Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object);

            if (state.Current.JsonClassInfo.TryGetParameterFromFastCache(unescapedPropertyName, ref state.Current, out jsonParameterInfo))
            {
                Debug.Assert(jsonParameterInfo != null);
                HandleParameterName(ref state.Current, unescapedPropertyName, jsonParameterInfo, options);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Lookup the constructor parameter given its name in the reader.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryLookupConstructorParameterFromSlowCache(
            ref ReadStack state,
            ReadOnlySpan<byte> unescapedPropertyName,
            JsonSerializerOptions options,
            out string unescapedPropertyNameAsString,
            out JsonParameterInfo? jsonParameterInfo)
        {
            if (state.Current.JsonClassInfo.TryGetParameterFromSlowCache(
                unescapedPropertyName,
                ref state.Current,
                out unescapedPropertyNameAsString,
                out jsonParameterInfo))
            {
                Debug.Assert(jsonParameterInfo != null);
                HandleParameterName(ref state.Current, unescapedPropertyName, jsonParameterInfo, options);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Lookup the constructor parameter given its name in the reader.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleParameterName(ref ReadStackFrame frame, ReadOnlySpan<byte> unescapedPropertyName, JsonParameterInfo jsonParameterInfo, JsonSerializerOptions options)
        {
            // Increment ConstructorParameterIndex so GetProperty() starts with the next parameter the next time this function is called.
            frame.CtorArgumentState.ParameterIndex++;

            // Support JsonException.Path.
            Debug.Assert(
                jsonParameterInfo.JsonPropertyName == null ||
                options.PropertyNameCaseInsensitive ||
                unescapedPropertyName.SequenceEqual(jsonParameterInfo.JsonPropertyName));

            frame.CtorArgumentState.JsonParameterInfo = jsonParameterInfo;

            if (jsonParameterInfo.JsonPropertyName == null)
            {
                byte[] propertyNameArray = unescapedPropertyName.ToArray();
                if (options.PropertyNameCaseInsensitive)
                {
                    // Each payload can have a different name here; remember the value on the temporary stack.
                    frame.JsonPropertyName = propertyNameArray;
                }
                else
                {
                    //Prevent future allocs by caching globally on the JsonPropertyInfo which is specific to a Type+PropertyName
                    // so it will match the incoming payload except when case insensitivity is enabled(which is handled above).
                    frame.CtorArgumentState.JsonParameterInfo.JsonPropertyName = propertyNameArray;
                }
            }

            frame.CtorArgumentState.JsonParameterInfo = jsonParameterInfo;
        }
    }
}
