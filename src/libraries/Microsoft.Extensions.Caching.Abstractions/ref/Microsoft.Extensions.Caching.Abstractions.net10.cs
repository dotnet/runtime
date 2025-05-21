// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Caching.Hybrid
{
    public abstract partial class HybridCache
    {
        public System.Threading.Tasks.ValueTask<T> GetOrCreateAsync<T>(
            System.ReadOnlySpan<char> key,
            System.Func<System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<T>> factory,
            Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions? options = null,
            System.Collections.Generic.IEnumerable<string>? tags = null,
            System.Threading.CancellationToken cancellationToken = default) => throw null;
        public virtual System.Threading.Tasks.ValueTask<T> GetOrCreateAsync<TState, T>(
            System.ReadOnlySpan<char> key,
            TState state,
            System.Func<TState, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<T>> factory,
            Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions? options = null,
            System.Collections.Generic.IEnumerable<string>? tags = null,
            System.Threading.CancellationToken cancellationToken = default) => throw null;
        public System.Threading.Tasks.ValueTask<T> GetOrCreateAsync<T>(
            ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler key,
            System.Func<System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<T>> factory,
            Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions? options = null,
            System.Collections.Generic.IEnumerable<string>? tags = null,
            System.Threading.CancellationToken cancellationToken = default) => throw null;
        public System.Threading.Tasks.ValueTask<T> GetOrCreateAsync<TState, T>(
            ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler key,
            TState state,
            System.Func<TState, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<T>> factory,
            Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions? options = null,
            System.Collections.Generic.IEnumerable<string>? tags = null,
            System.Threading.CancellationToken cancellationToken = default) => throw null;
    }
}
