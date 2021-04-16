// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Returns the number of elements in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence that contains elements to be counted.</param>
        /// <returns>The number of elements in the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The number of elements in <paramref name="source" /> is larger than <see cref="int.MaxValue" />.</exception>
        /// <remarks>
        /// <para>If the type of <paramref name="source" /> implements <see cref="ICollection{T}" />, that implementation is used to obtain the count of elements. Otherwise, this method determines the count.</para>
        /// <para>Use the <see cref="O:Enumerable.LongCount" /> method when you expect and want to allow the result to be greater than <see cref="int.MaxValue" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Count()` clause translates to an invocation of <see cref="O:Enumerable.Count" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Count{T}(IEnumerable{T})" /> to count the elements in an array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet22":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet22":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static int Count<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is ICollection<TSource> collectionoft)
            {
                return collectionoft.Count;
            }

            if (source is IIListProvider<TSource> listProv)
            {
                return listProv.GetCount(onlyIfCheap: false);
            }

            if (source is ICollection collection)
            {
                return collection.Count;
            }

            int count = 0;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                checked
                {
                    while (e.MoveNext())
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>Returns a number that represents how many elements in the specified sequence satisfy a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence that contains elements to be tested and counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A number that represents how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The number of elements in <paramref name="source" /> is larger than <see cref="int.MaxValue" />.</exception>
        /// <remarks>
        /// <para>If the type of <paramref name="source" /> implements <see cref="ICollection{T}" />, that implementation is used to obtain the count of elements. Otherwise, this method determines the count.</para>
        /// <para>You should use the <see cref="O:Enumerable.LongCount" /> method when you expect and want to allow the result to be greater than <see cref="int.MaxValue" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into Count()` clause translates to an invocation of <see cref="O:Enumerable.Count" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Count{T}(IEnumerable{T},Func{T,bool})" /> to count the elements in an array that satisfy a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet23":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet23":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static int Count<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            int count = 0;
            foreach (TSource element in source)
            {
                checked
                {
                    if (predicate(element))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        ///   Attempts to determine the number of elements in a sequence without forcing an enumeration.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence that contains elements to be counted.</param>
        /// <param name="count">
        ///     When this method returns, contains the count of <paramref name="source" /> if successful,
        ///     or zero if the method failed to determine the count.</param>
        /// <returns>
        ///   <see langword="true" /> if the count of <paramref name="source"/> can be determined without enumeration;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// <para>
        ///   The method performs a series of type tests, identifying common subtypes whose
        ///   count can be determined without enumerating; this includes <see cref="ICollection{T}"/>,
        ///   <see cref="ICollection"/> as well as internal types used in the LINQ implementation.
        /// </para>
        /// <para>
        ///   The method is typically a constant-time operation, but ultimately this depends on the complexity
        ///   characteristics of the underlying collection implementation.
        /// </para>
        /// </remarks>
        public static bool TryGetNonEnumeratedCount<TSource>(this IEnumerable<TSource> source, out int count)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is ICollection<TSource> collectionoft)
            {
                count = collectionoft.Count;
                return true;
            }

            if (source is IIListProvider<TSource> listProv)
            {
                int c = listProv.GetCount(onlyIfCheap: true);
                if (c >= 0)
                {
                    count = c;
                    return true;
                }
            }

            if (source is ICollection collection)
            {
                count = collection.Count;
                return true;
            }

            count = 0;
            return false;
        }

        /// <summary>Returns an <see cref="long" /> that represents the total number of elements in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> that contains the elements to be counted.</param>
        /// <returns>The number of elements in the source sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The number of elements exceeds <see cref="long.MaxValue" />.</exception>
        /// <remarks>
        /// <para>Use this method rather than <see cref="O:Enumerable.Count" /> when you expect the result to be greater than <see cref="int.MaxValue" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into LongCount()` clause translates to an invocation of <see cref="O:Enumerable.LongCount" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="LongCount{T}(IEnumerable{T})" /> to count the elements in an array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet47":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet47":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static long LongCount<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            long count = 0;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                checked
                {
                    while (e.MoveNext())
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>Returns an <see cref="long" /> that represents how many elements in a sequence satisfy a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> that contains the elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A number that represents how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="System.OverflowException">The number of matching elements exceeds <see cref="long.MaxValue" />.</exception>
        /// <remarks>
        /// <para>Use this method rather than <see cref="O:Enumerable.Count" /> when you expect the result to be greater than <see cref="int.MaxValue" />.</para>
        /// <para>In Visual Basic query expression syntax, an `Aggregate Into LongCount()` clause translates to an invocation of <see cref="O:Enumerable.LongCount" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="LongCount{T}(IEnumerable{T},Func{T,bool})" /> to count the elements in an array that satisfy a condition.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet48":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet48":::</example>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/aggregate-clause">Aggregate Clause (Visual Basic)</related>
        public static long LongCount<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            long count = 0;
            foreach (TSource element in source)
            {
                checked
                {
                    if (predicate(element))
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
