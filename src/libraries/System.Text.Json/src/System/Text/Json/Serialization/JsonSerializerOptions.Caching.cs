// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;

namespace System.Text.Json
{
    public sealed partial class JsonSerializerOptions
    {
        /// <summary>
        /// Encapsulates all cached metadata referenced by the current <see cref="JsonSerializerOptions" /> instance.
        /// Context can be shared across multiple equivalent options instances.
        /// </summary>
        private CachingContext? _cachingContext;

        // Simple LRU cache for the public (de)serialize entry points that avoid some lookups in _cachingContext.
        private volatile JsonTypeInfo? _lastTypeInfo;

        internal JsonTypeInfo GetOrAddJsonTypeInfo(Type type)
        {
            if (_cachingContext == null)
            {
                InitializeCachingContext();
                Debug.Assert(_cachingContext != null);
            }

            return _cachingContext.GetOrAddJsonTypeInfo(type);
        }

        internal bool TryGetJsonTypeInfo(Type type, [NotNullWhen(true)] out JsonTypeInfo? typeInfo)
        {
            if (_cachingContext == null)
            {
                typeInfo = null;
                return false;
            }

            return _cachingContext.TryGetJsonTypeInfo(type, out typeInfo);
        }

        internal bool IsJsonTypeInfoCached(Type type) => _cachingContext?.IsJsonTypeInfoCached(type) == true;

        /// <summary>
        /// Return the TypeInfo for root API calls.
        /// This has an LRU cache that is intended only for public API calls that specify the root type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonTypeInfo GetOrAddJsonTypeInfoForRootType(Type type)
        {
            JsonTypeInfo? jsonTypeInfo = _lastTypeInfo;

            if (jsonTypeInfo?.Type != type)
            {
                jsonTypeInfo = GetOrAddJsonTypeInfo(type);
                _lastTypeInfo = jsonTypeInfo;
            }

            return jsonTypeInfo;
        }

        internal void ClearCaches()
        {
            _cachingContext?.Clear();
            _lastTypeInfo = null;
        }

        private void InitializeCachingContext()
        {
            _cachingContext = TrackedCachingContexts.GetOrCreate(this);
            if (IsInitializedForReflectionSerializer)
            {
                _cachingContext.Options.IsInitializedForReflectionSerializer = true;
            }
        }

        /// <summary>
        /// Stores and manages all reflection caches for one or more <see cref="JsonSerializerOptions"/> instances.
        /// NB the type encapsulates the original options instance and only consults that one when building new types;
        /// this is to prevent multiple options instances from leaking into the object graphs of converters which
        /// could break user invariants.
        /// </summary>
        internal sealed class CachingContext
        {
            private readonly ConcurrentDictionary<Type, JsonConverter> _converterCache = new();
            private readonly ConcurrentDictionary<Type, JsonTypeInfo> _jsonTypeInfoCache = new();

            public CachingContext(JsonSerializerOptions options)
            {
                Options = options;
            }

            public JsonSerializerOptions Options { get; }
            // Property only accessed by reflection in testing -- do not remove.
            // If changing please ensure that src/ILLink.Descriptors.LibraryBuild.xml is up-to-date.
            public int Count => _converterCache.Count + _jsonTypeInfoCache.Count;
            public JsonConverter GetOrAddConverter(Type type) => _converterCache.GetOrAdd(type, Options.GetConverterFromType);
            public JsonTypeInfo GetOrAddJsonTypeInfo(Type type) => _jsonTypeInfoCache.GetOrAdd(type, Options.GetJsonTypeInfoFromContextOrCreate);
            public bool TryGetJsonTypeInfo(Type type, [NotNullWhen(true)] out JsonTypeInfo? typeInfo) => _jsonTypeInfoCache.TryGetValue(type, out typeInfo);
            public bool IsJsonTypeInfoCached(Type type) => _jsonTypeInfoCache.ContainsKey(type);

            public void Clear()
            {
                _converterCache.Clear();
                _jsonTypeInfoCache.Clear();
            }
        }

