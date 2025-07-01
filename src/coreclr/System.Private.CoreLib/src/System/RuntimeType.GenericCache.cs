// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static System.RuntimeType;

namespace System
{
    internal sealed partial class RuntimeType
    {
        /// <summary>
        /// A composite cache entry that can store multiple cache entries of different kinds.
        /// </summary>
        internal sealed class CompositeCacheEntry : IGenericCacheEntry
        {
            internal ActivatorCache? _activatorCache;
            internal CreateUninitializedCache? _createUninitializedCache;
            internal RuntimeTypeCache.FunctionPointerCache? _functionPointerCache;
            internal Array.ArrayInitializeCache? _arrayInitializeCache;
            internal IGenericCacheEntry? _enumInfo;
            internal BoxCache? _boxCache;

            void IGenericCacheEntry.InitializeCompositeCache(CompositeCacheEntry compositeEntry) => throw new UnreachableException();
        }

        /// <summary>
        /// A base interface for all cache entries that can be stored in <see cref="RuntimeTypeCache.GenericCache"/>.
        /// </summary>
        internal interface IGenericCacheEntry
        {
            public void InitializeCompositeCache(CompositeCacheEntry compositeEntry);
        }

        /// <summary>
        /// A typed cache entry. This type provides a base type that handles contruction of entries and maintenance of
        /// the <see cref="RuntimeTypeCache.GenericCache"/>  in a <see cref="RuntimeType"/> .
        /// </summary>
        /// <typeparam name="TCache">The cache entry type.</typeparam>
        internal interface IGenericCacheEntry<TCache> : IGenericCacheEntry
            where TCache : class, IGenericCacheEntry<TCache>
        {
            public static abstract TCache Create(RuntimeType type);

            public static abstract ref TCache? GetStorageRef(CompositeCacheEntry compositeEntry);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TCache GetOrCreate(RuntimeType type)
            {
                ref IGenericCacheEntry? genericCache = ref type.Cache.GenericCache;
                // Read the GenericCache once to avoid multiple reads of the same field.
                IGenericCacheEntry? currentCache = genericCache;
                if (currentCache is not null)
                {
                    if (currentCache is TCache existing)
                    {
                        return existing;
                    }
                    if (currentCache is CompositeCacheEntry composite)
                    {
                        TCache? existingComposite = TCache.GetStorageRef(composite);
                        if (existingComposite != null)
                            return existingComposite;
                    }
                }

                return CreateAndCache(type);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TCache? Find(RuntimeType type)
            {
                ref IGenericCacheEntry? genericCache = ref type.Cache.GenericCache;
                // Read the GenericCache once to avoid multiple reads of the same field.
                IGenericCacheEntry? currentCache = genericCache;
                if (currentCache is not null)
                {
                   if (currentCache is TCache existing)
                    {
                        return existing;
                    }
                    if (currentCache is CompositeCacheEntry composite)
                    {
                        return TCache.GetStorageRef(composite);
                    }
                }

                return null;
            }

            public static TCache Replace(RuntimeType type, TCache newEntry)
            {
                ref IGenericCacheEntry? genericCache = ref type.Cache.GenericCache;

                // If the existing cache is of the same type, we can replace it directly,
                // as long as it is not upgraded to a composite cache simultaneously.
                while (true)
                {
                    IGenericCacheEntry? existing = genericCache;
                    if (existing is not (null or TCache))
                        break; // We lost the race and we can no longer replace the cache directly.

                    if (Interlocked.CompareExchange(ref genericCache, newEntry, existing) == existing)
                        return newEntry;
                    // We lost the race, try again.
                }

                // If we get here, either we have a composite cache or we need to upgrade to a composite cache.
                while (true)
                {
                    IGenericCacheEntry existing = genericCache!;
                    if (existing is not CompositeCacheEntry compositeCache)
                    {
                        compositeCache = new CompositeCacheEntry();
                        existing.InitializeCompositeCache(compositeCache);
                        if (Interlocked.CompareExchange(ref genericCache, compositeCache, existing) != existing)
                            continue; // We lost the race, try again.
                    }

                    TCache? existingEntry = TCache.GetStorageRef(compositeCache);
                    if (Interlocked.CompareExchange(ref TCache.GetStorageRef(compositeCache), newEntry, existingEntry) == existingEntry)
                        return newEntry;
                    // We lost the race, try again.
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static TCache CreateAndCache(RuntimeType type)
            {
                while (true)
                {
                    ref IGenericCacheEntry? genericCache = ref type.Cache.GenericCache;

                    // Update to CompositeCacheEntry if necessary
                    IGenericCacheEntry? existing = genericCache;
                    if (existing is null)
                    {
                        TCache newEntry = TCache.Create(type);
                        if (Interlocked.CompareExchange(ref genericCache, newEntry, null) == null)
                            return newEntry;
                        // We lost the race, try again.
                    }
                    else
                    {
                        if (existing is TCache existingTyped)
                            return existingTyped;

                        if (existing is not CompositeCacheEntry compositeCache)
                        {
                            compositeCache = new CompositeCacheEntry();
                            existing.InitializeCompositeCache(compositeCache);
                            if (Interlocked.CompareExchange(ref genericCache, compositeCache, existing) != existing)
                                continue; // We lost the race, try again.
                        }

                        TCache newEntry = TCache.Create(type);
                        // Try to put our entry in the composite cache, but only if someone else hasn't set the entry.
                        TCache? currentEntry = Interlocked.CompareExchange(ref TCache.GetStorageRef(compositeCache), newEntry, null);

                        // If currentEntry == null, then we won the race.
                        // Otherwise, we lost and should return the existing entry.
                        return currentEntry ?? newEntry;
                    }
                }
            }
        }
    }
}
