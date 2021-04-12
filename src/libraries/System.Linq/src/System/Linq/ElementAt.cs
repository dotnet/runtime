// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
        /// <summary>Returns the element at a specified index in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> to return an element from.</param>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <returns>The element at the specified position in the source sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index" /> is less than 0 or greater than or equal to the number of elements in <paramref name="source" />.</exception>
        /// <remarks>If the type of <paramref name="source" /> implements <see cref="System.Collections.Generic.IList{T}" />, that implementation is used to obtain the element at the specified index. Otherwise, this method obtains the specified element.
        /// This method throws an exception if <paramref name="index" /> is out of range. To instead return a default value when the specified index is out of range, use the <see cref="O:System.Linq.Enumerable.ElementAtOrDefault" /> method.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:System.Linq.Enumerable.ElementAt" /> to return an element at a specific position.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet28":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet28":::</example>
        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IPartition<TSource> partition)
            {
                TSource? element = partition.TryGetElementAt(index, out bool found);
                if (found)
                {
                    return element!;
                }
            }
            else if (source is IList<TSource> list)
            {
                return list[index];
            }
            else if (TryGetElement(source, index, out TSource? element))
            {
                return element;
            }

            ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            return default;
        }

        /// <summary>Returns the element at a specified index in a sequence.</summary>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return an element from.</param>
        /// <param name="index">The index of the element to retrieve, which is either from the start or the end.</param>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="index" /> is outside the bounds of the <paramref name="source" /> sequence.
        /// </exception>
        /// <returns>The element at the specified position in the <paramref name="source" /> sequence.</returns>
        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, Index index)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (!index.IsFromEnd)
            {
                return source.ElementAt(index.Value);
            }

            if (source.TryGetNonEnumeratedCount(out int count))
            {
                return source.ElementAt(count - index.Value);
            }

            if (!TryGetElementFromEnd(source, index.Value, out TSource? element))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return element;
        }

        /// <summary>Returns the element at a specified index in a sequence or a default value if the index is out of range.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> to return an element from.</param>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <returns><see langword="default" />(<typeparamref name="TSource" />) if the index is outside the bounds of the source sequence; otherwise, the element at the specified position in the source sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>If the type of <paramref name="source" /> implements <see cref="System.Collections.Generic.IList{T}" />, that implementation is used to obtain the element at the specified index. Otherwise, this method obtains the specified element.
        /// The default value for reference and nullable types is <see langword="null" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:System.Linq.Enumerable.ElementAtOrDefault" />. This example uses an index that is outside the bounds of the array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet29":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet29":::</example>
        public static TSource? ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IPartition<TSource> partition)
            {
                return partition.TryGetElementAt(index, out bool _);
            }

            if (source is IList<TSource> list)
            {
                return index >= 0 && index < list.Count ? list[index] : default;
            }

            TryGetElement(source, index, out TSource? element);
            return element;

        }

        /// <summary>Returns the element at a specified index in a sequence or a default value if the index is out of range.</summary>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return an element from.</param>
        /// <param name="index">The index of the element to retrieve, which is either from the start or the end.</param>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <returns>
        ///   <see langword="default" /> if <paramref name="index" /> is outside the bounds of the <paramref name="source" /> sequence; otherwise, the element at the specified position in the <paramref name="source" /> sequence.
        /// </returns>
        public static TSource? ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, Index index)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (!index.IsFromEnd)
            {
                return source.ElementAtOrDefault(index.Value);
            }

            if (source.TryGetNonEnumeratedCount(out int count))
            {
                return source.ElementAtOrDefault(count - index.Value);
            }

            TryGetElementFromEnd(source, index.Value, out TSource? element);
            return element;
        }

        private static bool TryGetElement<TSource>(IEnumerable<TSource> source, int index, [MaybeNullWhen(false)] out TSource element)
        {
            Debug.Assert(source != null);

            if (index >= 0)
            {
                using IEnumerator<TSource> e = source.GetEnumerator();
                while (e.MoveNext())
                {
                    if (index == 0)
                    {
                        element = e.Current;
                        return true;
                    }

                    index--;
                }
            }

            element = default;
            return false;
        }

        private static bool TryGetElementFromEnd<TSource>(IEnumerable<TSource> source, int indexFromEnd, [MaybeNullWhen(false)] out TSource element)
        {
            Debug.Assert(source != null);

            if (indexFromEnd > 0)
            {
                using IEnumerator<TSource> e = source.GetEnumerator();
                if (e.MoveNext())
                {
                    Queue<TSource> queue = new();
                    queue.Enqueue(e.Current);
                    while (e.MoveNext())
                    {
                        if (queue.Count == indexFromEnd)
                        {
                            queue.Dequeue();
                        }

                        queue.Enqueue(e.Current);
                    }

                    if (queue.Count == indexFromEnd)
                    {
                        element = queue.Dequeue();
                        return true;
                    }
                }
            }

            element = default;
            return false;
        }
    }
}
