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
        /// A base interface for all cache entries that can be stored in <see cref="RuntimeType.GenericCache"/>.
        /// </summary>
        internal interface IGenericCacheEntry
        {
            /// <summary>
            /// The different kinds of entries that can be stored in <see cref="RuntimeType.GenericCache"/>.
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
        /// the <see cref="RuntimeType.GenericCache"/> storage.
        /// </summary>
        /// <typeparam name="TCache">The cache entry type.</typeparam>
        internal interface IGenericCacheEntry<TCache> : IGenericCacheEntry
            where TCache: class, IGenericCacheEntry<TCache>
        {
            GenericCacheKind IGenericCacheEntry.CacheKind => TCache.Kind;

            public static abstract TCache Create(RuntimeType type);

            private static CompositeCacheEntry GetOrUpgradeToCompositeCache(RuntimeType type)
            {
                // We may have multiple threads racing to add a new entry to the cache, causing an upgrade to a composite cache.
                // If we lose the race, it's okay (we'll re-create the missing entry again on another thread)
                // and trying to make the code resilient to this would be more expensive.
                // Just make sure we read the cache field once and set it if we need to update it.
                var maybeCompositeCache = type.GenericCache;
                if (maybeCompositeCache is not CompositeCacheEntry composite)
                {
                    // Convert the current cache into a composite cache.
                    composite = CompositeCacheEntry.Create((IGenericCacheEntry)maybeCompositeCache);
                    type.GenericCache = composite;
                }

                return composite;
            }

            public static TCache GetOrCreate(RuntimeType type)
            {
                if (type.GenericCache is null)
                {
                    TCache newCache = TCache.Create(type);
                    type.GenericCache = newCache;
                    return newCache;
                }
                else if (type.GenericCache is TCache existing)
                {
                    return existing;
                }

                CompositeCacheEntry composite = GetOrUpgradeToCompositeCache(type);

                if (composite.GetNestedCache(TCache.Kind) is TCache cache)
                {
                    return cache;
                }

                return (TCache)composite.OverwriteNestedCache(TCache.Create(type));
            }

            public static TCache? Find(RuntimeType type)
            {
                if (type.GenericCache is null)
                {
                    return null;
                }
                else if (type.GenericCache is TCache existing)
                {
                    return existing;
                }
                else if (type.GenericCache is CompositeCacheEntry composite)
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
                if (type.GenericCache is null)
                {
                    type.GenericCache = cache;
                    return;
                }

                // Always upgrade to a composite cache here.
                // We would like to be able to avoid this when the GenericCache is the same type as the current cache,
                // but we can't easily do a lock-free CompareExchange with the current design,
                // and we can't assume that we won't have one thread adding another item to the cache
                // while another is trying to overwrite the (currently) only entry in the cache.
                CompositeCacheEntry composite = GetOrUpgradeToCompositeCache(type);

                composite.OverwriteNestedCache(cache);
            }
        }
    }
}
