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
        /// <summary>Correlates the elements of two sequences based on key equality and groups the results.</summary>
        /// <typeparam name="TOuter"></typeparam>
        /// <typeparam name="TInner"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">
        /// A function to create a result element from an element from the first sequence
        /// and a collection of matching elements from the second sequence.
        /// </param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to use to hash and compare keys.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains elements of type <typeparamref name="TResult"/>
        /// that are obtained by performing a grouped join on two sequences.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="outer" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="inner" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="innerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>( // satisfies the C# query-expression pattern
            this IAsyncEnumerable<TOuter> outer,
            IAsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, TResult> resultSelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(outer);
            ThrowHelper.ThrowIfNull(inner);
            ThrowHelper.ThrowIfNull(outerKeySelector);
            ThrowHelper.ThrowIfNull(innerKeySelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                outer.IsKnownEmpty() ? Empty<TResult>() :
                Impl(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TOuter> outer,
                IAsyncEnumerable<TInner> inner,
                Func<TOuter, TKey> outerKeySelector,
                Func<TInner, TKey> innerKeySelector,
                Func<TOuter, IEnumerable<TInner>, TResult> resultSelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TOuter> e = outer.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        AsyncLookup<TKey, TInner> lookup = await AsyncLookup<TKey, TInner>.CreateForJoinAsync(inner, innerKeySelector, comparer, cancellationToken).ConfigureAwait(false);
                        do
                        {
                            TOuter item = e.Current;
                            yield return resultSelector(item, lookup[outerKeySelector(item)]);
                        }
                        while (await e.MoveNextAsync().ConfigureAwait(false));
                    }
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Correlates the elements of two sequences based on key equality and groups the results.</summary>
        /// <typeparam name="TOuter"></typeparam>
        /// <typeparam name="TInner"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">
        /// A function to create a result element from an element from the first sequence
        /// and a collection of matching elements from the second sequence.
        /// </param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to use to hash and compare keys.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains elements of type <typeparamref name="TResult"/>
        /// that are obtained by performing a grouped join on two sequences.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="outer" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="inner" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="innerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
            this IAsyncEnumerable<TOuter> outer,
            IAsyncEnumerable<TInner> inner,
            Func<TOuter, CancellationToken, ValueTask<TKey>> outerKeySelector,
            Func<TInner, CancellationToken, ValueTask<TKey>> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, CancellationToken, ValueTask<TResult>> resultSelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(outer);
            ThrowHelper.ThrowIfNull(inner);
            ThrowHelper.ThrowIfNull(outerKeySelector);
            ThrowHelper.ThrowIfNull(innerKeySelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                outer.IsKnownEmpty() ? Empty<TResult>() :
                Impl(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TOuter> outer,
                IAsyncEnumerable<TInner> inner,
                Func<TOuter, CancellationToken, ValueTask<TKey>> outerKeySelector,
                Func<TInner, CancellationToken, ValueTask<TKey>> innerKeySelector,
                Func<TOuter, IEnumerable<TInner>, CancellationToken, ValueTask<TResult>> resultSelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TOuter> e = outer.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        AsyncLookup<TKey, TInner> lookup = await AsyncLookup<TKey, TInner>.CreateForJoinAsync(inner, innerKeySelector, comparer, cancellationToken).ConfigureAwait(false);
                        do
                        {
                            TOuter item = e.Current;
                            yield return await resultSelector(
                                item,
                                lookup[await outerKeySelector(item, cancellationToken).ConfigureAwait(false)],
                                cancellationToken).ConfigureAwait(false);
                        }
                        while (await e.MoveNextAsync().ConfigureAwait(false));
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
