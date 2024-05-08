// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
            public enum GenericCacheKind
            {
                ArrayInitialize,
                EnumInfo,
                Activator,
                CreateUninitialized,
                FunctionPointer
            }

            public abstract GenericCacheKind CacheKind { get; }

            public static abstract GenericCacheKind Kind { get; }

            /// <summary>
            /// A composite cache entry that can store multiple cache entries of different kinds.
            /// </summary>
            /// <remarks>
            /// For better performance, each entry is stored as a separate field instead of something like a dictionary.
            /// </remarks>
            protected sealed class CompositeCacheEntry
            {
                private IGenericCacheEntry? _arrayInitializeCache;
                private IGenericCacheEntry? _enumInfoCache;
                private IGenericCacheEntry? _activatorCache;
                private IGenericCacheEntry? _createUninitializedCache;
                private IGenericCacheEntry? _functionPointerCache;

                private ref IGenericCacheEntry? GetCacheFieldForKind(IGenericCacheEntry.GenericCacheKind kind)
                {
                    switch (kind)
                    {
                        case IGenericCacheEntry.GenericCacheKind.ArrayInitialize:
                            return ref _arrayInitializeCache;
                        case IGenericCacheEntry.GenericCacheKind.EnumInfo:
                            return ref _enumInfoCache;
                        case IGenericCacheEntry.GenericCacheKind.Activator:
                            return ref _activatorCache;
                        case IGenericCacheEntry.GenericCacheKind.CreateUninitialized:
                            return ref _createUninitializedCache;
                        case IGenericCacheEntry.GenericCacheKind.FunctionPointer:
                            return ref _functionPointerCache;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(kind));
                    }
                }

                public static CompositeCacheEntry Create(IGenericCacheEntry cache)
                {
                    var composite = new CompositeCacheEntry();
                    composite.GetCacheFieldForKind(cache.CacheKind) = cache;
                    return composite;
                }

                public IGenericCacheEntry GetNestedCache(IGenericCacheEntry.GenericCacheKind kind)
                {
                    return GetCacheFieldForKind(kind)!;
                }

                public IGenericCacheEntry OverwriteNestedCache<T>(T cache)
                    where T : IGenericCacheEntry
                {
                    return GetCacheFieldForKind(T.Kind) = cache;
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

            public static TCache GetOrCreate(RuntimeType type)
            {
                if (type.GenericCache is null)
                {
                    TCache newCache = TCache.Create(type);
                    type.GenericCache = newCache;
                    return newCache;
                }
                else if (type.GenericCache is IGenericCacheEntry existing && existing.CacheKind == TCache.Kind)
                {
                    return (TCache)type.GenericCache;
                }

                if (type.GenericCache is not CompositeCacheEntry composite)
                {
                    // Convert the current cache into a composite cache.
                    composite = CompositeCacheEntry.Create((IGenericCacheEntry)type.GenericCache);
                    type.GenericCache = composite;
                }

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
                else if (type.GenericCache is IGenericCacheEntry existing && existing.CacheKind == TCache.Kind)
                {
                    return (TCache)type.GenericCache;
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
                else if (type.GenericCache is IGenericCacheEntry existing && existing.CacheKind == TCache.Kind)
                {
                    type.GenericCache = cache;
                    return;
                }

                if (type.GenericCache is not CompositeCacheEntry composite)
                {
                    // Convert the current cache into a composite cache.
                    composite = CompositeCacheEntry.Create((IGenericCacheEntry)type.GenericCache);
                    type.GenericCache = composite;
                }

                composite.OverwriteNestedCache(cache);
            }
        }
    }
}
