// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Provides multi-tier caching services building on <see cref="IDistributedCache"/> backends.
/// </summary>
public abstract class HybridCache
{
    /// <summary>
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="TState">The type of additional state required by <paramref name="factory"/>.</typeparam>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="factory">Provides the underlying data service if the data is not available in the cache.</param>
    /// <param name="state">The state required for <paramref name="factory"/>.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    public abstract ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="factory">Provides the underlying data service if the data is not available in the cache.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    public ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        => GetOrCreateAsync(key, factory, WrappedCallbackCache<T>.Instance, options, tags, cancellationToken);

#if NET
    /// <summary>
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="factory">Provides the underlying data service if the data is not available in the cache.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    public ValueTask<T> GetOrCreateAsync<T>(
        ReadOnlySpan<char> key,
        Func<CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => GetOrCreateAsync(key, factory, WrappedCallbackCache<T>.Instance, options, tags, cancellationToken);

    /// <summary>
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="TState">The type of additional state required by <paramref name="factory"/>.</typeparam>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="factory">Provides the underlying data service if the data is not available in the cache.</param>
    /// <param name="state">The state required for <paramref name="factory"/>.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    /// <remarks>Implementors may use the key span to attempt a local-cache synchronous 'get' without requiring the key as a <see cref="string"/>.</remarks>
    public virtual ValueTask<T> GetOrCreateAsync<TState, T>(
        ReadOnlySpan<char> key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => GetOrCreateAsync(key.ToString(), state, factory, options, tags, cancellationToken);

    /// <summary>
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="factory">Provides the underlying data service if the data is not available in the cache.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    public ValueTask<T> GetOrCreateAsync<T>(
        ref DefaultInterpolatedStringHandler key,
        Func<CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => ClearAndReturn(ref key,
            GetOrCreateAsync(key.Text, factory, WrappedCallbackCache<T>.Instance, options, tags, cancellationToken));

    /// <summary>
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="TState">The type of additional state required by <paramref name="factory"/>.</typeparam>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="factory">Provides the underlying data service if the data is not available in the cache.</param>
    /// <param name="state">The state required for <paramref name="factory"/>.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    public ValueTask<T> GetOrCreateAsync<TState, T>(
        ref DefaultInterpolatedStringHandler key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => ClearAndReturn(ref key,
            GetOrCreateAsync(key.Text, state, factory, options, tags, cancellationToken));
#endif

    /// <summary>
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="TState">The type of additional state required by <paramref name="factory"/>.</typeparam>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="state">The state required for <paramref name="factory"/>.</param>
    /// <param name="factory">Provides the underlying data service if the data is not available in the cache. The <see cref="HybridCacheEntryOptions"/> passed to the factory is mutable and allows it to influence cache entry options based on the result.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    public virtual async ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state,
        Func<TState, HybridCacheEntryOptions, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
    {
        var factoryOptions = options is null ? new HybridCacheEntryOptions() : options.Clone();

        // Suppress writes in the inner call so we can perform a single, correct SetAsync afterwards
        // using the options that the factory ultimately produced.
        // This introduces some race conditions, but that's considered better than not honoring the factory's options.
        var innerOptions = options is null ? new HybridCacheEntryOptions() : options.Clone();
        innerOptions.Flags = (innerOptions.Flags ?? HybridCacheEntryFlags.None)
            | HybridCacheEntryFlags.DisableLocalCacheWrite
            | HybridCacheEntryFlags.DisableDistributedCacheWrite;

        var packedState = new DefaultImplState<TState, T>(state, factory, factoryOptions);

        T value = await GetOrCreateAsync(key, packedState, static (packed, ct) =>
        {
            packed.FactoryRan = true;
            return packed.Factory(packed.State, packed.FactoryOptions, ct);
        }, innerOptions, tags, cancellationToken).ConfigureAwait(false);

        // Only the stampede leader observes FactoryRan == true; followers reuse the leader's value
        // (and the leader's SetAsync) without issuing a duplicate write.
        if (packedState.FactoryRan)
        {
            const HybridCacheEntryFlags BothWritesDisabled =
                HybridCacheEntryFlags.DisableLocalCacheWrite
                | HybridCacheEntryFlags.DisableDistributedCacheWrite;
            if ((factoryOptions.Flags & BothWritesDisabled) != BothWritesDisabled)
            {
                // Cancellation of the caller should not abort the write: the factory has already
                // produced a value that we are about to return; canceling SetAsync here would
                // discard a completed result and force the next caller to re-run the factory.
                await SetAsync(key, value, factoryOptions, tags, CancellationToken.None).ConfigureAwait(false);
            }
        }

        return value;
    }

    /// <summary>
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="factory">Provides the underlying data service if the data is not available in the cache. The <see cref="HybridCacheEntryOptions"/> passed to the factory is mutable and allows it to influence cache entry options based on the result.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    public ValueTask<T> GetOrCreateAsync<T>(string key,
        Func<HybridCacheEntryOptions, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        => GetOrCreateAsync(key, factory, WrappedOptionsCallbackCache<T>.Instance, options, tags, cancellationToken);

#if NET
    /// <summary>
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="factory">Provides the underlying data service if the data is not available in the cache. The <see cref="HybridCacheEntryOptions"/> passed to the factory is mutable and allows it to influence cache entry options based on the result.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    public ValueTask<T> GetOrCreateAsync<T>(
        ReadOnlySpan<char> key,
        Func<HybridCacheEntryOptions, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => GetOrCreateAsync(key, factory, WrappedOptionsCallbackCache<T>.Instance, options, tags, cancellationToken);

    /// <summary>
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="TState">The type of additional state required by <paramref name="factory"/>.</typeparam>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="state">The state required for <paramref name="factory"/>.</param>
    /// <param name="factory">Provides the underlying data service if the data is not available in the cache. The <see cref="HybridCacheEntryOptions"/> passed to the factory is mutable and allows it to influence cache entry options based on the result.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    public virtual ValueTask<T> GetOrCreateAsync<TState, T>(
        ReadOnlySpan<char> key,
        TState state,
        Func<TState, HybridCacheEntryOptions, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => GetOrCreateAsync(key.ToString(), state, factory, options, tags, cancellationToken);

    /// <summary>
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="factory">Provides the underlying data service if the data is not available in the cache. The <see cref="HybridCacheEntryOptions"/> passed to the factory is mutable and allows it to influence cache entry options based on the result.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    public ValueTask<T> GetOrCreateAsync<T>(
        ref DefaultInterpolatedStringHandler key,
        Func<HybridCacheEntryOptions, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => ClearAndReturn(ref key,
            GetOrCreateAsync(key.Text, factory, WrappedOptionsCallbackCache<T>.Instance, options, tags, cancellationToken));

    /// <summary>
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="TState">The type of additional state required by <paramref name="factory"/>.</typeparam>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="state">The state required for <paramref name="factory"/>.</param>
    /// <param name="factory">Provides the underlying data service if the data is not available in the cache. The <see cref="HybridCacheEntryOptions"/> passed to the factory is mutable and allows it to influence cache entry options based on the result.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    public ValueTask<T> GetOrCreateAsync<TState, T>(
        ref DefaultInterpolatedStringHandler key,
        TState state,
        Func<TState, HybridCacheEntryOptions, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => ClearAndReturn(ref key,
            GetOrCreateAsync(key.Text, state, factory, options, tags, cancellationToken));

    // It is *not* an error that this Clear occurs before the "await"; by definition, the implementation is *required* to copy
    // the value locally before an await, precisely because the ref-struct cannot bridge an await. Thus: we are fine to clean
    // the buffer even in the non-synchronous completion scenario.
    private static ValueTask<T> ClearAndReturn<T>(ref DefaultInterpolatedStringHandler key, ValueTask<T> result)
    {
        key.Clear();
        return result;
    }
#endif

    private static class WrappedOptionsCallbackCache<T>
    {
        public static readonly Func<Func<HybridCacheEntryOptions, CancellationToken, ValueTask<T>>, HybridCacheEntryOptions, CancellationToken, ValueTask<T>> Instance =
            static (callback, opts, ct) => callback(opts, ct);
    }

    private sealed class DefaultImplState<TState, T>
    {
        public readonly TState State;
        public readonly Func<TState, HybridCacheEntryOptions, CancellationToken, ValueTask<T>> Factory;
        public readonly HybridCacheEntryOptions FactoryOptions;
        public bool FactoryRan;

        public DefaultImplState(TState state, Func<TState, HybridCacheEntryOptions, CancellationToken, ValueTask<T>> factory, HybridCacheEntryOptions factoryOptions)
        {
            State = state;
            Factory = factory;
            FactoryOptions = factoryOptions;
        }
    }

    private static class WrappedCallbackCache<T> // per-T memoized helper that allows GetOrCreateAsync<T> and GetOrCreateAsync<TState, T> to share an implementation
    {
        // for the simple usage scenario (no TState), pack the original callback as the "state", and use a wrapper function that just unrolls and invokes from the state
        public static readonly Func<Func<CancellationToken, ValueTask<T>>, CancellationToken, ValueTask<T>> Instance = static (callback, ct) => callback(ct);
    }

    /// <summary>
    /// Asynchronously sets or overwrites the value associated with the key.
    /// </summary>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to create.</param>
    /// <param name="value">The value to assign for this cache entry.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache entry.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    public abstract ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously removes the value associated with the key if it exists.
    /// </summary>
    public abstract ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously removes the value associated with the key if it exists.
    /// </summary>
    /// <remarks>Implementors should treat <c>null</c> as empty</remarks>
    public virtual ValueTask RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        return keys switch
        {
            // for consistency with GetOrCreate/Set: interpret null as "none"
            null or ICollection<string> { Count: 0 } => default,
            ICollection<string> { Count: 1 } => RemoveAsync(keys.First(), cancellationToken),
            _ => ForEachAsync(this, keys, cancellationToken),
        };

        // default implementation is to call RemoveAsync for each key in turn
        static async ValueTask ForEachAsync(HybridCache @this, IEnumerable<string> keys, CancellationToken cancellationToken)
        {
            foreach (var key in keys)
            {
                await @this.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Asynchronously removes all values associated with the specified tags.
    /// </summary>
    /// <remarks>Implementors should treat <c>null</c> as empty</remarks>
    public virtual ValueTask RemoveByTagAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        return tags switch
        {
            // for consistency with GetOrCreate/Set: interpret null as "none"
            null or ICollection<string> { Count: 0 } => default,
            ICollection<string> { Count: 1 } => RemoveByTagAsync(tags.Single(), cancellationToken),
            _ => ForEachAsync(this, tags, cancellationToken),
        };

        // default implementation is to call RemoveByTagAsync for each key in turn
        static async ValueTask ForEachAsync(HybridCache @this, IEnumerable<string> keys, CancellationToken cancellationToken)
        {
            foreach (var key in keys)
            {
                await @this.RemoveByTagAsync(key, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Asynchronously removes all values associated with the specified tag.
    /// </summary>
    public abstract ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}
