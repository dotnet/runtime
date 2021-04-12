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
        /// <summary>Creates an array from a <see cref="System.Collections.Generic.IEnumerable{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> to create an array from.</param>
        /// <returns>An array that contains the elements from the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>The <see cref="System.Linq.Enumerable.ToArray{T}(System.Collections.Generic.IEnumerable{T})" /> method forces immediate query evaluation and returns an array that contains the query results. You can append this method to your query in order to obtain a cached copy of the query results.
        /// <see cref="O:System.Linq.Enumerable.ToList" /> has similar behavior but returns a <see cref="System.Collections.Generic.List{T}" /> instead of an array.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:System.Linq.Enumerable.ToArray" /> to force immediate query evaluation and return an array of results.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet104":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet104":::</example>
        public static TSource[] ToArray<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source is IIListProvider<TSource> arrayProvider
                ? arrayProvider.ToArray()
                : EnumerableHelpers.ToArray(source);
        }

        /// <summary>Creates a <see cref="System.Collections.Generic.List{T}" /> from an <see cref="System.Collections.Generic.IEnumerable{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="System.Collections.Generic.IEnumerable{T}" /> to create a <see cref="System.Collections.Generic.List{T}" /> from.</param>
        /// <returns>A <see cref="System.Collections.Generic.List{T}" /> that contains elements from the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>The <see cref="System.Linq.Enumerable.ToList{T}(System.Collections.Generic.IEnumerable{T})" /> method forces immediate query evaluation and returns a <see cref="System.Collections.Generic.List{T}" /> that contains the query results. You can append this method to your query in order to obtain a cached copy of the query results.
        /// <see cref="O:System.Linq.Enumerable.ToArray" /> has similar behavior but returns an array instead of a <see cref="System.Collections.Generic.List{T}" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:System.Linq.Enumerable.ToList" /> to force immediate query evaluation and return a <see cref="System.Collections.Generic.List{T}" /> that contains the query results.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet106":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet106":::</example>
        public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source is IIListProvider<TSource> listProvider ? listProvider.ToList() : new List<TSource>(source);
        }

        /// <summary>Creates a <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> from an <see cref="System.Collections.Generic.IEnumerable{T}" /> according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> to create a <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>A <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> that contains keys and values. The values within each group are in the same order as in <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.
        /// -or-
        /// <paramref name="keySelector" /> produces a key that is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="keySelector" /> produces duplicate keys for two elements.</exception>
        /// <remarks>The <see cref="System.Linq.Enumerable.ToDictionary{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2})" /> method uses the default equality comparer <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" /> to compare keys.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.ToDictionary{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2})" /> to create a <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> by using a key selector.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet105":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet105":::</example>
        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) where TKey : notnull =>
            ToDictionary(source, keySelector, null);

        /// <summary>Creates a <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> from an <see cref="System.Collections.Generic.IEnumerable{T}" /> according to a specified key selector function and key comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> to create a <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare keys.</param>
        /// <returns>A <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> that contains keys and values. The values within each group are in the same order as in <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.
        /// -or-
        /// <paramref name="keySelector" /> produces a key that is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="keySelector" /> produces duplicate keys for two elements.</exception>
        /// <remarks>If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" /> is used to compare keys.</remarks>
        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (keySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            int capacity = 0;
            if (source is ICollection<TSource> collection)
            {
                capacity = collection.Count;
                if (capacity == 0)
                {
                    return new Dictionary<TKey, TSource>(comparer);
                }

                if (collection is TSource[] array)
                {
                    return ToDictionary(array, keySelector, comparer);
                }

                if (collection is List<TSource> list)
                {
                    return ToDictionary(list, keySelector, comparer);
                }
            }

            Dictionary<TKey, TSource> d = new Dictionary<TKey, TSource>(capacity, comparer);
            foreach (TSource element in source)
            {
                d.Add(keySelector(element), element);
            }

            return d;
        }

        private static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(TSource[] source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            Dictionary<TKey, TSource> d = new Dictionary<TKey, TSource>(source.Length, comparer);
            for (int i = 0; i < source.Length; i++)
            {
                d.Add(keySelector(source[i]), source[i]);
            }

            return d;
        }

        private static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(List<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            Dictionary<TKey, TSource> d = new Dictionary<TKey, TSource>(source.Count, comparer);
            foreach (TSource element in source)
            {
                d.Add(keySelector(element), element);
            }

            return d;
        }

        /// <summary>Creates a <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> from an <see cref="System.Collections.Generic.IEnumerable{T}" /> according to specified key selector and element selector functions.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> to create a <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <returns>A <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> that contains values of type <typeparamref name="TElement" /> selected from the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> is <see langword="null" />.
        /// -or-
        /// <paramref name="keySelector" /> produces a key that is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="keySelector" /> produces duplicate keys for two elements.</exception>
        /// <remarks>The <see cref="System.Linq.Enumerable.ToDictionary{T1,T2,T3}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2},System.Func{T1,T3})" /> method uses the default equality comparer <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" /> to compare keys.</remarks>
        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) where TKey : notnull =>
            ToDictionary(source, keySelector, elementSelector, null);

        /// <summary>Creates a <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> from an <see cref="System.Collections.Generic.IEnumerable{T}" /> according to a specified key selector function, a comparer, and an element selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> to create a <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare keys.</param>
        /// <returns>A <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> that contains values of type <typeparamref name="TElement" /> selected from the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> is <see langword="null" />.
        /// -or-
        /// <paramref name="keySelector" /> produces a key that is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="keySelector" /> produces duplicate keys for two elements.</exception>
        /// <remarks>If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" /> is used to compare keys.</remarks>
        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (keySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            if (elementSelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementSelector);
            }

            int capacity = 0;
            if (source is ICollection<TSource> collection)
            {
                capacity = collection.Count;
                if (capacity == 0)
                {
                    return new Dictionary<TKey, TElement>(comparer);
                }

                if (collection is TSource[] array)
                {
                    return ToDictionary(array, keySelector, elementSelector, comparer);
                }

                if (collection is List<TSource> list)
                {
                    return ToDictionary(list, keySelector, elementSelector, comparer);
                }
            }

            Dictionary<TKey, TElement> d = new Dictionary<TKey, TElement>(capacity, comparer);
            foreach (TSource element in source)
            {
                d.Add(keySelector(element), elementSelector(element));
            }

            return d;
        }

        private static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(TSource[] source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            Dictionary<TKey, TElement> d = new Dictionary<TKey, TElement>(source.Length, comparer);
            for (int i = 0; i < source.Length; i++)
            {
                d.Add(keySelector(source[i]), elementSelector(source[i]));
            }

            return d;
        }

        private static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(List<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            Dictionary<TKey, TElement> d = new Dictionary<TKey, TElement>(source.Count, comparer);
            foreach (TSource element in source)
            {
                d.Add(keySelector(element), elementSelector(element));
            }

            return d;
        }

        /// <summary>Creates a <see cref="System.Collections.Generic.HashSet{T}" /> from an <see cref="System.Collections.Generic.IEnumerable{T}" />.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> to create a <see cref="System.Collections.Generic.HashSet{T}" /> from.</param>
        /// <returns>A <see cref="System.Collections.Generic.HashSet{T}" /> that contains values of type <typeparamref name="TSource" /> selected from the input sequence.</returns>
        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source) => source.ToHashSet(comparer: null);

        /// <summary>Creates a <see cref="System.Collections.Generic.HashSet{T}" /> from an <see cref="System.Collections.Generic.IEnumerable{T}" /> using the <paramref name="comparer" /> to compare keys.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}" /> to create a <see cref="System.Collections.Generic.HashSet{T}" /> from.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare keys.</param>
        /// <returns>A <see cref="System.Collections.Generic.HashSet{T}" /> that contains values of type <typeparamref name="TSource" /> selected from the input sequence.</returns>
        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            // Don't pre-allocate based on knowledge of size, as potentially many elements will be dropped.
            return new HashSet<TSource>(source, comparer);
        }

        /// <summary>Default initial capacity to use when creating sets for internal temporary storage.</summary>
        /// <remarks>This is based on the implicit size used in previous implementations, which used a custom Set type.</remarks>
        private const int DefaultInternalSetCapacity = 7;

        private static TSource[] HashSetToArray<TSource>(HashSet<TSource> set)
        {
            var result = new TSource[set.Count];
            set.CopyTo(result);
            return result;
        }

        private static List<TSource> HashSetToList<TSource>(HashSet<TSource> set)
        {
            var result = new List<TSource>(set.Count);

            foreach (TSource item in set)
            {
                result.Add(item);
            }

            return result;
        }
    }
}
