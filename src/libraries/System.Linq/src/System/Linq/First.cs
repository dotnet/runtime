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
        /// <summary>Returns the first element of a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}" /> to return the first element of.</param>
        /// <returns>The first element in the specified sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The source sequence is empty.</exception>
        /// <remarks>The <see cref="First{T}(IEnumerable{T})" /> method throws an exception if <paramref name="source" /> contains no elements. To instead return a default value when the source sequence is empty, use the <see cref="O:Enumerable.FirstOrDefault" /> method.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="First{T}(IEnumerable{T})" /> to return the first element of an array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet35":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet35":::</example>
        public static TSource First<TSource>(this IEnumerable<TSource> source)
        {
            TSource? first = source.TryGetFirst(out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoElementsException();
            }

            return first!;
        }

        /// <summary>Returns the first element in a sequence that satisfies a specified condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>The first element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">No element satisfies the condition in <paramref name="predicate" />.
        /// -or-
        /// The source sequence is empty.</exception>
        /// <remarks>The <see cref="First{T}(IEnumerable{T},Func{T,bool})" /> method throws an exception if no matching element is found in <paramref name="source" />. To instead return a default value when no matching element is found, use the <see cref="O:Enumerable.FirstOrDefault" /> method.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="First{T}(IEnumerable{T},Func{T,bool})" /> to return the first element of an array that satisfies a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet36":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet36":::</example>
        public static TSource First<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            TSource? first = source.TryGetFirst(predicate, out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoMatchException();
            }

            return first!;
        }

        /// <summary>Returns the first element of a sequence, or a default value if the sequence contains no elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}" /> to return the first element of.</param>
        /// <returns><see langword="default" />(<typeparamref name="TSource" />) if <paramref name="source" /> is empty; otherwise, the first element in <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>The default value for reference and nullable types is <see langword="null" />.
        /// The <see cref="O:Enumerable.FirstOrDefault" /> method does not provide a way to specify a default value. If you want to specify a default value other than `default(TSource)`, use the <see cref="DefaultIfEmpty{T}(IEnumerable{T},T)" /> method as described in the Example section.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="FirstOrDefault{T}(IEnumerable{T})" /> on an empty array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet37":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet37":::
        /// Sometimes the value of `default(TSource)` is not the default value that you want to use if the collection contains no elements. Instead of checking the result for the unwanted default value and then changing it if necessary, you can use the <see cref="DefaultIfEmpty{T}(IEnumerable{T},T)" /> method to specify the default value that you want to use if the collection is empty. Then, call <see cref="First{T}(IEnumerable{T})" /> to obtain the first element. The following code example uses both techniques to obtain a default value of 1 if a collection of numeric months is empty. Because the default value for an integer is 0, which does not correspond to any month, the default value must be specified as 1 instead. The first result variable is checked for the unwanted default value after the query has finished executing. The second result variable is obtained by using <see cref="DefaultIfEmpty{T}(IEnumerable{T},T)" /> to specify a default value of 1.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet126":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet126":::</example>
        public static TSource? FirstOrDefault<TSource>(this IEnumerable<TSource> source) =>
            source.TryGetFirst(out _);

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            TSource? first = source.TryGetFirst(out bool found);
            return found ? first! : defaultValue;
        }

        /// <summary>Returns the first element of the sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns><see langword="default" />(<typeparamref name="TSource" />) if <paramref name="source" /> is empty or if no element passes the test specified by <paramref name="predicate" />; otherwise, the first element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>The default value for reference and nullable types is <see langword="null" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="FirstOrDefault{T}(IEnumerable{T},Func{T,bool})" /> by passing in a predicate. In the second call to the method, there is no element in the array that satisfies the condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet38":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet38":::</example>
        public static TSource? FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) =>
            source.TryGetFirst(predicate, out _);

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
        {
            TSource? first = source.TryGetFirst(predicate, out bool found);
            return found ? first! : defaultValue;
        }


        private static TSource? TryGetFirst<TSource>(this IEnumerable<TSource> source, out bool found)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IPartition<TSource> partition)
            {
                return partition.TryGetFirst(out found);
            }

            if (source is IList<TSource> list)
            {
                if (list.Count > 0)
                {
                    found = true;
                    return list[0];
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        found = true;
                        return e.Current;
                    }
                }
            }

            found = false;
            return default;
        }

        private static TSource? TryGetFirst<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
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
                    found = true;
                    return element;
                }
            }

            found = false;
            return default;
        }
    }
}
