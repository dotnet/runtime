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
        /// <summary>Sorts the elements of a sequence in ascending order according to a key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="System.Linq.IOrderedEnumerable{T}" /> whose elements are sorted according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// To order a sequence by the values of the elements themselves, specify the identity function (`x => x` in Visual C# or `Function(x) x` in Visual Basic) for <paramref name="keySelector" />.
        /// Two methods are defined to extend the type <see cref="System.Linq.IOrderedEnumerable{T}" />, which is the return type of this method. These two methods, namely `ThenBy` and `ThenByDescending`, enable you to specify additional sort criteria to sort a sequence. `ThenBy` and `ThenByDescending` also return an <see cref="System.Linq.IOrderedEnumerable{T}" />, which means any number of consecutive calls to `ThenBy` or `ThenByDescending` can be made.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  Because <xref:System.Linq.IOrderedEnumerable%601> inherits from <xref:System.Collections.Generic.IEnumerable%601>, you can call <xref:System.Linq.Enumerable.OrderBy%2A> or <xref:System.Linq.Enumerable.OrderByDescending%2A> on the results of a call to <xref:System.Linq.Enumerable.OrderBy%2A>, <xref:System.Linq.Enumerable.OrderByDescending%2A>, <xref:System.Linq.Enumerable.ThenBy%2A> or <xref:System.Linq.Enumerable.ThenByDescending%2A>. Doing this introduces a new primary ordering that ignores the previously established ordering.
        /// ]]></format>
        /// This method compares keys by using the default comparer <see cref="O:System.Collections.Generic.Comparer{T}.Default" />.
        /// This method performs a stable sort; that is, if the keys of two elements are equal, the order of the elements is preserved. In contrast, an unstable sort does not preserve the order of elements that have the same key.
        /// In query expression syntax, an `orderby` (Visual C#) or `Order By` (Visual Basic) clause translates to an invocation of <see cref="O:System.Linq.Enumerable.OrderBy" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.OrderBy{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2})" /> to sort the elements of a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet70":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet70":::</example>
        /// <altmember cref="System.Linq.Enumerable.OrderByDescending{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2})"/>
        /// <altmember cref="System.Linq.Enumerable.OrderByDescending{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2},System.Collections.Generic.IComparer{T2})"/>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/orderby-clause">orderby clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/order-by-clause">Order By Clause (Visual Basic)</related>
        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
            new OrderedEnumerable<TSource, TKey>(source, keySelector, null, false, null);

        /// <summary>Sorts the elements of a sequence in ascending order by using a specified comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IComparer{T}" /> to compare keys.</param>
        /// <returns>An <see cref="System.Linq.IOrderedEnumerable{T}" /> whose elements are sorted according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// To order a sequence by the values of the elements themselves, specify the identity function (`x => x` in Visual C# or `Function(x) x` in Visual Basic) for <paramref name="keySelector" />.
        /// Two methods are defined to extend the type <see cref="System.Linq.IOrderedEnumerable{T}" />, which is the return type of this method. These two methods, namely `ThenBy` and `ThenByDescending`, enable you to specify additional sort criteria to sort a sequence. `ThenBy` and `ThenByDescending` also return an <see cref="System.Linq.IOrderedEnumerable{T}" />, which means any number of consecutive calls to `ThenBy` or `ThenByDescending` can be made.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  Because <xref:System.Linq.IOrderedEnumerable%601> inherits from <xref:System.Collections.Generic.IEnumerable%601>, you can call <xref:System.Linq.Enumerable.OrderBy%2A> or <xref:System.Linq.Enumerable.OrderByDescending%2A> on the results of a call to <xref:System.Linq.Enumerable.OrderBy%2A>, <xref:System.Linq.Enumerable.OrderByDescending%2A>, <xref:System.Linq.Enumerable.ThenBy%2A> or <xref:System.Linq.Enumerable.ThenByDescending%2A>. Doing this introduces a new primary ordering that ignores the previously established ordering.
        /// ]]></format>
        /// If <paramref name="comparer" /> is <see langword="null" />, the default comparer <see cref="O:System.Collections.Generic.Comparer{T}.Default" /> is used to compare keys.
        /// This method performs a stable sort; that is, if the keys of two elements are equal, the order of the elements is preserved. In contrast, an unstable sort does not preserve the order of elements that have the same key.</remarks>
        /// <altmember cref="System.Linq.Enumerable.OrderByDescending{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2})"/>
        /// <altmember cref="System.Linq.Enumerable.OrderByDescending{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2},System.Collections.Generic.IComparer{T2})"/>
        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer) =>
            new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, false, null);

        /// <summary>Sorts the elements of a sequence in descending order according to a key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="System.Linq.IOrderedEnumerable{T}" /> whose elements are sorted in descending order according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// To order a sequence by the values of the elements themselves, specify the identity function (`x => x` in Visual C# or `Function(x) x` in Visual Basic) for <paramref name="keySelector" />.
        /// For an example of this method, see <see cref="System.Linq.Enumerable.OrderByDescending{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2},System.Collections.Generic.IComparer{T2})" />.
        /// Two methods are defined to extend the type <see cref="System.Linq.IOrderedEnumerable{T}" />, which is the return type of this method. These two methods, namely `ThenBy` and `ThenByDescending`, enable you to specify additional sort criteria to sort a sequence. `ThenBy` and `ThenByDescending` also return an <see cref="System.Linq.IOrderedEnumerable{T}" />, which means any number of consecutive calls to `ThenBy` or `ThenByDescending` can be made.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  Because <xref:System.Linq.IOrderedEnumerable%601> inherits from <xref:System.Collections.Generic.IEnumerable%601>, you can call <xref:System.Linq.Enumerable.OrderBy%2A> or <xref:System.Linq.Enumerable.OrderByDescending%2A> on the results of a call to <xref:System.Linq.Enumerable.OrderBy%2A>, <xref:System.Linq.Enumerable.OrderByDescending%2A>, <xref:System.Linq.Enumerable.ThenBy%2A> or <xref:System.Linq.Enumerable.ThenByDescending%2A>. Doing this introduces a new primary ordering that ignores the previously established ordering.
        /// ]]></format>
        /// This method compares keys by using the default comparer <see cref="O:System.Collections.Generic.Comparer{T}.Default" />.
        /// This method performs a stable sort; that is, if the keys of two elements are equal, the order of the elements is preserved. In contrast, an unstable sort does not preserve the order of elements that have the same key.
        /// In query expression syntax, an `orderby descending` (Visual C#) or `Order By Descending` (Visual Basic) clause translates to an invocation of <see cref="O:System.Linq.Enumerable.OrderByDescending" />.</remarks>
        /// <altmember cref="System.Linq.Enumerable.OrderBy{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2})"/>
        /// <altmember cref="System.Linq.Enumerable.OrderBy{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2},System.Collections.Generic.IComparer{T2})"/>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/orderby-clause">orderby clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/order-by-clause">Order By Clause (Visual Basic)</related>
        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
            new OrderedEnumerable<TSource, TKey>(source, keySelector, null, true, null);

        /// <summary>Sorts the elements of a sequence in descending order by using a specified comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IComparer{T}" /> to compare keys.</param>
        /// <returns>An <see cref="System.Linq.IOrderedEnumerable{T}" /> whose elements are sorted in descending order according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// To order a sequence by the values of the elements themselves, specify the identity function (`x => x` in Visual C# or `Function(x) x` in Visual Basic) for <paramref name="keySelector" />.
        /// Two methods are defined to extend the type <see cref="System.Linq.IOrderedEnumerable{T}" />, which is the return type of this method. These two methods, namely `ThenBy` and `ThenByDescending`, enable you to specify additional sort criteria to sort a sequence. `ThenBy` and `ThenByDescending` also return an <see cref="System.Linq.IOrderedEnumerable{T}" />, which means any number of consecutive calls to `ThenBy` or `ThenByDescending` can be made.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  Because <xref:System.Linq.IOrderedEnumerable%601> inherits from <xref:System.Collections.Generic.IEnumerable%601>, you can call <xref:System.Linq.Enumerable.OrderBy%2A> or <xref:System.Linq.Enumerable.OrderByDescending%2A> on the results of a call to <xref:System.Linq.Enumerable.OrderBy%2A>, <xref:System.Linq.Enumerable.OrderByDescending%2A>, <xref:System.Linq.Enumerable.ThenBy%2A> or <xref:System.Linq.Enumerable.ThenByDescending%2A>. Doing this introduces a new primary ordering that ignores the previously established ordering.
        /// ]]></format>
        /// If <paramref name="comparer" /> is <see langword="null" />, the default comparer <see cref="O:System.Collections.Generic.Comparer{T}.Default" /> is used to compare keys.
        /// This method performs a stable sort; that is, if the keys of two elements are equal, the order of the elements is preserved. In contrast, an unstable sort does not preserve the order of elements that have the same key.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.OrderByDescending{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2},System.Collections.Generic.IComparer{T2})" /> to sort the elements of a sequence in descending order by using a transform function and a custom comparer.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet71":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet71":::</example>
        /// <altmember cref="System.Linq.Enumerable.OrderBy{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2})"/>
        /// <altmember cref="System.Linq.Enumerable.OrderBy{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2},System.Collections.Generic.IComparer{T2})"/>
        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer) =>
            new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, true, null);

        /// <summary>Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IOrderedEnumerable{T}" /> that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>An <see cref="System.Linq.IOrderedEnumerable{T}" /> whose elements are sorted according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// To order a sequence by the values of the elements themselves, specify the identity function (`x => x` in Visual C# or `Function(x) x` in Visual Basic) for <paramref name="keySelector" />.
        /// <see cref="O:System.Linq.Enumerable.ThenBy" /> and <see cref="O:System.Linq.Enumerable.ThenByDescending" /> are defined to extend the type <see cref="System.Linq.IOrderedEnumerable{T}" />, which is also the return type of these methods. This design enables you to specify multiple sort criteria by applying any number of <see cref="O:System.Linq.Enumerable.ThenBy" /> or <see cref="O:System.Linq.Enumerable.ThenByDescending" /> methods.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  Because <xref:System.Linq.IOrderedEnumerable%601> inherits from <xref:System.Collections.Generic.IEnumerable%601>, you can call <xref:System.Linq.Enumerable.OrderBy%2A> or <xref:System.Linq.Enumerable.OrderByDescending%2A> on the results of a call to <xref:System.Linq.Enumerable.OrderBy%2A>, <xref:System.Linq.Enumerable.OrderByDescending%2A>, <xref:System.Linq.Enumerable.ThenBy%2A> or <xref:System.Linq.Enumerable.ThenByDescending%2A>. Doing this introduces a new primary ordering that ignores the previously established ordering.
        /// ]]></format>
        /// This method compares keys by using the default comparer <see cref="O:System.Collections.Generic.Comparer{T}.Default" />.
        /// This method performs a stable sort; that is, if the keys of two elements are equal, the order of the elements is preserved. In contrast, an unstable sort does not preserve the order of elements that have the same key.
        /// In query expression syntax, an `orderby [first criterion], [second criterion]` (Visual C#) or `Order By [first criterion], [second criterion]` (Visual Basic) clause translates to an invocation of <see cref="O:System.Linq.Enumerable.ThenBy" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.ThenBy{T1,T2}(System.Linq.IOrderedEnumerable{T1},System.Func{T1,T2})" /> to perform a secondary ordering of the elements in a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet102":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet102":::</example>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/orderby-clause">orderby clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/order-by-clause">Order By Clause (Visual Basic)</related>
        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source.CreateOrderedEnumerable(keySelector, null, false);
        }

        /// <summary>Performs a subsequent ordering of the elements in a sequence in ascending order by using a specified comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IOrderedEnumerable{T}" /> that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IComparer{T}" /> to compare keys.</param>
        /// <returns>An <see cref="System.Linq.IOrderedEnumerable{T}" /> whose elements are sorted according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// To order a sequence by the values of the elements themselves, specify the identity function (`x => x` in Visual C# or `Function(x) x` in Visual Basic) for <paramref name="keySelector" />.
        /// <see cref="O:System.Linq.Enumerable.ThenBy" /> and <see cref="O:System.Linq.Enumerable.ThenByDescending" /> are defined to extend the type <see cref="System.Linq.IOrderedEnumerable{T}" />, which is also the return type of these methods. This design enables you to specify multiple sort criteria by applying any number of <see cref="O:System.Linq.Enumerable.ThenBy" /> or <see cref="O:System.Linq.Enumerable.ThenByDescending" /> methods.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  Because <xref:System.Linq.IOrderedEnumerable%601> inherits from <xref:System.Collections.Generic.IEnumerable%601>, you can call <xref:System.Linq.Enumerable.OrderBy%2A> or <xref:System.Linq.Enumerable.OrderByDescending%2A> on the results of a call to <xref:System.Linq.Enumerable.OrderBy%2A>, <xref:System.Linq.Enumerable.OrderByDescending%2A>, <xref:System.Linq.Enumerable.ThenBy%2A> or <xref:System.Linq.Enumerable.ThenByDescending%2A>. Doing this introduces a new primary ordering that ignores the previously established ordering.
        /// ]]></format>
        /// If <paramref name="comparer" /> is <see langword="null" />, the default comparer <see cref="O:System.Collections.Generic.Comparer{T}.Default" /> is used to compare keys.
        /// This method performs a stable sort; that is, if the keys of two elements are equal, the order of the elements is preserved. In contrast, an unstable sort does not preserve the order of elements that have the same key.</remarks>
        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source.CreateOrderedEnumerable(keySelector, comparer, false);
        }

        /// <summary>Performs a subsequent ordering of the elements in a sequence in descending order, according to a key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IOrderedEnumerable{T}" /> that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>An <see cref="System.Linq.IOrderedEnumerable{T}" /> whose elements are sorted in descending order according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// To order a sequence by the values of the elements themselves, specify the identity function (`x => x` in Visual C# or `Function(x) x` in Visual Basic) for <paramref name="keySelector" />.
        /// <see cref="O:System.Linq.Enumerable.ThenBy" /> and <see cref="O:System.Linq.Enumerable.ThenByDescending" /> are defined to extend the type <see cref="System.Linq.IOrderedEnumerable{T}" />, which is also the return type of these methods. This design enables you to specify multiple sort criteria by applying any number of <see cref="O:System.Linq.Enumerable.ThenBy" /> or <see cref="O:System.Linq.Enumerable.ThenByDescending" /> methods.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  Because <xref:System.Linq.IOrderedEnumerable%601> inherits from <xref:System.Collections.Generic.IEnumerable%601>, you can call <xref:System.Linq.Enumerable.OrderBy%2A> or <xref:System.Linq.Enumerable.OrderByDescending%2A> on the results of a call to <xref:System.Linq.Enumerable.OrderBy%2A>, <xref:System.Linq.Enumerable.OrderByDescending%2A>, <xref:System.Linq.Enumerable.ThenBy%2A> or <xref:System.Linq.Enumerable.ThenByDescending%2A>. Doing this introduces a new primary ordering that ignores the previously established ordering.
        /// ]]></format>
        /// This sorting method compares keys by using the default comparer <see cref="O:System.Collections.Generic.Comparer{T}.Default" />.
        /// This method performs a stable sort; that is, if the keys of two elements are equal, the order of the elements is preserved. In contrast, an unstable sort does not preserve the order of elements that have the same key.
        /// In Visual C# query expression syntax, an `orderby [first criterion], [second criterion] descending` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.ThenByDescending" />.
        /// In Visual Basic query expression syntax, an `Order By [first criterion], [second criterion] Descending` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.ThenByDescending" />.</remarks>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/orderby-clause">orderby clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/order-by-clause">Order By Clause (Visual Basic)</related>
        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source.CreateOrderedEnumerable(keySelector, null, true);
        }

        /// <summary>Performs a subsequent ordering of the elements in a sequence in descending order by using a specified comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Linq.IOrderedEnumerable{T}" /> that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IComparer{T}" /> to compare keys.</param>
        /// <returns>An <see cref="System.Linq.IOrderedEnumerable{T}" /> whose elements are sorted in descending order according to a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// To order a sequence by the values of the elements themselves, specify the identity function (`x => x` in Visual C# or `Function(x) x` in Visual Basic) for <paramref name="keySelector" />.
        /// <see cref="O:System.Linq.Enumerable.ThenBy" /> and <see cref="O:System.Linq.Enumerable.ThenByDescending" /> are defined to extend the type <see cref="System.Linq.IOrderedEnumerable{T}" />, which is also the return type of these methods. This design enables you to specify multiple sort criteria by applying any number of <see cref="O:System.Linq.Enumerable.ThenBy" /> or <see cref="O:System.Linq.Enumerable.ThenByDescending" /> methods.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  Because <xref:System.Linq.IOrderedEnumerable%601> inherits from <xref:System.Collections.Generic.IEnumerable%601>, you can call <xref:System.Linq.Enumerable.OrderBy%2A> or <xref:System.Linq.Enumerable.OrderByDescending%2A> on the results of a call to <xref:System.Linq.Enumerable.OrderBy%2A>, <xref:System.Linq.Enumerable.OrderByDescending%2A>, <xref:System.Linq.Enumerable.ThenBy%2A> or <xref:System.Linq.Enumerable.ThenByDescending%2A>. Doing this introduces a new primary ordering that ignores the previously established ordering.
        /// ]]></format>
        /// If <paramref name="comparer" /> is <see langword="null" />, the default comparer <see cref="O:System.Collections.Generic.Comparer{T}.Default" /> is used to compare keys.
        /// This method performs a stable sort; that is, if the keys of two elements are equal, the order of the elements is preserved. In contrast, an unstable sort does not preserve the order of elements that have the same key.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.ThenByDescending{T1,T2}(System.Linq.IOrderedEnumerable{T1},System.Func{T1,T2},System.Collections.Generic.IComparer{T2})" /> to perform a secondary ordering of the elements in a sequence in descending order by using a custom comparer.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet103":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet103":::</example>
        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source.CreateOrderedEnumerable(keySelector, comparer, true);
        }
    }

    /// <summary>Represents a sorted sequence.</summary>
    /// <typeparam name="TElement">The type of the elements of the sequence.</typeparam>
    /// <remarks>This type is enumerable because it inherits from <see cref="System.Collections.Generic.IEnumerable{T}" />.
    /// The extension methods <see cref="O:System.Linq.Enumerable.ThenBy" /> and <see cref="O:System.Linq.Enumerable.ThenByDescending" /> operate on objects of type <see cref="System.Linq.IOrderedEnumerable{T}" />. An object of type <see cref="System.Linq.IOrderedEnumerable{T}" /> can be obtained by calling one of the primary sort methods, <see cref="O:System.Linq.Enumerable.OrderBy" /> or <see cref="O:System.Linq.Enumerable.OrderByDescending" />, which return an <see cref="System.Linq.IOrderedEnumerable{T}" />. <see cref="O:System.Linq.Enumerable.ThenBy" /> and <see cref="O:System.Linq.Enumerable.ThenByDescending" />, the subordinate sort methods, in turn also return an object of type <see cref="System.Linq.IOrderedEnumerable{T}" />. This design allows for any number of consecutive calls to <see cref="O:System.Linq.Enumerable.ThenBy" /> or <see cref="O:System.Linq.Enumerable.ThenByDescending" />, where each call performs a subordinate ordering on the sorted data returned from the previous call.</remarks>
    /// <example>The following example demonstrates how to perform a primary and secondary ordering on an array of strings. It also demonstrates that the resulting <see cref="System.Linq.IOrderedEnumerable{T}" /> is enumerable.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.IOrderedEnumerable/CS/IOrderedEnumerable.cs" interactive="try-dotnet-method" id="Snippet1":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.IOrderedEnumerable/VB/IOrderedEnumerable.vb" id="Snippet1":::</example>
    public interface IOrderedEnumerable<out TElement> : IEnumerable<TElement>
    {
        /// <summary>Performs a subsequent ordering on the elements of an <see cref="System.Linq.IOrderedEnumerable{T}" /> according to a key.</summary>
        /// <typeparam name="TKey">The type of the key produced by <paramref name="keySelector" />.</typeparam>
        /// <param name="keySelector">The <see cref="System.Func{T1,T2}" /> used to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IComparer{T}" /> used to compare keys for placement in the returned sequence.</param>
        /// <param name="descending"><see langword="true" /> to sort the elements in descending order; <see langword="false" /> to sort the elements in ascending order.</param>
        /// <returns>An <see cref="System.Linq.IOrderedEnumerable{T}" /> whose elements are sorted according to a key.</returns>
        /// <remarks>The functionality provided by this method is like that provided by <see cref="O:System.Linq.Enumerable.ThenBy" /> or <see cref="O:System.Linq.Enumerable.ThenByDescending" />, depending on whether <paramref name="descending" /> is <see langword="true" /> or <see langword="false" />. They both perform a subordinate ordering of an already sorted sequence of type <see cref="System.Linq.IOrderedEnumerable{T}" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:System.Linq.IOrderedEnumerable{T}.CreateOrderedEnumerable" /> to perform a secondary ordering on an <see cref="System.Linq.IOrderedEnumerable{T}" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.IOrderedEnumerable/CS/IOrderedEnumerable.cs" interactive="try-dotnet-method" id="Snippet2":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.IOrderedEnumerable/VB/IOrderedEnumerable.vb" id="Snippet2":::</example>
        IOrderedEnumerable<TElement> CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey>? comparer, bool descending);
    }
}
