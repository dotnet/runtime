// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

using FoundProperty = System.ValueTuple<System.Text.Json.Serialization.Metadata.JsonPropertyInfo, System.Text.Json.JsonReaderState, long, byte[]?, string?>;
using FoundPropertyAsync = System.ValueTuple<System.Text.Json.Serialization.Metadata.JsonPropertyInfo, object?, string?>;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal abstract partial class ObjectWithParameterizedConstructorConverter<T> : ObjectDefaultConverter<T> where T : notnull
    {
        internal sealed override bool ConstructorIsParameterized => true;

        internal sealed override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, [MaybeNullWhen(false)] out T value)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;

            if (!jsonTypeInfo.UsesParameterizedConstructor || state.Current.IsPopulating)
            {
                // Fall back to default object converter in following cases:
                // - if user configuration has invalidated the parameterized constructor
                // - we're continuing populating an object.
                return base.OnTryRead(ref reader, typeToConvert, options, ref state, out value);
            }

            object obj;
            ArgumentState argumentState = state.Current.CtorArgumentState!;

            if (!state.SupportContinuation && !state.Current.CanContainMetadata)
            {
                // Fast path that avoids maintaining state variables.

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Type);
                }

                if (state.ParentProperty?.TryGetPrePopulatedValue(ref state) == true)
                {
                    object populatedObject = state.Current.ReturnValue!;
                    PopulatePropertiesFastPath(populatedObject, jsonTypeInfo, options, ref reader, ref state);
                    value = (T)populatedObject;
                    return true;
                }

                ReadOnlySpan<byte> originalSpan = reader.OriginalSpan;
                ReadOnlySequence<byte> originalSequence = reader.OriginalSequence;

                ReadConstructorArguments(ref state, ref reader, options);

                obj = (T)CreateObject(ref state.Current);

                jsonTypeInfo.OnDeserializing?.Invoke(obj);

                if (argumentState.FoundPropertyCount > 0)
                {
                    Utf8JsonReader tempReader;

                    FoundProperty[]? properties = argumentState.FoundProperties;
                    Debug.Assert(properties != null);

                    for (int i = 0; i < argumentState.FoundPropertyCount; i++)
                    {
                        JsonPropertyInfo jsonPropertyInfo = properties[i].Item1;
                        long resumptionByteIndex = properties[i].Item3;
                        byte[]? propertyNameArray = properties[i].Item4;
                        string? dataExtKey = properties[i].Item5;

                        tempReader = originalSequence.IsEmpty
                            ? new Utf8JsonReader(
                                originalSpan.Slice(checked((int)resumptionByteIndex)),
                                isFinalBlock: true,
                                state: properties[i].Item2)
                            : new Utf8JsonReader(
                                originalSequence.Slice(resumptionByteIndex),
                                isFinalBlock: true,
                                state: properties[i].Item2);

                        Debug.Assert(tempReader.TokenType == JsonTokenType.PropertyName);

                        state.Current.JsonPropertyName = propertyNameArray;
                        state.Current.JsonPropertyInfo = jsonPropertyInfo;
                        state.Current.NumberHandling = jsonPropertyInfo.EffectiveNumberHandling;

                        bool useExtensionProperty = dataExtKey != null;

                        if (useExtensionProperty)
                        {
                            Debug.Assert(jsonPropertyInfo == state.Current.JsonTypeInfo.ExtensionDataProperty);
                            state.Current.JsonPropertyNameAsString = dataExtKey;
                            JsonSerializer.CreateExtensionDataProperty(obj, jsonPropertyInfo, options);
                        }

                        ReadPropertyValue(obj, ref state, ref tempReader, jsonPropertyInfo, useExtensionProperty);
                    }

                    FoundProperty[] toReturn = argumentState.FoundProperties!;
                    argumentState.FoundProperties = null;
                    ArrayPool<FoundProperty>.Shared.Return(toReturn, clearArray: true);
                }
            }
            else
            {
                // Slower path that supports continuation and metadata reads.

                if (state.Current.ObjectState == StackFrameObjectState.None)
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Type);
                    }

                    state.Current.ObjectState = StackFrameObjectState.StartToken;
                }

                // Read any metadata properties.
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
                if ((state.Current.MetadataPropertyNames & MetadataPropertyName.Type) != 0 &&
                    state.Current.PolymorphicSerializationState != PolymorphicSerializationState.PolymorphicReEntryStarted &&
                    ResolvePolymorphicConverter(jsonTypeInfo, ref state) is JsonConverter polymorphicConverter)
                {
                    Debug.Assert(!IsValueType);
                    bool success = polymorphicConverter.OnTryReadAsObject(ref reader, polymorphicConverter.Type!, options, ref state, out object? objectResult);
                    value = (T)objectResult!;
                    state.ExitPolymorphicConverter(success);
                    return success;
                }

                // We need to populate before we started reading constructor arguments.
                // Metadata is disallowed with Populate option and therefore ordering here is irrelevant.
                // Since state.Current.IsPopulating is being checked early on in this method the continuation
                // will be handled there.
                if (state.ParentProperty?.TryGetPrePopulatedValue(ref state) == true)
                {
                    object populatedObject = state.Current.ReturnValue!;

                    jsonTypeInfo.OnDeserializing?.Invoke(populatedObject);
                    state.Current.ObjectState = StackFrameObjectState.CreatedObject;
                    state.Current.InitializeRequiredPropertiesValidationState(jsonTypeInfo);
                    return base.OnTryRead(ref reader, typeToConvert, options, ref state, out value);
                }

                // Handle metadata post polymorphic dispatch
                if (state.Current.ObjectState < StackFrameObjectState.ConstructorArguments)
                {
                    if (state.Current.CanContainMetadata)
                    {
                        JsonSerializer.ValidateMetadataForObjectConverter(ref state);
                    }

                    if (state.Current.MetadataPropertyNames == MetadataPropertyName.Ref)
                    {
                        value = JsonSerializer.ResolveReferenceId<T>(ref state);
                        return true;
                    }

                    BeginRead(ref state, options);

                    state.Current.ObjectState = StackFrameObjectState.ConstructorArguments;
                }

                if (!ReadConstructorArgumentsWithContinuation(ref state, ref reader, options))
                {
                    value = default;
                    return false;
                }

                obj = (T)CreateObject(ref state.Current);

                if ((state.Current.MetadataPropertyNames & MetadataPropertyName.Id) != 0)
                {
                    Debug.Assert(state.ReferenceId != null);
                    Debug.Assert(options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve);
                    state.ReferenceResolver.AddReference(state.ReferenceId, obj);
                    state.ReferenceId = null;
                }

                jsonTypeInfo.OnDeserializing?.Invoke(obj);

                if (argumentState.FoundPropertyCount > 0)
                {
                    for (int i = 0; i < argumentState.FoundPropertyCount; i++)
                    {
                        JsonPropertyInfo jsonPropertyInfo = argumentState.FoundPropertiesAsync![i].Item1;
                        object? propValue = argumentState.FoundPropertiesAsync![i].Item2;
                        string? dataExtKey = argumentState.FoundPropertiesAsync![i].Item3;

                        if (dataExtKey == null)
                        {
                            Debug.Assert(jsonPropertyInfo.Set != null);

                            if (propValue is not null || !jsonPropertyInfo.IgnoreNullTokensOnRead || default(T) is not null)
                            {
                                jsonPropertyInfo.Set(obj, propValue);

                                // if this is required property IgnoreNullTokensOnRead will always be false because we don't allow for both to be true
                                state.Current.MarkRequiredPropertyAsRead(jsonPropertyInfo);
                            }
                        }
                        else
                        {
                            Debug.Assert(jsonPropertyInfo == state.Current.JsonTypeInfo.ExtensionDataProperty);

                            JsonSerializer.CreateExtensionDataProperty(obj, jsonPropertyInfo, options);
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

                    FoundPropertyAsync[] toReturn = argumentState.FoundPropertiesAsync!;
                    argumentState.FoundPropertiesAsync = null;
                    ArrayPool<FoundPropertyAsync>.Shared.Return(toReturn, clearArray: true);
                }
            }

            jsonTypeInfo.OnDeserialized?.Invoke(obj);
            state.Current.ValidateAllRequiredPropertiesAreRead(jsonTypeInfo);

            // Unbox
            Debug.Assert(obj != null);
            value = (T)obj;

            // Check if we are trying to build the sorted cache.
            if (state.Current.PropertyRefCache != null)
            {
                state.Current.JsonTypeInfo.UpdateSortedPropertyCache(ref state.Current);
            }

            // Check if we are trying to build the sorted parameter cache.
            if (argumentState.ParameterRefCache != null)
            {
                state.Current.JsonTypeInfo.UpdateSortedParameterCache(ref state.Current);
            }

            return true;
        }

        protected abstract void InitializeConstructorArgumentCaches(ref ReadStack state, JsonSerializerOptions options);

        protected abstract bool ReadAndCacheConstructorArgument(scoped ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo);

        protected abstract object CreateObject(ref ReadStackFrame frame);

        /// <summary>
        /// Performs a full first pass of the JSON input and deserializes the ctor args.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadConstructorArguments(scoped ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            BeginRead(ref state, options);

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

                    if (!jsonParameterInfo.ShouldDeserialize)
                    {
                        // The Utf8JsonReader.Skip() method will fail fast if it detects that we're reading
                        // from a partially read buffer, regardless of whether the next value is available.
                        // This can result in erroneous failures in cases where a custom converter is calling
                        // into a built-in converter (cf. https://github.com/dotnet/runtime/issues/74108).
                        // For this reason we need to call the TrySkip() method instead -- the serializer
                        // should guarantee sufficient read-ahead has been performed for the current object.
                        bool success = reader.TrySkip();
                        Debug.Assert(success, "Serializer should guarantee sufficient read-ahead has been done.");

                        state.Current.EndConstructorParameter();
                        continue;
                    }

                    Debug.Assert(jsonParameterInfo.MatchingProperty != null);
                    ReadAndCacheConstructorArgument(ref state, ref reader, jsonParameterInfo);

                    state.Current.EndConstructorParameter();
                }
                else
                {
                    ReadOnlySpan<byte> unescapedPropertyName = JsonSerializer.GetPropertyName(ref state, ref reader);
                    JsonPropertyInfo jsonPropertyInfo = JsonSerializer.LookupProperty(
                        obj: null!,
                        unescapedPropertyName,
                        ref state,
                        options,
                        out _,
                        createExtensionProperty: false);

                    if (jsonPropertyInfo.CanDeserialize)
                    {
                        ArgumentState argumentState = state.Current.CtorArgumentState!;

                        if (argumentState.FoundProperties == null)
                        {
                            argumentState.FoundProperties =
                                ArrayPool<FoundProperty>.Shared.Rent(Math.Max(1, state.Current.JsonTypeInfo.PropertyCache!.Count));
                        }
                        else if (argumentState.FoundPropertyCount == argumentState.FoundProperties.Length)
                        {
                            // Rare case where we can't fit all the JSON properties in the rented pool; we have to grow.
                            // This could happen if there are duplicate properties in the JSON.

                            var newCache = ArrayPool<FoundProperty>.Shared.Rent(argumentState.FoundProperties.Length * 2);

                            argumentState.FoundProperties.CopyTo(newCache, 0);

                            FoundProperty[] toReturn = argumentState.FoundProperties;
                            argumentState.FoundProperties = newCache!;

                            ArrayPool<FoundProperty>.Shared.Return(toReturn, clearArray: true);
                        }

                        argumentState.FoundProperties[argumentState.FoundPropertyCount++] = (
                            jsonPropertyInfo,
                            reader.CurrentState,
                            reader.BytesConsumed,
                            state.Current.JsonPropertyName,
                            state.Current.JsonPropertyNameAsString);
                    }

                    bool success = reader.TrySkip();
                    Debug.Assert(success, "Serializer should guarantee sufficient read-ahead has been done.");

                    state.Current.EndProperty();
                }
            }
        }

        private bool ReadConstructorArgumentsWithContinuation(scoped ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            // Process all properties.
            while (true)
            {
                // Determine the property.
                if (state.Current.PropertyState == StackFramePropertyState.None)
                {
                    if (!reader.Read())
                    {
                        return false;
                    }

                    state.Current.PropertyState = StackFramePropertyState.ReadName;
                }

                JsonParameterInfo? jsonParameterInfo;
                JsonPropertyInfo? jsonPropertyInfo;

                if (state.Current.PropertyState < StackFramePropertyState.Name)
                {
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
                        ReadOnlySpan<byte> unescapedPropertyName = JsonSerializer.GetPropertyName(ref state, ref reader);
                        jsonPropertyInfo = JsonSerializer.LookupProperty(
                            obj: null!,
                            unescapedPropertyName,
                            ref state,
                            options,
                            out bool useExtensionProperty,
                            createExtensionProperty: false);

                        state.Current.UseExtensionProperty = useExtensionProperty;
                    }

                    state.Current.PropertyState = StackFramePropertyState.Name;
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
            scoped ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonParameterInfo jsonParameterInfo)
        {
            if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
            {
                if (!jsonParameterInfo.ShouldDeserialize)
                {
                    if (!reader.TrySkipPartial(targetDepth: state.Current.OriginalDepth + 1))
                    {
                        return false;
                    }

                    state.Current.EndConstructorParameter();
                    return true;
                }

                if (!reader.TryAdvanceWithOptionalReadAhead(jsonParameterInfo.EffectiveConverter.RequiresReadAhead))
                {
                    return false;
                }

                state.Current.PropertyState = StackFramePropertyState.ReadValue;
            }

            if (!ReadAndCacheConstructorArgument(ref state, ref reader, jsonParameterInfo))
            {
                return false;
            }

            state.Current.EndConstructorParameter();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HandlePropertyWithContinuation(
            scoped ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonPropertyInfo jsonPropertyInfo)
        {
            if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
            {
                if (!jsonPropertyInfo.CanDeserialize)
                {
                    if (!reader.TrySkipPartial(targetDepth: state.Current.OriginalDepth + 1))
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

                state.Current.PropertyState = StackFramePropertyState.ReadValue;
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

            Debug.Assert(jsonPropertyInfo.CanDeserialize);

            // Ensure that the cache has enough capacity to add this property.

            ArgumentState argumentState = state.Current.CtorArgumentState!;

            if (argumentState.FoundPropertiesAsync == null)
            {
                argumentState.FoundPropertiesAsync = ArrayPool<FoundPropertyAsync>.Shared.Rent(Math.Max(1, state.Current.JsonTypeInfo.PropertyCache!.Count));
            }
            else if (argumentState.FoundPropertyCount == argumentState.FoundPropertiesAsync!.Length)
            {
                // Rare case where we can't fit all the JSON properties in the rented pool; we have to grow.
                // This could happen if there are duplicate properties in the JSON.
                var newCache = ArrayPool<FoundPropertyAsync>.Shared.Rent(argumentState.FoundPropertiesAsync!.Length * 2);

                argumentState.FoundPropertiesAsync!.CopyTo(newCache, 0);

                FoundPropertyAsync[] toReturn = argumentState.FoundPropertiesAsync!;
                argumentState.FoundPropertiesAsync = newCache!;

                ArrayPool<FoundPropertyAsync>.Shared.Return(toReturn, clearArray: true);
            }

            // Cache the property name and value.
            argumentState.FoundPropertiesAsync![argumentState.FoundPropertyCount++] = (
                jsonPropertyInfo,
                propValue,
                state.Current.JsonPropertyNameAsString);

            state.Current.EndProperty();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginRead(scoped ref ReadStack state, JsonSerializerOptions options)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;

            jsonTypeInfo.ValidateCanBeUsedForPropertyMetadataSerialization();

            if (jsonTypeInfo.ParameterCount != jsonTypeInfo.ParameterCache!.Count)
            {
                ThrowHelper.ThrowInvalidOperationException_ConstructorParameterIncompleteBinding(Type);
            }

            state.Current.InitializeRequiredPropertiesValidationState(jsonTypeInfo);

            // Set current JsonPropertyInfo to null to avoid conflicts on push.
            state.Current.JsonPropertyInfo = null;

            Debug.Assert(state.Current.CtorArgumentState != null);

            InitializeConstructorArgumentCaches(ref state, options);
        }

        /// <summary>
        /// Lookup the constructor parameter given its name in the reader.
        /// </summary>
        protected virtual bool TryLookupConstructorParameter(
            scoped ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            [NotNullWhen(true)] out JsonParameterInfo? jsonParameterInfo)
        {
            Debug.Assert(state.Current.JsonTypeInfo.Kind == JsonTypeInfoKind.Object);

            ReadOnlySpan<byte> unescapedPropertyName = JsonSerializer.GetPropertyName(ref state, ref reader);

            jsonParameterInfo = state.Current.JsonTypeInfo.GetParameter(
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
    }
}
