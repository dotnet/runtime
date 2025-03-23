// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Correlates the elements of two sequences based on matching keys.</summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to use to hash and compare keys.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that has elements of type <typeparamref name="TResult"/>
        /// that are obtained by performing an inner join on two sequences.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="outer" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="inner" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="innerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>( // satisfies the C# query-expression pattern
            this IAsyncEnumerable<TOuter> outer,
            IAsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(outer);
            ThrowHelper.ThrowIfNull(inner);
            ThrowHelper.ThrowIfNull(outerKeySelector);
            ThrowHelper.ThrowIfNull(innerKeySelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                outer.IsKnownEmpty() || inner.IsKnownEmpty() ? Empty<TResult>() :
                Impl(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TOuter> outer, IAsyncEnumerable<TInner> inner,
                Func<TOuter, TKey> outerKeySelector,
                Func<TInner, TKey> innerKeySelector,
                Func<TOuter, TInner, TResult> resultSelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TOuter> e = outer.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        AsyncLookup<TKey, TInner> lookup = await AsyncLookup<TKey, TInner>.CreateForJoinAsync(inner, innerKeySelector, comparer, cancellationToken).ConfigureAwait(false);
                        if (lookup.Count != 0)
                        {
                            do
                            {
                                TOuter item = e.Current;
                                Grouping<TKey, TInner>? g = lookup.GetGrouping(outerKeySelector(item), create: false);
                                if (g is not null)
                                {
                                    int count = g._count;
                                    TInner[] elements = g._elements;
                                    for (int i = 0; i != count; ++i)
                                    {
                                        yield return resultSelector(item, elements[i]);
                                    }
                                }
                            }
                            while (await e.MoveNextAsync().ConfigureAwait(false));
                        }
                    }
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Correlates the elements of two sequences based on matching keys.</summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to use to hash and compare keys.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that has elements of type <typeparamref name="TResult"/>
        /// that are obtained by performing an inner join on two sequences.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="outer" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="inner" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="innerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(
            this IAsyncEnumerable<TOuter> outer,
            IAsyncEnumerable<TInner> inner,
            Func<TOuter, CancellationToken, ValueTask<TKey>> outerKeySelector,
            Func<TInner, CancellationToken, ValueTask<TKey>> innerKeySelector,
            Func<TOuter, TInner, CancellationToken, ValueTask<TResult>> resultSelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(outer);
            ThrowHelper.ThrowIfNull(inner);
            ThrowHelper.ThrowIfNull(outerKeySelector);
            ThrowHelper.ThrowIfNull(innerKeySelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                outer.IsKnownEmpty() || inner.IsKnownEmpty() ? Empty<TResult>() :
                Impl(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TOuter> outer,
                IAsyncEnumerable<TInner> inner,
                Func<TOuter, CancellationToken, ValueTask<TKey>> outerKeySelector,
                Func<TInner, CancellationToken, ValueTask<TKey>> innerKeySelector,
                Func<TOuter, TInner, CancellationToken, ValueTask<TResult>> resultSelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TOuter> e = outer.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        AsyncLookup<TKey, TInner> lookup = await AsyncLookup<TKey, TInner>.CreateForJoinAsync(inner, innerKeySelector, comparer, cancellationToken).ConfigureAwait(false);
                        if (lookup.Count != 0)
                        {
                            do
                            {
                                TOuter item = e.Current;
                                Grouping<TKey, TInner>? g = lookup.GetGrouping(await outerKeySelector(item, cancellationToken).ConfigureAwait(false), create: false);
                                if (g is not null)
                                {
                                    int count = g._count;
                                    TInner[] elements = g._elements;
                                    for (int i = 0; i != count; ++i)
                                    {
                                        yield return await resultSelector(item, elements[i], cancellationToken).ConfigureAwait(false);
                                    }
                                }
                            }
                            while (await e.MoveNextAsync().ConfigureAwait(false));
                        }
                    }
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
