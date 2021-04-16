// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    /// <summary>Provides a set of <see langword="static" /> (<see langword="Shared" /> in Visual Basic) methods for querying objects that implement <see cref="IEnumerable{T}" />.</summary>
    /// <remarks>The methods in this class provide an implementation of the standard query operators for querying data sources that implement <see cref="IEnumerable{T}" />. The standard query operators are general purpose methods that follow the LINQ pattern and enable you to express traversal, filter, and projection operations over data in any .NET-based programming language.
    /// The majority of the methods in this class are defined as extension methods that extend <see cref="IEnumerable{T}" />. This means they can be called like an instance method on any object that implements <see cref="IEnumerable{T}" />.
    /// Methods that are used in a query that returns a sequence of values do not consume the target data until the query object is enumerated. This is known as deferred execution. Methods that are used in a query that returns a singleton value execute and consume the target data immediately.</remarks>
    /// <related type="Article" href="https://msdn.microsoft.com/library/24cda21e-8af8-4632-b519-c404a839b9b2">Standard Query Operators Overview</related>
    /// <related type="Article" href="/dotnet/csharp/programming-guide/classes-and-structs/extension-methods">Extension Methods (C# Programming Guide)</related>
    /// <related type="Article" href="/dotnet/visual-basic/programming-guide/language-features/procedures/extension-methods">Extension Methods (Visual Basic)</related>
    public static partial class Enumerable
    {
        /// <summary>Determines whether two sequences are equal by comparing the elements by using the default equality comparer for their type.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{T}" /> to compare to <paramref name="second" />.</param>
        /// <param name="second">An <see cref="IEnumerable{T}" /> to compare to the first sequence.</param>
        /// <returns><see langword="true" /> if the two source sequences are of equal length and their corresponding elements are equal according to the default equality comparer for their type; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>The <see cref="SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})" /> method enumerates the two source sequences in parallel and compares corresponding elements by using the default equality comparer for <typeparamref name="TSource" />, <see cref="O:EqualityComparer{T}.Default" />.
        /// The default equality comparer, <see cref="O:EqualityComparer{T}.Default" />, is used to compare values of the types.
        /// To compare a custom data type, you need to override the <see cref="O:object.Equals" /> and the <see cref="O:object.GetHashCode" /> methods, and optionally implement the <see cref="System.IEquatable{T}" /> generic interface in the custom type. For more information, see the <see cref="O:EqualityComparer{T}.Default" /> property.</remarks>
        /// <example>The following code examples demonstrate how to use <see cref="SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})" /> to determine whether two sequences are equal. In the first two examples, the method determines whether the compared sequences contain references to the same objects. In the third and fourth examples, the method compares the actual data of the objects within the sequences.
        /// In this example the sequences are equal.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet32":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet32":::
        /// The following code example compares two sequences that are not equal. Note that the sequences contain identical data, but because the objects that they contain have different references, the sequences are not considered equal.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet33":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet33":::
        /// If you want to compare the actual data of the objects in the sequences instead of just comparing their references, you have to implement the <see cref="IEqualityComparer{T}" /> generic interface in your class. The following code example shows how to implement this interface in a helper class and provide <see cref="O:object.GetHashCode" /> and <see cref="O:object.Equals" /> methods.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/CS/EncapsulatedComparer.cs" id="Snippet9":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/VB/EncapsulatedComparer.vb" id="Snippet9":::
        /// After you implement this interface, you can use sequences of `ProductA` objects in the <see cref="SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/> method, as shown in the following example:
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/CS/EncapsulatedComparer.cs" id="Snippet8":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQEncapsulatedComparer/VB/EncapsulatedComparer.vb" id="Snippet8":::</example>
        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second) =>
            SequenceEqual(first, second, null);

        /// <summary>Determines whether two sequences are equal by comparing their elements by using a specified <see cref="IEqualityComparer{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{T}" /> to compare to <paramref name="second" />.</param>
        /// <param name="second">An <see cref="IEnumerable{T}" /> to compare to the first sequence.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}" /> to use to compare elements.</param>
        /// <returns><see langword="true" /> if the two source sequences are of equal length and their corresponding elements compare equal according to <paramref name="comparer" />; otherwise, <see langword="false" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>The <see cref="SequenceEqual{T}(IEnumerable{T},IEnumerable{T},IEqualityComparer{T})" /> method enumerates the two source sequences in parallel and compares corresponding elements by using the specified <see cref="IEqualityComparer{T}" />. If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="O:EqualityComparer{T}.Default" />, is used to compare elements.</remarks>
        /// <example>The following example shows how to implement an equality comparer that can be used in the <see cref="SequenceEqual{T}(IEnumerable{T},IEnumerable{T},IEqualityComparer{T})" /> method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet1":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet1":::
        /// After you implement this comparer, you can use sequences of `Product` objects in the <see cref="SequenceEqual{T}(IEnumerable{T},IEnumerable{T},IEqualityComparer{T})" /> method, as shown in the following example:
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_VBCSharp/CsLINQCustomComparer/CS/CustomComparer.cs" id="Snippet8":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_VBCSharp/CsLINQCustomComparer/VB/CustomComparer.vb" id="Snippet8":::</example>
        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            if (first == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            if (first is ICollection<TSource> firstCol && second is ICollection<TSource> secondCol)
            {
                if (first is TSource[] firstArray && second is TSource[] secondArray)
                {
                    return ((ReadOnlySpan<TSource>)firstArray).SequenceEqual(secondArray, comparer);
                }

                if (firstCol.Count != secondCol.Count)
                {
                    return false;
                }

                if (firstCol is IList<TSource> firstList && secondCol is IList<TSource> secondList)
                {
                    comparer ??= EqualityComparer<TSource>.Default;

                    int count = firstCol.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (!comparer.Equals(firstList[i], secondList[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            using (IEnumerator<TSource> e1 = first.GetEnumerator())
            using (IEnumerator<TSource> e2 = second.GetEnumerator())
            {
                comparer ??= EqualityComparer<TSource>.Default;

                while (e1.MoveNext())
                {
                    if (!(e2.MoveNext() && comparer.Equals(e1.Current, e2.Current)))
                    {
                        return false;
                    }
                }

                return !e2.MoveNext();
            }
        }
    }
}
