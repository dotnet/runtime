// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using static System.Linq.Utilities;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Produces the set union of two sequences by using the default equality comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{T}" /> whose distinct elements form the first set for the union.</param>
        /// <param name="second">An <see cref="IEnumerable{T}" /> whose distinct elements form the second set for the union.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>This method excludes duplicates from the return set. This is different behavior to the <see cref="O:Enumerable.Concat" /> method, which returns all the elements in the input sequences including duplicates.</para>
        /// <para>The default equality comparer, <see cref="O:EqualityComparer{T}.Default" />, is used to compare values of the types that implement the <see cref="IEqualityComparer{T}" /> generic interface. To compare a custom data type, you need to implement this interface and provide your own <see cref="O:object.GetHashCode" /> and <see cref="O:object.Equals" /> methods for the type.</para>
        /// <para>When the object returned by this method is enumerated, `Union` enumerates <paramref name="first" /> and <paramref name="second" /> in that order and yields each element that has not already been yielded.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Union{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/> to obtain the union of two sequences of integers.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet109":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet109":::
        /// If you want to compare sequences of objects of some custom data type, you have to implement the <see cref="System.IEquatable{T}" /> generic interface in a helper class. The following code example shows how to implement this interface in a custom data type and override <see cref="O:object.GetHashCode" /> and <see cref="O:object.Equals" /> methods.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/CS/EncapsulatedComparer.cs" id="Snippet9":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/VB/EncapsulatedComparer.vb" id="Snippet9":::
        /// After you implement this interface, you can use sequences of `ProductA` objects in the <see cref="Union{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/> method, as shown in the following example:
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/CS/EncapsulatedComparer.cs" id="Snippet10":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/VB/EncapsulatedComparer.vb" id="Snippet10":::
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/CS/EncapsulatedComparer.cs" id="Snippet4":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/VB/EncapsulatedComparer.vb" id="Snippet4":::</example>
        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second) => Union(first, second, comparer: null);

        /// <summary>Produces the set union of two sequences by using a specified <see cref="IEqualityComparer{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{T}" /> whose distinct elements form the first set for the union.</param>
        /// <param name="second">An <see cref="IEnumerable{T}" /> whose distinct elements form the second set for the union.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="O:EqualityComparer{T}.Default" />, is used to compare values.</para>
        /// <para>When the object returned by this method is enumerated, <see cref="O:Enumerable.Union" /> enumerates <paramref name="first" /> and <paramref name="second" /> in that order and yields each element that has not already been yielded.</para>
        /// <para>The <see cref="O:Enumerable.Concat" /> method differs from the <see cref="O:Enumerable.Union" /> method because the <see cref="O:Enumerable.Concat" /> method returns all the elements in the input sequences including duplicates, whereas <see cref="O:Enumerable.Union" /> returns only unique values.</para>
        /// </remarks>
        /// <example>The following example shows how to implement an equality comparer that can be used in the <see cref="O:Enumerable.Union" /> method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet1":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet1":::
        /// After you implement this comparer, you can use sequences of `Product` objects in the <see cref="O:Enumerable.Union" /> method, as shown in the following example:
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet2":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet2":::
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet4":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet4":::</example>
        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            if (first == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            return first is UnionIterator<TSource> union && AreEqualityComparersEqual(comparer, union._comparer) ? union.Union(second) : new UnionIterator2<TSource>(first, second, comparer);
        }

        /// <summary>Produces the set union of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{T}" /> whose distinct elements form the first set for the union.</param>
        /// <param name="second">An <see cref="IEnumerable{T}" /> whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to compare values.</para>
        /// <para>When the object returned by this method is enumerated, <see cref="O:Enumerable.UnionBy" /> enumerates <paramref name="first" /> and <paramref name="second" /> in that order and yields each element that has not already been yielded.</para>
        /// </remarks>
        public static IEnumerable<TSource> UnionBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector) => UnionBy(first, second, keySelector, null);

        /// <summary>Produces the set union of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{T}" /> whose distinct elements form the first set for the union.</param>
        /// <param name="second">An <see cref="IEnumerable{T}" /> whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to compare values.</para>
        /// <para>When the object returned by this method is enumerated, <see cref="O:Enumerable.UnionBy" /> enumerates <paramref name="first" /> and <paramref name="second" /> in that order and yields each element that has not already been yielded.</para>
        /// </remarks>
        public static IEnumerable<TSource> UnionBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (first is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }
            if (second is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            return UnionByIterator(first, second, keySelector, comparer);
        }

        private static IEnumerable<TSource> UnionByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            var set = new HashSet<TKey>(DefaultInternalSetCapacity, comparer);

            foreach (TSource element in first)
            {
                if (set.Add(keySelector(element)))
                {
                    yield return element;
                }
            }

            foreach (TSource element in second)
            {
                if (set.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        /// <summary>
        /// An iterator that yields distinct values from two or more <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private abstract partial class UnionIterator<TSource> : Iterator<TSource>
        {
            internal readonly IEqualityComparer<TSource>? _comparer;
            private IEnumerator<TSource>? _enumerator;
            private HashSet<TSource>? _set;

            protected UnionIterator(IEqualityComparer<TSource>? comparer)
            {
                _comparer = comparer;
            }

            public sealed override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                    _set = null;
                }

                base.Dispose();
            }

            internal abstract IEnumerable<TSource>? GetEnumerable(int index);

            internal abstract UnionIterator<TSource> Union(IEnumerable<TSource> next);

            private void SetEnumerator(IEnumerator<TSource> enumerator)
            {
                _enumerator?.Dispose();

                _enumerator = enumerator;
            }

            private void StoreFirst()
            {
                Debug.Assert(_enumerator != null);

                var set = new HashSet<TSource>(DefaultInternalSetCapacity, _comparer);
                TSource element = _enumerator.Current;
                set.Add(element);
                _current = element;
                _set = set;
            }

            private bool GetNext()
            {
                Debug.Assert(_enumerator != null);
                Debug.Assert(_set != null);

                HashSet<TSource> set = _set;

                while (_enumerator.MoveNext())
                {
                    TSource element = _enumerator.Current;
                    if (set.Add(element))
                    {
                        _current = element;
                        return true;
                    }
                }

                return false;
            }

            public sealed override bool MoveNext()
            {
                if (_state == 1)
                {
                    for (IEnumerable<TSource>? enumerable = GetEnumerable(0); enumerable != null; enumerable = GetEnumerable(_state - 1))
                    {
                        IEnumerator<TSource> enumerator = enumerable.GetEnumerator();
                        SetEnumerator(enumerator);

                        ++_state;
                        if (enumerator.MoveNext())
                        {
                            StoreFirst();
                            return true;
                        }
                    }
                }
                else if (_state > 0)
                {
                    while (true)
                    {
                        if (GetNext())
                        {
                            return true;
                        }

                        IEnumerable<TSource>? enumerable = GetEnumerable(_state - 1);
                        if (enumerable == null)
                        {
                            break;
                        }

                        SetEnumerator(enumerable.GetEnumerator());
                        ++_state;
                    }
                }

                Dispose();
                return false;
            }
        }

        /// <summary>
        /// An iterator that yields distinct values from two <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private sealed class UnionIterator2<TSource> : UnionIterator<TSource>
        {
            private readonly IEnumerable<TSource> _first;
            private readonly IEnumerable<TSource> _second;

            public UnionIterator2(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
                : base(comparer)
            {
                Debug.Assert(first != null);
                Debug.Assert(second != null);
                _first = first;
                _second = second;
            }

            public override Iterator<TSource> Clone() => new UnionIterator2<TSource>(_first, _second, _comparer);

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

            internal override UnionIterator<TSource> Union(IEnumerable<TSource> next)
            {
                var sources = new SingleLinkedNode<IEnumerable<TSource>>(_first).Add(_second).Add(next);
                return new UnionIteratorN<TSource>(sources, 2, _comparer);
            }
        }

        /// <summary>
        /// An iterator that yields distinct values from three or more <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private sealed class UnionIteratorN<TSource> : UnionIterator<TSource>
        {
            private readonly SingleLinkedNode<IEnumerable<TSource>> _sources;
            private readonly int _headIndex;

            public UnionIteratorN(SingleLinkedNode<IEnumerable<TSource>> sources, int headIndex, IEqualityComparer<TSource>? comparer)
                : base(comparer)
            {
                Debug.Assert(headIndex >= 2);
                Debug.Assert(sources?.GetCount() == headIndex + 1);

                _sources = sources;
                _headIndex = headIndex;
            }

            public override Iterator<TSource> Clone() => new UnionIteratorN<TSource>(_sources, _headIndex, _comparer);

            internal override IEnumerable<TSource>? GetEnumerable(int index) => index > _headIndex ? null : _sources.GetNode(_headIndex - index).Item;

            internal override UnionIterator<TSource> Union(IEnumerable<TSource> next)
            {
                if (_headIndex == int.MaxValue - 2)
                {
                    // In the unlikely case of this many unions, if we produced a UnionIteratorN
                    // with int.MaxValue then state would overflow before it matched it's index.
                    // So we use the naive approach of just having a left and right sequence.
                    return new UnionIterator2<TSource>(this, next, _comparer);
                }

                return new UnionIteratorN<TSource>(_sources.Add(next), _headIndex + 1, _comparer);
            }
        }
    }
}
