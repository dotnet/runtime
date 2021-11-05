// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            return first is ConcatIterator<TSource> firstConcat
                ? firstConcat.Concat(second)
                : new BaseConcatIterator<TSource>(first, second, rest: null);
        }

        /// <summary>
        /// Concatenates two or more sequences.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        /// <param name="rest">Specifies any subsequent sequences to concatenate.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that contains the concatenated elements of the two input sequences.</returns>
        public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, params IEnumerable<TSource>[] rest)
        {
            if (first == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            if (rest == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.rest);
            }

            return new BaseConcatIterator<TSource>(first, second, rest);
        }

        /// <summary>
        /// Represents the concatenation of two or more <see cref="IEnumerable{TSource}"/> in a single concat operation.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private sealed partial class BaseConcatIterator<TSource> : ConcatIterator<TSource>
        {
            /// <summary>
            /// The first source to concatenate.
            /// </summary>
            internal readonly IEnumerable<TSource> _first;

            /// <summary>
            /// The second source to concatenate.
            /// </summary>
            internal readonly IEnumerable<TSource> _second;

            /// <summary>
            /// Any remaining enumerables to concatenate.
            /// </summary>
            internal readonly IEnumerable<TSource>[] _rest;

            /// <summary>
            /// Initializes a new instance of the <see cref="BaseConcatIterator{TSource}"/> class.
            /// </summary>
            /// <param name="first">The first source to concatenate.</param>
            /// <param name="second">The second source to concatenate.</param>
            /// <param name="rest">Any remaining sources to concatenate.</param>
            internal BaseConcatIterator(IEnumerable<TSource> first, IEnumerable<TSource> second, IEnumerable<TSource>[]? rest)
            {
                Debug.Assert(first != null);
                Debug.Assert(second != null);

                _first = first;
                _second = second;
                _rest = rest ?? Array.Empty<IEnumerable<TSource>>();
            }

            public override Iterator<TSource> Clone() => new BaseConcatIterator<TSource>(_first, _second, _rest);

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                bool hasOnlyCollections = next is ICollection<TSource> && HasOnlyCollections;
                return new ChainedConcatIterator<TSource>(this, next, 2 + _rest.Length, hasOnlyCollections);
            }

            internal override IEnumerable<TSource>? GetEnumerable(int index)
            {
                Debug.Assert(index >= 0 && index <= 2 + _rest.Length);

                return index switch
                {
                    0 => _first,
                    1 => _second,
                    _ => index < 2 + _rest.Length ? _rest[index - 2] : null,
                };
            }

            private bool HasOnlyCollections
            {
                get
                {
                    if (_first is not ICollection<TSource> || _second is not ICollection<TSource>)
                    {
                        return false;
                    }

                    foreach (IEnumerable<TSource> segment in _rest)
                    {
                        if (segment is not ICollection<TSource>)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
        }

        /// <summary>
        /// Represents the two or more <see cref="IEnumerable{TSource}"/> chained concat operations.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        /// <remarks>
        /// To handle chains of >= 3 sources, we chain the <see cref="Concat"/> iterators together and allow
        /// <see cref="GetEnumerable"/> to fetch enumerables from the previous sources.  This means that rather
        /// than each <see cref="System.Collections.IEnumerator.MoveNext"/> and <see cref="IEnumerator{T}.Current"/> calls having to traverse all of the previous
        /// sources, we only have to traverse all of the previous sources once per chained enumerable.  An alternative
        /// would be to use an array to store all of the enumerables, but this has a much better memory profile and
        /// without much additional run-time cost.
        /// </remarks>
        private sealed partial class ChainedConcatIterator<TSource> : ConcatIterator<TSource>
        {
            /// <summary>
            /// The linked list of previous sources.
            /// </summary>
            private readonly ConcatIterator<TSource> _tail;

            /// <summary>
            /// The source associated with this iterator.
            /// </summary>
            private readonly IEnumerable<TSource> _head;

            /// <summary>
            /// The logical index associated with this iterator.
            /// </summary>
            private readonly int _headIndex;

            /// <summary>
            /// <c>true</c> if all sources this iterator concatenates implement <see cref="ICollection{TSource}"/>;
            /// otherwise, <c>false</c>.
            /// </summary>
            /// <remarks>
            /// This flag allows us to determine in O(1) time whether we can preallocate for ToArray/ToList,
            /// and whether we can get the count of the iterator cheaply.
            /// </remarks>
            private readonly bool _hasOnlyCollections;

            /// <summary>
            /// Initializes a new instance of the <see cref="ChainedConcatIterator{TSource}"/> class.
            /// </summary>
            /// <param name="tail">The linked list of previous sources.</param>
            /// <param name="head">The source associated with this iterator.</param>
            /// <param name="headIndex">The logical index associated with this iterator.</param>
            /// <param name="hasOnlyCollections">
            /// <c>true</c> if all sources this iterator concatenates implement <see cref="ICollection{TSource}"/>;
            /// otherwise, <c>false</c>.
            /// </param>
            internal ChainedConcatIterator(ConcatIterator<TSource> tail, IEnumerable<TSource> head, int headIndex, bool hasOnlyCollections)
            {
                Debug.Assert(tail != null);
                Debug.Assert(head != null);
                Debug.Assert(headIndex >= 2);

                _tail = tail;
                _head = head;
                _headIndex = headIndex;
                _hasOnlyCollections = hasOnlyCollections;
            }

            private ChainedConcatIterator<TSource>? PreviousN => _tail as ChainedConcatIterator<TSource>;

            public override Iterator<TSource> Clone() => new ChainedConcatIterator<TSource>(_tail, _head, _headIndex, _hasOnlyCollections);

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                if (_headIndex == int.MaxValue - 2)
                {
                    // In the unlikely case of this many concatenations, if we produced a ConcatNIterator
                    // with int.MaxValue then state would overflow before it matched its index.
                    // So we use the naive approach of just having a left and right sequence.
                    return new BaseConcatIterator<TSource>(this, next, rest: null);
                }

                bool hasOnlyCollections = _hasOnlyCollections && next is ICollection<TSource>;
                return new ChainedConcatIterator<TSource>(this, next, _headIndex + 1, hasOnlyCollections);
            }

            internal override IEnumerable<TSource>? GetEnumerable(int index)
            {
                Debug.Assert(index >= 0);

                if (index > _headIndex)
                {
                    return null;
                }

                ChainedConcatIterator<TSource>? node, previousN = this;
                do
                {
                    node = previousN;
                    if (index == node._headIndex)
                    {
                        return node._head;
                    }
                }
                while ((previousN = node.PreviousN) != null);

                Debug.Assert(node._tail is BaseConcatIterator<TSource>);
                return node._tail.GetEnumerable(index);
            }
        }

        /// <summary>
        /// Represents the concatenation of two or more <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private abstract partial class ConcatIterator<TSource> : Iterator<TSource>
        {
            /// <summary>
            /// The enumerator of the current source, if <see cref="MoveNext"/> has been called.
            /// </summary>
            private IEnumerator<TSource>? _enumerator;

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            /// <summary>
            /// Gets the enumerable at a logical index in this iterator.
            /// If the index is equal to the number of enumerables this iterator holds, <c>null</c> is returned.
            /// </summary>
            /// <param name="index">The logical index.</param>
            internal abstract IEnumerable<TSource>? GetEnumerable(int index);

            /// <summary>
            /// Creates a new iterator that concatenates this iterator with an enumerable.
            /// </summary>
            /// <param name="next">The next enumerable.</param>
            internal abstract ConcatIterator<TSource> Concat(IEnumerable<TSource> next);

            public override bool MoveNext()
            {
                if (_state == 1)
                {
                    _enumerator = GetEnumerable(0)!.GetEnumerator();
                    _state = 2;
                }

                if (_state > 1)
                {
                    while (true)
                    {
                        Debug.Assert(_enumerator != null);
                        if (_enumerator.MoveNext())
                        {
                            _current = _enumerator.Current;
                            return true;
                        }

                        IEnumerable<TSource>? next = GetEnumerable(_state++ - 1);
                        if (next != null)
                        {
                            _enumerator.Dispose();
                            _enumerator = next.GetEnumerator();
                            continue;
                        }

                        Dispose();
                        break;
                    }
                }

                return false;
            }
        }
    }
}
