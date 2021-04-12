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
        /// <summary>Returns a specified number of contiguous elements from the start of a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="count">The number of elements to return.</param>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains the specified number of elements from the start of the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// <see cref="O:System.Linq.Enumerable.Take" /> enumerates <paramref name="source" /> and yields elements until <paramref name="count" /> elements have been yielded or <paramref name="source" /> contains no more elements. If <paramref name="count" /> exceeds the number of elements in <paramref name="source" />, all elements of <paramref name="source" /> are returned.
        /// If <paramref name="count" /> is less than or equal to zero, <paramref name="source" /> is not enumerated and an empty <see cref="System.Collections.Generic.IEnumerable{T}" /> is returned.
        /// The <see cref="O:System.Linq.Enumerable.Take" /> and <see cref="O:System.Linq.Enumerable.Skip" /> methods are functional complements. Given a sequence `coll` and an integer `n`, concatenating the results of `coll.Take(n)` and `coll.Skip(n)` yields the same sequence as `coll`.
        /// In Visual Basic query expression syntax, a `Take` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.Take" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:System.Linq.Enumerable.Take" /> to return elements from the start of a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet99":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet99":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/take-clause">Take Clause (Visual Basic)</related>
        public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return count <= 0 ?
                Empty<TSource>() :
                TakeIterator<TSource>(source, count);
        }

        /// <summary>Returns a specified range of contiguous elements from a sequence.</summary>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="range">The range of elements to return, which has start and end indexes either from the start or the end.</param>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the specified <paramref name="range" /> of elements from the <paramref name="source" /> sequence.</returns>
        public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, Range range)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            Index start = range.Start;
            Index end = range.End;
            bool isStartIndexFromEnd = start.IsFromEnd;
            bool isEndIndexFromEnd = end.IsFromEnd;
            int startIndex = start.Value;
            int endIndex = end.Value;
            Debug.Assert(startIndex >= 0);
            Debug.Assert(endIndex >= 0);

            if (isStartIndexFromEnd)
            {
                if (startIndex == 0 || (isEndIndexFromEnd && endIndex >= startIndex))
                {
                    return Empty<TSource>();
                }
            }
            else if (!isEndIndexFromEnd)
            {
                return startIndex >= endIndex
                    ? Empty<TSource>()
                    : TakeRangeIterator(source, startIndex, endIndex);
            }

            return TakeRangeFromEndIterator(source, isStartIndexFromEnd, startIndex, isEndIndexFromEnd, endIndex);
        }

        private static IEnumerable<TSource> TakeRangeFromEndIterator<TSource>(IEnumerable<TSource> source, bool isStartIndexFromEnd, int startIndex, bool isEndIndexFromEnd, int endIndex)
        {
            Debug.Assert(source != null);
            Debug.Assert(isStartIndexFromEnd || isEndIndexFromEnd);
            Debug.Assert(isStartIndexFromEnd
                ? startIndex > 0 && (!isEndIndexFromEnd || startIndex > endIndex)
                : startIndex >= 0 && (isEndIndexFromEnd || startIndex < endIndex));

            // Attempt to extract the count of the source enumerator,
            // in order to convert fromEnd indices to regular indices.
            // Enumerable counts can change over time, so it is very
            // important that this check happens at enumeration time;
            // do not move it outside of the iterator method.
            if (source.TryGetNonEnumeratedCount(out int count))
            {
                startIndex = CalculateStartIndex(isStartIndexFromEnd, startIndex, count);
                endIndex = CalculateEndIndex(isEndIndexFromEnd, endIndex, count);

                if (startIndex < endIndex)
                {
                    foreach (TSource element in TakeRangeIterator(source, startIndex, endIndex))
                    {
                        yield return element;
                    }
                }

                yield break;
            }

            Queue<TSource> queue;

            if (isStartIndexFromEnd)
            {
                // TakeLast compat: enumerator should be disposed before yielding the first element.
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (!e.MoveNext())
                    {
                        yield break;
                    }

                    queue = new Queue<TSource>();
                    queue.Enqueue(e.Current);
                    count = 1;

                    while (e.MoveNext())
                    {
                        if (count < startIndex)
                        {
                            queue.Enqueue(e.Current);
                            ++count;
                        }
                        else
                        {
                            do
                            {
                                queue.Dequeue();
                                queue.Enqueue(e.Current);
                                checked { ++count; }
                            } while (e.MoveNext());
                            break;
                        }
                    }

                    Debug.Assert(queue.Count == Math.Min(count, startIndex));
                }

                startIndex = CalculateStartIndex(isStartIndexFromEnd: true, startIndex, count);
                endIndex = CalculateEndIndex(isEndIndexFromEnd, endIndex, count);
                Debug.Assert(endIndex - startIndex <= queue.Count);

                for (int rangeIndex = startIndex; rangeIndex < endIndex; rangeIndex++)
                {
                    yield return queue.Dequeue();
                }
            }
            else
            {
                Debug.Assert(!isStartIndexFromEnd && isEndIndexFromEnd);

                // SkipLast compat: the enumerator should be disposed at the end of the enumeration.
                using IEnumerator<TSource> e = source.GetEnumerator();

                count = 0;
                while (count < startIndex && e.MoveNext())
                {
                    ++count;
                }

                if (count == startIndex)
                {
                    queue = new Queue<TSource>();
                    while (e.MoveNext())
                    {
                        if (queue.Count == endIndex)
                        {
                            do
                            {
                                queue.Enqueue(e.Current);
                                yield return queue.Dequeue();
                            } while (e.MoveNext());

                            break;
                        }
                        else
                        {
                            queue.Enqueue(e.Current);
                        }
                    }
                }
            }

            static int CalculateStartIndex(bool isStartIndexFromEnd, int startIndex, int count) =>
                Math.Max(0, isStartIndexFromEnd ? count - startIndex : startIndex);

            static int CalculateEndIndex(bool isEndIndexFromEnd, int endIndex, int count) =>
                Math.Min(count, isEndIndexFromEnd ? count - endIndex : endIndex);
        }

        /// <summary>Returns elements from a sequence as long as a specified condition is true.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains the elements from the input sequence that occur before the element at which the test no longer passes.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The <see cref="System.Linq.Enumerable.TakeWhile{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,bool})" /> method tests each element of <paramref name="source" /> by using <paramref name="predicate" /> and yields the element if the result is <see langword="true" />. Enumeration stops when the predicate function returns <see langword="false" /> for an element or when <paramref name="source" /> contains no more elements.
        /// The <see cref="O:System.Linq.Enumerable.TakeWhile" /> and <see cref="O:System.Linq.Enumerable.SkipWhile" /> methods are functional complements. Given a sequence `coll` and a pure function `p`, concatenating the results of `coll.TakeWhile(p)` and `coll.SkipWhile(p)` yields the same sequence as `coll`.
        /// In Visual Basic query expression syntax, a `Take While` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.TakeWhile" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.TakeWhile{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,bool})" /> to return elements from the start of a sequence as long as a condition is true.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet100":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet100":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/take-while-clause">Take While Clause (Visual Basic)</related>
        public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            return TakeWhileIterator(source, predicate);
        }

        private static IEnumerable<TSource> TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            foreach (TSource element in source)
            {
                if (!predicate(element))
                {
                    break;
                }

                yield return element;
            }
        }

        /// <summary>Returns elements from a sequence as long as a specified condition is true. The element's index is used in the logic of the predicate function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="predicate">A function to test each source element for a condition; the second parameter of the function represents the index of the source element.</param>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains elements from the input sequence that occur before the element at which the test no longer passes.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The <see cref="System.Linq.Enumerable.TakeWhile{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,int,bool})" /> method tests each element of <paramref name="source" /> by using <paramref name="predicate" /> and yields the element if the result is <see langword="true" />. Enumeration stops when the predicate function returns <see langword="false" /> for an element or when <paramref name="source" /> contains no more elements.
        /// The first argument of <paramref name="predicate" /> represents the element to test. The second argument represents the zero-based index of the element within <paramref name="source" />.
        /// The <see cref="O:System.Linq.Enumerable.TakeWhile" /> and <see cref="O:System.Linq.Enumerable.SkipWhile" /> methods are functional complements. Given a sequence `coll` and a pure function `p`, concatenating the results of `coll.TakeWhile(p)` and `coll.SkipWhile(p)` yields the same sequence as `coll`.
        /// In Visual Basic query expression syntax, a `Take While` clause translates to an invocation of <see cref="O:System.Linq.Enumerable.TakeWhile" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.TakeWhile{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,int,bool})" /> to return elements from the start of a sequence as long as a condition that uses the element's index is true.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet101":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet101":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/take-while-clause">Take While Clause (Visual Basic)</related>
        public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            return TakeWhileIterator(source, predicate);
        }

        private static IEnumerable<TSource> TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked
                {
                    index++;
                }

                if (!predicate(element, index))
                {
                    break;
                }

                yield return element;
            }
        }

        /// <summary>Returns a new enumerable collection that contains the last <paramref name="count" /> elements from <paramref name="source" />.</summary>
        /// <typeparam name="TSource">The type of the elements in the enumerable collection.</typeparam>
        /// <param name="source">An enumerable collection instance.</param>
        /// <param name="count">The number of elements to take from the end of the collection.</param>
        /// <returns>A new enumerable collection that contains the last <paramref name="count" /> elements from <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>If <paramref name="count" /> is not a positive number, this method returns an empty enumerable collection.</remarks>
        public static IEnumerable<TSource> TakeLast<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return count <= 0 ?
                Empty<TSource>() :
                TakeRangeFromEndIterator(source,
                    isStartIndexFromEnd: true, startIndex: count,
                    isEndIndexFromEnd: true, endIndex: 0);
        }
    }
}
