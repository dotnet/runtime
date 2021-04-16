// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    /// <summary>
    /// Provides a set of <see langword="static" /> (<see langword="Shared" /> in Visual Basic) methods
    /// for querying objects that implement <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <remarks>
    /// The methods in this class provide an implementation of the standard query operators for querying data sources that
    /// implement <see cref="IEnumerable{T}" />. The standard query operators are general purpose methods that follow the
    /// LINQ pattern and enable you to express traversal, filter, and projection operations over data in any .NET-based
    /// programming language. The majority of the methods in this class are defined as extension methods that extend
    /// <see cref="IEnumerable{T}" />. This means they can be called like an instance method on any object that implements
    /// <see cref="IEnumerable{T}" />. Methods that are used in a query that returns a sequence of values do not consume the
    /// target data until the query object is enumerated. This is known as deferred execution. Methods that are used in a
    /// query that returns a singleton value execute and consume the target data immediately.
    /// </remarks>
    /// <related type="Article" href="https://msdn.microsoft.com/library/24cda21e-8af8-4632-b519-c404a839b9b2">Standard Query Operators Overview</related>
    /// <related type="Article" href="/dotnet/csharp/programming-guide/classes-and-structs/extension-methods">Extension Methods (C# Programming Guide)</related>
    /// <related type="Article" href="/dotnet/visual-basic/programming-guide/language-features/procedures/extension-methods">Extension Methods (Visual Basic)</related>
    public static partial class Enumerable
    {
        /// <summary>
        /// Returns an empty <see cref="IEnumerable{T}" /> that has the specified type argument.
        /// </summary>
        /// <typeparam name="TResult">The type to assign to the type parameter of the returned generic <see cref="IEnumerable{T}" />.</typeparam>
        /// <returns>An empty <see cref="IEnumerable{T}" /> whose type argument is <typeparamref name="TResult" />.</returns>
        /// <remarks>The <see cref="Empty{T}" /> method caches an empty sequence of type <typeparamref name="TResult" />. When the object it returns is enumerated, it yields no elements.
        /// In some cases, this method is useful for passing an empty sequence to a user-defined method that takes an <see cref="IEnumerable{T}" />. It can also be used to generate a neutral element for methods such as <see cref="O:Enumerable.Union" />. See the Example section for an example of this use of <see cref="Empty{T}" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="Empty{T}" /> to generate an empty <see cref="IEnumerable{T}" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet30":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet30":::
        /// The following code example demonstrates a possible application of the <see cref="Empty{T}" /> method. The <see cref="O:Enumerable.Aggregate" /> method is applied to a collection of string arrays. The elements of each array in the collection are added to the resulting <see cref="IEnumerable{T}" /> only if that array contains four or more elements. <see cref="O:Enumerable.Empty" /> is used to generate the seed value for <see cref="O:Enumerable.Aggregate" /> because if no array in the collection has four or more elements, only the empty sequence is returned.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet31":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet31":::</example>
        public static IEnumerable<TResult> Empty<TResult>() => EmptyPartition<TResult>.Instance;
    }
}
