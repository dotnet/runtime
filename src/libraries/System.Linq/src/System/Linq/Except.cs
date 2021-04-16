// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Produces the set difference of two sequences by using the default equality comparer to compare values.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{T}" /> whose elements that are not also in <paramref name="second" /> will be returned.</param>
        /// <param name="second">An <see cref="IEnumerable{T}" /> whose elements that also occur in the first sequence will cause those elements to be removed from the returned sequence.</param>
        /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The default equality comparer, <see cref="O:EqualityComparer{T}.Default" />, is used to compare values of the types.</para>
        /// <para>To compare a custom data type, you need to override the <see cref="O:object.Equals" /> and the <see cref="O:object.GetHashCode" /> methods, and optionally implement the <see cref="System.IEquatable{T}" /> generic interface in the custom type. For more information, see the <see cref="O:EqualityComparer{T}.Default" /> property.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use the <see cref="Except{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/> method to compare two sequences of numbers and return elements that appear only in the first sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet34":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet34":::
        /// If you want to compare sequences of objects of some custom data type, you have to implement the <see cref="System.IEquatable{T}" /> generic interface in a helper class. The following code example shows how to implement this interface in a custom data type and override <see cref="O:object.GetHashCode" /> and <see cref="O:object.Equals" /> methods.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/CS/EncapsulatedComparer.cs" id="Snippet9":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/VB/EncapsulatedComparer.vb" id="Snippet9":::
        /// After you implement this interface, you can use sequences of `ProductA` objects in the <see cref="Except{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/> method, as shown in the following example:
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/CS/EncapsulatedComparer.cs" id="Snippet7":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/VB/EncapsulatedComparer.vb" id="Snippet7":::</example>
        public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            return ExceptIterator(first, second, null);
        }

        /// <summary>Produces the set difference of two sequences by using the specified <see cref="IEqualityComparer{T}" /> to compare values.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{T}" /> whose elements that are not also in <paramref name="second" /> will be returned.</param>
        /// <param name="second">An <see cref="IEnumerable{T}" /> whose elements that also occur in the first sequence will cause those elements to be removed from the returned sequence.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="O:EqualityComparer{T}.Default" />, is used to compare values.</remarks>
        /// <example>If you want to compare sequences of objects of some custom data type, you have to implement the <see cref="IEqualityComparer{T}" /> generic interface in a helper class. The following code example shows how to implement this interface in a custom data type and provide <see cref="O:object.GetHashCode" /> and <see cref="O:object.Equals" /> methods.
        /// The following example shows how to implement an equality comparer that can be used in the <see cref="O:Enumerable.Except" /> method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet1":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet1":::
        /// After you implement this comparer, you can use sequences of `Product` objects in the <see cref="O:Enumerable.Except" /> method, as shown in the following example:
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet7":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet7":::</example>
        public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            if (first == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            return ExceptIterator(first, second, comparer);
        }

        public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector) => ExceptBy(first, second, keySelector, null);

        public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
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

            return ExceptByIterator(first, second, keySelector, comparer);
        }

        private static IEnumerable<TSource> ExceptIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            var set = new HashSet<TSource>(second, comparer);

            foreach (TSource element in first)
            {
                if (set.Add(element))
                {
                    yield return element;
                }
            }
        }

        private static IEnumerable<TSource> ExceptByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            var set = new HashSet<TKey>(second, comparer);

            foreach (TSource element in first)
            {
                if (set.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