        /// <summary>
        /// Defines a cache of CachingContexts; instead of using a ConditionalWeakTable which can be slow to traverse
        /// this approach uses a concurrent dictionary pointing to weak references of <see cref="CachingContext"/>.
        /// Relevant caching contexts are looked up using the equality comparison defined by <see cref="EqualityComparer"/>.
        /// </summary>
        internal static class TrackedCachingContexts
        {
            private const int MaxTrackedContexts = 64;
            private static readonly ConcurrentDictionary<JsonSerializerOptions, WeakReference<CachingContext>> s_cache =
                new(concurrencyLevel: 1, capacity: MaxTrackedContexts, new EqualityComparer());

            private const int EvictionCountHistory = 16;
            private static Queue<int> s_recentEvictionCounts = new(EvictionCountHistory);
            private static int s_evictionRunsToSkip;

            public static CachingContext GetOrCreate(JsonSerializerOptions options)
            {
                ConcurrentDictionary<JsonSerializerOptions, WeakReference<CachingContext>> cache = s_cache;

                if (cache.TryGetValue(options, out WeakReference<CachingContext>? wr) && wr.TryGetTarget(out CachingContext? ctx))
                {
                    return ctx;
                }

                lock (cache)
                {
                    if (cache.TryGetValue(options, out wr))
                    {
                        if (!wr.TryGetTarget(out ctx))
                        {
                            // Found a dangling weak reference; replenish with a fresh instance.
                            ctx = new CachingContext(options);
                            wr.SetTarget(ctx);
                        }

                        return ctx;
                    }

                    if (cache.Count == MaxTrackedContexts)
                    {
                        if (!TryEvictDanglingEntries())
                        {
                            // Cache is full; return a fresh instance.
                            return new CachingContext(options);
                        }
                    }

                    Debug.Assert(cache.Count < MaxTrackedContexts);

                    // Use a defensive copy of the options instance as key to
                    // avoid capturing references to any caching contexts.
                    var key = new JsonSerializerOptions(options)
                    {
                        // Copy fields ignored by the copy constructor
                        // but are necessary to determine equivalence.
                        _serializerContext = options._serializerContext,
                    };
                    Debug.Assert(key._cachingContext == null);

                    ctx = new CachingContext(options);
                    bool success = cache.TryAdd(key, new WeakReference<CachingContext>(ctx));
                    Debug.Assert(success);

                    return ctx;
                }
            }

            public static void Clear()
            {
                lock (s_cache)
                {
                    s_cache.Clear();
                    s_recentEvictionCounts.Clear();
                    s_evictionRunsToSkip = 0;
                }
            }

            private static bool TryEvictDanglingEntries()
            {
                // Worst case scenario, the cache has been filled with permanent entries.
                // Evictions are synchronized and each run is in the order of microseconds,
                // so we want to avoid triggering runs every time an instance is initialized,
                // For this reason we use a backoff strategy to average out the cost of eviction
                // across multiple initializations. The backoff count is determined by the eviction
                // rates of the most recent runs.

                Debug.Assert(Monitor.IsEntered(s_cache));

                if (s_evictionRunsToSkip > 0)
                {
                    --s_evictionRunsToSkip;
                    return false;
                }

                int currentEvictions = 0;
                foreach (KeyValuePair<JsonSerializerOptions, WeakReference<CachingContext>> kvp in s_cache)
                {
                    if (!kvp.Value.TryGetTarget(out _))
                    {
                        bool result = s_cache.TryRemove(kvp.Key, out _);
                        Debug.Assert(result);
                        currentEvictions++;
                    }
                }

                s_evictionRunsToSkip = EstimateEvictionRunsToSkip(currentEvictions);
                return currentEvictions > 0;

                // Estimate the number of eviction runs to skip based on recent eviction rates.
                static int EstimateEvictionRunsToSkip(int latestEvictionCount)
                {
                    Queue<int> recentEvictionCounts = s_recentEvictionCounts;

                    if (recentEvictionCounts.Count < EvictionCountHistory - 1)
                    {
                        // Insufficient data points to determine a skip count.
                        recentEvictionCounts.Enqueue(latestEvictionCount);
                        return 0;
                    }
                    else if (recentEvictionCounts.Count == EvictionCountHistory)
                    {
                        recentEvictionCounts.Dequeue();
                    }

                    recentEvictionCounts.Enqueue(latestEvictionCount);

                    // Calculate the total number of eviction in the latest runs
                    // - If we have at least one eviction per run, on average,
                    //   do not skip any future eviction runs.
                    // - Otherwise, skip ~the number of runs needed per one eviction.

                    int totalEvictions = 0;
                    foreach (int evictionCount in recentEvictionCounts)
                    {
                        totalEvictions += evictionCount;
                    }

                    int evictionRunsToSkip =
                        totalEvictions >= EvictionCountHistory ? 0 :
                        (int)Math.Round((double)EvictionCountHistory / Math.Max(totalEvictions, 1));

                    Debug.Assert(0 <= evictionRunsToSkip && evictionRunsToSkip <= EvictionCountHistory);
                    return evictionRunsToSkip;
                }
            }
        }

