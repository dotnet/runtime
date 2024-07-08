// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Metadata
{
    public abstract partial class JsonTypeInfo
    {
        /// <summary>
        /// Cached typeof(object). It is faster to cache this than to call typeof(object) multiple times.
        /// </summary>
        internal static readonly Type ObjectType = typeof(object);

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
        /// Stores a cache of UTF-8 encoded property names and their associated JsonPropertyInfo, if available.
        /// Consulted before the <see cref="PropertyIndex" /> lookup to avoid added allocations and decoding costs.
        /// The cache is grown on-demand appending encountered unbounded properties or alternative casings.
        /// </summary>
        private PropertyRef[] _utf8PropertyCache = [];

        /// <summary>
        /// Defines the core property lookup logic for a given unescaped UTF-8 encoded property name.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonPropertyInfo? GetProperty(ReadOnlySpan<byte> propertyName, ref ReadStackFrame frame, out byte[] utf8PropertyName)
        {
            Debug.Assert(IsConfigured);

            // The logic can be broken up into roughly three stages:
            // 1. Look up the UTF-8 property cache for potential exact matches in the encoding.
            // 2. If no match is found, decode to UTF-16 and look up the primary dictionary.
            // 3. Store the new result for potential inclusion to the UTF-8 cache once deserialization is complete.

            PropertyRef[] utf8PropertyCache = _utf8PropertyCache; // Keep a local copy of the cache in case it changes by another thread.
            ReadOnlySpan<PropertyRef> utf8PropertyCacheSpan = utf8PropertyCache;
            ulong key = PropertyRef.GetKey(propertyName);

            if (!utf8PropertyCacheSpan.IsEmpty)
            {
                PropertyRef propertyRef;

                // Start with the current property index, and then go forwards\backwards.
                int propertyIndex = frame.PropertyIndex;

                int count = utf8PropertyCacheSpan.Length;
                int iForward = Math.Min(propertyIndex, count);
                int iBackward = iForward - 1;

                while (true)
                {
                    if (iForward < count)
                    {
                        propertyRef = utf8PropertyCacheSpan[iForward];
                        if (propertyRef.Equals(propertyName, key))
                        {
                            utf8PropertyName = propertyRef.Utf8PropertyName;
                            return propertyRef.Info;
                        }

                        ++iForward;

                        if (iBackward >= 0)
                        {
                            propertyRef = utf8PropertyCacheSpan[iBackward];
                            if (propertyRef.Equals(propertyName, key))
                            {
                                utf8PropertyName = propertyRef.Utf8PropertyName;
                                return propertyRef.Info;
                            }

                            --iBackward;
                        }
                    }
                    else if (iBackward >= 0)
                    {
                        propertyRef = utf8PropertyCacheSpan[iBackward];
                        if (propertyRef.Equals(propertyName, key))
                        {
                            utf8PropertyName = propertyRef.Utf8PropertyName;
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
            if (PropertyIndex.TryLookupUtf8Key(propertyName, out JsonPropertyInfo? info) &&
                (!Options.PropertyNameCaseInsensitive || propertyName.SequenceEqual(info.NameAsUtf8Bytes)))
            {
                // We have an exact match in UTF8 encoding.
                utf8PropertyName = info.NameAsUtf8Bytes;
            }
            else
            {
                // Make a copy of the original Span.
                utf8PropertyName = propertyName.ToArray();
            }

            // Assuming there is capacity, store the new result for potential
            // inclusion to the UTF-8 cache once deserialization is complete.

            ref PropertyRefCacheBuilder? cacheBuilder = ref frame.PropertyRefCacheBuilder;
            if ((cacheBuilder?.TotalCount ?? utf8PropertyCache.Length) < PropertyRefCacheBuilder.MaxCapacity)
            {
                (cacheBuilder ??= new(utf8PropertyCache)).TryAdd(new(key, info, utf8PropertyName));
            }

            return info;
        }

        /// <summary>
        /// Attempts to update the UTF-8 property cache with the results gathered in the current deserialization operation.
        /// The update operation is done optimistically and results are discarded if the cache was updated by another thread.
        /// </summary>
        internal void UpdateUtf8PropertyCache(ref ReadStackFrame frame)
        {
            Debug.Assert(frame.PropertyRefCacheBuilder is { Count: > 0 });

            PropertyRef[]? currentCache = _utf8PropertyCache;
            PropertyRefCacheBuilder cacheBuilder = frame.PropertyRefCacheBuilder;

            if (currentCache == cacheBuilder.OriginalCache)
            {
                PropertyRef[] newCache = cacheBuilder.ToArray();
                Debug.Assert(newCache.Length <= PropertyRefCacheBuilder.MaxCapacity);
                _utf8PropertyCache = cacheBuilder.ToArray();
            }

            frame.PropertyRefCacheBuilder = null;
        }
    }
}
