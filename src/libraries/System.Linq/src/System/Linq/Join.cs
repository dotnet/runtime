// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

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
        /// <summary>Correlates the elements of two sequences based on matching keys. The default equality comparer is used to compare keys.</summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that has elements of type <typeparamref name="TResult" /> that are obtained by performing an inner join on two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The default equality comparer, <see cref="O:EqualityComparer{T}.Default" />, is used to hash and compare keys.
        /// A join refers to the operation of correlating the elements of two sources of information based on a common key. <see cref="O:Enumerable.Join" /> brings the two information sources and the keys by which they are matched together in one method call. This differs from the use of `SelectMany`, which requires more than one method call to perform the same operation.
        /// <see cref="O:Enumerable.Join" /> preserves the order of the elements of <paramref name="outer" />, and for each of these elements, the order of the matching elements of <paramref name="inner" />.
        /// In query expression syntax, a `join` (Visual C#) or `Join` (Visual Basic) clause translates to an invocation of <see cref="O:Enumerable.Join" />.
        /// In relational database terms, the <see cref="O:Enumerable.Join" /> method implements an inner equijoin. 'Inner' means that only elements that have a match in the other sequence are included in the results. An 'equijoin' is a join in which the keys are compared for equality. A left outer join operation has no dedicated standard query operator, but can be performed by using the <see cref="O:Enumerable.GroupJoin" /> method. See <a href="https://msdn.microsoft.com/library/442d176d-028c-4beb-8d22-407d4ef89107">Join Operations</a>.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="Join{T1,T2,T3,T4}(IEnumerable{T1},IEnumerable{T2},Func{T1,T3},Func{T2,T3},Func{T1,T2,T4})" /> to perform an inner join of two sequences based on a common key.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet42":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet42":::</example>
        /// <related type="Article" href="https://msdn.microsoft.com/library/442d176d-028c-4beb-8d22-407d4ef89107">Joining</related>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/join-clause">join clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/join-clause">Join Clause (Visual Basic)</related>
        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
        {
            if (outer == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outer);
            }

            if (inner == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inner);
            }

            if (outerKeySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outerKeySelector);
            }

            if (innerKeySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.innerKeySelector);
            }

            if (resultSelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            return JoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
        }

        /// <summary>Correlates the elements of two sequences based on matching keys. A specified <see cref="IEqualityComparer{T}" /> is used to compare keys.</summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}" /> to hash and compare keys.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that has elements of type <typeparamref name="TResult" /> that are obtained by performing an inner join on two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="O:EqualityComparer{T}.Default" />, is used to hash and compare keys.
        /// A join refers to the operation of correlating the elements of two sources of information based on a common key. <see cref="O:Enumerable.Join" /> brings the two information sources and the keys by which they are matched together in one method call. This differs from the use of `SelectMany`, which requires more than one method call to perform the same operation.
        /// <see cref="O:Enumerable.Join" /> preserves the order of the elements of <paramref name="outer" />, and for each of these elements, the order of the matching elements of <paramref name="inner" />.
        /// In relational database terms, the <see cref="O:Enumerable.Join" /> method implements an inner equijoin. 'Inner' means that only elements that have a match in the other sequence are included in the results. An 'equijoin' is a join in which the keys are compared for equality. A left outer join operation has no dedicated standard query operator, but can be performed by using the <see cref="O:Enumerable.GroupJoin" /> method. See <a href="https://msdn.microsoft.com/library/442d176d-028c-4beb-8d22-407d4ef89107">Join Operations</a>.</remarks>
        /// <related type="Article" href="https://msdn.microsoft.com/library/442d176d-028c-4beb-8d22-407d4ef89107">Joining</related>
        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (outer == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outer);
            }

            if (inner == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inner);
            }

            if (outerKeySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outerKeySelector);
            }

            if (innerKeySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.innerKeySelector);
            }

            if (resultSelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            return JoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }

        private static IEnumerable<TResult> JoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            using (IEnumerator<TOuter> e = outer.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    Lookup<TKey, TInner> lookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
                    if (lookup.Count != 0)
                    {
                        do
                        {
                            TOuter item = e.Current;
                            Grouping<TKey, TInner>? g = lookup.GetGrouping(outerKeySelector(item), create: false);
                            if (g != null)
                            {
                                int count = g._count;
                                TInner[] elements = g._elements;
                                for (int i = 0; i != count; ++i)
                                {
                                    yield return resultSelector(item, elements[i]);
                                }
                            }
                        }
                        while (e.MoveNext());
                    }
                }
            }
        }
    }
}