        /// <summary>
        /// Provides a conservative equality comparison for JsonSerializerOptions instances.
        /// If two instances are equivalent, they should generate identical metadata caches;
        /// the converse however does not necessarily hold.
        /// </summary>
        private sealed class EqualityComparer : IEqualityComparer<JsonSerializerOptions>
        {
            public bool Equals(JsonSerializerOptions? left, JsonSerializerOptions? right)
            {
                Debug.Assert(left != null && right != null);
                return
                    left._dictionaryKeyPolicy == right._dictionaryKeyPolicy &&
                    left._jsonPropertyNamingPolicy == right._jsonPropertyNamingPolicy &&
                    left._readCommentHandling == right._readCommentHandling &&
                    left._referenceHandler == right._referenceHandler &&
                    left._encoder == right._encoder &&
                    left._defaultIgnoreCondition == right._defaultIgnoreCondition &&
                    left._numberHandling == right._numberHandling &&
                    left._unknownTypeHandling == right._unknownTypeHandling &&
                    left._defaultBufferSize == right._defaultBufferSize &&
                    left._maxDepth == right._maxDepth &&
                    left._allowTrailingCommas == right._allowTrailingCommas &&
                    left._ignoreNullValues == right._ignoreNullValues &&
                    left._ignoreReadOnlyProperties == right._ignoreReadOnlyProperties &&
                    left._ignoreReadonlyFields == right._ignoreReadonlyFields &&
                    left._includeFields == right._includeFields &&
                    left._propertyNameCaseInsensitive == right._propertyNameCaseInsensitive &&
                    left._writeIndented == right._writeIndented &&
                    left._serializerContext == right._serializerContext &&
                    CompareConverters(left._converters, right._converters);

                static bool CompareConverters(ConverterList left, ConverterList right)
                {
                    int n;
                    if ((n = left.Count) != right.Count)
                    {
                        return false;
                    }

                    for (int i = 0; i < n; i++)
                    {
                        if (left[i] != right[i])
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            public int GetHashCode(JsonSerializerOptions options)
            {
                HashCode hc = default;

                hc.Add(options._dictionaryKeyPolicy);
                hc.Add(options._jsonPropertyNamingPolicy);
                hc.Add(options._readCommentHandling);
                hc.Add(options._referenceHandler);
                hc.Add(options._encoder);
                hc.Add(options._defaultIgnoreCondition);
                hc.Add(options._numberHandling);
                hc.Add(options._unknownTypeHandling);
                hc.Add(options._defaultBufferSize);
                hc.Add(options._maxDepth);
                hc.Add(options._allowTrailingCommas);
                hc.Add(options._ignoreNullValues);
                hc.Add(options._ignoreReadOnlyProperties);
                hc.Add(options._ignoreReadonlyFields);
                hc.Add(options._includeFields);
                hc.Add(options._propertyNameCaseInsensitive);
                hc.Add(options._writeIndented);
                hc.Add(options._serializerContext);

                for (int i = 0; i < options._converters.Count; i++)
                {
                    hc.Add(options._converters[i]);
                }

                return hc.ToHashCode();
            }

#if !NETCOREAPP
            /// <summary>
            /// Polyfill for System.HashCode.
            /// </summary>
            private struct HashCode
            {
                private int _hashCode;
                public void Add<T>(T? value) => _hashCode = (_hashCode, value).GetHashCode();
                public int ToHashCode() => _hashCode;
            }
#endif
        }
    }
}
