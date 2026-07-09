// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Provides a mutable view of the cache entry options in effect for a single <see cref="HybridCache"/> operation,
/// supplied to a factory callback so that it can adjust those options based on the result of the data fetch.
/// </summary>
/// <remarks>
/// The properties are initialized to the effective values for the operation (composed from the per-call and any
/// global options). A factory can change them to influence how the resulting value is cached. Implementations can
/// use <see cref="Revision"/> to detect whether the factory changed any value.
/// </remarks>
public sealed class HybridCacheEntryContext
{
    private TimeSpan? _expiration;
    private TimeSpan? _localCacheExpiration;
    private HybridCacheEntryFlags? _flags;
    private long? _localSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridCacheEntryContext"/> class with default (unset) values.
    /// </summary>
    /// <remarks>
    /// This is primarily intended for <see cref="HybridCache"/> implementations that need to construct a context
    /// to pass to a factory callback.
    /// </remarks>
    public HybridCacheEntryContext()
    {
    }

    internal HybridCacheEntryContext(HybridCacheEntryOptions? options)
    {
        if (options is not null)
        {
            _expiration = options.Expiration;
            _localCacheExpiration = options.LocalCacheExpiration;
            _flags = options.Flags;
            _localSize = options.LocalSize;
        }
    }

    /// <summary>
    /// Gets or sets the overall cache duration of this entry, passed to the backend distributed cache.
    /// </summary>
    public TimeSpan? Expiration
    {
        get => _expiration;
        set
        {
            if (_expiration != value)
            {
                _expiration = value;
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
        get => _localCacheExpiration;
        set
        {
            if (_localCacheExpiration != value)
            {
                _localCacheExpiration = value;
                BumpRevision();
            }
        }
    }

    /// <summary>
    /// Gets or sets additional flags that apply to the requested operation.
    /// </summary>
    public HybridCacheEntryFlags? Flags
    {
        get => _flags;
        set
        {
            if (_flags != value)
            {
                _flags = value;
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
        get => _localSize;
        set
        {
            if (_localSize != value)
            {
                _localSize = value;
                BumpRevision();
            }
        }
    }

    /// <summary>
    /// Gets a value that increments whenever a property on this instance is changed.
    /// </summary>
    /// <remarks>
    /// Implementations of <see cref="HybridCache"/> can capture this value before invoking a factory
    /// callback and compare it afterwards to determine whether the factory changed any value.
    /// The value increments only when a setter assigns a different value than the current one.
    /// </remarks>
    public int Revision { get; private set; }

    private void BumpRevision()
    {
        // unchecked: rollover is fine because callers compare for inequality, not ordering.
        unchecked
        {
            Revision++;
        }
    }

    internal HybridCacheEntryOptions ToOptions()
        => new HybridCacheEntryOptions
        {
            Expiration = _expiration,
            LocalCacheExpiration = _localCacheExpiration,
            Flags = _flags,
            LocalSize = _localSize,
        };
}
