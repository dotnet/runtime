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

            protected abstract GenericCacheKind CacheKind { get; }

            protected static abstract GenericCacheKind Kind { get; }

            /// <summary>
            /// A composite cache entry that can store multiple cache entries of different kinds.
            /// </summary>
            [InlineArray((int)GenericCacheKind.Count)]
            protected struct CompositeCacheEntry
            {
                // Typed as object as interfaces with static abstracts can't be
                // used directly as generic types.
                private object? _field;

                [UnscopedRef]
                private ref object? GetCacheFieldForKind(IGenericCacheEntry.GenericCacheKind kind)
                {
                    Span<object?> entries = this;
                    return ref entries[(int)kind];
                }

                public static CompositeCacheEntry Create(IGenericCacheEntry cache)
                {
                    CompositeCacheEntry composite = default;
                    composite.GetCacheFieldForKind(cache.CacheKind) = cache;
                    return composite;
                }

                public IGenericCacheEntry GetNestedCache(IGenericCacheEntry.GenericCacheKind kind)
                {
                    return (IGenericCacheEntry)GetCacheFieldForKind(kind)!;
                }

                public IGenericCacheEntry OverwriteNestedCache<T>(T cache)
                    where T : IGenericCacheEntry
                {
                    return (IGenericCacheEntry)(GetCacheFieldForKind(T.Kind) = cache);
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
            GenericCacheKind IGenericCacheEntry.CacheKind => TCache.Kind;

            public static abstract TCache Create(RuntimeType type);

            private static CompositeCacheEntry GetOrUpgradeToCompositeCache(ref object currentCache)
            {
                if (currentCache is not CompositeCacheEntry composite)
                {
                    // Convert the current cache into a composite cache.
                    composite = CompositeCacheEntry.Create((IGenericCacheEntry)currentCache);
                    currentCache = composite;
                }

                return composite;
            }

            public static TCache GetOrCreate(RuntimeType type)
            {
                ref object? genericCache = ref type.Cache.GenericCache;
                if (genericCache is null)
                {
                    TCache newCache = TCache.Create(type);
                    genericCache = newCache;
                    return newCache;
                }
                else if (genericCache is TCache existing)
                {
                    return existing;
                }

                CompositeCacheEntry composite = GetOrUpgradeToCompositeCache(ref genericCache);

                if (composite.GetNestedCache(TCache.Kind) is TCache cache)
                {
                    return cache;
                }

                return (TCache)composite.OverwriteNestedCache(TCache.Create(type));
            }

            public static TCache? Find(RuntimeType type)
            {
                object? genericCache = type.CacheIfExists?.GenericCache;
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
                    return (TCache)composite.GetNestedCache(TCache.Kind);
                }
                else
                {
                    return null;
                }
            }

            public static void Overwrite(RuntimeType type, TCache cache)
            {
                ref object? genericCache = ref type.Cache.GenericCache;
                if (genericCache is null)
                {
                    genericCache = cache;
                    return;
                }

                // Always upgrade to a composite cache here.
                // We would like to be able to avoid this when the GenericCache is the same type as the current cache,
                // but we can't easily do a lock-free CompareExchange with the current design,
                // and we can't assume that we won't have one thread adding another item to the cache
                // while another is trying to overwrite the (currently) only entry in the cache.
                CompositeCacheEntry composite = GetOrUpgradeToCompositeCache(ref genericCache);

                composite.OverwriteNestedCache(cache);
            }
        }
    }
}
