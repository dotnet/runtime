// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if NET
using System.Runtime.CompilerServices;
#endif
using Xunit;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

/// <summary>
/// Contract tests for the default <see cref="HybridCache.GetOrCreateAsync{TState, T}(string, TState, Func{TState, HybridCacheEntryContext, CancellationToken, ValueTask{T}}, HybridCacheEntryOptions?, IEnumerable{string}?, CancellationToken)"/>
/// implementation, exercised against a <see cref="HybridCache"/> subclass that does not override the new virtual.
/// <para>
/// The fake cache stores writes from any path (the abstract <see cref="HybridCache.GetOrCreateAsync{TState, T}(string, TState, Func{TState, CancellationToken, ValueTask{T}}, HybridCacheEntryOptions?, IEnumerable{string}?, CancellationToken)"/>
/// override or <see cref="HybridCache.SetAsync"/>) into the same observable <see cref="FakeHybridCache.Store"/>,
/// so the tests describe externally observable behavior and not the specific mechanism the default
/// implementation uses to update the cache.
/// </para>
/// </summary>
public class HybridCacheTests
{
    [Fact]
    public async Task ReturnsFactoryValue()
    {
        var cache = new FakeHybridCache();

        int result = await cache.GetOrCreateAsync(
            "k", state: 0,
            factory: (_, _, _) => new ValueTask<int>(42));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task FactoryReceivesNonNullMutableContext()
    {
        var cache = new FakeHybridCache();
        HybridCacheEntryContext? observed = null;

        await cache.GetOrCreateAsync(
            "k", state: 0,
            factory: (_, ctx, _) =>
            {
                observed = ctx;
                ctx.Expiration = TimeSpan.FromMinutes(5);
                return new ValueTask<int>(1);
            });

        Assert.NotNull(observed);
        Assert.Equal(TimeSpan.FromMinutes(5), observed!.Expiration);
    }

    [Fact]
    public async Task CallerOptions_AreNotMutated_ByFactory()
    {
        var cache = new FakeHybridCache();
        var callerOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(1),
            Flags = HybridCacheEntryFlags.None,
        };

        await cache.GetOrCreateAsync(
            "k", state: 0,
            factory: (_, ctx, _) =>
            {
                ctx.Expiration = TimeSpan.FromHours(1);
                ctx.LocalSize = 12345;
                ctx.Flags = HybridCacheEntryFlags.DisableCompression;
                return new ValueTask<int>(1);
            },
            options: callerOptions);

        Assert.Equal(TimeSpan.FromMinutes(1), callerOptions.Expiration);
        Assert.Null(callerOptions.LocalSize);
        Assert.Equal(HybridCacheEntryFlags.None, callerOptions.Flags);
    }

    [Fact]
    public async Task FactoryMutations_AreVisibleToCache()
    {
        var cache = new FakeHybridCache();

        await cache.GetOrCreateAsync(
            "k", state: 0,
            factory: (_, ctx, _) =>
            {
                ctx.Expiration = TimeSpan.FromHours(2);
                ctx.LocalSize = 999;
                return new ValueTask<int>(7);
            });

        StoredEntry entry = Assert.Contains("k", (IReadOnlyDictionary<string, StoredEntry>)cache.Store);
        Assert.Equal(7, entry.Value);
        Assert.NotNull(entry.Options);
        Assert.Equal(TimeSpan.FromHours(2), entry.Options!.Expiration);
        Assert.Equal(999, entry.Options.LocalSize);
    }

    [Fact]
    public async Task FactoryLeavesContextUnchanged_WritesWithOriginalOptions()
    {
        var cache = new FakeHybridCache();

        // The factory does not touch the context, so the write should reuse the caller's original
        // options (here null) rather than materializing a new all-null-valued instance.
        await cache.GetOrCreateAsync(
            "k", state: 0,
            factory: (_, _, _) => new ValueTask<int>(5));

        StoredEntry entry = Assert.Contains("k", (IReadOnlyDictionary<string, StoredEntry>)cache.Store);
        Assert.Equal(5, entry.Value);
        Assert.Null(entry.Options);
    }

    [Fact]
    public async Task FactoryDisablingBothWrites_SuppressesCacheWrite()
    {
        var cache = new FakeHybridCache();

        await cache.GetOrCreateAsync(
            "k", state: 0,
            factory: (_, ctx, _) =>
            {
                ctx.Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite;
                return new ValueTask<int>(1);
            });

        Assert.DoesNotContain("k", (IReadOnlyDictionary<string, StoredEntry>)cache.Store);
    }

