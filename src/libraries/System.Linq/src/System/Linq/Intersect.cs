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
        /// <summary>Produces the set intersection of two sequences by using the default equality comparer to compare values.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An <see cref="System.Collections.Generic.IEnumerable{T}" /> whose distinct elements that also appear in <paramref name="second" /> will be returned.</param>
        /// <param name="second">An <see cref="System.Collections.Generic.IEnumerable{T}" /> whose distinct elements that also appear in the first sequence will be returned.</param>
        /// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The intersection of two sets A and B is defined as the set that contains all the elements of A that also appear in B, but no other elements.
        /// When the object returned by this method is enumerated, `Intersect` yields distinct elements occurring in both sequences in the order in which they appear in <paramref name="first" />.
        /// The default equality comparer, <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" />, is used to compare values of the types.
        /// To compare a custom data type, you need to override the <see cref="O:object.Equals" /> and the <see cref="O:object.GetHashCode" /> methods, and optionally implement the <see cref="System.IEquatable{T}" /> generic interface in the custom type. For more information, see the <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" /> property.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="Intersect{TSource}(IEnumerable{TSource}, IEnumerable{TSource})" /> to return the elements that appear in each of two sequences of integers.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet41":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet41":::
        /// If you want to compare sequences of objects of some custom data type, you have to implement the <see cref="System.IEquatable{T}" /> generic interface in a helper class. The following code example shows how to implement this interface in a custom data type and override <see cref="O:object.GetHashCode" /> and <see cref="O:object.Equals" /> methods.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/CS/EncapsulatedComparer.cs" id="Snippet9":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/VB/EncapsulatedComparer.vb" id="Snippet9":::
        /// After you implement this interface, you can use sequences of `ProductA` objects in the `Intersect` method, as shown in the following example:
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/CS/EncapsulatedComparer.cs" id="Snippet10":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/VB/EncapsulatedComparer.vb" id="Snippet10":::
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/CS/EncapsulatedComparer.cs" id="Snippet3":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/VB/EncapsulatedComparer.vb" id="Snippet3":::</example>
        public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second) => Intersect(first, second, null);

        /// <summary>Produces the set intersection of two sequences by using the specified <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare values.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An <see cref="System.Collections.Generic.IEnumerable{T}" /> whose distinct elements that also appear in <paramref name="second" /> will be returned.</param>
        /// <param name="second">An <see cref="System.Collections.Generic.IEnumerable{T}" /> whose distinct elements that also appear in the first sequence will be returned.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The intersection of two sets A and B is defined as the set that contains all the elements of A that also appear in B, but no other elements.
        /// When the object returned by this method is enumerated, `Intersect` yields distinct elements occurring in both sequences in the order in which they appear in <paramref name="first" />.
        /// If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" />, is used to compare values.</remarks>
        /// <example>The following example shows how to implement an equality comparer that can be used in the `Intersect` method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet1":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet1":::
        /// After you implement this comparer, you can use sequences of `Product` objects in the `Intersect` method, as shown in the following example:
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet2":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet2":::
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet3":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet3":::</example>
        public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            if (first == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            return IntersectIterator(first, second, comparer);
        }

        public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector) => IntersectBy(first, second, keySelector, null);

        public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (first is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }
            if (second is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            return IntersectByIterator(first, second, keySelector, comparer);
        }

        private static IEnumerable<TSource> IntersectIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            var set = new HashSet<TSource>(second, comparer);

            foreach (TSource element in first)
            {
                if (set.Remove(element))
                {
                    yield return element;
                }
            }
        }

        private static IEnumerable<TSource> IntersectByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            var set = new HashSet<TKey>(second, comparer);

            foreach (TSource element in first)
            {
                if (set.Remove(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
