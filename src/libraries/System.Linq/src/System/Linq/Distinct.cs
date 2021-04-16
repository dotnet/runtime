// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    /// <summary>Provides a set of <see langword="static" /> (<see langword="Shared" /> in Visual Basic) methods for querying objects that implement <see cref="IEnumerable{T}" />.</summary>
    /// <remarks>The methods in this class provide an implementation of the standard query operators for querying data sources that implement <see cref="IEnumerable{T}" />. The standard query operators are general purpose methods that follow the LINQ pattern and enable you to express traversal, filter, and projection operations over data in any .NET-based programming language.
    /// The majority of the methods in this class are defined as extension methods that extend <see cref="IEnumerable{T}" />. This means they can be called like an instance method on any object that implements <see cref="IEnumerable{T}" />.
    /// Methods that are used in a query that returns a sequence of values do not consume the target data until the query object is enumerated. This is known as deferred execution. Methods that are used in a query that returns a singleton value execute and consume the target data immediately.</remarks>
    /// <related type="Article" href="https://msdn.microsoft.com/library/24cda21e-8af8-4632-b519-c404a839b9b2">Standard Query Operators Overview</related>
    /// <related type="Article" href="/dotnet/csharp/programming-guide/classes-and-structs/extension-methods">Extension Methods (C# Programming Guide)</related>
    /// <related type="Article" href="/dotnet/visual-basic/programming-guide/language-features/procedures/extension-methods">Extension Methods (Visual Basic)</related>
    public static partial class Enumerable
    {
        /// <summary>Returns distinct elements from a sequence by using the default equality comparer to compare values.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The <see cref="Distinct{T}(IEnumerable{T})" /> method returns an unordered sequence that contains no duplicate values. It uses the default equality comparer, <see cref="O:EqualityComparer{T}.Default" />, to compare values.
        /// In Visual Basic query expression syntax, a `Distinct` clause translates to an invocation of <see cref="O:Enumerable.Distinct" />.
        /// The default equality comparer, <see cref="O:EqualityComparer{T}.Default" />, is used to compare values of the types that implement the <see cref="System.IEquatable{T}" /> generic interface. To compare a custom data type, you need to implement this interface and provide your own <see cref="O:object.GetHashCode" /> and <see cref="O:object.Equals" /> methods for the type.
        /// For an example that uses <see cref="IEqualityComparer{T}" /> to define a custom comparer, see <see cref="Distinct{T}(IEnumerable{T},IEqualityComparer{T})" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="Distinct{T}(IEnumerable{T})" /> to return distinct elements from a sequence of integers.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet27":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet27":::
        /// If you want to return distinct elements from sequences of objects of some custom data type, you have to implement the <see cref="System.IEquatable{T}" /> generic interface in the class. The following code example shows how to implement this interface in a custom data type and provide <see cref="O:object.GetHashCode" /> and <see cref="O:object.Equals" /> methods.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/CS/EncapsulatedComparer.cs" id="Snippet1":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/VB/EncapsulatedComparer.vb" id="Snippet1":::
        /// After you implement this interface, you can use a sequence of `Product` objects in the <see cref="Distinct{T}(IEnumerable{T})" /> method, as shown in the following example:
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/CS/EncapsulatedComparer.cs" id="Snippet5":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/VB/EncapsulatedComparer.vb" id="Snippet5":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/distinct-clause">Distinct Clause (Visual Basic)</related>
        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source) => Distinct(source, null);

        /// <summary>Returns distinct elements from a sequence by using a specified <see cref="IEqualityComparer{T}" /> to compare values.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The <see cref="Distinct{T}(IEnumerable{T},IEqualityComparer{T})" /> method returns an unordered sequence that contains no duplicate values. If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="O:EqualityComparer{T}.Default" />, is used to compare values.</remarks>
        /// <example>The following example shows how to implement an equality comparer that can be used in the <see cref="O:Enumerable.Distinct" /> method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet1":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet1":::
        /// After you implement this comparer, you can use a sequence of `Product` objects in the <see cref="O:Enumerable.Distinct" /> method, as shown in the following example:
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet5":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet5":::</example>
        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return new DistinctIterator<TSource>(source, comparer);
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) => DistinctBy(source, keySelector, null);

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            return DistinctByIterator(source, keySelector, comparer);
        }

        private static IEnumerable<TSource> DistinctByIterator<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var set = new HashSet<TKey>(DefaultInternalSetCapacity, comparer);
                do
                {
                    TSource element = enumerator.Current;
                    if (set.Add(keySelector(element)))
                    {
                        yield return element;
                    }
                }
                while (enumerator.MoveNext());
            }
        }

        /// <summary>
        /// An iterator that yields the distinct values in an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private sealed partial class DistinctIterator<TSource> : Iterator<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly IEqualityComparer<TSource>? _comparer;
            private HashSet<TSource>? _set;
            private IEnumerator<TSource>? _enumerator;

            public DistinctIterator(IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer)
            {
                Debug.Assert(source != null);
                _source = source;
                _comparer = comparer;
            }

            public override Iterator<TSource> Clone() => new DistinctIterator<TSource>(_source, _comparer);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        if (!_enumerator.MoveNext())
                        {
                            Dispose();
                            return false;
                        }

                        TSource element = _enumerator.Current;
                        _set = new HashSet<TSource>(DefaultInternalSetCapacity, _comparer);
                        _set.Add(element);
                        _current = element;
                        _state = 2;
                        return true;
                    case 2:
                        Debug.Assert(_enumerator != null);
                        Debug.Assert(_set != null);
                        while (_enumerator.MoveNext())
                        {
                            element = _enumerator.Current;
                            if (_set.Add(element))
                            {
                                _current = element;
                                return true;
                            }
                        }

                        break;
                }

                Dispose();
                return false;
            }

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                    _set = null;
                }

                base.Dispose();
            }
        }
    }
}
