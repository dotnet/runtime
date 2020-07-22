// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using FoundProperties = System.ValueTuple<System.Text.Json.JsonPropertyInfo, System.Text.Json.JsonReaderState, long, byte[]?, string?>;
using FoundPropertiesAsync = System.ValueTuple<System.Text.Json.JsonPropertyInfo, object?, string?>;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal abstract partial class ObjectWithParameterizedConstructorConverter<T> : ObjectDefaultConverter<T> where T : notnull
    {
        internal sealed override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, [MaybeNullWhen(false)] out T value)
        {
            object obj;

            if (state.UseFastPath)
            {
                // Fast path that avoids maintaining state variables.

                ReadOnlySpan<byte> originalSpan = reader.OriginalSpan;

                ReadConstructorArguments(ref state, ref reader, options);

                obj = CreateObject(ref state.Current);

                if (state.Current.PropertyIndex > 0)
                {
                    Utf8JsonReader tempReader;

                    for (int i = 0; i < state.Current.PropertyIndex; i++)
                    {
                        JsonPropertyInfo jsonPropertyInfo = state.Current.CtorArgumentState!.FoundProperties![i].Item1;
                        long resumptionByteIndex = state.Current.CtorArgumentState.FoundProperties[i].Item3;
                        byte[]? propertyNameArray = state.Current.CtorArgumentState.FoundProperties[i].Item4;
                        string? dataExtKey = state.Current.CtorArgumentState.FoundProperties[i].Item5;

                        tempReader = new Utf8JsonReader(
                            originalSpan.Slice(checked((int)resumptionByteIndex)),
                            isFinalBlock: true,
                            state: state.Current.CtorArgumentState.FoundProperties[i].Item2);

                        Debug.Assert(tempReader.TokenType == JsonTokenType.PropertyName);

                        state.Current.JsonPropertyName = propertyNameArray;
                        state.Current.JsonPropertyInfo = jsonPropertyInfo;

                        bool useExtensionProperty = dataExtKey != null;

                        if (useExtensionProperty)
                        {
                            Debug.Assert(jsonPropertyInfo == state.Current.JsonClassInfo.DataExtensionProperty);
                            state.Current.JsonPropertyNameAsString = dataExtKey;
                            JsonSerializer.CreateDataExtensionProperty(obj, jsonPropertyInfo);
                        }

                        ReadPropertyValue(obj, ref state, ref tempReader, jsonPropertyInfo, useExtensionProperty);
                    }

                    ArrayPool<FoundProperties>.Shared.Return(state.Current.CtorArgumentState!.FoundProperties!, clearArray: true);
                }
            }
            else
            {
                // Slower path that supports continuation.

                if (state.Current.ObjectState == StackFrameObjectState.None)
                {
                    state.Current.ObjectState = StackFrameObjectState.StartToken;
                    BeginRead(ref state, ref reader,  options);
                }

                if (!ReadConstructorArgumentsWithContinuation(ref state, ref reader, options))
                {
                    value = default;
                    return false;
                }

                obj = CreateObject(ref state.Current);

                if (state.Current.CtorArgumentState!.FoundPropertyCount > 0)
                {
                    // Set the properties we've parsed so far.
                    for (int i = 0; i < state.Current.CtorArgumentState!.FoundPropertyCount; i++)
                    {
                        JsonPropertyInfo jsonPropertyInfo = state.Current.CtorArgumentState!.FoundPropertiesAsync![i].Item1;
                        object? propValue = state.Current.CtorArgumentState!.FoundPropertiesAsync![i].Item2;
                        string? dataExtKey = state.Current.CtorArgumentState!.FoundPropertiesAsync![i].Item3;

                        if (dataExtKey == null)
                        {
                            jsonPropertyInfo.SetExtensionDictionaryAsObject(obj, propValue);
                        }
                        else
                        {
                            Debug.Assert(jsonPropertyInfo == state.Current.JsonClassInfo.DataExtensionProperty);

                            JsonSerializer.CreateDataExtensionProperty(obj, jsonPropertyInfo);
                            object extDictionary = jsonPropertyInfo.GetValueAsObject(obj)!;

                            if (extDictionary is IDictionary<string, JsonElement> dict)
                            {
                                dict[dataExtKey] = (JsonElement)propValue!;
                            }
                            else
                            {
                                ((IDictionary<string, object>)extDictionary)[dataExtKey] = propValue!;
                            }
                        }
                    }

                    ArrayPool<FoundPropertiesAsync>.Shared.Return(state.Current.CtorArgumentState!.FoundPropertiesAsync!, clearArray: true);
                }
            }

            // Check if we are trying to build the sorted cache.
            if (state.Current.PropertyRefCache != null)
            {
                state.Current.JsonClassInfo.UpdateSortedPropertyCache(ref state.Current);
            }

            // Check if we are trying to build the sorted parameter cache.
            if (state.Current.CtorArgumentState!.ParameterRefCache != null)
            {
                state.Current.JsonClassInfo.UpdateSortedParameterCache(ref state.Current);
            }

            EndRead(ref state);

            value = (T)obj;

            return true;
        }

        protected abstract void InitializeConstructorArgumentCaches(ref ReadStack state, JsonSerializerOptions options);

        protected abstract bool ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo);

        protected abstract object CreateObject(ref ReadStackFrame frame);

        /// <summary>
        /// Performs a full first pass of the JSON input and deserializes the ctor args.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadConstructorArguments(ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            BeginRead(ref state, ref reader, options);

            while (true)
            {
                // Read the next property name or EndObject.
                reader.ReadWithVerify();

                JsonTokenType tokenType = reader.TokenType;

                if (tokenType == JsonTokenType.EndObject)
                {
                    return;
                }

                // Read method would have thrown if otherwise.
                Debug.Assert(tokenType == JsonTokenType.PropertyName);

                if (TryLookupConstructorParameter(ref state, ref reader, options, out JsonParameterInfo? jsonParameterInfo))
                {
                    // Set the property value.
                    reader.ReadWithVerify();

                    if (!(jsonParameterInfo!.ShouldDeserialize))
                    {
                        reader.Skip();
                        state.Current.EndConstructorParameter();
                        continue;
                    }

                    ReadAndCacheConstructorArgument(ref state, ref reader, jsonParameterInfo);

                    state.Current.EndConstructorParameter();
                }
                else
                {
                    ReadOnlySpan<byte> unescapedPropertyName = JsonSerializer.GetPropertyName(ref state, ref reader, options);
                    JsonPropertyInfo jsonPropertyInfo = JsonSerializer.LookupProperty(
                        obj: null!,
                        unescapedPropertyName,
                        ref state,
                        out _,
                        createExtensionProperty: false);

                    if (state.Current.CtorArgumentState!.FoundProperties == null)
                    {
                        state.Current.CtorArgumentState.FoundProperties =
                            ArrayPool<FoundProperties>.Shared.Rent(Math.Max(1, state.Current.JsonClassInfo.PropertyCache!.Count));
                    }
                    else if (state.Current.PropertyIndex - 1 == state.Current.CtorArgumentState.FoundProperties!.Length)
                    {
                        // Rare case where we can't fit all the JSON properties in the rented pool; we have to grow.
                        // This could happen if there are duplicate properties in the JSON.

                        var newCache = ArrayPool<FoundProperties>.Shared.Rent(state.Current.CtorArgumentState.FoundProperties!.Length * 2);

                        state.Current.CtorArgumentState.FoundProperties!.CopyTo(newCache, 0);

                        ArrayPool<FoundProperties>.Shared.Return(state.Current.CtorArgumentState.FoundProperties!, clearArray: true);

                        state.Current.CtorArgumentState.FoundProperties = newCache!;
                    }

                    state.Current.CtorArgumentState!.FoundProperties![state.Current.PropertyIndex - 1] = (
                        jsonPropertyInfo,
                        reader.CurrentState,
                        reader.BytesConsumed,
                        state.Current.JsonPropertyName,
                        state.Current.JsonPropertyNameAsString);

                    reader.Skip();

                    state.Current.EndProperty();
                }
            }
        }

        private bool ReadConstructorArgumentsWithContinuation(ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options)
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
                        return false;
                    }
                }

                JsonParameterInfo? jsonParameterInfo;
                JsonPropertyInfo? jsonPropertyInfo;

                if (state.Current.PropertyState < StackFramePropertyState.Name)
                {
                    state.Current.PropertyState = StackFramePropertyState.Name;

                    JsonTokenType tokenType = reader.TokenType;

                    if (tokenType == JsonTokenType.EndObject)
                    {
                        return true;
                    }

                    // Read method would have thrown if otherwise.
                    Debug.Assert(tokenType == JsonTokenType.PropertyName);

                    if (TryLookupConstructorParameter(
                        ref state,
                        ref reader,
                        options,
                        out jsonParameterInfo))
                    {
                        jsonPropertyInfo = null;
                    }
                    else
                    {
                        ReadOnlySpan<byte> unescapedPropertyName = JsonSerializer.GetPropertyName(ref state, ref reader, options);
                        jsonPropertyInfo = JsonSerializer.LookupProperty(
                            obj: null!,
                            unescapedPropertyName,
                            ref state,
                            out bool useExtensionProperty,
                            createExtensionProperty: false);

                        state.Current.UseExtensionProperty = useExtensionProperty;
                    }
                }
                else
                {
                    jsonParameterInfo = state.Current.CtorArgumentState!.JsonParameterInfo;
                    jsonPropertyInfo = state.Current.JsonPropertyInfo;
                }

                if (jsonParameterInfo != null)
                {
                    Debug.Assert(jsonPropertyInfo == null);

                    if (!HandleConstructorArgumentWithContinuation(ref state, ref reader, jsonParameterInfo))
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandleConstructorArgumentWithContinuation(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonParameterInfo jsonParameterInfo)
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

            if (!ReadAndCacheConstructorArgument(ref state, ref reader, jsonParameterInfo))
            {
                return false;
            }

            state.Current.EndConstructorParameter();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HandlePropertyWithContinuation(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonPropertyInfo jsonPropertyInfo)
        {
            if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
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

                if (!ReadAheadPropertyValue(ref state, ref reader, jsonPropertyInfo))
                {
                    return false;
                }
            }

            object? propValue;

            if (state.Current.UseExtensionProperty)
            {
                if (!jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out propValue))
                {
                    return false;
                }
            }
            else
            {
                if (!jsonPropertyInfo.ReadJsonAsObject(ref state, ref reader, out propValue))
                {
                    return false;
                }
            }

            // Ensure that the cache has enough capacity to add this property.

            if (state.Current.CtorArgumentState!.FoundPropertiesAsync == null)
            {
                state.Current.CtorArgumentState.FoundPropertiesAsync =
                    ArrayPool<FoundPropertiesAsync>.Shared.Rent(Math.Max(1, state.Current.JsonClassInfo.PropertyCache!.Count));
            }
            else if (state.Current.CtorArgumentState.FoundPropertyCount == state.Current.CtorArgumentState.FoundPropertiesAsync!.Length)
            {
                // Rare case where we can't fit all the JSON properties in the rented pool; we have to grow.
                // This could happen if there are duplicate properties in the JSON.
                var newCache = ArrayPool<FoundPropertiesAsync>.Shared.Rent(
                    state.Current.CtorArgumentState.FoundPropertiesAsync!.Length * 2);

                state.Current.CtorArgumentState.FoundPropertiesAsync!.CopyTo(newCache, 0);

                ArrayPool<FoundPropertiesAsync>.Shared.Return(
                    state.Current.CtorArgumentState.FoundPropertiesAsync!, clearArray: true);

                state.Current.CtorArgumentState.FoundPropertiesAsync = newCache!;
            }

            // Cache the property name and value.
            state.Current.CtorArgumentState.FoundPropertiesAsync[state.Current.CtorArgumentState.FoundPropertyCount++] = (
                jsonPropertyInfo,
                propValue,
                state.Current.JsonPropertyNameAsString);

            state.Current.EndProperty();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginRead(ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
            }

            if (state.Current.JsonClassInfo.ParameterCount != state.Current.JsonClassInfo.ParameterCache!.Count)
            {
                ThrowHelper.ThrowInvalidOperationException_ConstructorParameterIncompleteBinding(ConstructorInfo!, TypeToConvert);
            }

            // Set current JsonPropertyInfo to null to avoid conflicts on push.
            state.Current.JsonPropertyInfo = null;

            Debug.Assert(state.Current.CtorArgumentState != null);

            InitializeConstructorArgumentCaches(ref state, options);
        }

        protected virtual void EndRead(ref ReadStack state) { }

        /// <summary>
        /// Lookup the constructor parameter given its name in the reader.
        /// </summary>
        protected virtual bool TryLookupConstructorParameter(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            out JsonParameterInfo? jsonParameterInfo)
        {
            Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object);

            ReadOnlySpan<byte> unescapedPropertyName = JsonSerializer.GetPropertyName(ref state, ref reader, options);

            jsonParameterInfo = state.Current.JsonClassInfo.GetParameter(
                unescapedPropertyName,
                ref state.Current,
                out byte[] utf8PropertyName);

            // Increment ConstructorParameterIndex so GetParameter() checks the next parameter first when called again.
            state.Current.CtorArgumentState!.ParameterIndex++;

            // For case insensitive and missing property support of JsonPath, remember the value on the temporary stack.
            state.Current.JsonPropertyName = utf8PropertyName;

            state.Current.CtorArgumentState.JsonParameterInfo = jsonParameterInfo;

            state.Current.NumberHandling = jsonParameterInfo?.NumberHandling;

            return jsonParameterInfo != null;
        }

        internal override bool ConstructorIsParameterized => true;
    }
}
