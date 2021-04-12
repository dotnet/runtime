// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    /// <summary>Provides a set of <see langword="static" /> (<see langword="Shared" /> in Visual Basic) methods for querying objects that implement <see cref="System.Collections.Generic.IEnumerable{T}" />.</summary>
    /// <remarks>The methods in this class provide an implementation of the standard query operators for querying data sources that implement <see cref="System.Collections.Generic.IEnumerable{T}" />. The standard query operators are general purpose methods that follow the LINQ pattern and enable you to express traversal, filter, and projection operations over data in any .NET-based programming language.
    /// The majority of the methods in this class are defined as extension methods that extend <see cref="System.Collections.Generic.IEnumerable{T}" />. This means they can be called like an instance method on any object that implements <see cref="System.Collections.Generic.IEnumerable{T}" />.
    /// Methods that are used in a query that returns a sequence of values do not consume the target data until the query object is enumerated. This is known as deferred execution. Methods that are used in a query that returns a singleton value execute and consume the target data immediately.</remarks>
    /// <related type="Article" href="https://msdn.microsoft.com/library/24cda21e-8af8-4632-b519-c404a839b9b2">Standard Query Operators Overview</related>
    /// <related type="Article" href="/dotnet/csharp/programming-guide/classes-and-structs/extension-methods">Extension Methods (C# Programming Guide)</related>
    /// <related type="Article" href="/dotnet/visual-basic/programming-guide/language-features/procedures/extension-methods">Extension Methods (Visual Basic)</related>
    public static partial class Enumerable
    {
        /// <summary>Concatenates two sequences.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains the concatenated elements of the two input sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The <see cref="System.Linq.Enumerable.Concat{T}(System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEnumerable{T})" /> method differs from the <see cref="O:System.Linq.Enumerable.Union" /> method because the <see cref="System.Linq.Enumerable.Concat{T}(System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEnumerable{T})" /> method returns all the original elements in the input sequences. The <see cref="O:System.Linq.Enumerable.Union" /> method returns only unique elements.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.Concat{T}(System.Collections.Generic.IEnumerable{T},System.Collections.Generic.IEnumerable{T})" /> to concatenate two sequences.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet20":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet20":::
        /// An alternative way of concatenating two sequences is to construct a collection, for example an array, of sequences and then apply the <see cref="O:System.Linq.Enumerable.SelectMany" /> method, passing it the identity selector function. The following example demonstrates this use of <see cref="O:System.Linq.Enumerable.SelectMany" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet112":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet112":::</example>
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
                : new Concat2Iterator<TSource>(first, second);
        }

        /// <summary>
        /// Represents the concatenation of two <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private sealed partial class Concat2Iterator<TSource> : ConcatIterator<TSource>
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
            /// Initializes a new instance of the <see cref="Concat2Iterator{TSource}"/> class.
            /// </summary>
            /// <param name="first">The first source to concatenate.</param>
            /// <param name="second">The second source to concatenate.</param>
            internal Concat2Iterator(IEnumerable<TSource> first, IEnumerable<TSource> second)
            {
                Debug.Assert(first != null);
                Debug.Assert(second != null);

                _first = first;
                _second = second;
            }

            public override Iterator<TSource> Clone() => new Concat2Iterator<TSource>(_first, _second);

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                bool hasOnlyCollections = next is ICollection<TSource> &&
                                          _first is ICollection<TSource> &&
                                          _second is ICollection<TSource>;
                return new ConcatNIterator<TSource>(this, next, 2, hasOnlyCollections);
            }

            internal override IEnumerable<TSource>? GetEnumerable(int index)
            {
                Debug.Assert(index >= 0 && index <= 2);

                return index switch
                {
                    0 => _first,
                    1 => _second,
                    _ => null,
                };
            }
        }

        /// <summary>
        /// Represents the concatenation of three or more <see cref="IEnumerable{TSource}"/>.
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
        private sealed partial class ConcatNIterator<TSource> : ConcatIterator<TSource>
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
            /// Initializes a new instance of the <see cref="ConcatNIterator{TSource}"/> class.
            /// </summary>
            /// <param name="tail">The linked list of previous sources.</param>
            /// <param name="head">The source associated with this iterator.</param>
            /// <param name="headIndex">The logical index associated with this iterator.</param>
            /// <param name="hasOnlyCollections">
            /// <c>true</c> if all sources this iterator concatenates implement <see cref="ICollection{TSource}"/>;
            /// otherwise, <c>false</c>.
            /// </param>
            internal ConcatNIterator(ConcatIterator<TSource> tail, IEnumerable<TSource> head, int headIndex, bool hasOnlyCollections)
            {
                Debug.Assert(tail != null);
                Debug.Assert(head != null);
                Debug.Assert(headIndex >= 2);

                _tail = tail;
                _head = head;
                _headIndex = headIndex;
                _hasOnlyCollections = hasOnlyCollections;
            }

            private ConcatNIterator<TSource>? PreviousN => _tail as ConcatNIterator<TSource>;

            public override Iterator<TSource> Clone() => new ConcatNIterator<TSource>(_tail, _head, _headIndex, _hasOnlyCollections);

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                if (_headIndex == int.MaxValue - 2)
                {
                    // In the unlikely case of this many concatenations, if we produced a ConcatNIterator
                    // with int.MaxValue then state would overflow before it matched its index.
                    // So we use the naive approach of just having a left and right sequence.
                    return new Concat2Iterator<TSource>(this, next);
                }

                bool hasOnlyCollections = _hasOnlyCollections && next is ICollection<TSource>;
                return new ConcatNIterator<TSource>(this, next, _headIndex + 1, hasOnlyCollections);
            }

            internal override IEnumerable<TSource>? GetEnumerable(int index)
            {
                Debug.Assert(index >= 0);

                if (index > _headIndex)
                {
                    return null;
                }

                ConcatNIterator<TSource>? node, previousN = this;
                do
                {
                    node = previousN;
                    if (index == node._headIndex)
                    {
                        return node._head;
                    }
                }
                while ((previousN = node.PreviousN) != null);

                Debug.Assert(index == 0 || index == 1);
                Debug.Assert(node._tail is Concat2Iterator<TSource>);
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
