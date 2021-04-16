// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
        /// <summary>Returns the last element of a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return the last element of.</param>
        /// <returns>The value at the last position in the source sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The source sequence is empty.</exception>
        /// <remarks>The <see cref="Last{T}(IEnumerable{T})" /> method throws an exception if <paramref name="source" /> contains no elements. To instead return a default value when the source sequence is empty, use the <see cref="O:Enumerable.LastOrDefault" /> method.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="Last{T}(IEnumerable{T})" /> to return the last element of an array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet43":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet43":::</example>
        public static TSource Last<TSource>(this IEnumerable<TSource> source)
        {
            TSource? last = source.TryGetLast(out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoElementsException();
            }

            return last!;
        }

        /// <summary>Returns the last element of a sequence that satisfies a specified condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>The last element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">No element satisfies the condition in <paramref name="predicate" />.
        /// -or-
        /// The source sequence is empty.</exception>
        /// <remarks>The <see cref="Last{T}(IEnumerable{T},Func{T,bool})" /> method throws an exception if no matching element is found in <paramref name="source" />. To instead return a default value when no matching element is found, use the <see cref="O:Enumerable.LastOrDefault" /> method.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="Last{T}(IEnumerable{T},Func{T,bool})" /> to return the last element of an array that satisfies a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet44":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet44":::</example>
        public static TSource Last<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            TSource? last = source.TryGetLast(predicate, out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoMatchException();
            }

            return last!;
        }

        /// <summary>Returns the last element of a sequence, or a default value if the sequence contains no elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return the last element of.</param>
        /// <returns><see langword="default" />(<typeparamref name="TSource" />) if the source sequence is empty; otherwise, the last element in the <see cref="IEnumerable{T}" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>The default value for reference and nullable types is <see langword="null" />.
        /// The <see cref="O:Enumerable.LastOrDefault" /> method does not provide a way to specify a default value. If you want to specify a default value other than `default(TSource)`, use the <see cref="DefaultIfEmpty{T}(IEnumerable{T},T)" /> method as described in the Example section.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="LastOrDefault{T}(IEnumerable{T})" /> on an empty array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet45":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet45":::
        /// Sometimes the value of `default(TSource)` is not the default value that you want to use if the collection contains no elements. Instead of checking the result for the unwanted default value and then changing it if necessary, you can use the <see cref="DefaultIfEmpty{T}(IEnumerable{T},T)" /> method to specify the default value that you want to use if the collection is empty. Then, call <see cref="Last{T}(IEnumerable{T})" /> to obtain the last element. The following code example uses both techniques to obtain a default value of 1 if a collection of numeric days of the month is empty. Because the default value for an integer is 0, which does not correspond to any day of the month, the default value must be specified as 1 instead. The first result variable is checked for the unwanted default value after the query has finished executing. The second result variable is obtained by using <see cref="DefaultIfEmpty{T}(IEnumerable{T},T)" /> to specify a default value of 1.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet127":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet127":::</example>
        public static TSource? LastOrDefault<TSource>(this IEnumerable<TSource> source) =>
            source.TryGetLast(out _);


        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            TSource? last = source.TryGetLast(out bool found);
            return found ? last! : defaultValue;
        }

        /// <summary>Returns the last element of a sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns><see langword="default" />(<typeparamref name="TSource" />) if the sequence is empty or if no elements pass the test in the predicate function; otherwise, the last element that passes the test in the predicate function.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>The default value for reference and nullable types is <see langword="null" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="LastOrDefault{T}(IEnumerable{T},Func{T,bool})" /> by passing in a predicate. In the second call to the method, there is no element in the sequence that satisfies the condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet46":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet46":::</example>
        public static TSource? LastOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            => source.TryGetLast(predicate, out _);

        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
        {
            var last = source.TryGetLast(predicate, out bool found);
            return found ? last! : defaultValue;
        }

        private static TSource? TryGetLast<TSource>(this IEnumerable<TSource> source, out bool found)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IPartition<TSource> partition)
            {
                return partition.TryGetLast(out found);
            }

            if (source is IList<TSource> list)
            {
                int count = list.Count;
                if (count > 0)
                {
                    found = true;
                    return list[count - 1];
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        TSource result;
                        do
                        {
                            result = e.Current;
                        }
                        while (e.MoveNext());

                        found = true;
                        return result;
                    }
                }
            }

            found = false;
            return default;
        }

        private static TSource? TryGetLast<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            if (source is OrderedEnumerable<TSource> ordered)
            {
                return ordered.TryGetLast(predicate, out found);
            }

            if (source is IList<TSource> list)
            {
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    TSource result = list[i];
                    if (predicate(result))
                    {
                        found = true;
                        return result;
                    }
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    while (e.MoveNext())
                    {
                        TSource result = e.Current;
                        if (predicate(result))
                        {
                            while (e.MoveNext())
                            {
                                TSource element = e.Current;
                                if (predicate(element))
                                {
                                    result = element;
                                }
                            }

                            found = true;
                            return result;
                        }
                    }
                }
            }

            found = false;
            return default;
        }
    }
}
