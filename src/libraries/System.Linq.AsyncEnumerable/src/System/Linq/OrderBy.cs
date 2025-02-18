// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Sorts the elements of a sequence in ascending order.</summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TElement}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static IOrderedAsyncEnumerable<T> Order<T>(
            this IAsyncEnumerable<T> source,
            IComparer<T>? comparer = null) =>
            OrderBy(source, EnumerableSorter<T>.IdentityFunc, comparer);

        /// <summary>Sorts the elements of a sequence in ascending order.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TElement}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IOrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>( // satisfies the C# query-expression pattern
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return
                source.IsKnownEmpty() ? EmptyAsyncEnumerable<TSource>.Instance :
                new OrderedIterator<TSource, TKey>(source, keySelector, comparer, false, null);
        }

        /// <summary>Sorts the elements of a sequence in ascending order.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TElement}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IOrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            IComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return
                source.IsKnownEmpty() ? EmptyAsyncEnumerable<TSource>.Instance :
                new OrderedIterator<TSource, TKey>(source, keySelector, comparer, false, null);
        }

        /// <summary>Sorts the elements of a sequence in descending order.</summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TElement}"/> whose elements are sorted in descending order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static IOrderedAsyncEnumerable<T> OrderDescending<T>(
            this IAsyncEnumerable<T> source,
            IComparer<T>? comparer = null) =>
            OrderByDescending(source, EnumerableSorter<T>.IdentityFunc, comparer);

        /// <summary>Sorts the elements of a sequence in descending order.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TElement}"/> whose elements are sorted in descending order according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IOrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>( // satisfies the C# query-expression pattern
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return
                source.IsKnownEmpty() ? EmptyAsyncEnumerable<TSource>.Instance :
                new OrderedIterator<TSource, TKey>(source, keySelector, comparer, true, null);
        }

        /// <summary>Sorts the elements of a sequence in descending order.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TElement}"/> whose elements are sorted in descending order according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IOrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            IComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return
                source.IsKnownEmpty() ? EmptyAsyncEnumerable<TSource>.Instance :
                new OrderedIterator<TSource, TKey>(source, keySelector, comparer, true, null);
        }

        /// <summary>Performs a subsequent ordering of the elements in a sequence in ascending order.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TElement}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IOrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>( // satisfies the C# query-expression pattern
            this IOrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);

            return source.CreateOrderedAsyncEnumerable(keySelector, comparer, descending: false);
        }

        /// <summary>Performs a subsequent ordering of the elements in a sequence in ascending order.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TElement}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IOrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(
            this IOrderedAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            IComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);

            return source.CreateOrderedAsyncEnumerable(keySelector, comparer, descending: false);
        }

        /// <summary>Performs a subsequent ordering of the elements in a sequence in descending order.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TElement}"/> whose elements are sorted in descending order according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IOrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>( // satisfies the C# query-expression pattern
            this IOrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);

            return source.CreateOrderedAsyncEnumerable(keySelector, comparer, descending: true);
        }

        /// <summary>Performs a subsequent ordering of the elements in a sequence in descending order.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TElement}"/> whose elements are sorted in descending order according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IOrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(
            this IOrderedAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            IComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);

            return source.CreateOrderedAsyncEnumerable(keySelector, comparer, descending: true);
        }

        private abstract partial class OrderedIterator<TElement> : IOrderedAsyncEnumerable<TElement>
        {
            internal readonly IAsyncEnumerable<TElement> _source;

            protected OrderedIterator(IAsyncEnumerable<TElement> source) => _source = source;

            private protected ValueTask<int[]> CreateSortedMapAsync(TElement[] buffer, CancellationToken cancellationToken) =>
                GetEnumerableSorter().SortAsync(buffer, buffer.Length, cancellationToken);

            internal abstract EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement>? next = null);

            public IOrderedAsyncEnumerable<TElement> CreateOrderedAsyncEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey>? comparer, bool descending) =>
                new OrderedIterator<TElement, TKey>(_source, keySelector, comparer, @descending, this);

            public IOrderedAsyncEnumerable<TElement> CreateOrderedAsyncEnumerable<TKey>(Func<TElement, CancellationToken, ValueTask<TKey>> keySelector, IComparer<TKey>? comparer, bool descending) =>
                new OrderedIterator<TElement, TKey>(_source, keySelector, comparer, @descending, this);

            public abstract IAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken cancellationToken);
        }

        private sealed partial class OrderedIterator<TElement, TKey> : OrderedIterator<TElement>
        {
            private readonly OrderedIterator<TElement>? _parent;
            private readonly object _keySelector;
            private readonly IComparer<TKey> _comparer;
            private readonly bool _descending;

            internal OrderedIterator(IAsyncEnumerable<TElement> source, object keySelector, IComparer<TKey>? comparer, bool descending, OrderedIterator<TElement>? parent) :
                base(source)
            {
                Debug.Assert(keySelector is Func<TElement, TKey> or Func<TElement, CancellationToken, ValueTask<TKey>>);

                _parent = parent;
                _keySelector = keySelector;
                _comparer = comparer ?? Comparer<TKey>.Default;
                _descending = descending;
            }

            internal override EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement>? next)
            {
                // Special case the common use of string with default comparer. Comparer<string>.Default checks the
                // thread's Culture on each call which is an overhead which is not required, because we are about to
                // do a sort which remains on the current thread (and EnumerableSorter is not used afterwards).
                IComparer<TKey> comparer = _comparer;
                if (typeof(TKey) == typeof(string) && comparer == Comparer<string>.Default)
                {
                    comparer = (IComparer<TKey>)StringComparer.CurrentCulture;
                }

                EnumerableSorter<TElement> sorter = new EnumerableSorter<TElement, TKey>(_keySelector, comparer, _descending, next);
                if (_parent is not null)
                {
                    sorter = _parent.GetEnumerableSorter(sorter);
                }

                return sorter;
            }

            public override async IAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken cancellationToken)
            {
                TElement[] buffer = await _source.ToArrayAsync(cancellationToken).ConfigureAwait(false);
                if (buffer.Length > 0)
                {
                    int[] map = await CreateSortedMapAsync(buffer, cancellationToken).ConfigureAwait(false);
                    for (int i = 0; i < map.Length; i++)
                    {
                        yield return buffer[map[i]];
                    }
                }
            }
        }

        private abstract class EnumerableSorter<TElement> : IComparer<int>
        {
            /// <summary>Function that returns its input unmodified.</summary>
            /// <remarks>
            /// Used for reference equality in order to avoid unnecessary computation when a caller
            /// can benefit from knowing that the produced value is identical to the input.
            /// </remarks>
            internal static readonly Func<TElement, TElement> IdentityFunc = e => e;

            internal abstract Task ComputeKeysAsync(TElement[] elements, int count, CancellationToken cancellationToken);

            public abstract int Compare(int index1, int index2);

            internal async ValueTask<int[]> SortAsync(TElement[] elements, int count, CancellationToken cancellationToken)
            {
                await ComputeKeysAsync(elements, count, cancellationToken).ConfigureAwait(false);

                int[] map = new int[count];
                for (int i = 0; i < map.Length; i++)
                {
                    map[i] = i;
                }

                QuickSort(map, 0, count - 1);

                return map;
            }

            protected abstract void QuickSort(int[] map, int left, int right);
        }

        private sealed class EnumerableSorter<TElement, TKey> : EnumerableSorter<TElement>, IComparer<int>
        {
            private readonly object _keySelector;
            private readonly IComparer<TKey> _comparer;
            private readonly bool _descending;
            private readonly EnumerableSorter<TElement>? _next;
            private TKey[]? _keys;

            internal EnumerableSorter(object keySelector, IComparer<TKey> comparer, bool descending, EnumerableSorter<TElement>? next)
            {
                _keySelector = keySelector;
                _comparer = comparer;
                _descending = descending;
                _next = next;
            }

            internal override async Task ComputeKeysAsync(TElement[] elements, int count, CancellationToken cancellationToken)
            {
                object keySelector = _keySelector;
                if (ReferenceEquals(keySelector, IdentityFunc))
                {
                    // The key selector is our known identity function, which means we don't
                    // need to invoke the key selector for every element.  Further, we can just
                    // use the original array as the keys (even if count is smaller, as the additional
                    // values will just be ignored).
                    Debug.Assert(typeof(TKey) == typeof(TElement));
                    _keys = (TKey[])(object)elements;
                }
                else
                {
                    var keys = new TKey[count];
                    if (keySelector is Func<TElement, TKey> syncSelector)
                    {
                        for (int i = 0; i < keys.Length; i++)
                        {
                            keys[i] = syncSelector(elements[i]);
                        }
                    }
                    else
                    {
                        var asyncSelector = (Func<TElement, CancellationToken, ValueTask<TKey>>)keySelector;
                        for (int i = 0; i < keys.Length; i++)
                        {
                            keys[i] = await asyncSelector(elements[i], cancellationToken).ConfigureAwait(false);
                        }
                    }
                    _keys = keys;
                }

                _next?.ComputeKeysAsync(elements, count, cancellationToken);
            }

            public override int Compare(int index1, int index2)
            {
                TKey[]? keys = _keys;
                Debug.Assert(keys is not null);

                int c = _comparer.Compare(keys[index1], keys[index2]);
                if (c == 0)
                {
                    if (_next is null)
                    {
                        return index1 - index2; // ensure stability of sort
                    }

                    return _next.Compare(index1, index2);
                }

                // -c will result in a negative value for int.MinValue (-int.MinValue == int.MinValue).
                // Flipping keys earlier is more likely to trigger something strange in a comparer,
                // particularly as it comes to the sort being stable.
                return (_descending != (c > 0)) ? 1 : -1;
            }

            protected override void QuickSort(int[] keys, int lo, int hi)
            {
#if NET
                if (typeof(TKey).IsValueType && _next is null && _comparer == Comparer<TKey>.Default)
                {
                    // We can use Comparer<TKey>.Default.Compare and benefit from devirtualization and inlining.
                    // We can also avoid extra steps to check whether we need to deal with a subsequent tie breaker (_next).
                    new Span<int>(keys, lo, hi - lo + 1).Sort(!_descending ?
                        Compare_DefaultComparer_NoNext_Ascending :
                        Compare_DefaultComparer_NoNext_Descending);

                    int Compare_DefaultComparer_NoNext_Ascending(int index1, int index2)
                    {
                        Debug.Assert(typeof(TKey).IsValueType);
                        Debug.Assert(_comparer == Comparer<TKey>.Default);
                        Debug.Assert(_next is null);
                        Debug.Assert(!_descending);

                        TKey[]? keys = _keys;
                        Debug.Assert(keys is not null);

                        int c = Comparer<TKey>.Default.Compare(keys[index1], keys[index2]);
                        return
                            c == 0 ? index1 - index2 : // ensure stability of sort
                            c;
                    }

                    int Compare_DefaultComparer_NoNext_Descending(int index1, int index2)
                    {
                        Debug.Assert(typeof(TKey).IsValueType);
                        Debug.Assert(_comparer == Comparer<TKey>.Default);
                        Debug.Assert(_next is null);
                        Debug.Assert(_descending);

                        TKey[]? keys = _keys;
                        Debug.Assert(keys is not null);

                        int c = Comparer<TKey>.Default.Compare(keys[index2], keys[index1]);
                        return
                            c == 0 ? index1 - index2 : // ensure stability of sort
                            c;
                    }
                }
                else
#endif
                {
#if NET
                    new Span<int>(keys, lo, hi - lo + 1).Sort(Compare);
#else
                    Array.Sort(keys, lo, hi - lo + 1, this);
#endif
                }
            }
        }
    }

    /// <summary>Represents a sorted asynchronous sequence.</summary>
    /// <typeparam name="TElement">The type of the elements of the sequence.</typeparam>
    /// <remarks>This interface is not intended to be implemented by user code. It supports the .NET infrastructure.</remarks>
    public interface IOrderedAsyncEnumerable<out TElement> : IAsyncEnumerable<TElement>
    {
        /// <summary>Performs a subsequent ordering on the elements of an <see cref="IOrderedAsyncEnumerable{TElement}"/> according to a key.</summary>
        /// <typeparam name="TKey">The type of the key produced by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> used to compare keys for placement in the returned sequence.</param>
        /// <param name="descending">true to sort the elements in descending order; false to sort the elements in ascending order.</param>
        /// <returns>An <see cref="IOrderedAsyncEnumerable{TElement}"/> whose elements are sorted according to a key.</returns>
        IOrderedAsyncEnumerable<TElement> CreateOrderedAsyncEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey>? comparer, bool descending);

        /// <summary>Performs a subsequent ordering on the elements of an <see cref="IOrderedAsyncEnumerable{TElement}"/> according to a key.</summary>
        /// <typeparam name="TKey">The type of the key produced by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">The function used to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> used to compare keys for placement in the returned sequence.</param>
        /// <param name="descending">true to sort the elements in descending order; false to sort the elements in ascending order.</param>
        /// <returns>An <see cref="IOrderedAsyncEnumerable{TElement}"/> whose elements are sorted according to a key.</returns>
        IOrderedAsyncEnumerable<TElement> CreateOrderedAsyncEnumerable<TKey>(Func<TElement, CancellationToken, ValueTask<TKey>> keySelector, IComparer<TKey>? comparer, bool descending);
    }
}
