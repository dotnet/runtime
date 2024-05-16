// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    internal sealed partial class RuntimeType
    {
        /// <summary>
        /// A base interface for all cache entries that can be stored in <see cref="RuntimeTypeCache.GenericCache"/>.
        /// </summary>
        internal interface IGenericCacheEntry
        {
            /// <summary>
            /// The different kinds of entries that can be stored in <see cref="RuntimeTypeCache.GenericCache"/>.
            /// </summary>
            protected enum GenericCacheKind
            {
                ArrayInitialize,
                EnumInfo,
                Activator,
                CreateUninitialized,
                FunctionPointer,
                /// <summary>
                /// The number of different kinds of cache entries. This should always be the last entry.
                /// </summary>
                Count
            }

            protected abstract GenericCacheKind Kind { get; }

            /// <summary>
            /// A composite cache entry that can store multiple cache entries of different kinds.
            /// </summary>
            protected sealed class CompositeCacheEntry : IGenericCacheEntry
            {
                GenericCacheKind IGenericCacheEntry.Kind => throw new UnreachableException();

                [InlineArray((int)GenericCacheKind.Count)]
                private struct Storage
                {
                    // Typed as object as interfaces with static abstracts can't be
                    // used directly as generic types.
                    private IGenericCacheEntry? _field;
                }

                private Storage _storage;

                public CompositeCacheEntry(IGenericCacheEntry cache)
                {
                    _storage[(int)cache.Kind] = cache;
                }

                public IGenericCacheEntry? GetNestedCache(GenericCacheKind kind)
                {
                    return _storage[(int)kind];
                }

                public IGenericCacheEntry OverwriteNestedCache(GenericCacheKind kind, IGenericCacheEntry cache)
                {
                    _storage[(int)kind] = cache;
                    return cache;
                }
            }
        }

        /// <summary>
        /// A typed cache entry. This type provides a base type that handles contruction of entries and maintenance of
        /// the <see cref="RuntimeTypeCache.GenericCache"/>  in a <see cref="RuntimeType"/> .
        /// </summary>
        /// <typeparam name="TCache">The cache entry type.</typeparam>
        internal interface IGenericCacheEntry<TCache> : IGenericCacheEntry
            where TCache: class, IGenericCacheEntry<TCache>
        {
            GenericCacheKind IGenericCacheEntry.Kind => TCache.Kind;

            protected static new abstract GenericCacheKind Kind { get; }

            public static abstract TCache Create(RuntimeType type);

            private static CompositeCacheEntry GetOrUpgradeToCompositeCache(ref IGenericCacheEntry currentCache)
            {
                if (currentCache is not CompositeCacheEntry composite)
                {
                    // Convert the current cache into a composite cache.
                    currentCache = composite = new(currentCache);
                }

                return composite;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TCache GetOrCreate(RuntimeType type)
            {
                ref IGenericCacheEntry? genericCache = ref type.Cache.GenericCache;
                // Read the GenericCache once to avoid multiple reads of the same field.
                IGenericCacheEntry? currentCache = genericCache;
                if (currentCache is null)
                {
                    TCache newCache = TCache.Create(type);
                    genericCache = newCache;
                    return newCache;
                }
                else if (currentCache is TCache existing)
                {
                    return existing;
                }

                CompositeCacheEntry composite = GetOrUpgradeToCompositeCache(ref currentCache);
                // Update the GenericCache with the new composite cache if it changed.
                // If we race here it's okay, we might just end up re-creating a new entry next time.
                genericCache = currentCache;

                if (composite.GetNestedCache(TCache.Kind) is TCache cache)
                {
                    return cache;
                }

                return (TCache)composite.OverwriteNestedCache(TCache.Kind, TCache.Create(type));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TCache? Find(RuntimeType type)
            {
                IGenericCacheEntry? genericCache = type.CacheIfExists?.GenericCache;
                if (genericCache is null)
                {
                    return null;
                }
                else if (genericCache is TCache existing)
                {
                    return existing;
                }
                else if (genericCache is CompositeCacheEntry composite)
                {
                    return (TCache?)composite.GetNestedCache(TCache.Kind);
                }
                else
                {
                    return null;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Overwrite(RuntimeType type, TCache cache)
            {
                ref IGenericCacheEntry? genericCache = ref type.Cache.GenericCache;
                IGenericCacheEntry? currentCache = genericCache;
                if (currentCache is null)
                {
                    genericCache = cache;
                    return;
                }

                // Always upgrade to a composite cache here.
                // We would like to be able to avoid this when the GenericCache is the same type as the current cache,
                // but we can't easily do a lock-free CompareExchange with the current design,
                // and we can't assume that we won't have one thread adding another item to the cache
                // while another is trying to overwrite the (currently) only entry in the cache.
                CompositeCacheEntry composite = GetOrUpgradeToCompositeCache(ref currentCache);
                genericCache = currentCache;
                composite.OverwriteNestedCache(TCache.Kind, cache);
            }
        }
    }
}