    [Fact]
    public async Task FactoryDisablingOnlyOneWrite_StillWrites()
    {
        var cache = new FakeHybridCache();

        await cache.GetOrCreateAsync(
            "k", state: 0,
            factory: (_, ctx, _) =>
            {
                ctx.Flags = HybridCacheEntryFlags.DisableLocalCacheWrite;
                return new ValueTask<int>(1);
            });

        Assert.Contains("k", (IReadOnlyDictionary<string, StoredEntry>)cache.Store);
    }

    [Fact]
    public async Task FactoryNotInvoked_LeavesExistingEntryUntouched()
    {
        var cache = new FakeHybridCache();
        var preExistingOptions = new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) };
        cache.Store["k"] = new StoredEntry(17, preExistingOptions, Tags: null);

        int result = await cache.GetOrCreateAsync<int, int>(
            "k", state: 0,
            factory: (_, _, _) => throw new Xunit.Sdk.XunitException("factory should not have run"));

        Assert.Equal(17, result);
        // pre-existing entry should not be touched
        Assert.Same(preExistingOptions, cache.Store["k"].Options);
    }

    [Fact]
    public async Task CancellingCallerToken_DoesNotPreventCacheWrite()
    {
        var cache = new FakeHybridCache();
        using var cts = new CancellationTokenSource();

        await cache.GetOrCreateAsync(
            "k", state: 0,
            factory: (_, _, _) =>
            {
                // cancel after the factory has produced a value, before the cache write runs
                cts.Cancel();
                return new ValueTask<int>(1);
            },
            cancellationToken: cts.Token);

        // The fake skips any write whose CancellationToken is already canceled, so this
        // assertion fails for any implementation that forwards the caller's token to the
        // post-factory cache write.
        Assert.Contains("k", (IReadOnlyDictionary<string, StoredEntry>)cache.Store);
    }

    [Fact]
    public async Task NoStateOverload_BehavesLikeStatefulOverload()
    {
        var cache = new FakeHybridCache();

        int result = await cache.GetOrCreateAsync<int>(
            "k",
            factory: (ctx, _) =>
            {
                ctx.Expiration = TimeSpan.FromMinutes(3);
                return new ValueTask<int>(99);
            });

        Assert.Equal(99, result);
        StoredEntry entry = cache.Store["k"];
        Assert.Equal(TimeSpan.FromMinutes(3), entry.Options!.Expiration);
    }

    [Fact]
    public async Task TagsArePropagatedToCacheWrite()
    {
        var cache = new FakeHybridCache();
        var tags = new[] { "a", "b" };

        await cache.GetOrCreateAsync(
            "k", state: 0,
            factory: (_, _, _) => new ValueTask<int>(1),
            tags: tags);

        StoredEntry entry = cache.Store["k"];
        Assert.NotNull(entry.Tags);
        Assert.Equal(tags, entry.Tags!.ToArray());
    }

