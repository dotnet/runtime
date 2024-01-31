// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal CachingContext CacheContext
        {
            get
            {
                Debug.Assert(IsReadOnly);
                return _cachingContext ?? GetOrCreate();

                CachingContext GetOrCreate()
                {
                    CachingContext ctx = TrackedCachingContexts.GetOrCreate(this);
                    return Interlocked.CompareExchange(ref _cachingContext, ctx, null) ?? ctx;
                }
            }
        }

        private CachingContext? _cachingContext;

        // Simple LRU cache for the public (de)serialize entry points that avoid some lookups in _cachingContext.
        private volatile JsonTypeInfo? _lastTypeInfo;

        /// <summary>
        /// Gets the <see cref="JsonTypeInfo"/> contract metadata resolved by the current <see cref="JsonSerializerOptions"/> instance.
        /// </summary>
        /// <param name="type">The type to resolve contract metadata for.</param>
        /// <returns>The contract metadata resolved for <paramref name="type"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="type"/> is not valid for serialization.</exception>
        /// <remarks>
        /// Returned metadata can be downcast to <see cref="JsonTypeInfo{T}"/> and used with the relevant <see cref="JsonSerializer"/> overloads.
        ///
        /// If the <see cref="JsonSerializerOptions"/> instance is locked for modification, the method will return a cached instance for the metadata.
        /// </remarks>
        public JsonTypeInfo GetTypeInfo(Type type)
        {
            if (type is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(type));
            }

            if (JsonTypeInfo.IsInvalidForSerialization(type))
            {
                ThrowHelper.ThrowArgumentException_CannotSerializeInvalidType(nameof(type), type, null, null);
            }

            return GetTypeInfoInternal(type, resolveIfMutable: true);
        }

        /// <summary>
        /// Tries to get the <see cref="JsonTypeInfo"/> contract metadata resolved by the current <see cref="JsonSerializerOptions"/> instance.
        /// </summary>
        /// <param name="type">The type to resolve contract metadata for.</param>
        /// <param name="typeInfo">The resolved contract metadata, or <see langword="null" /> if not contract could be resolved.</param>
        /// <returns><see langword="true"/> if a contract for <paramref name="type"/> was found, or <see langword="false"/> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="type"/> is not valid for serialization.</exception>
        /// <remarks>
        /// Returned metadata can be downcast to <see cref="JsonTypeInfo{T}"/> and used with the relevant <see cref="JsonSerializer"/> overloads.
        ///
        /// If the <see cref="JsonSerializerOptions"/> instance is locked for modification, the method will return a cached instance for the metadata.
        /// </remarks>
        public bool TryGetTypeInfo(Type type, [NotNullWhen(true)] out JsonTypeInfo? typeInfo)
        {
            if (type is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(type));
            }

            if (JsonTypeInfo.IsInvalidForSerialization(type))
            {
                ThrowHelper.ThrowArgumentException_CannotSerializeInvalidType(nameof(type), type, null, null);
            }

            typeInfo = GetTypeInfoInternal(type, ensureNotNull: null, resolveIfMutable: true);
            return typeInfo is not null;
        }

        /// <summary>
        /// Same as GetTypeInfo but without validation and additional knobs.
        /// </summary>
        [return: NotNullIfNotNull(nameof(ensureNotNull))]
        internal JsonTypeInfo? GetTypeInfoInternal(
            Type type,
            bool ensureConfigured = true,
            // We can't assert non-nullability on the basis of boolean parameters,
            // so use a nullable representation instead to piggy-back on the NotNullIfNotNull attribute.
            bool? ensureNotNull = true,
            bool resolveIfMutable = false,
            bool fallBackToNearestAncestorType = false)
        {
            Debug.Assert(!fallBackToNearestAncestorType || IsReadOnly, "ancestor resolution should only be invoked in read-only options.");
            Debug.Assert(ensureNotNull is null or true, "Explicitly passing false will result in invalid result annotation.");

            JsonTypeInfo? typeInfo = null;

            if (IsReadOnly)
            {
                typeInfo = CacheContext.GetOrAddTypeInfo(type, fallBackToNearestAncestorType);
                if (ensureConfigured)
                {
                    typeInfo?.EnsureConfigured();
                }
            }
            else if (resolveIfMutable)
            {
                typeInfo = GetTypeInfoNoCaching(type);
            }

            if (typeInfo is null && ensureNotNull == true)
            {
                ThrowHelper.ThrowNotSupportedException_NoMetadataForType(type, TypeInfoResolver);
            }

            return typeInfo;
        }

        internal bool TryGetTypeInfoCached(Type type, [NotNullWhen(true)] out JsonTypeInfo? typeInfo)
        {
            if (_cachingContext == null)
            {
                typeInfo = null;
                return false;
            }

            return _cachingContext.TryGetTypeInfo(type, out typeInfo);
        }

        /// <summary>
        /// Return the TypeInfo for root API calls.
        /// This has an LRU cache that is intended only for public API calls that specify the root type.
        /// </summary>
        internal JsonTypeInfo GetTypeInfoForRootType(Type type, bool fallBackToNearestAncestorType = false)
        {
            JsonTypeInfo? jsonTypeInfo = _lastTypeInfo;

            if (jsonTypeInfo?.Type != type)
            {
                _lastTypeInfo = jsonTypeInfo = GetTypeInfoInternal(type, fallBackToNearestAncestorType: fallBackToNearestAncestorType);
            }

            return jsonTypeInfo;
        }

        internal bool TryGetPolymorphicTypeInfoForRootType(object rootValue, [NotNullWhen(true)] out JsonTypeInfo? polymorphicTypeInfo)
        {
            Debug.Assert(rootValue != null);

            Type runtimeType = rootValue.GetType();
            if (runtimeType != JsonTypeInfo.ObjectType)
            {
                // To determine the contract for an object value:
                // 1. Find the JsonTypeInfo for the runtime type with fallback to the nearest ancestor, if not available.
                // 2. If the resolved type is deriving from a polymorphic type, use the contract of the polymorphic type instead.
                polymorphicTypeInfo = GetTypeInfoForRootType(runtimeType, fallBackToNearestAncestorType: true);
                if (polymorphicTypeInfo.AncestorPolymorphicType is { } ancestorPolymorphicType)
                {
                    polymorphicTypeInfo = ancestorPolymorphicType;
                }
                return true;
            }

            polymorphicTypeInfo = null;
            return false;
        }

        // Caches the resolved JsonTypeInfo<object> for faster access during root-level object type serialization.
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal JsonTypeInfo ObjectTypeInfo
        {
            get
            {
                Debug.Assert(IsReadOnly);
                return _objectTypeInfo ??= GetTypeInfoInternal(JsonTypeInfo.ObjectType);
            }
        }

        private JsonTypeInfo? _objectTypeInfo;

        internal void ClearCaches()
        {
            _cachingContext?.Clear();
            _lastTypeInfo = null;
            _objectTypeInfo = null;
        }

        /// <summary>
        /// Stores and manages all reflection caches for one or more <see cref="JsonSerializerOptions"/> instances.
        /// NB the type encapsulates the original options instance and only consults that one when building new types;
        /// this is to prevent multiple options instances from leaking into the object graphs of converters which
        /// could break user invariants.
        /// </summary>
        internal sealed class CachingContext
        {
            private readonly ConcurrentDictionary<Type, CacheEntry> _cache = new();
#if !NETCOREAPP
            private readonly Func<Type, CacheEntry> _cacheEntryFactory;
#endif

            public CachingContext(JsonSerializerOptions options, int hashCode)
            {
                Options = options;
                HashCode = hashCode;
#if !NETCOREAPP
                _cacheEntryFactory = type => CreateCacheEntry(type, this);
#endif
            }

            public JsonSerializerOptions Options { get; }
            public int HashCode { get; }
            // Property only accessed by reflection in testing -- do not remove.
            // If changing please ensure that src/ILLink.Descriptors.LibraryBuild.xml is up-to-date.
            public int Count => _cache.Count;

            public JsonTypeInfo? GetOrAddTypeInfo(Type type, bool fallBackToNearestAncestorType = false)
            {
                CacheEntry entry = GetOrAddCacheEntry(type);
                return fallBackToNearestAncestorType && !entry.HasResult
                    ? FallBackToNearestAncestor(type, entry)
                    : entry.GetResult();
            }

            public bool TryGetTypeInfo(Type type, [NotNullWhen(true)] out JsonTypeInfo? typeInfo)
            {
                _cache.TryGetValue(type, out CacheEntry? entry);
                typeInfo = entry?.TypeInfo;
                return typeInfo is not null;
            }

            public void Clear()
            {
                _cache.Clear();
            }

            private CacheEntry GetOrAddCacheEntry(Type type)
            {
#if NETCOREAPP
                return _cache.GetOrAdd(type, CreateCacheEntry, this);
#else
                return _cache.GetOrAdd(type, _cacheEntryFactory);
#endif
            }

            private static CacheEntry CreateCacheEntry(Type type, CachingContext context)
            {
                try
                {
                    JsonTypeInfo? typeInfo = context.Options.GetTypeInfoNoCaching(type);
                    return new CacheEntry(typeInfo);
                }
                catch (Exception ex)
                {
                    ExceptionDispatchInfo edi = ExceptionDispatchInfo.Capture(ex);
                    return new CacheEntry(edi);
                }
            }

            private JsonTypeInfo? FallBackToNearestAncestor(Type type, CacheEntry entry)
            {
                Debug.Assert(!entry.HasResult);

                CacheEntry? nearestAncestor = entry.IsNearestAncestorResolved
                    ? entry.NearestAncestor
                    : DetermineNearestAncestor(type, entry);

                return nearestAncestor?.GetResult();
            }

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
                Justification = "We only need to examine the interface types that are supported by the underlying resolver.")]
            private CacheEntry? DetermineNearestAncestor(Type type, CacheEntry entry)
            {
                // In cases where the underlying TypeInfoResolver returns `null` for a given type,
                // this method traverses the hierarchy above the type to determine potential
                // ancestors for which the resolver does provide metadata. This can be useful in
                // cases where we're using a source generator and are trying to serialize private
                // implementations of an interface that is supported by the source generator.
                // NB this algorithm runs lazily and unsynchronized *after* the CacheEntry has been looked up
                // from the global cache, so care should be taken to avoid potential race conditions.
                //
                // IMPORTANT: nearest-ancestor resolution should be reserved for weakly-typed serialization.
                // Attempting to use it in strongly typed operations or deserialization will invariably
                // result in an invalid cast exception, so use with caution.

                Debug.Assert(!entry.HasResult);
                CacheEntry? candidate = null;
                Type? candidateType = null;

                for (Type? current = type.BaseType; current != null; current = current.BaseType)
                {
                    if (current == JsonTypeInfo.ObjectType)
                    {
                        // Avoid falling back to the contract for object since it's polymorphic
                        // and it would try to send us back to the runtime type that isn't supported.
                        break;
                    }

                    candidate = GetOrAddCacheEntry(current);
                    if (candidate.HasResult)
                    {
                        // We found a type in the class hierarchy that has a contract -- stop looking further up.
                        candidateType = current;
                        break;
                    }
                }

                foreach (Type interfaceType in type.GetInterfaces())
                {
                    CacheEntry interfaceEntry = GetOrAddCacheEntry(interfaceType);
                    if (interfaceEntry.HasResult)
                    {
                        if (candidateType != null)
                        {
                            if (interfaceType.IsAssignableFrom(candidateType))
                            {
                                // The previous candidate is more derived than the
                                // current interface -- keep our previous choice.
                                continue;
                            }
                            else if (candidateType.IsAssignableFrom(interfaceType))
                            {
                                // The current interface is more derived than the
                                // previous candidate -- replace the candidate value.
                            }
                            else
                            {
                                // We have found two possible ancestors that are not in subtype relationship.
                                // This indicates we have encountered a diamond ambiguity -- abort search and record an exception.
                                NotSupportedException nse = ThrowHelper.GetNotSupportedException_AmbiguousMetadataForType(type, candidateType, interfaceType);
                                candidate = new CacheEntry(ExceptionDispatchInfo.Capture(nse));
                                break;
                            }
                        }

                        candidate = interfaceEntry;
                        candidateType = interfaceType;
                    }
                }

                entry.NearestAncestor = candidate;
                entry.IsNearestAncestorResolved = true;
                return candidate;
            }

            private sealed class CacheEntry
            {
                public readonly bool HasResult;
                public readonly JsonTypeInfo? TypeInfo;
                public readonly ExceptionDispatchInfo? ExceptionDispatchInfo;

                public volatile bool IsNearestAncestorResolved;
                public CacheEntry? NearestAncestor;

                public CacheEntry(JsonTypeInfo? typeInfo)
                {
                    TypeInfo = typeInfo;
                    HasResult = typeInfo is not null;
                }

                public CacheEntry(ExceptionDispatchInfo exception)
                {
                    ExceptionDispatchInfo = exception;
                    HasResult = true;
                }

                public JsonTypeInfo? GetResult()
                {
                    ExceptionDispatchInfo?.Throw();
                    return TypeInfo;
                }
            }
        }

        /// <summary>
        /// Defines a cache of CachingContexts; instead of using a ConditionalWeakTable which can be slow to traverse
        /// this approach uses a fixed-size array of weak references of <see cref="CachingContext"/> that can be looked up lock-free.
        /// Relevant caching contexts are looked up by linear traversal using the equality comparison defined by <see cref="EqualityComparer"/>.
        /// </summary>
        internal static class TrackedCachingContexts
        {
            private const int MaxTrackedContexts = 64;
            private static readonly WeakReference<CachingContext>?[] s_trackedContexts = new WeakReference<CachingContext>[MaxTrackedContexts];
            private static readonly EqualityComparer s_optionsComparer = new();

            public static CachingContext GetOrCreate(JsonSerializerOptions options)
            {
                Debug.Assert(options.IsReadOnly, "Cannot create caching contexts for mutable JsonSerializerOptions instances");
                Debug.Assert(options._typeInfoResolver != null);

                int hashCode = s_optionsComparer.GetHashCode(options);

                if (TryGetContext(options, hashCode, out int firstUnpopulatedIndex, out CachingContext? result))
                {
                    return result;
                }
                else if (firstUnpopulatedIndex < 0)
                {
                    // Cache is full; return a fresh instance.
                    return new CachingContext(options, hashCode);
                }

                lock (s_trackedContexts)
                {
                    if (TryGetContext(options, hashCode, out firstUnpopulatedIndex, out result))
                    {
                        return result;
                    }

                    var ctx = new CachingContext(options, hashCode);

                    if (firstUnpopulatedIndex >= 0)
                    {
                        // Cache has capacity -- store the context in the first available index.
                        ref WeakReference<CachingContext>? weakRef = ref s_trackedContexts[firstUnpopulatedIndex];

                        if (weakRef is null)
                        {
                            weakRef = new(ctx);
                        }
                        else
                        {
                            Debug.Assert(weakRef.TryGetTarget(out _) is false);
                            weakRef.SetTarget(ctx);
                        }
                    }

                    return ctx;
                }
            }

            private static bool TryGetContext(
                JsonSerializerOptions options,
                int hashCode,
                out int firstUnpopulatedIndex,
                [NotNullWhen(true)] out CachingContext? result)
            {
                WeakReference<CachingContext>?[] trackedContexts = s_trackedContexts;

                firstUnpopulatedIndex = -1;
                for (int i = 0; i < trackedContexts.Length; i++)
                {
                    WeakReference<CachingContext>? weakRef = trackedContexts[i];

                    if (weakRef is null || !weakRef.TryGetTarget(out CachingContext? ctx))
                    {
                        if (firstUnpopulatedIndex < 0)
                        {
                            firstUnpopulatedIndex = i;
                        }
                    }
                    else if (hashCode == ctx.HashCode && s_optionsComparer.Equals(options, ctx.Options))
                    {
                        result = ctx;
                        return true;
                    }
                }

                result = null;
                return false;
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
                    left._preferredObjectCreationHandling == right._preferredObjectCreationHandling &&
                    left._unknownTypeHandling == right._unknownTypeHandling &&
                    left._unmappedMemberHandling == right._unmappedMemberHandling &&
                    left._defaultBufferSize == right._defaultBufferSize &&
                    left._maxDepth == right._maxDepth &&
                    left._allowTrailingCommas == right._allowTrailingCommas &&
                    left._ignoreNullValues == right._ignoreNullValues &&
                    left._ignoreReadOnlyProperties == right._ignoreReadOnlyProperties &&
                    left._ignoreReadonlyFields == right._ignoreReadonlyFields &&
                    left._includeFields == right._includeFields &&
                    left._propertyNameCaseInsensitive == right._propertyNameCaseInsensitive &&
                    left._writeIndented == right._writeIndented &&
                    left._indentCharacter == right._indentCharacter &&
                    left._indentSize == right._indentSize &&
                    left._typeInfoResolver == right._typeInfoResolver &&
                    CompareLists(left._converters, right._converters);

                static bool CompareLists<TValue>(ConfigurationList<TValue>? left, ConfigurationList<TValue>? right)
                    where TValue : class?
                {
                    // equates null with empty lists
                    if (left is null)
                        return right is null || right.Count == 0;

                    if (right is null)
                        return left.Count == 0;

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

                AddHashCode(ref hc, options._dictionaryKeyPolicy);
                AddHashCode(ref hc, options._jsonPropertyNamingPolicy);
                AddHashCode(ref hc, options._readCommentHandling);
                AddHashCode(ref hc, options._referenceHandler);
                AddHashCode(ref hc, options._encoder);
                AddHashCode(ref hc, options._defaultIgnoreCondition);
                AddHashCode(ref hc, options._numberHandling);
                AddHashCode(ref hc, options._preferredObjectCreationHandling);
                AddHashCode(ref hc, options._unknownTypeHandling);
                AddHashCode(ref hc, options._unmappedMemberHandling);
                AddHashCode(ref hc, options._defaultBufferSize);
                AddHashCode(ref hc, options._maxDepth);
                AddHashCode(ref hc, options._allowTrailingCommas);
                AddHashCode(ref hc, options._ignoreNullValues);
                AddHashCode(ref hc, options._ignoreReadOnlyProperties);
                AddHashCode(ref hc, options._ignoreReadonlyFields);
                AddHashCode(ref hc, options._includeFields);
                AddHashCode(ref hc, options._propertyNameCaseInsensitive);
                AddHashCode(ref hc, options._writeIndented);
                AddHashCode(ref hc, options._indentCharacter);
                AddHashCode(ref hc, options._indentSize);
                AddHashCode(ref hc, options._typeInfoResolver);
                AddListHashCode(ref hc, options._converters);

                return hc.ToHashCode();

                static void AddListHashCode<TValue>(ref HashCode hc, ConfigurationList<TValue>? list)
                {
                    // equates null with empty lists
                    if (list is null)
                        return;

                    int n = list.Count;
                    for (int i = 0; i < n; i++)
                    {
                        AddHashCode(ref hc, list[i]);
                    }
                }

                static void AddHashCode<TValue>(ref HashCode hc, TValue? value)
                {
                    if (typeof(TValue).IsValueType)
                    {
                        hc.Add(value);
                    }
                    else
                    {
                        Debug.Assert(!typeof(TValue).IsSealed, "Sealed reference types like string should not use this method.");
                        hc.Add(RuntimeHelpers.GetHashCode(value));
                    }
                }
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
