// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Json.Serialization.Metadata
{
    public partial class JsonTypeInfo
    {
        /// <summary>
        /// Cached typeof(object). It is faster to cache this than to call typeof(object) multiple times.
        /// </summary>
        internal static readonly Type ObjectType = typeof(object);

        // The length of the property name embedded in the key (in bytes).
        // The key is a ulong (8 bytes) containing the first 7 bytes of the property name
        // followed by a byte representing the length.
        private const int PropertyNameKeyLength = 7;

        // The limit to how many constructor parameter names from the JSON are cached in _parameterRefsSorted before using _parameterCache.
        private const int ParameterNameCountCacheThreshold = 32;

        // The limit to how many property names from the JSON are cached in _propertyRefsSorted before using PropertyCache.
        private const int PropertyNameCountCacheThreshold = 64;

        // The number of parameters the deserialization constructor has. If this is not equal to ParameterCache.Count, this means
        // that not all parameters are bound to object properties, and an exception will be thrown if deserialization is attempted.
        internal int ParameterCount { get; private set; }

        // All of the serializable parameters on a POCO constructor keyed on parameter name.
        // Only parameters which bind to properties are cached.
        internal JsonPropertyDictionary<JsonParameterInfo>? ParameterCache;

        // All of the serializable properties on a POCO (except the optional extension property) keyed on property name.
        internal JsonPropertyDictionary<JsonPropertyInfo>? PropertyCache;

        // Fast cache of constructor parameters by first JSON ordering; may not contain all parameters. Accessed before ParameterCache.
        // Use an array (instead of List<T>) for highest performance.
        private volatile ParameterRef[]? _parameterRefsSorted;

        // Fast cache of properties by first JSON ordering; may not contain all properties. Accessed before PropertyCache.
        // Use an array (instead of List<T>) for highest performance.
        private volatile PropertyRef[]? _propertyRefsSorted;

        internal Func<JsonSerializerContext, JsonPropertyInfo[]>? PropInitFunc;

        internal static JsonPropertyInfo AddProperty(
            MemberInfo memberInfo,
            Type memberType,
            Type parentClassType,
            JsonNumberHandling? parentTypeNumberHandling,
            JsonSerializerOptions options)
        {
            JsonIgnoreCondition? ignoreCondition = JsonPropertyInfo.GetAttribute<JsonIgnoreAttribute>(memberInfo)?.Condition;
            if (ignoreCondition == JsonIgnoreCondition.Always)
            {
                return JsonPropertyInfo.CreateIgnoredPropertyPlaceholder(memberInfo, options);
            }

            JsonConverter converter = GetConverter(
                memberType,
                parentClassType,
                memberInfo,
                out Type runtimeType,
                options);

            return CreateProperty(
                declaredPropertyType: memberType,
                runtimePropertyType: runtimeType,
                memberInfo,
                parentClassType,
                converter,
                options,
                parentTypeNumberHandling,
                ignoreCondition);
        }

        internal static JsonPropertyInfo CreateProperty(
            Type declaredPropertyType,
            Type? runtimePropertyType,
            MemberInfo? memberInfo,
            Type parentClassType,
            JsonConverter converter,
            JsonSerializerOptions options,
            JsonNumberHandling? parentTypeNumberHandling = null,
            JsonIgnoreCondition? ignoreCondition = null)
        {
            // Create the JsonPropertyInfo instance.
            JsonPropertyInfo jsonPropertyInfo = converter.CreateJsonPropertyInfo();

            jsonPropertyInfo.Initialize(
                parentClassType,
                declaredPropertyType,
                runtimePropertyType,
                runtimeClassType: converter.ConverterStrategy,
                memberInfo,
                converter,
                ignoreCondition,
                parentTypeNumberHandling,
                options);

            return jsonPropertyInfo;
        }

        /// <summary>
        /// Create a <see cref="JsonPropertyInfo"/> for a given Type.
        /// See <seealso cref="JsonTypeInfo.PropertyInfoForTypeInfo"/>.
        /// </summary>
        internal static JsonPropertyInfo CreatePropertyInfoForTypeInfo(
            Type declaredPropertyType,
            Type runtimePropertyType,
            JsonConverter converter,
            JsonNumberHandling? numberHandling,
            JsonSerializerOptions options)
        {
            JsonPropertyInfo jsonPropertyInfo = CreateProperty(
                declaredPropertyType: declaredPropertyType,
                runtimePropertyType: runtimePropertyType,
                memberInfo: null, // Not a real property so this is null.
                parentClassType: JsonTypeInfo.ObjectType, // a dummy value (not used)
                converter: converter,
                options,
                parentTypeNumberHandling: numberHandling);

            Debug.Assert(jsonPropertyInfo.IsForTypeInfo);

            return jsonPropertyInfo;
        }

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonPropertyInfo GetProperty(
            ReadOnlySpan<byte> propertyName,
            ref ReadStackFrame frame,
            out byte[] utf8PropertyName)
        {
            PropertyRef propertyRef;

            ulong key = GetKey(propertyName);

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
                        propertyRef = localPropertyRefsSorted[iForward];
                        if (IsPropertyRefEqual(propertyRef, propertyName, key))
                        {
                            utf8PropertyName = propertyRef.NameFromJson;
                            return propertyRef.Info;
                        }

                        ++iForward;

                        if (iBackward >= 0)
                        {
                            propertyRef = localPropertyRefsSorted[iBackward];
                            if (IsPropertyRefEqual(propertyRef, propertyName, key))
                            {
                                utf8PropertyName = propertyRef.NameFromJson;
                                return propertyRef.Info;
                            }

                            --iBackward;
                        }
                    }
                    else if (iBackward >= 0)
                    {
                        propertyRef = localPropertyRefsSorted[iBackward];
                        if (IsPropertyRefEqual(propertyRef, propertyName, key))
                        {
                            utf8PropertyName = propertyRef.NameFromJson;
                            return propertyRef.Info;
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

            // No cached item was found. Try the main dictionary which has all of the properties.
            Debug.Assert(PropertyCache != null);

            if (PropertyCache.TryGetValue(JsonHelpers.Utf8GetString(propertyName), out JsonPropertyInfo? info))
            {
                Debug.Assert(info != null);

                if (Options.PropertyNameCaseInsensitive)
                {
                    if (propertyName.SequenceEqual(info.NameAsUtf8Bytes))
                    {
                        Debug.Assert(key == GetKey(info.NameAsUtf8Bytes.AsSpan()));

                        // Use the existing byte[] reference instead of creating another one.
                        utf8PropertyName = info.NameAsUtf8Bytes!;
                    }
                    else
                    {
                        // Make a copy of the original Span.
                        utf8PropertyName = propertyName.ToArray();
                    }
                }
                else
                {
                    Debug.Assert(key == GetKey(info.NameAsUtf8Bytes.AsSpan()));
                    utf8PropertyName = info.NameAsUtf8Bytes;
                }
            }
            else
            {
                info = JsonPropertyInfo.s_missingProperty;

                // Make a copy of the original Span.
                utf8PropertyName = propertyName.ToArray();
            }

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

                    Debug.Assert(info != null);

                    propertyRef = new PropertyRef(key, info, utf8PropertyName);
                    frame.PropertyRefCache.Add(propertyRef);
                }
            }

            return info;
        }

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonParameterInfo? GetParameter(
            ReadOnlySpan<byte> propertyName,
            ref ReadStackFrame frame,
            out byte[] utf8PropertyName)
        {
            ParameterRef parameterRef;

            ulong key = GetKey(propertyName);

            // Keep a local copy of the cache in case it changes by another thread.
            ParameterRef[]? localParameterRefsSorted = _parameterRefsSorted;

            // If there is an existing cache, then use it.
            if (localParameterRefsSorted != null)
            {
                // Start with the current parameter index, and then go forwards\backwards.
                int parameterIndex = frame.CtorArgumentState!.ParameterIndex;

                int count = localParameterRefsSorted.Length;
                int iForward = Math.Min(parameterIndex, count);
                int iBackward = iForward - 1;

                while (true)
                {
                    if (iForward < count)
                    {
                        parameterRef = localParameterRefsSorted[iForward];
                        if (IsParameterRefEqual(parameterRef, propertyName, key))
                        {
                            utf8PropertyName = parameterRef.NameFromJson;
                            return parameterRef.Info;
                        }

                        ++iForward;

                        if (iBackward >= 0)
                        {
                            parameterRef = localParameterRefsSorted[iBackward];
                            if (IsParameterRefEqual(parameterRef, propertyName, key))
                            {
                                utf8PropertyName = parameterRef.NameFromJson;
                                return parameterRef.Info;
                            }

                            --iBackward;
                        }
                    }
                    else if (iBackward >= 0)
                    {
                        parameterRef = localParameterRefsSorted[iBackward];
                        if (IsParameterRefEqual(parameterRef, propertyName, key))
                        {
                            utf8PropertyName = parameterRef.NameFromJson;
                            return parameterRef.Info;
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

            // No cached item was found. Try the main dictionary which has all of the parameters.
            Debug.Assert(ParameterCache != null);

            if (ParameterCache.TryGetValue(JsonHelpers.Utf8GetString(propertyName), out JsonParameterInfo? info))
            {
                Debug.Assert(info != null);

                if (Options.PropertyNameCaseInsensitive)
                {
                    if (propertyName.SequenceEqual(info.NameAsUtf8Bytes))
                    {
                        Debug.Assert(key == GetKey(info.NameAsUtf8Bytes.AsSpan()));

                        // Use the existing byte[] reference instead of creating another one.
                        utf8PropertyName = info.NameAsUtf8Bytes!;
                    }
                    else
                    {
                        // Make a copy of the original Span.
                        utf8PropertyName = propertyName.ToArray();
                    }
                }
                else
                {
                    Debug.Assert(key == GetKey(info.NameAsUtf8Bytes!.AsSpan()));
                    utf8PropertyName = info.NameAsUtf8Bytes!;
                }
            }
            else
            {
                Debug.Assert(info == null);

                // Make a copy of the original Span.
                utf8PropertyName = propertyName.ToArray();
            }

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
                if (frame.CtorArgumentState!.ParameterRefCache != null)
                {
                    cacheCount += frame.CtorArgumentState.ParameterRefCache.Count;
                }

                // Check again to append the cache up to the threshold.
                if (cacheCount < ParameterNameCountCacheThreshold)
                {
                    if (frame.CtorArgumentState.ParameterRefCache == null)
                    {
                        frame.CtorArgumentState.ParameterRefCache = new List<ParameterRef>();
                    }

                    parameterRef = new ParameterRef(key, info!, utf8PropertyName);
                    frame.CtorArgumentState.ParameterRefCache.Add(parameterRef);
                }
            }

            return info;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPropertyRefEqual(in PropertyRef propertyRef, ReadOnlySpan<byte> propertyName, ulong key)
        {
            if (key == propertyRef.Key)
            {
                // We compare the whole name, although we could skip the first 7 bytes (but it's not any faster)
                if (propertyName.Length <= PropertyNameKeyLength ||
                    propertyName.SequenceEqual(propertyRef.NameFromJson))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsParameterRefEqual(in ParameterRef parameterRef, ReadOnlySpan<byte> parameterName, ulong key)
        {
            if (key == parameterRef.Key)
            {
                // We compare the whole name, although we could skip the first 7 bytes (but it's not any faster)
                if (parameterName.Length <= PropertyNameKeyLength ||
                    parameterName.SequenceEqual(parameterRef.NameFromJson))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get a key from the property name.
        /// The key consists of the first 7 bytes of the property name and then the length.
        /// </summary>
        // AggressiveInlining used since this method is only called from two locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong GetKey(ReadOnlySpan<byte> name)
        {
            ulong key;

            ref byte reference = ref MemoryMarshal.GetReference(name);
            int length = name.Length;

            if (length > 7)
            {
                key = Unsafe.ReadUnaligned<ulong>(ref reference) & 0x00ffffffffffffffL;
                key |= (ulong)Math.Min(length, 0xff) << 56;
            }
            else
            {
                key =
                    length > 5 ? Unsafe.ReadUnaligned<uint>(ref reference) | (ulong)Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref reference, 4)) << 32 :
                    length > 3 ? Unsafe.ReadUnaligned<uint>(ref reference) :
                    length > 1 ? Unsafe.ReadUnaligned<ushort>(ref reference) : 0UL;
                key |= (ulong)length << 56;

                if ((length & 1) != 0)
                {
                    var offset = length - 1;
                    key |= (ulong)Unsafe.Add(ref reference, offset) << (offset * 8);
                }
            }

#if DEBUG
            // Verify key contains the embedded bytes as expected.
            // Note: the expected properties do not hold true on big-endian platforms
            if (BitConverter.IsLittleEndian)
            {
                const int BitsInByte = 8;
                Debug.Assert(
                    // Verify embedded property name.
                    (name.Length < 1 || name[0] == ((key & ((ulong)0xFF << BitsInByte * 0)) >> BitsInByte * 0)) &&
                    (name.Length < 2 || name[1] == ((key & ((ulong)0xFF << BitsInByte * 1)) >> BitsInByte * 1)) &&
                    (name.Length < 3 || name[2] == ((key & ((ulong)0xFF << BitsInByte * 2)) >> BitsInByte * 2)) &&
                    (name.Length < 4 || name[3] == ((key & ((ulong)0xFF << BitsInByte * 3)) >> BitsInByte * 3)) &&
                    (name.Length < 5 || name[4] == ((key & ((ulong)0xFF << BitsInByte * 4)) >> BitsInByte * 4)) &&
                    (name.Length < 6 || name[5] == ((key & ((ulong)0xFF << BitsInByte * 5)) >> BitsInByte * 5)) &&
                    (name.Length < 7 || name[6] == ((key & ((ulong)0xFF << BitsInByte * 6)) >> BitsInByte * 6)) &&
                    // Verify embedded length.
                    (name.Length >= 0xFF || (key & ((ulong)0xFF << BitsInByte * 7)) >> BitsInByte * 7 == (ulong)name.Length) &&
                    (name.Length < 0xFF || (key & ((ulong)0xFF << BitsInByte * 7)) >> BitsInByte * 7 == 0xFF));
            }
#endif

            return key;
        }

        internal void UpdateSortedPropertyCache(ref ReadStackFrame frame)
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

        internal void UpdateSortedParameterCache(ref ReadStackFrame frame)
        {
            Debug.Assert(frame.CtorArgumentState!.ParameterRefCache != null);

            // frame.PropertyRefCache is only read\written by a single thread -- the thread performing
            // the deserialization for a given object instance.

            List<ParameterRef> listToAppend = frame.CtorArgumentState.ParameterRefCache;

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

            frame.CtorArgumentState.ParameterRefCache = null;
        }

        internal void InitializePropCache()
        {
            Debug.Assert(PropertyInfoForTypeInfo.ConverterStrategy == ConverterStrategy.Object);

            JsonSerializerContext? context = Options._context;
            Debug.Assert(context != null);

            if (PropInitFunc == null)
            {
                ThrowHelper.ThrowInvalidOperationException_NoMetadataForTypeProperties(context, Type);
                return;
            }

            JsonPropertyInfo[] array = PropInitFunc(context);
            var properties = new JsonPropertyDictionary<JsonPropertyInfo>(Options.PropertyNameCaseInsensitive, array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                JsonPropertyInfo property = array[i];
                if (!properties.TryAdd(property.NameAsString, property))
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(Type, property);
                }
            }

            // Avoid threading issues by populating a local cache, and assigning it to the global cache after completion.
            PropertyCache = properties;
        }
    }
}
