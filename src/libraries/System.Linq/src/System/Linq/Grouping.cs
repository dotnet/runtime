// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    /// <summary>Provides a set of <see langword="static" /> (<see langword="Shared" /> in Visual Basic) methods for querying objects that implement <see cref="IEnumerable{T}" />.</summary>
    /// <remarks>
    /// <para>The methods in this class provide an implementation of the standard query operators for querying data sources that implement <see cref="IEnumerable{T}" />. The standard query operators are general purpose methods that follow the LINQ pattern and enable you to express traversal, filter, and projection operations over data in any .NET-based programming language.</para>
    /// <para>The majority of the methods in this class are defined as extension methods that extend <see cref="IEnumerable{T}" />. This means they can be called like an instance method on any object that implements <see cref="IEnumerable{T}" />.</para>
    /// <para>Methods that are used in a query that returns a sequence of values do not consume the target data until the query object is enumerated. This is known as deferred execution. Methods that are used in a query that returns a singleton value execute and consume the target data immediately.</para>
    /// </remarks>
    /// <related type="Article" href="https://msdn.microsoft.com/library/24cda21e-8af8-4632-b519-c404a839b9b2">Standard Query Operators Overview</related>
    /// <related type="Article" href="/dotnet/csharp/programming-guide/classes-and-structs/extension-methods">Extension Methods (C# Programming Guide)</related>
    /// <related type="Article" href="/dotnet/visual-basic/programming-guide/language-features/procedures/extension-methods">Extension Methods (Visual Basic)</related>
    public static partial class Enumerable
    {
        /// <summary>Groups the elements of a sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An <c>IEnumerable&lt;IGrouping&lt;TKey, TSource&gt;&gt;</c> in C# or <c>IEnumerable(Of IGrouping(Of TKey, TSource))</c> in Visual Basic where each <see cref="IGrouping{T1,T2}" /> object contains a sequence of objects and a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  For examples of `GroupBy`, see the following articles:
        /// >
        /// > - <xref:System.Linq.Enumerable.GroupBy%60%603%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2C%60%601%7D%2CSystem.Func%7B%60%600%2C%60%602%7D%29>
        /// > - <xref:System.Linq.Enumerable.GroupBy%60%603%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2C%60%601%7D%2CSystem.Func%7B%60%601%2CSystem.Collections.Generic.IEnumerable%7B%60%600%7D%2C%60%602%7D%29>
        /// > - <xref:System.Linq.Enumerable.GroupBy%60%604%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2C%60%601%7D%2CSystem.Func%7B%60%600%2C%60%602%7D%2CSystem.Func%7B%60%601%2CSystem.Collections.Generic.IEnumerable%7B%60%602%7D%2C%60%603%7D%29>
        /// ]]></format>
        /// The <see cref="GroupBy{T1,T2}(IEnumerable{T1},Func{T1,T2})" /> method returns a collection of <see cref="IGrouping{T1,T2}" /> objects, one for each distinct key that was encountered. An <see cref="IGrouping{T1,T2}" /> is an <see cref="IEnumerable{T}" /> that also has a key associated with its elements.
        /// The <see cref="IGrouping{T1,T2}" /> objects are yielded in an order based on the order of the elements in <paramref name="source" /> that produced the first key of each <see cref="IGrouping{T1,T2}" />. Elements in a grouping are yielded in the order they appear in <paramref name="source" />.
        /// The default equality comparer <see cref="O:EqualityComparer{T}.Default" /> is used to compare keys.
        /// In query expression syntax, a `group by` (Visual C#) or `Group By Into` (Visual Basic) clause translates to an invocation of <see cref="O:Enumerable.GroupBy" />. For more information and usage examples, see [group clause](/dotnet/csharp/language-reference/keywords/group-clause) and [Group By Clause](/dotnet/visual-basic/language-reference/queries/group-by-clause).</remarks>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/group-clause">group clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/group-by-clause">Group By Clause (Visual Basic)</related>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
            new GroupedEnumerable<TSource, TKey>(source, keySelector, null);

        /// <summary>Groups the elements of a sequence according to a specified key selector function and compares the keys by using a specified comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}" /> to compare keys.</param>
        /// <returns>An <c>IEnumerable&lt;IGrouping&lt;TKey, TSource&gt;&gt;</c> in C# or <c>IEnumerable(Of IGrouping(Of TKey, TSource))</c> in Visual Basic where each <see cref="IGrouping{T1,T2}" /> object contains a collection of objects and a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  For examples of `GroupBy`, see the following articles:
        /// >
        /// > - <xref:System.Linq.Enumerable.GroupBy%60%603%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2C%60%601%7D%2CSystem.Func%7B%60%600%2C%60%602%7D%29>
        /// > - <xref:System.Linq.Enumerable.GroupBy%60%603%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2C%60%601%7D%2CSystem.Func%7B%60%601%2CSystem.Collections.Generic.IEnumerable%7B%60%600%7D%2C%60%602%7D%29>
        /// > - <xref:System.Linq.Enumerable.GroupBy%60%604%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2C%60%601%7D%2CSystem.Func%7B%60%600%2C%60%602%7D%2CSystem.Func%7B%60%601%2CSystem.Collections.Generic.IEnumerable%7B%60%602%7D%2C%60%603%7D%29>
        /// ]]></format>
        /// The <see cref="GroupBy{T1,T2}(IEnumerable{T1},Func{T1,T2},IEqualityComparer{T2})" /> method returns a collection of <see cref="IGrouping{T1,T2}" /> objects, one for each distinct key that was encountered. An <see cref="IGrouping{T1,T2}" /> is an <see cref="IEnumerable{T}" /> that also has a key associated with its elements.
        /// The <see cref="IGrouping{T1,T2}" /> objects are yielded in an order based on the order of the elements in <paramref name="source" /> that produced the first key of each <see cref="IGrouping{T1,T2}" />. Elements in a grouping are yielded in the order they appear in <paramref name="source" />.
        /// If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer <see cref="O:EqualityComparer{T}.Default" /> is used to compare keys.
        /// If two keys are considered equal according to <paramref name="comparer" />, the first key is chosen as the key for that grouping.
        /// In query expression syntax, a `group by` (Visual C#) or `Group By Into` (Visual Basic) clause translates to an invocation of <see cref="O:Enumerable.GroupBy" />. For more information and usage examples, see [group clause](/dotnet/csharp/language-reference/keywords/group-clause) and [Group By Clause](/dotnet/visual-basic/language-reference/queries/group-by-clause).</remarks>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/group-clause">group clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/group-by-clause">Group By Clause (Visual Basic)</related>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer) =>
            new GroupedEnumerable<TSource, TKey>(source, keySelector, comparer);

        /// <summary>Groups the elements of a sequence according to a specified key selector function and projects the elements for each group by using a specified function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TElement">The type of the elements in the <see cref="IGrouping{T1,T2}" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in the <see cref="IGrouping{T1,T2}" />.</param>
        /// <returns>An <c>IEnumerable&lt;IGrouping&lt;TKey, TElement&gt;&gt;</c> in C# or <c>IEnumerable(Of IGrouping(Of TKey, TElement))</c> in Visual Basic where each <see cref="IGrouping{T1,T2}" /> object contains a collection of objects of type <typeparamref name="TElement" /> and a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The <see cref="GroupBy{T1,T2,T3}(IEnumerable{T1},Func{T1,T2},Func{T1,T3})" /> method returns a collection of <see cref="IGrouping{T1,T2}" /> objects, one for each distinct key that was encountered. An <see cref="IGrouping{T1,T2}" /> is an <see cref="IEnumerable{T}" /> that also has a key associated with its elements.</para>
        /// <para>The <see cref="IGrouping{T1,T2}" /> objects are yielded in an order based on the order of the elements in <paramref name="source" /> that produced the first key of each <see cref="IGrouping{T1,T2}" />. Elements in a grouping are yielded in the order that the elements that produced them appear in <paramref name="source" />.</para>
        /// <para>The default equality comparer <see cref="O:EqualityComparer{T}.Default" /> is used to compare keys.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="GroupBy{T1,T2,T3}(IEnumerable{T1},Func{T1,T2},Func{T1,T3})" /> to group the elements of a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet39":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet39":::
        /// In query expression syntax, a `group by` (Visual C#) or `Group By Into` (Visual Basic) clause translates to an invocation of <see cref="O:Enumerable.GroupBy" />. The translation of the query expression in the following example is equivalent to the query in the example above.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet122":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet122":::
        /// > [!NOTE]
        /// >  In a Visual C# or Visual Basic query expression, the element and key selection expressions occur in the reverse order from their argument positions in a call to the <see cref="GroupBy{T1,T2,T3}(IEnumerable{T1},Func{T1,T2},Func{T1,T3})" /> method.</example>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/group-clause">group clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/group-by-clause">Group By Clause (Visual Basic)</related>
        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) =>
            new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, null);

        /// <summary>Groups the elements of a sequence according to a key selector function. The keys are compared by using a comparer and each group's elements are projected by using a specified function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TElement">The type of the elements in the <see cref="IGrouping{T1,T2}" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an <see cref="IGrouping{T1,T2}" />.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}" /> to compare keys.</param>
        /// <returns>An <c>IEnumerable&lt;IGrouping&lt;TKey, TElement&gt;&gt;</c> in C# or <c>IEnumerable(Of IGrouping(Of TKey, TElement))</c> in Visual Basic where each <see cref="IGrouping{T1,T2}" /> object contains a collection of objects of type <typeparamref name="TElement" /> and a key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  For examples of `GroupBy`, see the following articles:
        /// >
        /// > - <xref:System.Linq.Enumerable.GroupBy%60%603%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2C%60%601%7D%2CSystem.Func%7B%60%600%2C%60%602%7D%29>
        /// > - <xref:System.Linq.Enumerable.GroupBy%60%603%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2C%60%601%7D%2CSystem.Func%7B%60%601%2CSystem.Collections.Generic.IEnumerable%7B%60%600%7D%2C%60%602%7D%29>
        /// > - <xref:System.Linq.Enumerable.GroupBy%60%604%28System.Collections.Generic.IEnumerable%7B%60%600%7D%2CSystem.Func%7B%60%600%2C%60%601%7D%2CSystem.Func%7B%60%600%2C%60%602%7D%2CSystem.Func%7B%60%601%2CSystem.Collections.Generic.IEnumerable%7B%60%602%7D%2C%60%603%7D%29>
        /// ]]></format>
        /// The <see cref="GroupBy{T1,T2,T3}(IEnumerable{T1},Func{T1,T2},Func{T1,T3},IEqualityComparer{T2})" /> method returns a collection of <see cref="IGrouping{T1,T2}" /> objects, one for each distinct key that was encountered. An <see cref="IGrouping{T1,T2}" /> is an <see cref="IEnumerable{T}" /> that also has a key associated with its elements.
        /// The <see cref="IGrouping{T1,T2}" /> objects are yielded in an order based on the order of the elements in <paramref name="source" /> that produced the first key of each <see cref="IGrouping{T1,T2}" />. Elements in a grouping are yielded in the order that the elements that produced them appear in <paramref name="source" />.
        /// If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer <see cref="O:EqualityComparer{T}.Default" /> is used to compare keys.
        /// If two keys are considered equal according to <paramref name="comparer" />, the first key is chosen as the key for that grouping.
        /// In query expression syntax, a `group by` (Visual C#) or `Group By Into` (Visual Basic) clause translates to an invocation of <see cref="O:Enumerable.GroupBy" />. For more information and usage examples, see [group clause](/dotnet/csharp/language-reference/keywords/group-clause) and [Group By Clause](/dotnet/visual-basic/language-reference/queries/group-by-clause).</remarks>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/group-clause">group clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/group-by-clause">Group By Clause (Visual Basic)</related>
        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer) =>
            new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);

        /// <summary>Groups the elements of a sequence according to a specified key selector function and creates a result value from each group and its key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <returns>A collection of elements of type <typeparamref name="TResult" /> where each element represents a projection over a group and its key.</returns>
        /// <remarks>In query expression syntax, a `group by` (Visual C#) or `Group By Into` (Visual Basic) clause translates to an invocation of <see cref="O:Enumerable.GroupBy" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="GroupBy{T1,T2,T3}(IEnumerable{T1},Func{T1,T2},Func{T2,IEnumerable{T1},T3})" /> to group the elements of a sequence and project a sequence of results of type <typeparamref name="TResult" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet15":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet15":::</example>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/group-clause">group clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/group-by-clause">Group By Clause (Visual Basic)</related>
        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector) =>
            new GroupedResultEnumerable<TSource, TKey, TResult>(source, keySelector, resultSelector, null);

        /// <summary>Groups the elements of a sequence according to a specified key selector function and creates a result value from each group and its key. The elements of each group are projected by using a specified function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each <see cref="IGrouping{T1,T2}" />.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an <see cref="IGrouping{T1,T2}" />.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <returns>A collection of elements of type <typeparamref name="TResult" /> where each element represents a projection over a group and its key.</returns>
        /// <remarks>In query expression syntax, a `group by` (Visual C#) or `Group By Into` (Visual Basic) clause translates to an invocation of <see cref="O:Enumerable.GroupBy" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="GroupBy{T1,T2,T3,T4}(IEnumerable{T1},Func{T1,T2},Func{T1,T3},Func{T2,IEnumerable{T3},T4})" /> to group the projected elements of a sequence and then project a sequence of results of type <typeparamref name="TResult" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet125":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet125":::</example>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/group-clause">group clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/group-by-clause">Group By Clause (Visual Basic)</related>
        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector) =>
            new GroupedResultEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, null);

        /// <summary>Groups the elements of a sequence according to a specified key selector function and creates a result value from each group and its key. The keys are compared by using a specified comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}" /> to compare keys with.</param>
        /// <returns>A collection of elements of type <typeparamref name="TResult" /> where each element represents a projection over a group and its key.</returns>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/group-clause">group clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/group-by-clause">Group By Clause (Visual Basic)</related>
        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey>? comparer) =>
            new GroupedResultEnumerable<TSource, TKey, TResult>(source, keySelector, resultSelector, comparer);

        /// <summary>Groups the elements of a sequence according to a specified key selector function and creates a result value from each group and its key. Key values are compared by using a specified comparer, and the elements of each group are projected by using a specified function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each <see cref="IGrouping{T1,T2}" />.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an <see cref="IGrouping{T1,T2}" />.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}" /> to compare keys with.</param>
        /// <returns>A collection of elements of type <typeparamref name="TResult" /> where each element represents a projection over a group and its key.</returns>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/group-clause">group clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/group-by-clause">Group By Clause (Visual Basic)</related>
        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey>? comparer) =>
            new GroupedResultEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, comparer);
    }

    /// <summary>Represents a collection of objects that have a common key.</summary>
    /// <typeparam name="TKey">The type of the key of the <see cref="IGrouping{T1,T2}" />.</typeparam>
    /// <typeparam name="TElement">The type of the values in the <see cref="IGrouping{T1,T2}" />.</typeparam>
    /// <remarks>
    /// <para>An <see cref="IGrouping{T1,T2}" /> is an <see cref="IEnumerable{T}" /> that additionally has a key. The key represents the attribute that is common to each value in the <see cref="IGrouping{T1,T2}" />.</para>
    /// <para>The values of an <see cref="IGrouping{T1,T2}" /> are accessed much as the elements of an <see cref="IEnumerable{T}" /> are accessed. For example, you can access the values by using a `foreach` in Visual C# or `For Each` in Visual Basic loop to iterate through the <see cref="IGrouping{T1,T2}" /> object. The Example section contains a code example that shows you how to access both the key and the values of an <see cref="IGrouping{T1,T2}" /> object.</para>
    /// <para>The <see cref="IGrouping{T1,T2}" /> type is used by the <see cref="O:Enumerable.GroupBy" /> standard query operator methods, which return a sequence of elements of type <see cref="IGrouping{T1,T2}" />.</para>
    /// </remarks>
    /// <example>The following example demonstrates how to work with an <see cref="IGrouping{T1,T2}" /> object.
    /// In this example, <see cref="Enumerable.GroupBy{T1,T2}(IEnumerable{T1},Func{T1,T2})" /> is called on the array of <see cref="System.Reflection.MemberInfo" /> objects returned by <see cref="O:System.Type.GetMembers" />. <see cref="Enumerable.GroupBy{T1,T2}(IEnumerable{T1},Func{T1,T2})" /> groups the objects based on the value of their <see cref="O:System.Reflection.MemberInfo.MemberType" /> property. Each unique value for <see cref="O:System.Reflection.MemberInfo.MemberType" /> in the array of <see cref="System.Reflection.MemberInfo" /> objects becomes a key for a new <see cref="IGrouping{T1,T2}" /> object, and the <see cref="System.Reflection.MemberInfo" /> objects that have that key form the <see cref="IGrouping{T1,T2}" /> object's sequence of values.
    /// Finally, the <see cref="O:Enumerable.First" /> method is called on the sequence of <see cref="IGrouping{T1,T2}" /> objects to obtain just the first <see cref="IGrouping{T1,T2}" /> object.
    /// The example then outputs the key of the <see cref="IGrouping{T1,T2}" /> object and the <see cref="O:System.Reflection.MemberInfo.Name" /> property of each value in the <see cref="IGrouping{T1,T2}" /> object's sequence of values. Notice that to access an <see cref="IGrouping{T1,T2}" /> object's sequence of values, you simply use the <see cref="IGrouping{T1,T2}" /> variable itself.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.IGrouping/CS/igrouping.cs" interactive="try-dotnet-method" id="Snippet1":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.IGrouping/VB/IGrouping.vb" id="Snippet1":::</example>
    public interface IGrouping<out TKey, out TElement> : IEnumerable<TElement>
    {
        /// <summary>Gets the key of the <see cref="IGrouping{T1,T2}" />.</summary>
        /// <value>The key of the <see cref="IGrouping{T1,T2}" />.</value>
        /// <remarks>The key of an <see cref="IGrouping{T1,T2}" /> represents the attribute that is common to each value in the <see cref="IGrouping{T1,T2}" />.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="O:IGrouping{T1,T2}.Key" /> property to label each <see cref="IGrouping{T1,T2}" /> object in a sequence of <see cref="IGrouping{T1,T2}" /> objects. The <see cref="Enumerable.GroupBy{T1,T2}(IEnumerable{T1},Func{T1,T2})" /> method is used to obtain a sequence of <see cref="IGrouping{T1,T2}" /> objects. The `foreach` in Visual C# or `For Each` in Visual Basic loop then iterates through each <see cref="IGrouping{T1,T2}" /> object, outputting its key and the number of values it contains.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.IGrouping/CS/igrouping.cs" interactive="try-dotnet-method" id="Snippet2":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.IGrouping/VB/IGrouping.vb" id="Snippet2":::</example>
        TKey Key { get; }
    }

    // It is (unfortunately) common to databind directly to Grouping.Key.
    // Because of this, we have to declare this internal type public so that we
    // can mark the Key property for public reflection.
    //
    // To limit the damage, the toolchain makes this type appear in a hidden assembly.
    // (This is also why it is no longer a nested type of Lookup<,>).
    [DebuggerDisplay("Key = {Key}")]
    [DebuggerTypeProxy(typeof(SystemLinq_GroupingDebugView<,>))]
    public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>, IList<TElement>
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
            for (int i = 0; i < _count; i++)
            {
                yield return _elements[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // DDB195907: implement IGrouping<>.Key implicitly
        // so that WPF binding works on this property.
        public TKey Key => _key;

        int ICollection<TElement>.Count => _count;

        bool ICollection<TElement>.IsReadOnly => true;

        void ICollection<TElement>.Add(TElement item) => ThrowHelper.ThrowNotSupportedException();

        void ICollection<TElement>.Clear() => ThrowHelper.ThrowNotSupportedException();

        bool ICollection<TElement>.Contains(TElement item) => Array.IndexOf(_elements, item, 0, _count) >= 0;

        void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex) =>
            Array.Copy(_elements, 0, array, arrayIndex, _count);

        bool ICollection<TElement>.Remove(TElement item)
        {
            ThrowHelper.ThrowNotSupportedException();
            return false;
        }

        int IList<TElement>.IndexOf(TElement item) => Array.IndexOf(_elements, item, 0, _count);

        void IList<TElement>.Insert(int index, TElement item) => ThrowHelper.ThrowNotSupportedException();

        void IList<TElement>.RemoveAt(int index) => ThrowHelper.ThrowNotSupportedException();

        TElement IList<TElement>.this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
                }

                return _elements[index];
            }

            set
            {
                ThrowHelper.ThrowNotSupportedException();
            }
        }
    }

    internal sealed partial class GroupedResultEnumerable<TSource, TKey, TElement, TResult> : IEnumerable<TResult>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly Func<TSource, TElement> _elementSelector;
        private readonly IEqualityComparer<TKey>? _comparer;
        private readonly Func<TKey, IEnumerable<TElement>, TResult> _resultSelector;

        public GroupedResultEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }
            if (elementSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementSelector);
            }
            if (resultSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            _source = source;
            _keySelector = keySelector;
            _elementSelector = elementSelector;
            _comparer = comparer;
            _resultSelector = resultSelector;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            Lookup<TKey, TElement> lookup = Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer);
            return lookup.ApplyResultSelector(_resultSelector).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal sealed partial class GroupedResultEnumerable<TSource, TKey, TResult> : IEnumerable<TResult>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly IEqualityComparer<TKey>? _comparer;
        private readonly Func<TKey, IEnumerable<TSource>, TResult> _resultSelector;

        public GroupedResultEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }
            if (resultSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            _source = source;
            _keySelector = keySelector;
            _resultSelector = resultSelector;
            _comparer = comparer;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            Lookup<TKey, TSource> lookup = Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer);
            return lookup.ApplyResultSelector(_resultSelector).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal sealed partial class GroupedEnumerable<TSource, TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly Func<TSource, TElement> _elementSelector;
        private readonly IEqualityComparer<TKey>? _comparer;

        public GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }
            if (elementSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementSelector);
            }

            _source = source;
            _keySelector = keySelector;
            _elementSelector = elementSelector;
            _comparer = comparer;
        }

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator() =>
            Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal sealed partial class GroupedEnumerable<TSource, TKey> : IEnumerable<IGrouping<TKey, TSource>>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly IEqualityComparer<TKey>? _comparer;

        public GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            _source = source;
            _keySelector = keySelector;
            _comparer = comparer;
        }

        public IEnumerator<IGrouping<TKey, TSource>> GetEnumerator() =>
            Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
