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
/// The properties on this type are mutable so that a factory callback supplied to
/// <see cref="HybridCache.GetOrCreateAsync{TState, T}(string, TState, Func{TState, HybridCacheEntryOptions, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask{T}}, HybridCacheEntryOptions?, System.Collections.Generic.IEnumerable{string}?, System.Threading.CancellationToken)"/>
/// can adjust them based on the result of the data fetch. Implementations can use <see cref="Revision"/>
/// to detect whether the factory mutated the options.
/// </remarks>
public sealed class HybridCacheEntryOptions
{
    // memoize when possible; invalidated whenever Expiration changes
    private DistributedCacheEntryOptions? _dc;

    /// <summary>
    /// Gets or sets the overall cache duration of this entry, passed to the backend distributed cache.
    /// </summary>
    public TimeSpan? Expiration
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _dc = null;
                BumpRevision();
            }
        }
    }

    /// <summary>
    /// Gets or sets the expiration for the local (in-process) cache entry.
    /// </summary>
    /// <remarks>
    /// When retrieving a cached value from an external cache store, this value will be used to calculate the local
    /// cache expiration, not exceeding the remaining overall cache lifetime.
    /// </remarks>
    public TimeSpan? LocalCacheExpiration
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                BumpRevision();
            }
        }
    }

    /// <summary>
    /// Gets or sets additional flags that apply to the requested operation.
    /// </summary>
    public HybridCacheEntryFlags? Flags
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                BumpRevision();
            }
        }
    }

    /// <summary>
    /// Gets or sets the size to assign to entries in the local (in-process) cache.
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
    public long? LocalSize
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                BumpRevision();
            }
        }
    }

    /// <summary>
    /// Gets a value that increments whenever a property on this instance is changed.
    /// </summary>
    /// <remarks>
    /// Implementations of <see cref="HybridCache"/> can capture this value before invoking a factory
    /// callback and compare it afterwards to determine whether the factory mutated the options.
    /// The value increments only when a setter assigns a different value than the current one.
    /// </remarks>
    public int Revision { get; private set; }

    private void BumpRevision()
    {
        // unchecked: rollover should be fine because callers compare for inequality, not ordering
        unchecked
        {
            Revision++;
        }
    }

    // DefaultHybridCache (in MS.E.Caching.Hybrid) uses these through UnsafeAccessor.
    // Since this assembly (ME.E.Caching.Abstractions) is now part of the shared framework,
    // it is effectively automatically updated when TFM is updated, so we shouldn't remove these methods.
    internal DistributedCacheEntryOptions? ToDistributedCacheEntryOptions()
        => Expiration is null ? null : (_dc ??= new() { AbsoluteExpirationRelativeToNow = Expiration });

    internal HybridCacheEntryOptions Clone()
    {
        // shallow copy is sufficient: all settable state is value-typed and _dc is recomputable.
        var clone = (HybridCacheEntryOptions)MemberwiseClone();
        clone._dc = null;
        return clone;
    }
}
