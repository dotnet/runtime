// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// Fetches <paramref name="destination"/>.Length bytes starting at <paramref name="address"/>
/// directly from the underlying target, bypassing any active cache. Supplied to
/// <see cref="ITargetReadCache.ReadBuffer"/> implementations as the cache-miss fallback.
/// </summary>
public delegate void RawReadDelegate(ulong address, Span<byte> destination);

/// <summary>
/// A pluggable read cache installed for the lifetime of a
/// <see cref="Target.BeginCacheScope"/> scope. Implementations decide whether to satisfy a
/// request from cached state or fall through to the underlying target via
/// the supplied <see cref="RawReadDelegate"/>.
/// </summary>
/// <remarks>
/// Implementations are not required to be thread-safe; the cDAC reader serializes
/// access to a target.
/// </remarks>
public interface ITargetReadCache : IDisposable
{
    /// <summary>
    /// Read <c>destination.Length</c> bytes starting at <paramref name="address"/>. On a cache
    /// miss the implementation must call <paramref name="fallback"/> to fetch the bytes from the
    /// underlying target. Exceptions thrown by <paramref name="fallback"/> must be allowed to
    /// propagate to the caller.
    /// </summary>
    void ReadBuffer(ulong address, Span<byte> destination, RawReadDelegate fallback);

    /// <summary>
    /// Drop any cached state. Called by the host when the cache scope ends, and may be called
    /// by users to invalidate mid-scope (for example after a target write).
    /// </summary>
    void Invalidate();
}
