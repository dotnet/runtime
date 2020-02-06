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
    internal abstract partial class ObjectWithParameterizedConstructorConverter<T> : JsonObjectConverter<T>
    {
        private const int JsonPropertyNameKeyLength = 7;

        // The limit to how many constructor parameter names from the JSON are cached in _parameterRefsSorted before using _parameterCache.
        private const int ParameterNameCountCacheThreshold = 32;

        // Fast cache of constructor parameters by first JSON ordering; may not contain all parameters. Accessed before _parameterCache.
        // Use an array (instead of List<T>) for highest performance.
        private volatile ParameterRef[]? _parameterRefsSorted;

        // Fast cache of properties by first JSON ordering; may not contain all properties. Accessed before PropertyCache.
        // Use an array (instead of List<T>) for highest performance.
        private volatile PropertyRef[]? _propertyRefsSorted;

        // The limit to how many property names from the JSON are cached in _propertyRefsSorted before using PropertyCache.
        protected const int PropertyNameCountCacheThreshold = 64;

        protected abstract void InitializeConstructorArgumentCaches(ref ReadStackFrame frame, JsonSerializerOptions options);

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
                    unescapedPropertyName = JsonSerializer.GetUnescapedString(escapedPropertyName, idx);
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
                    if (TryGetPropertyFromFastCache(
                        ref state.Current,
                        unescapedPropertyName,
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
                        jsonPropertyInfo = GetPropertyFromSlowCache(
                            ref state.Current,
                            unescapedPropertyName,
                            unescapedPropertyNameAsString,
                            options);

                        Debug.Assert(jsonPropertyInfo != null);

                        HandleProperty(ref state, ref reader, unescapedPropertyName, jsonPropertyInfo, options);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleProperty(ref ReadStack state, ref Utf8JsonReader reader, ReadOnlySpan<byte> unescapedPropertyName, JsonPropertyInfo jsonPropertyInfo, JsonSerializerOptions options)
        {
            bool useExtensionProperty;
            // Determine if we should use the extension property.
            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                if (DataExtensionProperty != null)
                {
                    if (state.Current.DataExtension == null)
                    {
                        if (DataExtensionProperty.RuntimeClassInfo.CreateObject == null)
                        {
                            ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(DataExtensionProperty.DeclaredPropertyType);
                        }

                        state.Current.DataExtension = DataExtensionProperty.RuntimeClassInfo.CreateObject();
                    }

                    state.Current.JsonPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);
                    jsonPropertyInfo = DataExtensionProperty;
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
                if (state.Current.FoundProperties == null)
                {
                    state.Current.FoundProperties =
                        ArrayPool<ValueTuple<
                            JsonPropertyInfo,
                            JsonReaderState,
                            long,
                            byte[]?>>.Shared.Rent(PropertyNameCountCacheThreshold);
                }
                else  if (state.Current.FoundPropertyCount == state.Current.FoundProperties!.Length)
                {
                    // Case where we can't fit all the JSON properties in the rented pool, we have to grow.
                    var newCache = ArrayPool<ValueTuple<JsonPropertyInfo, JsonReaderState, long, byte[]?>>.Shared.Rent(state.Current.FoundProperties!.Length * 2);
                    state.Current.FoundProperties!.CopyTo(newCache, 0);

                    ArrayPool<ValueTuple<JsonPropertyInfo, JsonReaderState, long, byte[]?>>.Shared.Return(state.Current.FoundProperties!, clearArray: true);
                    state.Current.FoundProperties = newCache!;
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

                state.Current.FoundProperties![state.Current.FoundPropertyCount++] = (
                    jsonPropertyInfo,
                    reader.CurrentState,
                    reader.BytesConsumed,
                    propertyNameArray);

                reader.Skip();
            }
            else
            {
                reader.Read();

                if (_dataExtensionIsObject)
                {
                    jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out object? extDataValue);
                    ((IDictionary<string, object>)state.Current.DataExtension!)[state.Current.JsonPropertyNameAsString!] = extDataValue!;
                }
                else
                {
                    jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out JsonElement extDataValue);
                    ((IDictionary<string, JsonElement>)state.Current.DataExtension!)[state.Current.JsonPropertyNameAsString!] = extDataValue!;
                }

                // Ensure any exception thrown in the next read does not have a property in its JsonPath.
                state.Current.EndProperty();
            }
        }

        protected abstract bool ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo, JsonSerializerOptions options);

        /// <summary>
        /// Lookup the constructor parameter given its name in the reader.
        /// </summary>
        // AggressiveInlining used although a large method it is only called from two locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryLookupConstructorParameterFromFastCache(
            ref ReadStack state,
            ReadOnlySpan<byte> unescapedPropertyName,
            JsonSerializerOptions options,
            out JsonParameterInfo? jsonParameterInfo)
        {
            Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object);

            if (TryGetParameterFromFastCache(unescapedPropertyName, ref state.Current, out jsonParameterInfo))
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
            if (TryGetParameterFromSlowCache(
                ref state.Current,
                unescapedPropertyName,
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
            frame.ConstructorParameterIndex++;

            // Support JsonException.Path.
            Debug.Assert(
                jsonParameterInfo.JsonPropertyName == null ||
                options.PropertyNameCaseInsensitive ||
                unescapedPropertyName.SequenceEqual(jsonParameterInfo.JsonPropertyName));

            frame.JsonConstructorParameterInfo = jsonParameterInfo;

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
                    frame.JsonConstructorParameterInfo.JsonPropertyName = propertyNameArray;
                }
            }

            frame.JsonConstructorParameterInfo = jsonParameterInfo;
        }

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetParameterFromFastCache(
            ReadOnlySpan<byte> unescapedPropertyName,
            ref ReadStackFrame frame,
            out JsonParameterInfo? jsonParameterInfo)
        {
            ulong unescapedPropertyNameKey = JsonClassInfo.GetKey(unescapedPropertyName);

            JsonParameterInfo? info = null;

            // Keep a local copy of the cache in case it changes by another thread.
            ParameterRef[]? localParameterRefsSorted = _parameterRefsSorted;

            // If there is an existing cache, then use it.
            if (localParameterRefsSorted != null)
            {
                // Start with the current parameter index, and then go forwards\backwards.
                int parameterIndex = frame.ConstructorParameterIndex;

                int count = localParameterRefsSorted.Length;
                int iForward = Math.Min(parameterIndex, count);
                int iBackward = iForward - 1;

                while (true)
                {
                    if (iForward < count)
                    {
                        ParameterRef parameterRef = localParameterRefsSorted[iForward];
                        if (TryIsParameterRefEqual(parameterRef, unescapedPropertyName, unescapedPropertyNameKey, ref info))
                        {
                            jsonParameterInfo = info;
                            return true;
                        }

                        ++iForward;

                        if (iBackward >= 0)
                        {
                            parameterRef = localParameterRefsSorted[iBackward];
                            if (TryIsParameterRefEqual(parameterRef, unescapedPropertyName, unescapedPropertyNameKey, ref info))
                            {
                                jsonParameterInfo = info;
                                return true;
                            }

                            --iBackward;
                        }
                    }
                    else if (iBackward >= 0)
                    {
                        ParameterRef parameterRef = localParameterRefsSorted[iBackward];
                        if (TryIsParameterRefEqual(parameterRef, unescapedPropertyName, unescapedPropertyNameKey, ref info))
                        {
                            jsonParameterInfo = info;
                            return true;
                        }

                        --iBackward;
                    }
                    else
                    {
                        // Property was not found.
                        break;
                    }
                }
            }

            jsonParameterInfo = null;
            return false;
        }

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetParameterFromSlowCache(
            ref ReadStackFrame frame,
            ReadOnlySpan<byte> unescapedPropertyName,
            out string unescapedPropertyNameAsString,
            out JsonParameterInfo? jsonParameterInfo)
        {
            unescapedPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);

            Debug.Assert(ParameterCache != null);

            if (!ParameterCache.TryGetValue(unescapedPropertyNameAsString, out JsonParameterInfo? info))
            {
                // Constructor parameter not found. We'll check if it's a property next.
                jsonParameterInfo = null;
                return false;
            }

            jsonParameterInfo = info;
            Debug.Assert(info != null);

            // Two code paths to get here:
            // 1) key == info.PropertyNameKey. Exact match found.
            // 2) key != info.PropertyNameKey. Match found due to case insensitivity.
            // TODO: recheck these conditions
            Debug.Assert(JsonClassInfo.GetKey(unescapedPropertyName) == info.ParameterNameKey ||
                unescapedPropertyNameAsString.Equals(info.NameAsString, StringComparison.OrdinalIgnoreCase));

            // Keep a local copy of the cache in case it changes by another thread.
            ParameterRef[]? localParameterRefsSorted = _parameterRefsSorted;

            // Check if we should add this to the cache.
            // Only cache up to a threshold length and then just use the dictionary when an item is not found in the cache.
            int cacheCount = 0;
            if (localParameterRefsSorted != null)
            {
                cacheCount = localParameterRefsSorted.Length;
            }

            // Do a quick check for the stable (after warm-up) case.
            if (cacheCount < ParameterNameCountCacheThreshold)
            {
                // Do a slower check for the warm-up case.
                if (frame.ParameterRefCache != null)
                {
                    cacheCount += frame.ParameterRefCache.Count;
                }

                // Check again to append the cache up to the threshold.
                if (cacheCount < ParameterNameCountCacheThreshold)
                {
                    if (frame.ParameterRefCache == null)
                    {
                        frame.ParameterRefCache = new List<ParameterRef>();
                    }

                    ParameterRef parameterRef = new ParameterRef(JsonClassInfo.GetKey(unescapedPropertyName), jsonParameterInfo);
                    frame.ParameterRefCache.Add(parameterRef);
                }
            }

            return true;
        }

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetPropertyFromFastCache(
            ref ReadStackFrame frame,
            ReadOnlySpan<byte> unescapedPropertyName,
            out JsonPropertyInfo? jsonPropertyInfo)
        {
            ulong unescapedPropertyNameKey = JsonClassInfo.GetKey(unescapedPropertyName);

            JsonPropertyInfo? info = null;

            // Keep a local copy of the cache in case it changes by another thread.
            PropertyRef[]? localPropertyRefsSorted = _propertyRefsSorted;

            // If there is an existing cache, then use it.
            if (localPropertyRefsSorted != null)
            {
                // Start with the current property index, and then go forwards\backwards.
                int propertyIndex = frame.PropertyIndex;

                int count = localPropertyRefsSorted.Length;
                int iForward = Math.Min(propertyIndex, count);
                int iBackward = iForward - 1;

                while (true)
                {
                    if (iForward < count)
                    {
                        PropertyRef propertyRef = localPropertyRefsSorted[iForward];
                        if (TryIsPropertyRefEqual(propertyRef, unescapedPropertyName, unescapedPropertyNameKey, ref info))
                        {
                            jsonPropertyInfo = info;
                            return true;
                        }

                        ++iForward;

                        if (iBackward >= 0)
                        {
                            propertyRef = localPropertyRefsSorted[iBackward];
                            if (TryIsPropertyRefEqual(propertyRef, unescapedPropertyName, unescapedPropertyNameKey, ref info))
                            {
                                jsonPropertyInfo = info;
                                return true;
                            }

                            --iBackward;
                        }
                    }
                    else if (iBackward >= 0)
                    {
                        PropertyRef propertyRef = localPropertyRefsSorted[iBackward];
                        if (TryIsPropertyRefEqual(propertyRef, unescapedPropertyName, unescapedPropertyNameKey, ref info))
                        {
                            jsonPropertyInfo = info;
                            return true;
                        }

                        --iBackward;
                    }
                    else
                    {
                        // Property was not found.
                        break;
                    }
                }
            }

            jsonPropertyInfo = null;
            return false;
        }

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonPropertyInfo GetPropertyFromSlowCache(
            ref ReadStackFrame frame,
            ReadOnlySpan<byte> unescapedPropertyName,
            string unescapedPropertyNameAsString,
            JsonSerializerOptions options)
        {
            // Keep a local copy of the cache in case it changes by another thread.
            PropertyRef[]? localPropertyRefsSorted = _propertyRefsSorted;

            Debug.Assert(_propertyCache != null);

            if (!_propertyCache.TryGetValue(unescapedPropertyNameAsString, out JsonPropertyInfo? info))
            {
                info = JsonPropertyInfo.s_missingProperty;
            }

            Debug.Assert(info != null);

            // Three code paths to get here:
            // 1) info == s_missingProperty. Property not found.
            // 2) key == info.PropertyNameKey. Exact match found.
            // 3) key != info.PropertyNameKey. Match found due to case insensitivity.
            Debug.Assert(
                info == JsonPropertyInfo.s_missingProperty ||
                JsonClassInfo.GetKey(unescapedPropertyName) == info.PropertyNameKey ||
                options.PropertyNameCaseInsensitive);

            // Check if we should add this to the cache.
            // Only cache up to a threshold length and then just use the dictionary when an item is not found in the cache.
            int cacheCount = 0;
            if (localPropertyRefsSorted != null)
            {
                cacheCount = localPropertyRefsSorted.Length;
            }

            // Do a quick check for the stable (after warm-up) case.
            if (cacheCount < PropertyNameCountCacheThreshold)
            {
                // Do a slower check for the warm-up case.
                if (frame.PropertyRefCache != null)
                {
                    cacheCount += frame.PropertyRefCache.Count;
                }

                // Check again to append the cache up to the threshold.
                if (cacheCount < PropertyNameCountCacheThreshold)
                {
                    if (frame.PropertyRefCache == null)
                    {
                        frame.PropertyRefCache = new List<PropertyRef>();
                    }

                    PropertyRef propertyRef = new PropertyRef(JsonClassInfo.GetKey(unescapedPropertyName), info);
                    frame.PropertyRefCache.Add(propertyRef);
                }
            }

            return info;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryIsParameterRefEqual(in ParameterRef parameterRef, ReadOnlySpan<byte> parameterName, ulong key, [NotNullWhen(true)] ref JsonParameterInfo? info)
        {
            if (key == parameterRef.Key)
            {
                // We compare the whole name, although we could skip the first 7 bytes (but it's not any faster)
                if (parameterName.Length <= JsonPropertyNameKeyLength ||
                    parameterName.SequenceEqual(parameterRef.Info.ParameterName))
                {
                    info = parameterRef.Info;
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryIsPropertyRefEqual(in PropertyRef propertyRef, ReadOnlySpan<byte> propertyName, ulong key, [NotNullWhen(true)] ref JsonPropertyInfo? info)
        {
            if (key == propertyRef.Key)
            {
                // We compare the whole name, although we could skip the first 7 bytes (but it's not any faster)
                if (propertyName.Length <= JsonPropertyNameKeyLength ||
                    propertyName.SequenceEqual(propertyRef.Info.Name))
                {
                    info = propertyRef.Info;
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateSortedParameterCache(ref ReadStackFrame frame)
        {
            Debug.Assert(frame.ParameterRefCache != null);

            // frame.PropertyRefCache is only read\written by a single thread -- the thread performing
            // the deserialization for a given object instance.

            List<ParameterRef> listToAppend = frame.ParameterRefCache;

            // _parameterRefsSorted can be accessed by multiple threads, so replace the reference when
            // appending to it. No lock() is necessary.

            if (_parameterRefsSorted != null)
            {
                List<ParameterRef> replacementList = new List<ParameterRef>(_parameterRefsSorted);
                Debug.Assert(replacementList.Count <= ParameterNameCountCacheThreshold);

                // Verify replacementList will not become too large.
                while (replacementList.Count + listToAppend.Count > ParameterNameCountCacheThreshold)
                {
                    // This code path is rare; keep it simple by using RemoveAt() instead of RemoveRange() which requires calculating index\count.
                    listToAppend.RemoveAt(listToAppend.Count - 1);
                }

                // Add the new items; duplicates are possible but that is tolerated during property lookup.
                replacementList.AddRange(listToAppend);
                _parameterRefsSorted = replacementList.ToArray();
            }
            else
            {
                _parameterRefsSorted = listToAppend.ToArray();
            }

            frame.ParameterRefCache = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateSortedPropertyCache(ref ReadStackFrame frame)
        {
            Debug.Assert(frame.PropertyRefCache != null);

            // frame.PropertyRefCache is only read\written by a single thread -- the thread performing
            // the deserialization for a given object instance.

            List<PropertyRef> listToAppend = frame.PropertyRefCache;

            // _propertyRefsSorted can be accessed by multiple threads, so replace the reference when
            // appending to it. No lock() is necessary.

            if (_propertyRefsSorted != null)
            {
                List<PropertyRef> replacementList = new List<PropertyRef>(_propertyRefsSorted);
                Debug.Assert(replacementList.Count <= PropertyNameCountCacheThreshold);

                // Verify replacementList will not become too large.
                while (replacementList.Count + listToAppend.Count > PropertyNameCountCacheThreshold)
                {
                    // This code path is rare; keep it simple by using RemoveAt() instead of RemoveRange() which requires calculating index\count.
                    listToAppend.RemoveAt(listToAppend.Count - 1);
                }

                // Add the new items; duplicates are possible but that is tolerated during property lookup.
                replacementList.AddRange(listToAppend);
                _propertyRefsSorted = replacementList.ToArray();
            }
            else
            {
                _propertyRefsSorted = listToAppend.ToArray();
            }

            frame.PropertyRefCache = null;
        }
    }
}
