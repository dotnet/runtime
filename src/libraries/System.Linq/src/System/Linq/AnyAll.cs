// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
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
        /// <summary>Determines whether a sequence contains any elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="System.Collections.Generic.IEnumerable{T}" /> to check for emptiness.</param>
        /// <returns><see langword="true" /> if the source sequence contains any elements; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  This method does not return any one element of a collection. Instead, it determines whether the collection contains any elements.
        /// ]]></format>
        /// The enumeration of <paramref name="source" /> is stopped as soon as the result can be determined.
        /// In Visual Basic query expression syntax, an `Aggregate Into Any()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Any" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:System.Linq.Enumerable.Any" /> to determine whether a sequence contains any elements.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet5":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet5":::
        /// The Boolean value that the <see cref="System.Linq.Enumerable.Any{T}(System.Collections.Generic.IEnumerable{T})" /> method returns is typically used in the predicate of a `where` clause (`Where` clause in Visual Basic) or a direct call to the <see cref="System.Linq.Enumerable.Where{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,bool})" /> method. The following example demonstrates this use of the `Any` method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet130":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet130":::</example>
        /// <altmember cref="System.Linq.Enumerable.Any{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,bool})"/>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static bool Any<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is ICollection<TSource> collectionoft)
            {
                return collectionoft.Count != 0;
            }
            else if (source is IIListProvider<TSource> listProv)
            {
                // Note that this check differs from the corresponding check in
                // Count (whereas otherwise this method parallels it).  If the count
                // can't be retrieved cheaply, that likely means we'd need to iterate
                // through the entire sequence in order to get the count, and in that
                // case, we'll generally be better off falling through to the logic
                // below that only enumerates at most a single element.
                int count = listProv.GetCount(onlyIfCheap: true);
                if (count >= 0)
                {
                    return count != 0;
                }
            }
            else if (source is ICollection collection)
            {
                return collection.Count != 0;
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                return e.MoveNext();
            }
        }

        /// <summary>Determines whether any element of a sequence satisfies a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> whose elements to apply the predicate to.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns><see langword="true" /> if the source sequence is not empty and at least one of its elements passes the test in the specified predicate; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  This method does not return any one element of a collection. Instead, it determines whether any elements of a collection satisfy a condition.
        /// ]]></format>
        /// The enumeration of <paramref name="source" /> is stopped as soon as the result can be determined.
        /// In Visual Basic query expression syntax, an `Aggregate Into Any()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Any" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:System.Linq.Enumerable.Any" /> to determine whether any element in a sequence satisfies a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet6":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet6":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            foreach (TSource element in source)
            {
                if (predicate(element))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Determines whether all elements of a sequence satisfy a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains the elements to apply the predicate to.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns><see langword="true" /> if every element of the source sequence passes the test in the specified predicate, or if the sequence is empty; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  This method does not return all the elements of a collection. Instead, it determines whether all the elements of a collection satisfy a condition.
        /// ]]></format>
        /// The enumeration of <paramref name="source" /> is stopped as soon as the result can be determined.
        /// In Visual Basic query expression syntax, an `Aggregate Into All()` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.All" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:System.Linq.Enumerable.All" /> to determine whether all the elements in a sequence satisfy a condition. Variable `allStartWithB` is true if all the pet names start with "B" or if the `pets` array is empty.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet4":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet4":::
        /// The Boolean value that the <see cref="O:System.Linq.Enumerable.All" /> method returns is typically used in the predicate of a `where` clause (`Where` clause in Visual Basic) or a direct call to the <see cref="O:System.Linq.Enumerable.Where" /> method. The following example demonstrates this use of the `All` method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet129":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet129":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static bool All<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            foreach (TSource element in source)
            {
                if (!predicate(element))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
