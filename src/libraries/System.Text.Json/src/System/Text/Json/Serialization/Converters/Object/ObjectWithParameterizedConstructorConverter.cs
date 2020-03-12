// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal abstract partial class ObjectWithParameterizedConstructorConverter<T> : ObjectDefaultConverter<T> where T : notnull
    {
        protected abstract void InitializeConstructorArgumentCaches(ref ReadStack state, JsonSerializerOptions options);

        protected abstract bool ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo, JsonSerializerOptions options);

        protected abstract object CreateObject(ref ReadStack state);

        internal override bool ConstructorIsParameterized => true;

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, [MaybeNullWhen(false)] out T value)
        {
            bool shouldReadPreservedReferences = options.ReferenceHandling.ShouldReadPreservedReferences();

            object? obj = null;

            if (!state.SupportContinuation && !shouldReadPreservedReferences)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    // This includes `null` tokens for structs as they can't be `null`.
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                // Set state.Current.JsonPropertyInfo to null so there's no conflict on state.Push()
                state.Current.JsonPropertyInfo = null!;

                InitializeConstructorArgumentCaches(ref state, options);

                ReadOnlySpan<byte> originalSpan = reader.OriginalSpan;

                ReadConstructorArguments(ref reader, options, ref state);

                obj = CreateObject(ref state);

                if (state.Current.CtorArgumentState.FoundPropertyCount > 0)
                {
                    Utf8JsonReader tempReader;

                    for (int i = 0; i < state.Current.CtorArgumentState.FoundPropertyCount; i++)
                    {
                        JsonPropertyInfo jsonPropertyInfo = state.Current.CtorArgumentState.FoundProperties![i].Item1;
                        long resumptionByteIndex = state.Current.CtorArgumentState.FoundProperties[i].Item3;
                        byte[]? propertyNameArray = state.Current.CtorArgumentState.FoundProperties[i].Item4;

                        tempReader = new Utf8JsonReader(
                            originalSpan.Slice(checked((int)resumptionByteIndex)),
                            isFinalBlock: true,
                            state: state.Current.CtorArgumentState.FoundProperties[i].Item2);

                        Debug.Assert(tempReader.TokenType == JsonTokenType.PropertyName);
                        tempReader.Read();

                        if (propertyNameArray == null)
                        {
                            propertyNameArray = jsonPropertyInfo.JsonPropertyName;
                        }
                        else
                        {
                            Debug.Assert(options.PropertyNameCaseInsensitive);
                            state.Current.JsonPropertyName = propertyNameArray;
                        }

                        // Support JsonException.Path.
                        Debug.Assert(
                            jsonPropertyInfo.JsonPropertyName == null ||
                            options.PropertyNameCaseInsensitive ||
                            ((ReadOnlySpan<byte>)propertyNameArray!).SequenceEqual(jsonPropertyInfo.JsonPropertyName));

                        state.Current.JsonPropertyInfo = jsonPropertyInfo;

                        jsonPropertyInfo.ReadJsonAndSetMember(obj, ref state, ref tempReader);
                    }

                    ArrayPool<ValueTuple<JsonPropertyInfo, JsonReaderState, long, byte[]?>>.Shared.Return(
                        state.Current.CtorArgumentState.FoundProperties!,
                        clearArray: true);
                }
#if DEBUG
                else
                {
                    Debug.Assert(state.Current.CtorArgumentState.FoundProperties == null);
                }
#endif
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

                    // Set state.Current.JsonPropertyInfo to null so there's no conflict on state.Push()
                    state.Current.JsonPropertyInfo = null!;

                    InitializeConstructorArgumentCaches(ref state, options);
                }

                // Handle the metadata properties.
                if (state.Current.ObjectState < StackFrameObjectState.MetadataPropertyValue)
                {
                    if (shouldReadPreservedReferences)
                    {
                        if (!reader.Read())
                        {
                            value = default!;
                            return false;
                        }

                        ReadOnlySpan<byte> propertyName = reader.GetSpan();
                        MetadataPropertyName metadata = JsonSerializer.GetMetadataPropertyName(propertyName);

                        if (metadata == MetadataPropertyName.Id ||
                            metadata == MetadataPropertyName.Ref ||
                            metadata == MetadataPropertyName.Values)
                        {
                            ThrowHelper.ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotHonored(TypeToConvert);
                        }

                        // Skip the read of the first property name, since we already read it above.
                        state.Current.PropertyState = StackFramePropertyState.ReadName;
                    }

                    state.Current.ObjectState = StackFrameObjectState.MetadataPropertyValue;
                }

                if (!ReadConstructorArgumentsWithContinuation(ref state, ref reader, shouldReadPreservedReferences, options))
                {
                    value = default;
                    return false;
                }

                obj = CreateObject(ref state);

                if (state.Current.CtorArgumentState.FoundPropertyCount > 0)
                {
                    Utf8JsonReader tempReader;
                    // Set the properties we've parsed so far.
                    for (int i = 0; i < state.Current.CtorArgumentState.FoundPropertyCount; i++)
                    {
                        JsonPropertyInfo jsonPropertyInfo = state.Current.CtorArgumentState.FoundPropertiesAsync![i].Item1;
                        byte[] propertyValueArray = state.Current.CtorArgumentState.FoundPropertiesAsync[i].Item3;
                        byte[]? propertyNameArray = state.Current.CtorArgumentState.FoundPropertiesAsync[i].Item4;

                        tempReader = new Utf8JsonReader(
                            propertyValueArray,
                            isFinalBlock: true,
                            state: state.Current.CtorArgumentState.FoundPropertiesAsync[i].Item2);

                        Debug.Assert(tempReader.TokenType == JsonTokenType.PropertyName);
                        tempReader.Read();

                        if (propertyNameArray == null)
                        {
                            propertyNameArray = jsonPropertyInfo.JsonPropertyName;
                        }
                        else
                        {
                            Debug.Assert(options.PropertyNameCaseInsensitive);
                            state.Current.JsonPropertyName = propertyNameArray;
                        }

                        // Support JsonException.Path.
                        Debug.Assert(
                            jsonPropertyInfo.JsonPropertyName == null ||
                            options.PropertyNameCaseInsensitive ||
                            ((ReadOnlySpan<byte>)propertyNameArray!).SequenceEqual(jsonPropertyInfo.JsonPropertyName));

                        state.Current.JsonPropertyInfo = jsonPropertyInfo;

                        jsonPropertyInfo.ReadJsonAndSetMember(obj, ref state, ref tempReader);
                    }

                    ArrayPool<ValueTuple<JsonPropertyInfo, JsonReaderState, byte[]?, byte[]?>>.Shared.Return(
                        state.Current.CtorArgumentState.FoundPropertiesAsync!,
                        clearArray: true);
                }
#if DEBUG
                else
                {
                    Debug.Assert(state.Current.CtorArgumentState.FoundPropertiesAsync == null);
                }
#endif
            }

            // Set extension data, if any.
            if (state.Current.CtorArgumentState.DataExtension != null)
            {
                state.Current.JsonClassInfo.DataExtensionProperty!.SetValueAsObject(obj, state.Current.CtorArgumentState.DataExtension);
            }

            // Check if we are trying to build the sorted parameter cache.
            if (state.Current.CtorArgumentState.ParameterRefCache != null)
            {
                state.Current.JsonClassInfo.UpdateSortedParameterCache(ref state.Current);
            }

            // Check if we are trying to build the sorted property cache.
            if (state.Current.PropertyRefCache != null)
            {
                state.Current.JsonClassInfo.UpdateSortedPropertyCache(ref state.Current);
            }

            Debug.Assert(obj != null);
            value = (T)obj;

            return true;
        }
    }
}
