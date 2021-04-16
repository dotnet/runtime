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
        /// <summary>Returns the input typed as <see cref="IEnumerable{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to type as <see cref="IEnumerable{T}" />.</param>
        /// <returns>The input sequence typed as <see cref="IEnumerable{T}" />.</returns>
        /// <remarks>The <see cref="AsEnumerable{T}(IEnumerable{T})" /> method has no effect other than to change the compile-time type of <paramref name="source" /> from a type that implements <see cref="IEnumerable{T}" /> to <see cref="IEnumerable{T}" /> itself.
        /// <see cref="AsEnumerable{T}(IEnumerable{T})" /> can be used to choose between query implementations when a sequence implements <see cref="IEnumerable{T}" /> but also has a different set of public query methods available. For example, given a generic class `Table` that implements <see cref="IEnumerable{T}" /> and has its own methods such as `Where`, `Select`, and `SelectMany`, a call to `Where` would invoke the public `Where` method of `Table`. A `Table` type that represents a database table could have a `Where` method that takes the predicate argument as an expression tree and converts the tree to SQL for remote execution. If remote execution is not desired, for example because the predicate invokes a local method, the <see cref="O:Enumerable.AsEnumerable" /> method can be used to hide the custom methods and instead make the standard query operators available.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="AsEnumerable{T}(IEnumerable{T})" /> to hide a type's custom `Where` method when the standard query operator implementation is desired.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet108":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet108":::</example>
        public static IEnumerable<TSource> AsEnumerable<TSource>(this IEnumerable<TSource> source) => source;
    }
}
