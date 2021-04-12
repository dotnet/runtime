// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        /// <summary>Returns the only element of a sequence, and throws an exception if there is not exactly one element in the sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> to return the single element of.</param>
        /// <returns>The single element of the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The input sequence contains more than one element.
        /// -or-
        /// The input sequence is empty.</exception>
        /// <remarks>The <see cref="System.Linq.Enumerable.Single{T}(System.Collections.Generic.IEnumerable{T})" /> method throws an exception if the input sequence is empty. To instead return <see langword="null" /> when the input sequence is empty, use <see cref="O:System.Linq.Enumerable.SingleOrDefault" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.Single{T}(System.Collections.Generic.IEnumerable{T})" /> to select the only element of an array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet79":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet79":::
        /// The following code example demonstrates that <see cref="System.Linq.Enumerable.Single{T}(System.Collections.Generic.IEnumerable{T})" /> throws an exception when the sequence does not contain exactly one element.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet80":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet80":::</example>
        public static TSource Single<TSource>(this IEnumerable<TSource> source)
        {
            TSource? single = source.TryGetSingle(out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoElementsException();
            }

            return single!;
        }
        /// <summary>Returns the only element of a sequence that satisfies a specified condition, and throws an exception if more than one such element exists.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> to return a single element from.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <returns>The single element of the input sequence that satisfies a condition.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">No element satisfies the condition in <paramref name="predicate" />.
        /// -or-
        /// More than one element satisfies the condition in <paramref name="predicate" />.
        /// -or-
        /// The source sequence is empty.</exception>
        /// <remarks>The <see cref="System.Linq.Enumerable.Single{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,bool})" /> method throws an exception if the input sequence contains no matching element. To instead return <see langword="null" /> when no matching element is found, use <see cref="O:System.Linq.Enumerable.SingleOrDefault" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.Single{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,bool})" /> to select the only element of an array that satisfies a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet81":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet81":::
        /// The following code example demonstrates that <see cref="System.Linq.Enumerable.Single{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,bool})" /> throws an exception when the sequence does not contain exactly one element that satisfies the condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet82":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet82":::</example>
        public static TSource Single<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            TSource? single = source.TryGetSingle(predicate, out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoMatchException();
            }

            return single!;
        }

        /// <summary>Returns the only element of a sequence, or a default value if the sequence is empty; this method throws an exception if there is more than one element in the sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> to return the single element of.</param>
        /// <returns>The single element of the input sequence, or <see langword="default" />(<typeparamref name="TSource" />) if the sequence contains no elements.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The input sequence contains more than one element.</exception>
        /// <remarks>The default value for reference and nullable types is <see langword="null" />.
        /// The <see cref="O:System.Linq.Enumerable.SingleOrDefault" /> method does not provide a way to specify a default value. If you want to specify a default value other than `default(TSource)`, use the <see cref="System.Linq.Enumerable.DefaultIfEmpty{T}(System.Collections.Generic.IEnumerable{T},T)" /> method as described in the Example section.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.SingleOrDefault{T}(System.Collections.Generic.IEnumerable{T})" /> to select the only element of an array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet83":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet83":::
        /// The following code example demonstrates that <see cref="System.Linq.Enumerable.SingleOrDefault{T}(System.Collections.Generic.IEnumerable{T})" /> returns a default value when the sequence is empty.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet84":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet84":::
        /// Sometimes the value of `default(TSource)` is not the default value that you want to use if the collection contains no elements. Instead of checking the result for the unwanted default value and then changing it if necessary, you can use the <see cref="System.Linq.Enumerable.DefaultIfEmpty{T}(System.Collections.Generic.IEnumerable{T},T)" /> method to specify the default value that you want to use if the collection is empty. Then, call <see cref="System.Linq.Enumerable.Single{T}(System.Collections.Generic.IEnumerable{T})" /> to obtain the element. The following code example uses both techniques to obtain a default value of 1 if a collection of page numbers is empty. Because the default value for an integer is 0, which is not usually a valid page number, the default value must be specified as 1 instead. The first result variable is checked for the unwanted default value after the query has finished executing. The second result variable is obtained by using <see cref="System.Linq.Enumerable.DefaultIfEmpty{T}(System.Collections.Generic.IEnumerable{T},T)" /> to specify a default value of 1.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet128":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet128":::</example>
        public static TSource? SingleOrDefault<TSource>(this IEnumerable<TSource> source)
            => source.TryGetSingle(out _);

        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            var single = source.TryGetSingle(out bool found);
            return found ? single! : defaultValue;
        }

        /// <summary>Returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; this method throws an exception if more than one element satisfies the condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> to return a single element from.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <returns>The single element of the input sequence that satisfies the condition, or <see langword="default" />(<typeparamref name="TSource" />) if no such element is found.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">More than one element satisfies the condition in <paramref name="predicate" />.</exception>
        /// <remarks>The default value for reference and nullable types is <see langword="null" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.SingleOrDefault{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,bool})" /> to select the only element of an array that satisfies a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet85":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet85":::
        /// The following code example demonstrates that <see cref="System.Linq.Enumerable.SingleOrDefault{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,bool})" /> returns a default value when the sequence contains no elements that satisfy the condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet86":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet86":::</example>
        public static TSource? SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            => source.TryGetSingle(predicate, out _);

        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
        {
            var single = source.TryGetSingle(predicate, out bool found);
            return found ? single! : defaultValue;
        }

        private static TSource? TryGetSingle<TSource>(this IEnumerable<TSource> source, out bool found)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IList<TSource> list)
            {
                switch (list.Count)
                {
                    case 0:
                        found = false;
                        return default;
                    case 1:
                        found = true;
                        return list[0];
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (!e.MoveNext())
                    {
                        found = false;
                        return default;
                    }

                    TSource result = e.Current;
                    if (!e.MoveNext())
                    {
                        found = true;
                        return result;
                    }
                }
            }

            found = false;
            ThrowHelper.ThrowMoreThanOneElementException();
            return default;
        }

        private static TSource? TryGetSingle<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    TSource result = e.Current;
                    if (predicate(result))
                    {
                        while (e.MoveNext())
                        {
                            if (predicate(e.Current))
                            {
                                ThrowHelper.ThrowMoreThanOneMatchException();
                            }
                        }
                        found = true;
                        return result;
                    }
                }
            }

            found = false;
            return default;
        }
    }
}
