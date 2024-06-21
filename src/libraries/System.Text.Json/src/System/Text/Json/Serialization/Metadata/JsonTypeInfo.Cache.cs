// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    public abstract partial class JsonTypeInfo
    {
        /// <summary>
        /// Cached typeof(object). It is faster to cache this than to call typeof(object) multiple times.
        /// </summary>
        internal static readonly Type ObjectType = typeof(object);

        // The length of the property name embedded in the key (in bytes).
        // The key is a ulong (8 bytes) containing the first 7 bytes of the property name
        // followed by a byte representing the length.
        private const int PropertyNameKeyLength = 7;

        // The limit to how many property names from the JSON are cached in _propertyRefsSorted before using PropertyCache.
        private const int PropertyNameCountCacheThreshold = 64;

        // The number of parameters the deserialization constructor has. If this is not equal to ParameterCache.Count, this means
        // that not all parameters are bound to object properties, and an exception will be thrown if deserialization is attempted.
        internal int ParameterCount { get; private protected set; }

        // All of the serializable parameters on a POCO constructor
        internal ReadOnlySpan<JsonParameterInfo> ParameterCache
        {
            get
            {
                Debug.Assert(IsConfigured && _parameterCache is not null);
                return _parameterCache;
            }
        }

        internal bool UsesParameterizedConstructor
        {
            get
            {
                Debug.Assert(IsConfigured);
                return _parameterCache != null;
            }
        }

        private JsonParameterInfo[]? _parameterCache;

        // All of the serializable properties on a POCO (minus the extension property).
        internal ReadOnlySpan<JsonPropertyInfo> PropertyCache
        {
            get
            {
                Debug.Assert(IsConfigured && _propertyCache is not null);
                return _propertyCache;
            }
        }

        private JsonPropertyInfo[]? _propertyCache;

        // All of the serializable properties on a POCO (minus the extension property) keyed on property name.
        internal Dictionary<string, JsonPropertyInfo> PropertyIndex
        {
            get
            {
                Debug.Assert(IsConfigured && _propertyIndex is not null);
                return _propertyIndex;
            }
        }

        private Dictionary<string, JsonPropertyInfo>? _propertyIndex;

        // Fast cache of properties by first JSON ordering; may not contain all properties. Accessed before PropertyCache.
        // Use an array (instead of List<T>) for highest performance.
        private volatile PropertyRef[]? _propertyRefsSorted;

        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        internal JsonPropertyInfo CreatePropertyUsingReflection(Type propertyType, Type? declaringType)
        {
            JsonPropertyInfo jsonPropertyInfo;

            if (Options.TryGetTypeInfoCached(propertyType, out JsonTypeInfo? jsonTypeInfo))
            {
                // If a JsonTypeInfo has already been cached for the property type,
                // avoid reflection-based initialization by delegating construction
                // of JsonPropertyInfo<T> construction to the property type metadata.
                jsonPropertyInfo = jsonTypeInfo.CreateJsonPropertyInfo(declaringTypeInfo: this, declaringType, Options);
            }
            else
            {
                // Metadata for `propertyType` has not been registered yet.
                // Use reflection to instantiate the correct JsonPropertyInfo<T>
                Type propertyInfoType = typeof(JsonPropertyInfo<>).MakeGenericType(propertyType);
                jsonPropertyInfo = (JsonPropertyInfo)propertyInfoType.CreateInstanceNoWrapExceptions(
                    parameterTypes: new Type[] { typeof(Type), typeof(JsonTypeInfo), typeof(JsonSerializerOptions) },
                    parameters: new object[] { declaringType ?? Type, this, Options })!;
            }

            Debug.Assert(jsonPropertyInfo.PropertyType == propertyType);
            return jsonPropertyInfo;
        }

        /// <summary>
        /// Creates a JsonPropertyInfo whose property type matches the type of this JsonTypeInfo instance.
        /// </summary>
        private protected abstract JsonPropertyInfo CreateJsonPropertyInfo(JsonTypeInfo declaringTypeInfo, Type? declaringType, JsonSerializerOptions options);

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonPropertyInfo GetProperty(
            ReadOnlySpan<byte> propertyName,
            ref ReadStackFrame frame,
            out byte[] utf8PropertyName)
        {
            PropertyRef propertyRef;

            ValidateCanBeUsedForPropertyMetadataSerialization();
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
            if (PropertyIndex.TryGetValue(JsonHelpers.Utf8GetString(propertyName), out JsonPropertyInfo? info))
            {
                Debug.Assert(info != null, "PropertyCache contains null JsonPropertyInfo");

                if (Options.PropertyNameCaseInsensitive)
                {
                    if (propertyName.SequenceEqual(info.NameAsUtf8Bytes))
                    {
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
                    frame.PropertyRefCache ??= new List<PropertyRef>();

                    Debug.Assert(info != null);

                    propertyRef = new PropertyRef(key, info, utf8PropertyName);
                    frame.PropertyRefCache.Add(propertyRef);
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
                    (name.Length < 0xFF || (key & ((ulong)0xFF << BitsInByte * 7)) >> BitsInByte * 7 == 0xFF),
                    "Embedded bytes not as expected");
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
    }
}
