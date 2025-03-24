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
        /// <summary>Groups the elements of a sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> of elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{IGrouping}"/> where each <see cref="IGrouping{TKey, TElement}"/>
        /// contains a sequence of objects and a key.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>( // satisfies the C# query-expression pattern
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return
                source.IsKnownEmpty() ? Empty<IGrouping<TKey, TSource>>() :
                Impl(source, keySelector, comparer, default);

            static async IAsyncEnumerable<IGrouping<TKey, TSource>> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                foreach (IGrouping<TKey, TSource> item in await ToLookupAsync(source, keySelector, comparer, cancellationToken).ConfigureAwait(false))
                {
                    yield return item;
                }
            }
        }

        /// <summary>Groups the elements of a sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> of elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{IGrouping}"/> where each <see cref="IGrouping{TKey, TElement}"/>
        /// contains a sequence of objects and a key.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return
                source.IsKnownEmpty() ? Empty<IGrouping<TKey, TSource>>() :
                Impl(source, keySelector, comparer, default);

            static async IAsyncEnumerable<IGrouping<TKey, TSource>> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                foreach (IGrouping<TKey, TSource> item in await ToLookupAsync(source, keySelector, comparer, cancellationToken).ConfigureAwait(false))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Groups the elements of a sequence according to a key selector function. The keys
        /// are compared by using a comparer and each group's elements are projected by using
        /// a specified function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in the <see cref="IGrouping{TKey, TElement}"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> of elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an <see cref="IGrouping{TKey, TElement}"/>.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{IGrouping}"/> where each <see cref="IGrouping{TKey, TElement}"/>
        /// contains a sequence of objects of type <typeparamref name="TElement"/> and a key.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="elementSelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>( // satisfies the C# query-expression pattern
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(elementSelector);

            return
                source.IsKnownEmpty() ? Empty<IGrouping<TKey, TElement>>() :
                Impl(source, keySelector, elementSelector, comparer, default);

            static async IAsyncEnumerable<IGrouping<TKey, TElement>> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                Func<TSource, TElement> elementSelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                foreach (IGrouping<TKey, TElement> item in await ToLookupAsync(source, keySelector, elementSelector, comparer, cancellationToken).ConfigureAwait(false))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Groups the elements of a sequence according to a key selector function. The keys
        /// are compared by using a comparer and each group's elements are projected by using
        /// a specified function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in the <see cref="IGrouping{TKey, TElement}"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> of elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an <see cref="IGrouping{TKey, TElement}"/>.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{IGrouping}"/> where each <see cref="IGrouping{TKey, TElement}"/>
        /// contains a sequence of objects of type <typeparamref name="TElement"/> and a key.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="elementSelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            Func<TSource, CancellationToken, ValueTask<TElement>> elementSelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(elementSelector);

            return
                source.IsKnownEmpty() ? Empty<IGrouping<TKey, TElement>>() :
                Impl(source, keySelector, elementSelector, comparer, default);

            static async IAsyncEnumerable<IGrouping<TKey, TElement>> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                Func<TSource, CancellationToken, ValueTask<TElement>> elementSelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                foreach (IGrouping<TKey, TElement> item in await ToLookupAsync(source, keySelector, elementSelector, comparer, cancellationToken).ConfigureAwait(false))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Groups the elements of a sequence according to a specified key selector function
        /// and creates a result value from each group and its key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by resultSelector.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> of elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>
        /// A collection of elements of type <typeparamref name="TResult"/> where each element represents
        /// a projection over a group and its key.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> GroupBy<TSource, TKey, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, IEnumerable<TSource>, TResult> resultSelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, keySelector, resultSelector, comparer, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                Func<TKey, IEnumerable<TSource>, TResult> resultSelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                if (await ToLookupAsync(source, keySelector, comparer, cancellationToken).ConfigureAwait(false) is AsyncLookup<TKey, TSource> lookup)
                {
                    foreach (TResult item in lookup.ApplyResultSelector(resultSelector))
                    {
                        yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// Groups the elements of a sequence according to a specified key selector function
        /// and creates a result value from each group and its key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by resultSelector.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> of elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>
        /// A collection of elements of type <typeparamref name="TResult"/> where each element represents
        /// a projection over a group and its key.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> GroupBy<TSource, TKey, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            Func<TKey, IEnumerable<TSource>, CancellationToken, ValueTask<TResult>> resultSelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, keySelector, resultSelector, comparer, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                Func<TKey, IEnumerable<TSource>, CancellationToken, ValueTask<TResult>> resultSelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                if (await ToLookupAsync(source, keySelector, comparer, cancellationToken).ConfigureAwait(false) is AsyncLookup<TKey, TSource> lookup)
                {
                    await foreach (TResult item in lookup.ApplyResultSelector(resultSelector, cancellationToken).ConfigureAwait(false))
                    {
                        yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// Groups the elements of a sequence according to a specified key selector function
        /// and creates a result value from each group and its key. Key values are compared
        /// by using a specified comparer, and the elements of each group are projected by
        /// using a specified function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each <see cref="IGrouping{TKey, TElement}"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> of elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an <see cref="IGrouping{TKey, TElement}"/>.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A collection of elements of type <typeparamref name="TResult"/> where each element represents a projection over a group and its key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="elementSelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(elementSelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, keySelector, elementSelector, resultSelector, comparer, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                Func<TSource, TElement> elementSelector,
                Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                if (await ToLookupAsync(source, keySelector, elementSelector, comparer, cancellationToken).ConfigureAwait(false) is AsyncLookup<TKey, TElement> lookup)
                {
                    foreach (TResult item in lookup.ApplyResultSelector(resultSelector))
                    {
                        yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// Groups the elements of a sequence according to a specified key selector function
        /// and creates a result value from each group and its key. Key values are compared
        /// by using a specified comparer, and the elements of each group are projected by
        /// using a specified function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each <see cref="IGrouping{TKey, TElement}"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> of elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an <see cref="IGrouping{TKey, TElement}"/>.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A collection of elements of type <typeparamref name="TResult"/> where each element represents a projection over a group and its key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="elementSelector" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            Func<TSource, CancellationToken, ValueTask<TElement>> elementSelector,
            Func<TKey, IEnumerable<TElement>, CancellationToken, ValueTask<TResult>> resultSelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(elementSelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, keySelector, elementSelector, resultSelector, comparer, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                Func<TSource, CancellationToken, ValueTask<TElement>> elementSelector,
                Func<TKey, IEnumerable<TElement>, CancellationToken, ValueTask<TResult>> resultSelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                if (await ToLookupAsync(source, keySelector, elementSelector, comparer, cancellationToken).ConfigureAwait(false) is AsyncLookup<TKey, TElement> lookup)
                {
                    await foreach (TResult item in lookup.ApplyResultSelector(resultSelector, cancellationToken).ConfigureAwait(false))
                    {
                        yield return item;
                    }
                }
            }
        }

        internal sealed class Grouping<TKey, TElement> : IGrouping<TKey, TElement>, IList<TElement>
        {
            internal readonly TKey _key;
            internal readonly int _hashCode;
            internal TElement[] _elements;
            internal int _count;
            internal Grouping<TKey, TElement>? _hashNext;
            internal Grouping<TKey, TElement>? _next;

            internal Grouping(TKey key, int hashCode)
            {
                _key = key;
                _hashCode = hashCode;
                _elements = new TElement[1];
            }

            internal void Add(TElement element)
            {
                if (_elements.Length == _count)
                {
                    Array.Resize(ref _elements, checked(_count * 2));
                }

                _elements[_count] = element;
                _count++;
            }

            internal void Trim()
            {
                if (_elements.Length != _count)
                {
                    Array.Resize(ref _elements, _count);
                }
            }

            public IEnumerator<TElement> GetEnumerator()
            {
                Debug.Assert(_count > 0, "A grouping should only have been created if an element was being added to it.");
                for (int i = 0; i < _count; i++)
                {
                    yield return _elements[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public TKey Key => _key;

            int ICollection<TElement>.Count => _count;

            bool ICollection<TElement>.IsReadOnly => true;

            void ICollection<TElement>.Add(TElement item) => throw new NotSupportedException();

            void ICollection<TElement>.Clear() => throw new NotSupportedException();

            bool ICollection<TElement>.Contains(TElement item) => Array.IndexOf(_elements, item, 0, _count) >= 0;

            void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex) =>
                Array.Copy(_elements, 0, array, arrayIndex, _count);

            bool ICollection<TElement>.Remove(TElement item) => throw new NotSupportedException();

            int IList<TElement>.IndexOf(TElement item) => Array.IndexOf(_elements, item, 0, _count);

            void IList<TElement>.Insert(int index, TElement item) => throw new NotSupportedException();

            void IList<TElement>.RemoveAt(int index) => throw new NotSupportedException();

            TElement IList<TElement>.this[int index]
            {
                get
                {
                    if ((uint)index >= (uint)_count)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
                    }

                    return _elements[index];
                }
                set => throw new NotSupportedException();
            }
        }
    }
}
