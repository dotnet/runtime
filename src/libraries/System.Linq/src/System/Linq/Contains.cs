// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        /// <summary>Determines whether a sequence contains a specified element by using the default equality comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence in which to locate a value.</param>
        /// <param name="value">The value to locate in the sequence.</param>
        /// <returns><see langword="true" /> if the source sequence contains an element that has the specified value; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>If the type of <paramref name="source" /> implements <see cref="System.Collections.Generic.ICollection{T}" />, the `Contains` method in that implementation is invoked to obtain the result. Otherwise, this method determines whether <paramref name="source" /> contains the specified element.
        /// Enumeration is terminated as soon as a matching element is found.
        /// Elements are compared to the specified value by using the default equality comparer, <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.Contains{T}(System.Collections.Generic.IEnumerable{T},T)" /> to determine whether an array contains a specific element.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet21":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet21":::</example>
        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value) =>
            source is ICollection<TSource> collection ? collection.Contains(value) :
            Contains(source, value, null);

        /// <summary>Determines whether a sequence contains a specified element by using a specified <see cref="System.Collections.Generic.IEqualityComparer{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence in which to locate a value.</param>
        /// <param name="value">The value to locate in the sequence.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns><see langword="true" /> if the source sequence contains an element that has the specified value; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>Enumeration is terminated as soon as a matching element is found.
        /// If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" />, is used to compare elements to the specified value.</remarks>
        /// <example>The following example shows how to implement an equality comparer that can be used in the <see cref="O:System.Linq.Enumerable.Contains" /> method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet1":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet1":::
        /// After you implement this comparer, you can use a sequence of `Product` objects in the <see cref="O:System.Linq.Enumerable.Contains" /> method, as shown in the following example:
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