#if NET
    [Fact]
    public async Task ReadOnlySpanKey_FactoryMutationsReachCache()
    {
        var cache = new FakeHybridCache();
        HybridCacheEntryContext? observed = null;

        int result = await cache.GetOrCreateAsync(
            "span-key".AsSpan(), state: 0,
            factory: (_, ctx, _) =>
            {
                observed = ctx;
                ctx.Expiration = TimeSpan.FromMinutes(4);
                ctx.LocalSize = 321;
                return new ValueTask<int>(55);
            });

        Assert.Equal(55, result);
        Assert.NotNull(observed);
        StoredEntry entry = cache.Store["span-key"];
        Assert.Equal(55, entry.Value);
        Assert.Equal(TimeSpan.FromMinutes(4), entry.Options!.Expiration);
        Assert.Equal(321, entry.Options.LocalSize);
    }

    [Fact]
    public async Task ReadOnlySpanKey_NoStateOverload_FactoryMutationsReachCache()
    {
        var cache = new FakeHybridCache();

        int result = await cache.GetOrCreateAsync(
            "span-key-nostate".AsSpan(),
            factory: (ctx, _) =>
            {
                ctx.Expiration = TimeSpan.FromMinutes(6);
                return new ValueTask<int>(77);
            });

        Assert.Equal(77, result);
        StoredEntry entry = cache.Store["span-key-nostate"];
        Assert.Equal(77, entry.Value);
        Assert.Equal(TimeSpan.FromMinutes(6), entry.Options!.Expiration);
    }

    [Fact]
    public async Task InterpolatedStringKey_FactoryMutationsReachCache_AndClearsHandler()
    {
        var cache = new FakeHybridCache();

        // A DefaultInterpolatedStringHandler is a ref struct and cannot be a local in an async method,
        // so the synchronous handler work — including asserting the overload cleared it — lives in a
        // non-async local function that hands back the pending operation.
        ValueTask<int> pending = Invoke(cache);
        int result = await pending;

        Assert.Equal(42, result);
        StoredEntry entry = cache.Store["dish-key"];
        Assert.Equal(42, entry.Value);
        Assert.Equal(TimeSpan.FromMinutes(9), entry.Options!.Expiration);

        static ValueTask<int> Invoke(FakeHybridCache cache)
        {
            var key = new DefaultInterpolatedStringHandler(literalLength: 8, formattedCount: 0);
            key.AppendLiteral("dish-key");

            ValueTask<int> pending = cache.GetOrCreateAsync(
                ref key, state: 0,
                factory: (_, ctx, _) =>
                {
                    ctx.Expiration = TimeSpan.FromMinutes(9);
                    return new ValueTask<int>(42);
                });

            // key.Clear() must run before the overload returns, on every completion path (here the
            // factory ran, so the underlying operation may complete asynchronously).
            Assert.Equal(string.Empty, key.ToString());
            return pending;
        }
    }

    [Fact]
    public async Task InterpolatedStringKey_ClearsHandler_OnSynchronousCacheHit()
    {
        var cache = new FakeHybridCache();
        cache.Store["dish-hit"] = new StoredEntry(123, Options: null, Tags: null);

        // A pre-existing entry means the factory is never invoked and the operation completes
        // synchronously; the handler must still be cleared.
        ValueTask<int> pending = Invoke(cache);
        int result = await pending;

        Assert.Equal(123, result);

        static ValueTask<int> Invoke(FakeHybridCache cache)
        {
            var key = new DefaultInterpolatedStringHandler(literalLength: 8, formattedCount: 0);
            key.AppendLiteral("dish-hit");

            ValueTask<int> pending = cache.GetOrCreateAsync<int, int>(
                ref key, state: 0,
                factory: (_, _, _) => throw new Xunit.Sdk.XunitException("factory should not run on a cache hit"));

            Assert.Equal(string.Empty, key.ToString());
            return pending;
        }
    }
#endif

    private sealed record StoredEntry(object? Value, HybridCacheEntryOptions? Options, IEnumerable<string>? Tags);

    /// <summary>
    /// A minimal <see cref="HybridCache"/> backed by an in-memory dictionary. Writes from either the
    /// abstract <see cref="GetOrCreateAsync{TState, T}(string, TState, Func{TState, CancellationToken, ValueTask{T}}, HybridCacheEntryOptions?, IEnumerable{string}?, CancellationToken)"/>
    /// override or <see cref="SetAsync{T}"/> land in <see cref="Store"/>, so the test assertions
    /// are agnostic to which mechanism the default implementation uses.
    /// </summary>
    private sealed class FakeHybridCache : HybridCache
    {
        public Dictionary<string, StoredEntry> Store { get; } = new();

        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            if (Store.TryGetValue(key, out StoredEntry? existing) && existing.Value is T cached)
            {
                return new ValueTask<T>(cached);
            }

            return InvokeAndStoreAsync(key, state, factory, options, tags, cancellationToken);
        }

        private async ValueTask<T> InvokeAndStoreAsync<TState, T>(
            string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options, IEnumerable<string>? tags, CancellationToken cancellationToken)
        {
            T value = await factory(state, cancellationToken).ConfigureAwait(false);

            const HybridCacheEntryFlags BothWritesDisabled =
                HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite;
            HybridCacheEntryFlags flags = options?.Flags ?? HybridCacheEntryFlags.None;
            if ((flags & BothWritesDisabled) != BothWritesDisabled)
            {
                TryWrite(key, value, options, tags, cancellationToken);
            }

            return value;
        }

        public override ValueTask SetAsync<T>(
            string key, T value, HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        {
            TryWrite(key, value, options, tags, cancellationToken);
            return default;
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) => default;

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) => default;

        // Writes that arrive with an already-canceled token are dropped, so the test for
        // "caller cancellation must not abort the post-factory write" actually has teeth.
        private void TryWrite(string key, object? value, HybridCacheEntryOptions? options, IEnumerable<string>? tags, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Store[key] = new StoredEntry(value, options, tags);
        }
    }
}
