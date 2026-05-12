// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Provides a context for the factory callback in <see cref="HybridCache.GetOrCreateAsync{TState, T}(string, TState, Func{TState, HybridCacheEntryContext, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask{T}}, HybridCacheEntryOptions?, System.Collections.Generic.IEnumerable{string}?, System.Threading.CancellationToken)"/>,
/// allowing the factory to influence cache entry options based on the result of the data fetch.
/// </summary>
/// <remarks>
/// All settable properties are <see langword="null"/> by default, meaning the value from
/// <see cref="CurrentOptions"/> (or the global default) is used. Setting a property to a
/// non-<see langword="null"/> value overrides the corresponding option for this cache entry.
/// <para>
/// Because the factory executes after cache reads have already been attempted, only write-related
/// flags in <see cref="Flags"/> are effective:
/// <see cref="HybridCacheEntryFlags.DisableLocalCacheWrite"/>,
/// <see cref="HybridCacheEntryFlags.DisableDistributedCacheWrite"/>, and
/// <see cref="HybridCacheEntryFlags.DisableCompression"/>.
/// Read-related flags (<see cref="HybridCacheEntryFlags.DisableLocalCacheRead"/>,
/// <see cref="HybridCacheEntryFlags.DisableDistributedCacheRead"/>, and
/// <see cref="HybridCacheEntryFlags.DisableUnderlyingData"/>) set on the context are ignored.
/// </para>
/// </remarks>
public sealed class HybridCacheEntryContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HybridCacheEntryContext"/> class.
    /// </summary>
    /// <param name="currentOptions">The options currently in effect for this cache operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="currentOptions"/> is <see langword="null"/>.</exception>
    public HybridCacheEntryContext(HybridCacheEntryOptions currentOptions)
    {
        ArgumentNullException.ThrowIfNull(currentOptions);
        CurrentOptions = currentOptions;
    }

    /// <summary>
    /// Gets the options currently in effect for this cache operation.
    /// </summary>
    public HybridCacheEntryOptions CurrentOptions { get; }

    /// <summary>
    /// Gets or sets the overall cache duration for this entry, overriding <see cref="HybridCacheEntryOptions.Expiration"/>.
    /// </summary>
    public TimeSpan? Expiration { get; set; }

    /// <summary>
    /// Gets or sets the local cache duration for this entry, overriding <see cref="HybridCacheEntryOptions.LocalCacheExpiration"/>.
    /// </summary>
    public TimeSpan? LocalCacheExpiration { get; set; }

    /// <summary>
    /// Gets or sets additional flags for this entry, overriding <see cref="HybridCacheEntryOptions.Flags"/>.
    /// </summary>
    /// <remarks>
    /// Because the factory executes after cache reads have already been attempted, only write-related
    /// flags are effective: <see cref="HybridCacheEntryFlags.DisableLocalCacheWrite"/>,
    /// <see cref="HybridCacheEntryFlags.DisableDistributedCacheWrite"/>, and
    /// <see cref="HybridCacheEntryFlags.DisableCompression"/>. Read-related flags
    /// (<see cref="HybridCacheEntryFlags.DisableLocalCacheRead"/>,
    /// <see cref="HybridCacheEntryFlags.DisableDistributedCacheRead"/>, and
    /// <see cref="HybridCacheEntryFlags.DisableUnderlyingData"/>) set here are ignored.
    /// </remarks>
    public HybridCacheEntryFlags? Flags { get; set; }

    /// <summary>
    /// Gets or sets the size to assign to this entry in the local (in-process) cache,
    /// overriding <see cref="HybridCacheEntryOptions.LocalCacheSize"/>.
    /// </summary>
    /// <remarks>
    /// The units are determined by the underlying local cache implementation. When the local cache
    /// is a <c>MemoryCache</c> configured with a size limit,
    /// this value corresponds to <see cref="Microsoft.Extensions.Caching.Memory.ICacheEntry.Size"/>.
    /// When <see langword="null"/>, the implementation may compute a default size (for example, from the
    /// serialized payload length). The sentinel value <c>-1</c> can be used to reset the size back to
    /// the implementation-determined default.
    /// </remarks>
    public long? LocalCacheSize { get; set; }
}
