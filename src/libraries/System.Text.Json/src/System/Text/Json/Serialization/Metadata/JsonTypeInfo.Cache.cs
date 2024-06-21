// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

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

        /// <summary>
        /// The maximum number of entries to be stored in <see cref="_utf8PropertyCache" />.
        /// </summary>
        private const int MaxUtf8PropertyCacheCapacity = 64;

        /// <summary>
        /// Stores a cache of UTF-8 encoded property names and their associated JsonPropertyInfo, if available.
        /// Consulted before the <see cref="PropertyIndex" /> lookup to avoid added allocations and decoding costs.
        /// The cache is grown on-demand appending encountered unbounded properties or alternative casings.
        /// </summary>
        private volatile PropertyRef[]? _utf8PropertyCache;

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

        /// <summary>
        /// Defines the core property lookup logic for a given unescaped UTF-8 encoded property name.
        /// </summary>
        internal JsonPropertyInfo? GetProperty(ReadOnlySpan<byte> propertyName, ref ReadStackFrame frame, out byte[] utf8PropertyName)
        {
            // The logic can be broken up into roughly three stages:
            // 1. Look up the UTF-8 property cache for potential exact matches in the encoding.
            // 2. If no match is found, decode to UTF-16 and look up the primary dictionary.
            // 3. Store the new result for potential inclusion to the UTF-8 cache once deserialization is complete.

            PropertyRef[]? utf8PropertyCache = _utf8PropertyCache; // Keep a local copy of the cache in case it changes by another thread.
            ReadOnlySpan<PropertyRef> utf8PropertyCacheSpan = utf8PropertyCache;
            ulong key = GetKey(propertyName);

            if (!utf8PropertyCacheSpan.IsEmpty)
            {
                // Start with the current property index, and then go forwards\backwards.
                int propertyIndex = frame.PropertyIndex;

                int count = utf8PropertyCacheSpan.Length;
                int iForward = Math.Min(propertyIndex, count);
                int iBackward = iForward;

                while (iForward - iBackward < count)
                {
                    if (iForward < count)
                    {
                        PropertyRef propertyRef = utf8PropertyCacheSpan[iForward++];
                        if (IsPropertyRefEqual(propertyRef, propertyName, key))
                        {
                            utf8PropertyName = propertyRef.Utf8PropertyName;
                            return propertyRef.Info;
                        }
                    }

                    if (iBackward > 0)
                    {
                        PropertyRef propertyRef = utf8PropertyCacheSpan[--iBackward];
                        if (IsPropertyRefEqual(propertyRef, propertyName, key))
                        {
                            utf8PropertyName = propertyRef.Utf8PropertyName;
                            return propertyRef.Info;
                        }
                    }
                }
            }

            // No cached item was found. Try the main dictionary which has all of the properties.
            if (PropertyIndex.TryLookupUtf8Key(propertyName, out JsonPropertyInfo? info))
            {
                if (!Options.PropertyNameCaseInsensitive || propertyName.SequenceEqual(info.NameAsUtf8Bytes))
                {
                    // We have an exact match in UTF8 encoding.
                    utf8PropertyName = info.NameAsUtf8Bytes;
                }
                else
                {
                    // Make a copy of the original Span.
                    utf8PropertyName = propertyName.ToArray();
                }
            }
            else
            {
                // Make a copy of the original Span.
                utf8PropertyName = propertyName.ToArray();
            }

            // Assuming there is capacity, store the new result for potential
            // inclusion to the UTF-8 cache once deserialization is complete.

            ref PropertyRefList? entriesToAdd = ref frame.NewPropertyRefs;
            int cacheCount = utf8PropertyCacheSpan.Length + (entriesToAdd?.Count ?? 0);
            if (cacheCount < MaxUtf8PropertyCacheCapacity)
            {
                entriesToAdd ??= new PropertyRefList(originalCache: utf8PropertyCache);
                entriesToAdd.Add(new(key, info, utf8PropertyName));
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
                    propertyName.SequenceEqual(propertyRef.Utf8PropertyName))
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
                    int offset = length - 1;
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

        /// <summary>
        /// Attempts to update the UTF-8 property cache with the results gathered in the current deserialization operation.
        /// The update operation is done optimistically and results are discarded if the cache was updated concurrently.
        /// This is done to avoid polluting the cache with duplicate entries being appended by contending threads.
        /// </summary>
        internal void UpdateUtf8PropertyCache(ref ReadStackFrame frame)
        {
            Debug.Assert(frame.NewPropertyRefs is { Count: > 0 });

            PropertyRef[]? currentCache = _utf8PropertyCache;
            PropertyRefList newPropertyRefs = frame.NewPropertyRefs;

            if (currentCache == newPropertyRefs.OriginalCache)
            {
                Debug.Assert((currentCache?.Length ?? 0) + newPropertyRefs.Count <= MaxUtf8PropertyCacheCapacity);
                PropertyRef[] newCache = currentCache is null ? [.. newPropertyRefs] : [.. currentCache, .. newPropertyRefs];
                Interlocked.CompareExchange(ref _utf8PropertyCache, newCache, currentCache);
            }

            frame.NewPropertyRefs = null;
        }
    }
}
