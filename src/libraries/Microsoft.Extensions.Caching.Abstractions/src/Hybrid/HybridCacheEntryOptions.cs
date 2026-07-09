// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Specifies additional options (for example, expiration) that apply to a <see cref="HybridCache"/> operation. When options
/// can be specified at multiple levels (for example, globally and per-call), the values are composed; the
/// most granular non-null value is used, with null values being inherited. If no value is specified at
/// any level, the implementation can choose a reasonable default.
/// </summary>
/// <remarks>
/// This type is immutable. To adjust cache entry options based on the result of a data fetch, use one of the
/// <see cref="HybridCache.GetOrCreateAsync{TState, T}(string, TState, Func{TState, HybridCacheEntryContext, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask{T}}, HybridCacheEntryOptions?, System.Collections.Generic.IEnumerable{string}?, System.Threading.CancellationToken)"/>
/// overloads, whose factory callback receives a mutable <see cref="HybridCacheEntryContext"/>.
/// </remarks>
public sealed class HybridCacheEntryOptions
{
    // memoize when possible; safe because this type is immutable.
    private DistributedCacheEntryOptions? _dc;

    /// <summary>
    /// Gets the overall cache duration of this entry, passed to the backend distributed cache.
    /// </summary>
    public TimeSpan? Expiration { get; init; }

    /// <summary>
    /// Gets the expiration for the local (in-process) cache entry.
    /// </summary>
    /// <remarks>
    /// When retrieving a cached value from an external cache store, this value will be used to calculate the local
    /// cache expiration, not exceeding the remaining overall cache lifetime.
    /// </remarks>
    public TimeSpan? LocalCacheExpiration { get; init; }

    /// <summary>
    /// Gets additional flags that apply to the requested operation.
    /// </summary>
    public HybridCacheEntryFlags? Flags { get; init; }

    /// <summary>
    /// Gets the size to assign to entries in the local (in-process) cache.
    /// </summary>
    /// <remarks>
    /// The units are determined by the underlying local cache implementation. When the local cache
    /// is an <see cref="Memory.IMemoryCache"/> configured with a size limit, this value corresponds to
    /// <see cref="Memory.ICacheEntry.Size"/>.
    /// <para>
    /// When <see langword="null"/>, the implementation may compute a default size (for example, from
    /// the serialized payload length).
    /// </para>
    /// </remarks>
    public long? LocalSize { get; init; }

    // DefaultHybridCache (in MS.E.Caching.Hybrid) uses this through UnsafeAccessor.
    // Since this assembly (ME.E.Caching.Abstractions) is now part of the shared framework,
    // it is effectively automatically updated when TFM is updated, so we shouldn't remove this method.
    internal DistributedCacheEntryOptions? ToDistributedCacheEntryOptions()
        => Expiration is null ? null : (_dc ??= new() { AbsoluteExpirationRelativeToNow = Expiration });
}
