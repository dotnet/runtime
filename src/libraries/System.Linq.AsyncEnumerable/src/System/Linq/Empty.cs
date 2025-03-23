// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>
        /// Returns an empty <see cref="IAsyncEnumerable{T}"/> that has the specified type argument.
        /// </summary>
        /// <typeparam name="TResult">The type of the elements of the sequence.</typeparam>
        /// <returns>An empty <see cref="IAsyncEnumerable{T}"/> whose type argument is <typeparamref name="TResult"/>.</returns>
        public static IAsyncEnumerable<TResult> Empty<TResult>() => EmptyAsyncEnumerable<TResult>.Instance;

        /// <summary>Determines whether <paramref name="source"/> is known to be an always-empty enumerable.</summary>
        private static bool IsKnownEmpty<TResult>(this IAsyncEnumerable<TResult> source) =>
            ReferenceEquals(source, EmptyAsyncEnumerable<TResult>.Instance);

        private sealed class EmptyAsyncEnumerable<TResult> :
            IAsyncEnumerable<TResult>, IAsyncEnumerator<TResult>, IOrderedAsyncEnumerable<TResult>
        {
            public static readonly EmptyAsyncEnumerable<TResult> Instance = new();

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) => this;

            public ValueTask<bool> MoveNextAsync() => default;

            public TResult Current => default!;

            public ValueTask DisposeAsync() => default;

            public IOrderedAsyncEnumerable<TResult> CreateOrderedAsyncEnumerable<TKey>(Func<TResult, TKey> keySelector, IComparer<TKey>? comparer, bool descending)
            {
                ThrowHelper.ThrowIfNull(keySelector);
                return this;
            }

            public IOrderedAsyncEnumerable<TResult> CreateOrderedAsyncEnumerable<TKey>(Func<TResult, CancellationToken, ValueTask<TKey>> keySelector, IComparer<TKey>? comparer, bool descending)
            {
                ThrowHelper.ThrowIfNull(keySelector);
                return this;
            }
        }
    }
}
