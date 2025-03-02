// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>
        /// Creates a <see cref="ILookup{TKey, TElement}"/> from an <see cref="IAsyncEnumerable{T}"/>
        /// according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to create a <see cref="ILookup{TKey, TElement}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="ILookup{TKey, TElement}"/> that contains keys and values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static ValueTask<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey>? comparer = null,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), keySelector, comparer);

            static async ValueTask<ILookup<TKey, TSource>> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                IEqualityComparer<TKey>? comparer)
            {
                ConfiguredCancelableAsyncEnumerable<TSource>.Enumerator e = source.GetAsyncEnumerator();
                try
                {
                    if (!await e.MoveNextAsync())
                    {
                        return EmptyLookup<TKey, TSource>.Instance;
                    }

                    AsyncLookup<TKey, TSource> lookup = new(comparer);
                    do
                    {
                        TSource item = e.Current;
                        lookup.GetGrouping(keySelector(item), create: true)!.Add(item);
                    }
                    while (await e.MoveNextAsync());

                    return lookup;
                }
                finally
                {
                    await e.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="ILookup{TKey, TElement}"/> from an <see cref="IAsyncEnumerable{T}"/>
        /// according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to create a <see cref="ILookup{TKey, TElement}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="ILookup{TKey, TElement}"/> that contains keys and values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static ValueTask<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            IEqualityComparer<TKey>? comparer = null,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return Impl(source, keySelector, comparer, cancellationToken);

            static async ValueTask<ILookup<TKey, TSource>> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                IEqualityComparer<TKey>? comparer,
                CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        return EmptyLookup<TKey, TSource>.Instance;
                    }

                    AsyncLookup<TKey, TSource> lookup = new(comparer);
                    do
                    {
                        TSource item = e.Current;
                        lookup.GetGrouping(await keySelector(item, cancellationToken).ConfigureAwait(false), create: true)!.Add(item);
                    }
                    while (await e.MoveNextAsync().ConfigureAwait(false));

                    return lookup;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="ILookup{TKey, TElement}"/> from an <see cref="IAsyncEnumerable{T}"/>
        /// according to a specified key selector function and element selector functions.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to create a <see cref="ILookup{TKey, TElement}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="ILookup{TKey, TElement}"/> that contains keys and values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static ValueTask<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey>? comparer = null,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(elementSelector);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), keySelector, elementSelector, comparer);

            static async ValueTask<ILookup<TKey, TElement>> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                Func<TSource, TElement> elementSelector,
                IEqualityComparer<TKey>? comparer)
            {
                ConfiguredCancelableAsyncEnumerable<TSource>.Enumerator e = source.GetAsyncEnumerator();
                try
                {
                    if (!await e.MoveNextAsync())
                    {
                        return EmptyLookup<TKey, TElement>.Instance;
                    }

                    AsyncLookup<TKey, TElement> lookup = new(comparer);
                    do
                    {
                        TSource item = e.Current;
                        lookup.GetGrouping(keySelector(item), create: true)!.Add(elementSelector(item));
                    }
                    while (await e.MoveNextAsync());

                    return lookup;
                }
                finally
                {
                    await e.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="ILookup{TKey, TElement}"/> from an <see cref="IAsyncEnumerable{T}"/>
        /// according to a specified key selector function and element selector functions.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to create a <see cref="ILookup{TKey, TElement}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="ILookup{TKey, TElement}"/> that contains keys and values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static ValueTask<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            Func<TSource, CancellationToken, ValueTask<TElement>> elementSelector,
            IEqualityComparer<TKey>? comparer = null,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(elementSelector);

            return Impl(source, keySelector, elementSelector, comparer, cancellationToken);

            static async ValueTask<ILookup<TKey, TElement>> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                Func<TSource, CancellationToken, ValueTask<TElement>> elementSelector,
                IEqualityComparer<TKey>? comparer,
                CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        return EmptyLookup<TKey, TElement>.Instance;
                    }

                    AsyncLookup<TKey, TElement> lookup = new(comparer);
                    do
                    {
                        TSource item = e.Current;
                        lookup.GetGrouping(await keySelector(item, cancellationToken).ConfigureAwait(false), create: true)!.Add(await elementSelector(item, cancellationToken).ConfigureAwait(false));
                    }
                    while (await e.MoveNextAsync().ConfigureAwait(false));

                    return lookup;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        [DebuggerDisplay("Count = 0")]
        private sealed class EmptyLookup<TKey, TElement> : ILookup<TKey, TElement>, IList<IGrouping<TKey, TElement>>, IReadOnlyCollection<IGrouping<TKey, TElement>>
        {
            public static readonly EmptyLookup<TKey, TElement> Instance = new();

            public bool IsReadOnly => true;

            public int Count => 0;

            public IEnumerable<TElement> this[TKey key] => [];

            public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator() => Enumerable.Empty<IGrouping<TKey, TElement>>().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool Contains(TKey key) => false;

            public bool Contains(IGrouping<TKey, TElement> item) => false;

            public void CopyTo(IGrouping<TKey, TElement>[] array, int arrayIndex)
            {
                ThrowHelper.ThrowIfNull(array);
                if ((uint)arrayIndex > (uint)array.Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(arrayIndex));
                }
            }

            public int IndexOf(IGrouping<TKey, TElement> item) => -1;

            public void Add(IGrouping<TKey, TElement> item) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public IGrouping<TKey, TElement> this[int index]
            {
                get => throw new ArgumentOutOfRangeException(nameof(index));
                set => throw new NotSupportedException();
            }

            public void Insert(int index, IGrouping<TKey, TElement> item) => throw new NotSupportedException();

            public bool Remove(IGrouping<TKey, TElement> item) => throw new NotSupportedException();

            public void RemoveAt(int index) => throw new NotSupportedException();
        }

        [DebuggerDisplay("Count = {Count}")]
        private sealed class AsyncLookup<TKey, TElement> : ILookup<TKey, TElement>
        {
            private readonly IEqualityComparer<TKey> _comparer;
            private Grouping<TKey, TElement>[] _groupings;
            internal Grouping<TKey, TElement>? _lastGrouping;
            private int _count;

            internal AsyncLookup(IEqualityComparer<TKey>? comparer)
            {
                _comparer = comparer ?? EqualityComparer<TKey>.Default;
                _groupings = new Grouping<TKey, TElement>[7];
            }

            internal static async ValueTask<AsyncLookup<TKey, TElement>> CreateForJoinAsync(
                IAsyncEnumerable<TElement> source,
                Func<TElement, TKey> keySelector,
                IEqualityComparer<TKey>? comparer,
                CancellationToken cancellationToken)
            {
                Debug.Assert(source is not null);
                Debug.Assert(keySelector is not null);

                AsyncLookup<TKey, TElement> lookup = new(comparer);
                await foreach (TElement item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    TKey key = keySelector(item);
                    if (key is not null)
                    {
                        lookup.GetGrouping(key, create: true)!.Add(item);
                    }
                }

                return lookup;
            }

            internal static async ValueTask<AsyncLookup<TKey, TElement>> CreateForJoinAsync(
                IAsyncEnumerable<TElement> source,
                Func<TElement, CancellationToken, ValueTask<TKey>> keySelector,
                IEqualityComparer<TKey>? comparer,
                CancellationToken cancellationToken)
            {
                Debug.Assert(source is not null);
                Debug.Assert(keySelector is not null);

                AsyncLookup<TKey, TElement> lookup = new(comparer);
                await foreach (TElement item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    TKey key = await keySelector(item, cancellationToken).ConfigureAwait(false);
                    if (key is not null)
                    {
                        lookup.GetGrouping(key, create: true)!.Add(item);
                    }
                }

                return lookup;
            }

            public int Count => _count;

            public IEnumerable<TElement> this[TKey key] => GetGrouping(key, create: false) ?? Enumerable.Empty<TElement>();

            public bool Contains(TKey key) => GetGrouping(key, create: false) is not null;

            public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
            {
                Grouping<TKey, TElement>? g = _lastGrouping;
                if (g is not null)
                {
                    do
                    {
                        g = g._next;

                        Debug.Assert(g is not null);
                        yield return g;
                    }
                    while (g != _lastGrouping);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            internal Grouping<TKey, TElement>? GetGrouping(TKey key, bool create)
            {
                int hashCode = (key is null) ? 0 : _comparer.GetHashCode(key) & 0x7FFFFFFF;
                for (Grouping<TKey, TElement>? g = _groupings[(uint)hashCode % _groupings.Length]; g is not null; g = g._hashNext)
                {
                    if (g._hashCode == hashCode && _comparer.Equals(g._key, key))
                    {
                        return g;
                    }
                }

                if (create)
                {
                    if (_count == _groupings.Length)
                    {
                        Resize();
                    }

                    int index = hashCode % _groupings.Length;
                    Grouping<TKey, TElement> g = new(key, hashCode)
                    {
                        _hashNext = _groupings[index]
                    };
                    _groupings[index] = g;
                    if (_lastGrouping is null)
                    {
                        g._next = g;
                    }
                    else
                    {
                        g._next = _lastGrouping._next;
                        _lastGrouping._next = g;
                    }

                    _lastGrouping = g;
                    _count++;
                    return g;
                }

                return null;
            }

            private void Resize()
            {
                int newSize = checked((_count * 2) + 1);
                Grouping<TKey, TElement>[] newGroupings = new Grouping<TKey, TElement>[newSize];
                Grouping<TKey, TElement> g = _lastGrouping!;
                do
                {
                    g = g._next!;
                    int index = g._hashCode % newSize;
                    g._hashNext = newGroupings[index];
                    newGroupings[index] = g;
                }
                while (g != _lastGrouping);

                _groupings = newGroupings;
            }

            internal IEnumerable<TResult> ApplyResultSelector<TResult>(
                Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
            {
                Grouping<TKey, TElement>? g = _lastGrouping;
                if (g is not null)
                {
                    do
                    {
                        g = g._next;

                        Debug.Assert(g is not null);
                        g.Trim();
                        yield return resultSelector(g._key, g._elements);
                    }
                    while (g != _lastGrouping);
                }
            }

            internal async IAsyncEnumerable<TResult> ApplyResultSelector<TResult>(
                Func<TKey, IEnumerable<TElement>, CancellationToken, ValueTask<TResult>> resultSelector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                Grouping<TKey, TElement>? g = _lastGrouping;
                if (g is not null)
                {
                    do
                    {
                        g = g._next;

                        Debug.Assert(g is not null);
                        g.Trim();
                        yield return await resultSelector(g._key, g._elements, cancellationToken).ConfigureAwait(false);
                    }
                    while (g != _lastGrouping);
                }
            }
        }
    }
}
