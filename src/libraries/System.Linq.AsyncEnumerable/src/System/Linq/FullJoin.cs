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
        /// <summary>Correlates the elements of two async sequences based on matching keys, producing a result for matched and unmatched elements.</summary>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to use to hash and compare keys.</param>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <returns>An <see cref="IAsyncEnumerable{T}" /> that has elements of type <typeparamref name="TResult" /> that are obtained by performing a full outer join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outer" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="inner" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="innerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(
            this IAsyncEnumerable<TOuter> outer,
            IAsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter?, TInner?, TResult> resultSelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ArgumentNullException.ThrowIfNull(outer);
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(outerKeySelector);
            ArgumentNullException.ThrowIfNull(innerKeySelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            return Impl(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TOuter> outer, IAsyncEnumerable<TInner> inner,
                Func<TOuter, TKey> outerKeySelector,
                Func<TInner, TKey> innerKeySelector,
                Func<TOuter?, TInner?, TResult> resultSelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                AsyncLookup<TKey, TInner> innerLookup = await AsyncLookup<TKey, TInner>.CreateForJoinAsync(inner, innerKeySelector, comparer, cancellationToken);

                HashSet<Grouping<TKey, TInner>>? matchedGroupings = innerLookup.Count != 0
                    ? new HashSet<Grouping<TKey, TInner>>()
                    : null;

                await foreach (TOuter item in outer.WithCancellation(cancellationToken))
                {
                    Grouping<TKey, TInner>? g = innerLookup.GetGrouping(outerKeySelector(item), create: false);
                    if (g is null)
                    {
                        yield return resultSelector(item, default);
                    }
                    else
                    {
                        matchedGroupings!.Add(g);
                        int count = g._count;
                        TInner[] elements = g._elements;
                        for (int i = 0; i != count; ++i)
                        {
                            yield return resultSelector(item, elements[i]);
                        }
                    }
                }

                if (matchedGroupings is null || matchedGroupings.Count < innerLookup.Count)
                {
                    Grouping<TKey, TInner>? g = innerLookup._lastGrouping;
                    if (g is not null)
                    {
                        do
                        {
                            g = g._next!;
                            if (matchedGroupings is null || !matchedGroupings.Contains(g))
                            {
                                int count = g._count;
                                TInner[] elements = g._elements;
                                for (int i = 0; i != count; ++i)
                                {
                                    yield return resultSelector(default, elements[i]);
                                }
                            }
                        }
                        while (g != innerLookup._lastGrouping);
                    }
                }
            }
        }

        /// <summary>Correlates the elements of two async sequences based on matching keys, producing a result for matched and unmatched elements.</summary>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to use to hash and compare keys.</param>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <returns>An <see cref="IAsyncEnumerable{T}" /> that has elements of type <typeparamref name="TResult" /> that are obtained by performing a full outer join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outer" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="inner" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="innerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(
            this IAsyncEnumerable<TOuter> outer,
            IAsyncEnumerable<TInner> inner,
            Func<TOuter, CancellationToken, ValueTask<TKey>> outerKeySelector,
            Func<TInner, CancellationToken, ValueTask<TKey>> innerKeySelector,
            Func<TOuter?, TInner?, CancellationToken, ValueTask<TResult>> resultSelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ArgumentNullException.ThrowIfNull(outer);
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(outerKeySelector);
            ArgumentNullException.ThrowIfNull(innerKeySelector);
            ArgumentNullException.ThrowIfNull(resultSelector);

            return Impl(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TOuter> outer,
                IAsyncEnumerable<TInner> inner,
                Func<TOuter, CancellationToken, ValueTask<TKey>> outerKeySelector,
                Func<TInner, CancellationToken, ValueTask<TKey>> innerKeySelector,
                Func<TOuter?, TInner?, CancellationToken, ValueTask<TResult>> resultSelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                AsyncLookup<TKey, TInner> innerLookup = await AsyncLookup<TKey, TInner>.CreateForJoinAsync(inner, innerKeySelector, comparer, cancellationToken);

                HashSet<Grouping<TKey, TInner>>? matchedGroupings = innerLookup.Count != 0
                    ? new HashSet<Grouping<TKey, TInner>>()
                    : null;

                await foreach (TOuter item in outer.WithCancellation(cancellationToken))
                {
                    Grouping<TKey, TInner>? g = innerLookup.GetGrouping(await outerKeySelector(item, cancellationToken), create: false);
                    if (g is null)
                    {
                        yield return await resultSelector(item, default, cancellationToken);
                    }
                    else
                    {
                        matchedGroupings!.Add(g);
                        int count = g._count;
                        TInner[] elements = g._elements;
                        for (int i = 0; i != count; ++i)
                        {
                            yield return await resultSelector(item, elements[i], cancellationToken);
                        }
                    }
                }

                if (matchedGroupings is null || matchedGroupings.Count < innerLookup.Count)
                {
                    Grouping<TKey, TInner>? g = innerLookup._lastGrouping;
                    if (g is not null)
                    {
                        do
                        {
                            g = g._next!;
                            if (matchedGroupings is null || !matchedGroupings.Contains(g))
                            {
                                int count = g._count;
                                TInner[] elements = g._elements;
                                for (int i = 0; i != count; ++i)
                                {
                                    yield return await resultSelector(default, elements[i], cancellationToken);
                                }
                            }
                        }
                        while (g != innerLookup._lastGrouping);
                    }
                }
            }
        }

        /// <summary>Correlates the elements of two async sequences based on matching keys, producing a tuple for matched and unmatched elements.</summary>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to use to hash and compare keys.</param>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <returns>An <see cref="IAsyncEnumerable{T}" /> that has elements of type <c>(TOuter?, TInner?)</c> that are obtained by performing a full outer join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outer" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="inner" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="innerKeySelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<(TOuter? Outer, TInner? Inner)> FullJoin<TOuter, TInner, TKey>(
            this IAsyncEnumerable<TOuter> outer,
            IAsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            return FullJoin(outer, inner, outerKeySelector, innerKeySelector, static (outer, inner) => (outer, inner), comparer);
        }

        /// <summary>Correlates the elements of two async sequences based on matching keys, producing a tuple for matched and unmatched elements.</summary>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to use to hash and compare keys.</param>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <returns>An <see cref="IAsyncEnumerable{T}" /> that has elements of type <c>(TOuter?, TInner?)</c> that are obtained by performing a full outer join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outer" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="inner" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="innerKeySelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<(TOuter? Outer, TInner? Inner)> FullJoin<TOuter, TInner, TKey>(
            this IAsyncEnumerable<TOuter> outer,
            IAsyncEnumerable<TInner> inner,
            Func<TOuter, CancellationToken, ValueTask<TKey>> outerKeySelector,
            Func<TInner, CancellationToken, ValueTask<TKey>> innerKeySelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            return FullJoin(outer, inner, outerKeySelector, innerKeySelector, static (outer, inner, ct) => new ValueTask<(TOuter?, TInner?)>((outer, inner)), comparer);
        }
    }
}
