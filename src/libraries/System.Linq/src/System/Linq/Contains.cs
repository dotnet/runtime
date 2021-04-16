// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Determines whether a sequence contains a specified element by using the default equality comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence in which to locate a value.</param>
        /// <param name="value">The value to locate in the sequence.</param>
        /// <returns><see langword="true" /> if the source sequence contains an element that has the specified value; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>If the type of <paramref name="source" /> implements <see cref="ICollection{T}" />, the `Contains` method in that implementation is invoked to obtain the result. Otherwise, this method determines whether <paramref name="source" /> contains the specified element.</para>
        /// <para>Enumeration is terminated as soon as a matching element is found.</para>
        /// <para>Elements are compared to the specified value by using the default equality comparer, <see cref="O:EqualityComparer{T}.Default" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Contains{T}(IEnumerable{T},T)" /> to determine whether an array contains a specific element.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet21":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet21":::</example>
        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value) =>
            source is ICollection<TSource> collection ? collection.Contains(value) :
            Contains(source, value, null);

        /// <summary>Determines whether a sequence contains a specified element by using a specified <see cref="IEqualityComparer{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence in which to locate a value.</param>
        /// <param name="value">The value to locate in the sequence.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns><see langword="true" /> if the source sequence contains an element that has the specified value; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>Enumeration is terminated as soon as a matching element is found.</para>
        /// <para>If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="O:EqualityComparer{T}.Default" />, is used to compare elements to the specified value.</para>
        /// </remarks>
        /// <example>The following example shows how to implement an equality comparer that can be used in the <see cref="O:Enumerable.Contains" /> method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet1":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet1":::
        /// After you implement this comparer, you can use a sequence of `Product` objects in the <see cref="O:Enumerable.Contains" /> method, as shown in the following example:
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet6":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet6":::</example>
        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value, IEqualityComparer<TSource>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (comparer == null)
            {
                foreach (TSource element in source)
                {
                    if (EqualityComparer<TSource>.Default.Equals(element, value)) // benefits from devirtualization and likely inlining
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (TSource element in source)
                {
                    if (comparer.Equals(element, value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
