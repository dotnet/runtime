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
        /// <summary>Produces the set intersection of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IAsyncEnumerable{T}" /> whose distinct elements that also appear in <paramref name="second" /> will be returned.</param>
        /// <param name="second">An <see cref="IAsyncEnumerable{T}" /> whose distinct elements that also appear in the first sequence will be returned.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{TKey}" /> to compare keys.</param>
        /// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The intersection of two sets A and B is defined as the set that contains all the elements of A that also appear in B, but no other elements.</para>
        /// <para>When the object returned by this method is enumerated, `Intersect` yields distinct elements occurring in both sequences in the order in which they appear in <paramref name="first" />.</para>
        /// <para>If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to compare values.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> IntersectBy<TSource, TKey>(
            this IAsyncEnumerable<TSource> first,
            IAsyncEnumerable<TKey> second,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);
            ThrowHelper.ThrowIfNull(keySelector);

            return
                first.IsKnownEmpty() || second.IsKnownEmpty() ? Empty<TSource>() :
                Impl(first, second, keySelector, comparer, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> first,
                IAsyncEnumerable<TKey> second,
                Func<TSource, TKey> keySelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                HashSet<TKey> set;
                IAsyncEnumerator<TKey> e = second.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield break;
                    }

                    set = new(comparer);
                    do
                    {
                        set.Add(e.Current);
                    }
                    while (await e.MoveNextAsync().ConfigureAwait(false));
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }

                await foreach (TSource element in first.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (set.Remove(keySelector(element)))
                    {
                        yield return element;
                    }
                }
            }
        }

        /// <summary>Produces the set intersection of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IAsyncEnumerable{T}" /> whose distinct elements that also appear in <paramref name="second" /> will be returned.</param>
        /// <param name="second">An <see cref="IAsyncEnumerable{T}" /> whose distinct elements that also appear in the first sequence will be returned.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{TKey}" /> to compare keys.</param>
        /// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The intersection of two sets A and B is defined as the set that contains all the elements of A that also appear in B, but no other elements.</para>
        /// <para>When the object returned by this method is enumerated, `Intersect` yields distinct elements occurring in both sequences in the order in which they appear in <paramref name="first" />.</para>
        /// <para>If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to compare values.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> IntersectBy<TSource, TKey>(
            this IAsyncEnumerable<TSource> first,
            IAsyncEnumerable<TKey> second,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);
            ThrowHelper.ThrowIfNull(keySelector);

            return
                first.IsKnownEmpty() || second.IsKnownEmpty() ? Empty<TSource>() :
                Impl(first, second, keySelector, comparer, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> first,
                IAsyncEnumerable<TKey> second,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                HashSet<TKey> set;
                IAsyncEnumerator<TKey> e = second.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield break;
                    }

                    set = new(comparer);
                    do
                    {
                        set.Add(e.Current);
                    }
                    while (await e.MoveNextAsync().ConfigureAwait(false));
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }

                await foreach (TSource element in first.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (set.Remove(await keySelector(element, cancellationToken).ConfigureAwait(false)))
                    {
                        yield return element;
                    }
                }
            }
        }
    }
}
