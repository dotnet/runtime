// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Filters the elements of an <see cref="IEnumerable" /> based on a specified type.</summary>
        /// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
        /// <param name="source">The <see cref="IEnumerable" /> whose elements to filter.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains elements from the input sequence of type <typeparamref name="TResult" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The <see cref="OfType{T}(IEnumerable)" /> method returns only those elements in <paramref name="source" /> that can be cast to type <typeparamref name="TResult" />. To instead receive an exception if an element cannot be cast to type <typeparamref name="TResult" />, use <see cref="Cast{T}(IEnumerable)" />.</para>
        /// <para>This method is one of the few standard query operator methods that can be applied to a collection that has a non-parameterized type, such as an <see cref="ArrayList" />. This is because <see cref="OfType" /> extends the type <see cref="IEnumerable" />. <see cref="OfType" /> cannot only be applied to collections that are based on the parameterized <see cref="IEnumerable{T}" /> type, but collections that are based on the non-parameterized <see cref="IEnumerable" /> type also.</para>
        /// <para>By applying <see cref="OfType" /> to a collection that implements <see cref="IEnumerable" />, you gain the ability to query the collection by using the standard query operators. For example, specifying a type argument of <see cref="object" /> to <see cref="OfType" /> would return an object of type `IEnumerable&lt;Object&gt;` in C# or `IEnumerable(Of Object)` in Visual Basic, to which the standard query operators can be applied.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="OfType" /> to filter the elements of an <see cref="IEnumerable" />.
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

        /// <summary>Casts the elements of an <see cref="IEnumerable" /> to the specified type.</summary>
        /// <typeparam name="TResult">The type to cast the elements of <paramref name="source" /> to.</typeparam>
        /// <param name="source">The <see cref="IEnumerable" /> that contains the elements to be cast to type <typeparamref name="TResult" />.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains each element of the source sequence cast to the specified type.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidCastException">An element in the sequence cannot be cast to type <typeparamref name="TResult" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The <see cref="Cast{T}(IEnumerable)" /> method enables the standard query operators to be invoked on non-generic collections by supplying the necessary type information. For example, <see cref="ArrayList" /> does not implement <see cref="IEnumerable{T}" />, but by calling <see cref="Cast{T}(IEnumerable)" /> on the <see cref="ArrayList" /> object, the standard query operators can then be used to query the sequence.
        /// If an element cannot be converted to type <typeparamref name="TResult" />, this method throws a <see cref="System.InvalidCastException" />.
        /// The source sequence for this method is <see cref="IEnumerable" />, which means the elements have the compile-time static type of `object`. The only type conversions that are performed by this method are reference conversions and unboxing conversions. The runtime type of the elements in the collection must match the target type, or in the case of value types, the runtime type of elements must be the result of a boxing conversion of the target type. Other conversion types, such as those between different numeric types, are not allowed.
        /// To obtain only those elements that can be converted to type <typeparamref name="TResult" />, use the <see cref="OfType" /> method instead of <see cref="Cast{T}(IEnumerable)" />.
        /// In a query expression, an explicitly typed iteration variable translates to an invocation of <see cref="Cast{T}(IEnumerable)" />. This example shows the syntax for an explicitly typed range variable.
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
        /// <example>The following code example demonstrates how to use <see cref="Cast{T}(IEnumerable)" /> to enable the use of the standard query operators on an <see cref="ArrayList" />.
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
