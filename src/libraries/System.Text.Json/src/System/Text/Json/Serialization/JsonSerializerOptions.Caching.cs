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
        /// Same as GetTypeInfo but without validation and additional knobs.
        /// </summary>
        internal JsonTypeInfo GetTypeInfoInternal(Type type, bool ensureConfigured = true, bool resolveIfMutable = false)
        {
            JsonTypeInfo? typeInfo = null;

            if (IsReadOnly)
            {
                typeInfo = GetCachingContext()?.GetOrAddJsonTypeInfo(type);
                if (ensureConfigured)
                {
                    typeInfo?.EnsureConfigured();
                }
            }
            else if (resolveIfMutable)
            {
                typeInfo = GetTypeInfoNoCaching(type);
            }

            if (typeInfo == null)
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

            return _cachingContext.TryGetJsonTypeInfo(type, out typeInfo);
        }

        /// <summary>
        /// Return the TypeInfo for root API calls.
        /// This has an LRU cache that is intended only for public API calls that specify the root type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonTypeInfo GetTypeInfoForRootType(Type type)
        {
            JsonTypeInfo? jsonTypeInfo = _lastTypeInfo;

            if (jsonTypeInfo?.Type != type)
            {
                _lastTypeInfo = jsonTypeInfo = GetTypeInfoInternal(type);
            }

            return jsonTypeInfo;
        }

        // Caches the resolved JsonTypeInfo<object> for faster access during root-level object type serialization.
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

        private CachingContext? GetCachingContext()
        {
            Debug.Assert(IsReadOnly);

            return _cachingContext ??= TrackedCachingContexts.GetOrCreate(this);
        }

        /// <summary>
        /// Stores and manages all reflection caches for one or more <see cref="JsonSerializerOptions"/> instances.
        /// NB the type encapsulates the original options instance and only consults that one when building new types;
        /// this is to prevent multiple options instances from leaking into the object graphs of converters which
        /// could break user invariants.
        /// </summary>
        internal sealed class CachingContext
        {
            private readonly ConcurrentDictionary<Type, JsonTypeInfo?> _jsonTypeInfoCache = new();

            public CachingContext(JsonSerializerOptions options, int hashCode)
            {
                Options = options;
                HashCode = hashCode;
            }

            public JsonSerializerOptions Options { get; }
            public int HashCode { get; }
            // Property only accessed by reflection in testing -- do not remove.
            // If changing please ensure that src/ILLink.Descriptors.LibraryBuild.xml is up-to-date.
            public int Count => _jsonTypeInfoCache.Count;

            public JsonTypeInfo? GetOrAddJsonTypeInfo(Type type) => _jsonTypeInfoCache.GetOrAdd(type, Options.GetTypeInfoNoCaching);
            public bool TryGetJsonTypeInfo(Type type, [NotNullWhen(true)] out JsonTypeInfo? typeInfo) => _jsonTypeInfoCache.TryGetValue(type, out typeInfo);

            public void Clear()
            {
                _jsonTypeInfoCache.Clear();
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
                    left._typeInfoResolver == right._typeInfoResolver &&
                    CompareLists(left._converters, right._converters);

                static bool CompareLists<TValue>(ConfigurationList<TValue> left, ConfigurationList<TValue> right)
                    where TValue : class?
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

                AddHashCode(ref hc, options._dictionaryKeyPolicy);
                AddHashCode(ref hc, options._jsonPropertyNamingPolicy);
                AddHashCode(ref hc, options._readCommentHandling);
                AddHashCode(ref hc, options._referenceHandler);
                AddHashCode(ref hc, options._encoder);
                AddHashCode(ref hc, options._defaultIgnoreCondition);
                AddHashCode(ref hc, options._numberHandling);
                AddHashCode(ref hc, options._unknownTypeHandling);
                AddHashCode(ref hc, options._defaultBufferSize);
                AddHashCode(ref hc, options._maxDepth);
                AddHashCode(ref hc, options._allowTrailingCommas);
                AddHashCode(ref hc, options._ignoreNullValues);
                AddHashCode(ref hc, options._ignoreReadOnlyProperties);
                AddHashCode(ref hc, options._ignoreReadonlyFields);
                AddHashCode(ref hc, options._includeFields);
                AddHashCode(ref hc, options._propertyNameCaseInsensitive);
                AddHashCode(ref hc, options._writeIndented);
                AddHashCode(ref hc, options._typeInfoResolver);
                AddListHashCode(ref hc, options._converters);

                return hc.ToHashCode();

                static void AddListHashCode<TValue>(ref HashCode hc, ConfigurationList<TValue> list)
                {
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
