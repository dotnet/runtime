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
        /// <summary>Bypasses a specified number of elements in a sequence and then returns the remaining elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return elements from.</param>
        /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the elements that occur after the specified index in the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// If <paramref name="source" /> contains fewer than <paramref name="count" /> elements, an empty <see cref="IEnumerable{T}" /> is returned. If <paramref name="count" /> is less than or equal to zero, all elements of <paramref name="source" /> are yielded.
        /// The <see cref="O:Enumerable.Take" /> and <see cref="O:Enumerable.Skip" /> methods are functional complements. Given a sequence `coll` and an integer `n`, concatenating the results of `coll.Take(n)` and `coll.Skip(n)` yields the same sequence as `coll`.
        /// In Visual Basic query expression syntax, a `Skip` clause translates to an invocation of <see cref="O:Enumerable.Skip" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:Enumerable.Skip" /> to skip a specified number of elements in a sorted array and return the remaining elements.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet87":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet87":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/skip-clause">Skip Clause (Visual Basic)</related>
        public static IEnumerable<TSource> Skip<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (count <= 0)
            {
                // Return source if not actually skipping, but only if it's a type from here, to avoid
                // issues if collections are used as keys or otherwise must not be aliased.
                if (source is Iterator<TSource> || source is IPartition<TSource>)
                {
                    return source;
                }

                count = 0;
            }
            else if (source is IPartition<TSource> partition)
            {
                return partition.Skip(count);
            }

            return SkipIterator(source, count);
        }

        /// <summary>Bypasses elements in a sequence as long as a specified condition is true and then returns the remaining elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>The <see cref="SkipWhile{T}(IEnumerable{T},Func{T,bool})" /> method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// This method tests each element of <paramref name="source" /> by using <paramref name="predicate" /> and skips the element if the result is <see langword="true" />. After the predicate function returns <see langword="false" /> for an element, that element and the remaining elements in <paramref name="source" /> are yielded and there are no more invocations of <paramref name="predicate" />.
        /// If <paramref name="predicate" /> returns <see langword="true" /> for all elements in the sequence, an empty <see cref="IEnumerable{T}" /> is returned.
        /// The <see cref="O:Enumerable.TakeWhile" /> and <see cref="O:Enumerable.SkipWhile" /> methods are functional complements. Given a sequence `coll` and a pure function `p`, concatenating the results of `coll.TakeWhile(p)` and `coll.SkipWhile(p)` yields the same sequence as `coll`.
        /// In Visual Basic query expression syntax, a `Skip While` clause translates to an invocation of <see cref="O:Enumerable.SkipWhile" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="SkipWhile{T}(IEnumerable{T},Func{T,bool})" /> to skip elements of an array as long as a condition is true.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet88":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet88":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/skip-while-clause">Skip While Clause (Visual Basic)</related>
        public static IEnumerable<TSource> SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            return SkipWhileIterator(source, predicate);
        }

        private static IEnumerable<TSource> SkipWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    TSource element = e.Current;
                    if (!predicate(element))
                    {
                        yield return element;
                        while (e.MoveNext())
                        {
                            yield return e.Current;
                        }

                        yield break;
                    }
                }
            }
        }

        /// <summary>Bypasses elements in a sequence as long as a specified condition is true and then returns the remaining elements. The element's index is used in the logic of the predicate function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return elements from.</param>
        /// <param name="predicate">A function to test each source element for a condition; the second parameter of the function represents the index of the source element.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The <see cref="SkipWhile{T}(IEnumerable{T},Func{T,int,bool})" /> method tests each element of <paramref name="source" /> by using <paramref name="predicate" /> and skips the element if the result is <see langword="true" />. After the predicate function returns <see langword="false" /> for an element, that element and the remaining elements in <paramref name="source" /> are yielded and there are no more invocations of <paramref name="predicate" />.
        /// If <paramref name="predicate" /> returns <see langword="true" /> for all elements in the sequence, an empty <see cref="IEnumerable{T}" /> is returned.
        /// The first argument of <paramref name="predicate" /> represents the element to test. The second argument represents the zero-based index of the element within <paramref name="source" />.
        /// The <see cref="O:Enumerable.TakeWhile" /> and <see cref="O:Enumerable.SkipWhile" /> methods are functional complements. Given a sequence `coll` and a pure function `p`, concatenating the results of `coll.TakeWhile(p)` and `coll.SkipWhile(p)` yields the same sequence as `coll`.
        /// In Visual Basic query expression syntax, a `Skip While` clause translates to an invocation of <see cref="O:Enumerable.SkipWhile" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="SkipWhile{T}(IEnumerable{T},Func{T,int,bool})" /> to skip elements of an array as long as a condition that depends on the element's index is true.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet89":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet89":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/skip-while-clause">Skip While Clause (Visual Basic)</related>
        public static IEnumerable<TSource> SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            return SkipWhileIterator(source, predicate);
        }

        private static IEnumerable<TSource> SkipWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                int index = -1;
                while (e.MoveNext())
                {
                    checked
                    {
                        index++;
                    }

                    TSource element = e.Current;
                    if (!predicate(element, index))
                    {
                        yield return element;
                        while (e.MoveNext())
                        {
                            yield return e.Current;
                        }

                        yield break;
                    }
                }
            }
        }

        /// <summary>Returns a new enumerable collection that contains the elements from <paramref name="source" /> with the last <paramref name="count" /> elements of the source collection omitted.</summary>
        /// <typeparam name="TSource">The type of the elements in the enumerable collection.</typeparam>
        /// <param name="source">An enumerable collection instance.</param>
        /// <param name="count">The number of elements to omit from the end of the collection.</param>
        /// <returns>A new enumerable collection that contains the elements from <paramref name="source" /> minus <paramref name="count" /> elements from the end of the collection.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>If <paramref name="count" /> is not a positive number, this method returns an identical copy of the <paramref name="source" /> enumerable collection.</remarks>
        public static IEnumerable<TSource> SkipLast<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return count <= 0 ?
                source.Skip(0) :
                TakeRangeFromEndIterator(source,
                    isStartIndexFromEnd: false, startIndex: 0,
                    isEndIndexFromEnd: true, endIndex: count);
        }
    }
}
