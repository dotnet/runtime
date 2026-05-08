// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Concatenates two sequences.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the concatenated elements of the two input sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TSource> Concat<TSource>(
            this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);

            if (first.IsKnownEmpty())
            {
                return second;
            }

            if (second.IsKnownEmpty())
            {
                return first;
            }

            return first is ConcatAsyncIterator<TSource> firstConcat
                ? firstConcat.Concat(second)
                : new Concat2AsyncIterator<TSource>(first, second);
        }

        /// <summary>
        /// Represents the concatenation of two <see cref="IAsyncEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private sealed class Concat2AsyncIterator<TSource> : ConcatAsyncIterator<TSource>
        {
            /// <summary>
            /// The first source to concatenate.
            /// </summary>
            internal readonly IAsyncEnumerable<TSource> _first;

            /// <summary>
            /// The second source to concatenate.
            /// </summary>
            internal readonly IAsyncEnumerable<TSource> _second;

            /// <summary>
            /// Initializes a new instance of the <see cref="Concat2AsyncIterator{TSource}"/> class.
            /// </summary>
            /// <param name="first">The first source to concatenate.</param>
            /// <param name="second">The second source to concatenate.</param>
            internal Concat2AsyncIterator(IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second)
            {
                Debug.Assert(first is not null);
                Debug.Assert(second is not null);

                _first = first;
                _second = second;
            }

            private protected override AsyncIterator<TSource> Clone() => new Concat2AsyncIterator<TSource>(_first, _second);

            internal override ConcatAsyncIterator<TSource> Concat(IAsyncEnumerable<TSource> next)
            {
                return new ConcatNAsyncIterator<TSource>(this, next, 2);
            }

            internal override IAsyncEnumerable<TSource>? GetAsyncEnumerable(int index)
            {
                Debug.Assert(index >= 0);

                return index switch
                {
                    0 => _first,
                    1 => _second,
                    _ => null,
                };
            }
        }

        /// <summary>
        /// Represents the concatenation of three or more <see cref="IAsyncEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        /// <remarks>
        /// To handle chains of >= 3 sources, we chain the <see cref="Concat"/> iterators together and allow
        /// <see cref="GetAsyncEnumerable"/> to fetch enumerables from the previous sources.  This means that rather
        /// than each MoveNextAsync and Current calls having to traverse all of the previous
        /// sources, we only have to traverse all of the previous sources once per chained enumerable.  An alternative
        /// would be to use an array to store all of the enumerables, but this has a much better memory profile and
        /// without much additional run-time cost.
        /// </remarks>
        private sealed class ConcatNAsyncIterator<TSource> : ConcatAsyncIterator<TSource>
        {
            /// <summary>
            /// The linked list of previous sources.
            /// </summary>
            private readonly ConcatAsyncIterator<TSource> _tail;

            /// <summary>
            /// The source associated with this iterator.
            /// </summary>
            private readonly IAsyncEnumerable<TSource> _head;

            /// <summary>
            /// The logical index associated with this iterator.
            /// </summary>
            private readonly int _headIndex;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConcatNAsyncIterator{TSource}"/> class.
            /// </summary>
            /// <param name="tail">The linked list of previous sources.</param>
            /// <param name="head">The source associated with this iterator.</param>
            /// <param name="headIndex">The logical index associated with this iterator.</param>
            internal ConcatNAsyncIterator(ConcatAsyncIterator<TSource> tail, IAsyncEnumerable<TSource> head, int headIndex)
            {
                Debug.Assert(tail is not null);
                Debug.Assert(head is not null);
                Debug.Assert(headIndex >= 2);

                _tail = tail;
                _head = head;
                _headIndex = headIndex;
            }

            private ConcatNAsyncIterator<TSource>? PreviousN => _tail as ConcatNAsyncIterator<TSource>;

            private protected override AsyncIterator<TSource> Clone() => new ConcatNAsyncIterator<TSource>(_tail, _head, _headIndex);

            internal override ConcatAsyncIterator<TSource> Concat(IAsyncEnumerable<TSource> next)
            {
                if (_headIndex == int.MaxValue - 2)
                {
                    // In the unlikely case of this many concatenations, if we produced a ConcatNAsyncIterator
                    // with int.MaxValue then state would overflow before it matched its index.
                    // So we use the naive approach of just having a left and right sequence.
                    return new Concat2AsyncIterator<TSource>(this, next);
                }

                return new ConcatNAsyncIterator<TSource>(this, next, _headIndex + 1);
            }

            internal override IAsyncEnumerable<TSource>? GetAsyncEnumerable(int index)
            {
                Debug.Assert(index >= 0);

                if (index > _headIndex)
                {
                    return null;
                }

                ConcatNAsyncIterator<TSource>? node, previousN = this;
                do
                {
                    node = previousN;
                    if (index == node._headIndex)
                    {
                        return node._head;
                    }
                }
                while ((previousN = node.PreviousN) is not null);

                Debug.Assert(index == 0 || index == 1);
                Debug.Assert(node._tail is Concat2AsyncIterator<TSource>);
                return node._tail.GetAsyncEnumerable(index);
            }
        }

        /// <summary>
        /// Represents the concatenation of two or more <see cref="IAsyncEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private abstract class ConcatAsyncIterator<TSource> : AsyncIterator<TSource>
        {
            /// <summary>
            /// The enumerator of the current source, if <see cref="MoveNextAsync"/> has been called.
            /// </summary>
            private IAsyncEnumerator<TSource>? _enumerator;

            public override async ValueTask DisposeAsync()
            {
                if (_enumerator is not null)
                {
                    await _enumerator.DisposeAsync().ConfigureAwait(false);
                    _enumerator = null;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            /// <summary>
            /// Gets the enumerable at a logical index in this iterator.
            /// If the index is equal to the number of enumerables this iterator holds, <c>null</c> is returned.
            /// </summary>
            /// <param name="index">The logical index.</param>
            internal abstract IAsyncEnumerable<TSource>? GetAsyncEnumerable(int index);

            /// <summary>
            /// Creates a new iterator that concatenates this iterator with an enumerable.
            /// </summary>
            /// <param name="next">The next enumerable.</param>
            internal abstract ConcatAsyncIterator<TSource> Concat(IAsyncEnumerable<TSource> next);

            public override async ValueTask<bool> MoveNextAsync()
            {
                if (_state == 1)
                {
                    _enumerator = GetAsyncEnumerable(0)!.GetAsyncEnumerator(_cancellationToken);
                    _state = 2;
                }

                if (_state > 1)
                {
                    while (true)
                    {
                        Debug.Assert(_enumerator is not null);
                        if (await _enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            _current = _enumerator.Current;
                            return true;
                        }

                        IAsyncEnumerable<TSource>? next = GetAsyncEnumerable(_state++ - 1);
                        if (next is not null)
                        {
                            await _enumerator.DisposeAsync().ConfigureAwait(false);
                            _enumerator = next.GetAsyncEnumerator(_cancellationToken);
                            continue;
                        }

                        await DisposeAsync().ConfigureAwait(false);
                        break;
                    }
                }

                return false;
            }
        }
    }
}
