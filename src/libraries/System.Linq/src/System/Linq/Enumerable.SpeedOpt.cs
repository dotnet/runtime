// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>
        /// Returns an empty <see cref="IEnumerable{T}" /> that has the specified type argument.
        /// </summary>
        /// <typeparam name="TResult">The type to assign to the type parameter of the returned generic <see cref="IEnumerable{T}" />.</typeparam>
        /// <returns>An empty <see cref="IEnumerable{T}" /> whose type argument is <typeparamref name="TResult" />.</returns>
        /// <remarks>
        /// <para>The <see cref="Empty{T}" /> method caches an empty sequence of type <typeparamref name="TResult" />. When the object it returns is enumerated, it yields no elements.</para>
        /// <para>In some cases, this method is useful for passing an empty sequence to a user-defined method that takes an <see cref="IEnumerable{T}" />. It can also be used to generate a neutral element for methods such as <see cref="O:Enumerable.Union" />. See the Example section for an example of this use of <see cref="Empty{T}" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Empty{T}" /> to generate an empty <see cref="IEnumerable{T}" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet30":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet30":::
        /// The following code example demonstrates a possible application of the <see cref="Empty{T}" /> method. The <see cref="O:Enumerable.Aggregate" /> method is applied to a collection of string arrays. The elements of each array in the collection are added to the resulting <see cref="IEnumerable{T}" /> only if that array contains four or more elements. <see cref="O:Enumerable.Empty" /> is used to generate the seed value for <see cref="O:Enumerable.Aggregate" /> because if no array in the collection has four or more elements, only the empty sequence is returned.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet31":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet31":::</example>
        public static IEnumerable<TResult> Empty<TResult>() => EmptyPartition<TResult>.Instance;
    }
}
