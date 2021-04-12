// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
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
        /// <summary>Filters the elements of an <see cref="System.Collections.IEnumerable" /> based on a specified type.</summary>
        /// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
        /// <param name="source">The <see cref="System.Collections.IEnumerable" /> whose elements to filter.</param>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains elements from the input sequence of type <typeparamref name="TResult" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The <see cref="System.Linq.Enumerable.OfType{T}(System.Collections.IEnumerable)" /> method returns only those elements in <paramref name="source" /> that can be cast to type <typeparamref name="TResult" />. To instead receive an exception if an element cannot be cast to type <typeparamref name="TResult" />, use <see cref="System.Linq.Enumerable.Cast{T}(System.Collections.IEnumerable)" />.
        /// This method is one of the few standard query operator methods that can be applied to a collection that has a non-parameterized type, such as an <see cref="System.Collections.ArrayList" />. This is because <see cref="System.Linq.Enumerable.OfType" /> extends the type <see cref="System.Collections.IEnumerable" />. <see cref="System.Linq.Enumerable.OfType" /> cannot only be applied to collections that are based on the parameterized <see cref="System.Collections.Generic.IEnumerable{T}" /> type, but collections that are based on the non-parameterized <see cref="System.Collections.IEnumerable" /> type also.
        /// By applying <see cref="System.Linq.Enumerable.OfType" /> to a collection that implements <see cref="System.Collections.IEnumerable" />, you gain the ability to query the collection by using the standard query operators. For example, specifying a type argument of <see cref="object" /> to <see cref="System.Linq.Enumerable.OfType" /> would return an object of type `IEnumerable&lt;Object&gt;` in C# or `IEnumerable(Of Object)` in Visual Basic, to which the standard query operators can be applied.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.OfType" /> to filter the elements of an <see cref="System.Collections.IEnumerable" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet69":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet69":::</example>
        public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return OfTypeIterator<TResult>(source);
        }

        private static IEnumerable<TResult> OfTypeIterator<TResult>(IEnumerable source)
        {
            foreach (object? obj in source)
            {
                if (obj is TResult result)
                {
                    yield return result;
                }
            }
        }

        /// <summary>Casts the elements of an <see cref="System.Collections.IEnumerable" /> to the specified type.</summary>
        /// <typeparam name="TResult">The type to cast the elements of <paramref name="source" /> to.</typeparam>
        /// <param name="source">The <see cref="System.Collections.IEnumerable" /> that contains the elements to be cast to type <typeparamref name="TResult" />.</param>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains each element of the source sequence cast to the specified type.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidCastException">An element in the sequence cannot be cast to type <typeparamref name="TResult" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The <see cref="System.Linq.Enumerable.Cast{T}(System.Collections.IEnumerable)" /> method enables the standard query operators to be invoked on non-generic collections by supplying the necessary type information. For example, <see cref="System.Collections.ArrayList" /> does not implement <see cref="System.Collections.Generic.IEnumerable{T}" />, but by calling <see cref="System.Linq.Enumerable.Cast{T}(System.Collections.IEnumerable)" /> on the <see cref="System.Collections.ArrayList" /> object, the standard query operators can then be used to query the sequence.
        /// If an element cannot be converted to type <typeparamref name="TResult" />, this method throws a <see cref="System.InvalidCastException" />.
        /// The source sequence for this method is <see cref="System.Collections.IEnumerable" />, which means the elements have the compile-time static type of `object`. The only type conversions that are performed by this method are reference conversions and unboxing conversions. The runtime type of the elements in the collection must match the target type, or in the case of value types, the runtime type of elements must be the result of a boxing conversion of the target type. Other conversion types, such as those between different numeric types, are not allowed.
        /// To obtain only those elements that can be converted to type <typeparamref name="TResult" />, use the <see cref="System.Linq.Enumerable.OfType" /> method instead of <see cref="System.Linq.Enumerable.Cast{T}(System.Collections.IEnumerable)" />.
        /// In a query expression, an explicitly typed iteration variable translates to an invocation of <see cref="System.Linq.Enumerable.Cast{T}(System.Collections.IEnumerable)" />. This example shows the syntax for an explicitly typed range variable.
        /// <code class="lang-csharp">
        /// from int i in objects
        /// </code>
        /// <code class="lang-vb">
        /// From i As Integer In objects
        /// </code>
        /// Use the `select` clause of a query to perform other conversion types, like the implicit numeric conversions. The following example uses both the `Cast` method and a `select` statement to convert a sequence of boxed integers to a sequence of doubles.
        /// <code class="lang-csharp">
        /// IEnumerable sequence = Enumerable.Range(0, 10);
        /// var doubles = from int item in sequence
        /// select (double)item;
        /// </code>
        /// <code class="lang-vb">
        /// Dim sequence As IEnumerable = Enumerable.Range(0, 10)
        /// Dim doubles = From item As Integer In sequence
        /// Select CType(item, Double)
        /// </code>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.Cast{T}(System.Collections.IEnumerable)" /> to enable the use of the standard query operators on an <see cref="System.Collections.ArrayList" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet19":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet19":::</example>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/from-clause">from clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/from-clause">From Clause (Visual Basic)</related>
        public static IEnumerable<
#nullable disable // there's no way to annotate the connection of the nullability of TResult to that of the source
                TResult
#nullable restore
                > Cast<TResult>(this IEnumerable source)
        {
            if (source is IEnumerable<TResult> typedSource)
            {
                return typedSource;
            }

            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return CastIterator<TResult>(source);
        }

        private static IEnumerable<TResult> CastIterator<TResult>(IEnumerable source)
        {
            foreach (object obj in source)
            {
                yield return (TResult)obj;
            }
        }
    }
}
