// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

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
        /// <summary>Correlates the elements of two sequences based on equality of keys and groups the results. The default equality comparer is used to compare keys.</summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from an element from the first sequence and a collection of matching elements from the second sequence.</param>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains elements of type <typeparamref name="TResult" /> that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The default equality comparer, <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" />, is used to hash and compare keys.
        /// <see cref="O:System.Linq.Enumerable.GroupJoin" /> produces hierarchical results, which means that elements from <paramref name="outer" /> are paired with collections of matching elements from <paramref name="inner" />. `GroupJoin` enables you to base your results on a whole set of matches for each element of <paramref name="outer" />.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  If there are no correlated elements in `inner` for a given element of `outer`, the sequence of matches for that element will be empty but will still appear in the results.
        /// ]]></format>
        /// The <paramref name="resultSelector" /> function is called only one time for each <paramref name="outer" /> element together with a collection of all the <paramref name="inner" /> elements that match the <paramref name="outer" /> element. This differs from the <see cref="O:System.Linq.Enumerable.Join" /> method, in which the result selector function is invoked on pairs that contain one element from <paramref name="outer" /> and one element from <paramref name="inner" />.
        /// `GroupJoin` preserves the order of the elements of <paramref name="outer" />, and for each element of <paramref name="outer" />, the order of the matching elements from <paramref name="inner" />.
        /// <see cref="O:System.Linq.Enumerable.GroupJoin" /> has no direct equivalent in traditional relational database terms. However, this method does implement a superset of inner joins and left outer joins. Both of these operations can be written in terms of a grouped join. See <a href="https://msdn.microsoft.com/library/442d176d-028c-4beb-8d22-407d4ef89107">Join Operations</a>.
        /// In query expression syntax, a `join … into` (Visual C#) or `Group Join` (Visual Basic) clause translates to an invocation of <see cref="O:System.Linq.Enumerable.GroupJoin" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.GroupJoin{T1,T2,T3,T4}(System.Collections.Generic.IEnumerable{T1},System.Collections.Generic.IEnumerable{T2},System.Func{T1,T3},System.Func{T2,T3},System.Func{T1,System.Collections.Generic.IEnumerable{T2},T4})" /> to perform a grouped join on two sequences.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet40":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet40":::</example>
        /// <related type="Article" href="https://msdn.microsoft.com/library/442d176d-028c-4beb-8d22-407d4ef89107">Join Operations</related>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/join-clause">join clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/group-join-clause">Group Join Clause (Visual Basic)</related>
        public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
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

            return GroupJoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
        }

        /// <summary>Correlates the elements of two sequences based on key equality and groups the results. A specified <see cref="System.Collections.Generic.IEqualityComparer{T}" /> is used to compare keys.</summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from an element from the first sequence and a collection of matching elements from the second sequence.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to hash and compare keys.</param>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains elements of type <typeparamref name="TResult" /> that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" />, is used to hash and compare keys.
        /// <see cref="O:System.Linq.Enumerable.GroupJoin" /> produces hierarchical results, which means that elements from <paramref name="outer" /> are paired with collections of matching elements from <paramref name="inner" />. `GroupJoin` enables you to base your results on a whole set of matches for each element of <paramref name="outer" />.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  If there are no correlated elements in `inner` for a given element of `outer`, the sequence of matches for that element will be empty but will still appear in the results.
        /// ]]></format>
        /// The <paramref name="resultSelector" /> function is called only one time for each <paramref name="outer" /> element together with a collection of all the <paramref name="inner" /> elements that match the <paramref name="outer" /> element. This differs from the <see cref="O:System.Linq.Enumerable.Join" /> method in which the result selector function is invoked on pairs that contain one element from <paramref name="outer" /> and one element from <paramref name="inner" />.
        /// `GroupJoin` preserves the order of the elements of <paramref name="outer" />, and for each element of <paramref name="outer" />, the order of the matching elements from <paramref name="inner" />.
        /// <see cref="O:System.Linq.Enumerable.GroupJoin" /> has no direct equivalent in traditional relational database terms. However, this method does implement a superset of inner joins and left outer joins. Both of these operations can be written in terms of a grouped join. See <a href="https://msdn.microsoft.com/library/442d176d-028c-4beb-8d22-407d4ef89107">Join Operations</a>.</remarks>
        /// <related type="Article" href="https://msdn.microsoft.com/library/442d176d-028c-4beb-8d22-407d4ef89107">Performing Join Operations</related>
        public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
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

            return GroupJoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }

        private static IEnumerable<TResult> GroupJoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            using (IEnumerator<TOuter> e = outer.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    Lookup<TKey, TInner> lookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
                    do
                    {
                        TOuter item = e.Current;
                        yield return resultSelector(item, lookup[outerKeySelector(item)]);
                    }
                    while (e.MoveNext());
                }
            }
        }
    }
}
