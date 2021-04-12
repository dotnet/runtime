// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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
        /// <summary>Creates a <see cref="System.Linq.Lookup{T1,T2}" /> from an <see cref="System.Collections.Generic.IEnumerable{T}" /> according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">The <see cref="System.Collections.Generic.IEnumerable{T}" /> to create a <see cref="System.Linq.Lookup{T1,T2}" /> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>A <see cref="System.Linq.Lookup{T1,T2}" /> that contains keys and values. The values within each group are in the same order as in <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>The <see cref="System.Linq.Enumerable.ToLookup{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2})" /> method returns a <see cref="System.Linq.Lookup{T1,T2}" />, a one-to-many dictionary that maps keys to collections of values. A <see cref="System.Linq.Lookup{T1,T2}" /> differs from a <see cref="System.Collections.Generic.Dictionary{T1,T2}" />, which performs a one-to-one mapping of keys to single values.
        /// The default equality comparer <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" /> is used to compare keys.</remarks>
        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
            ToLookup(source, keySelector, null);

        /// <summary>Creates a <see cref="System.Linq.Lookup{T1,T2}" /> from an <see cref="System.Collections.Generic.IEnumerable{T}" /> according to a specified key selector function and key comparer.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <param name="source">The <see cref="System.Collections.Generic.IEnumerable{T}" /> to create a <see cref="System.Linq.Lookup{T1,T2}" /> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare keys.</param>
        /// <returns>A <see cref="System.Linq.Lookup{T1,T2}" /> that contains keys and values. The values within each group are in the same order as in <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.</exception>
        /// <remarks>The <see cref="System.Linq.Enumerable.ToLookup{T1,T2}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2},System.Collections.Generic.IEqualityComparer{T2})" /> method returns a <see cref="System.Linq.Lookup{T1,T2}" />, a one-to-many dictionary that maps keys to collections of values. A <see cref="System.Linq.Lookup{T1,T2}" /> is different to a <see cref="System.Collections.Generic.Dictionary{T1,T2}" />, which performs a one-to-one mapping of keys to single values.
        /// If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" /> is used to compare keys.</remarks>
        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (keySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            return Lookup<TKey, TSource>.Create(source, keySelector, comparer);
        }

        /// <summary>Creates a <see cref="System.Linq.Lookup{T1,T2}" /> from an <see cref="System.Collections.Generic.IEnumerable{T}" /> according to specified key selector and element selector functions.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector" />.</typeparam>
        /// <param name="source">The <see cref="System.Collections.Generic.IEnumerable{T}" /> to create a <see cref="System.Linq.Lookup{T1,T2}" /> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <returns>A <see cref="System.Linq.Lookup{T1,T2}" /> that contains values of type <typeparamref name="TElement" /> selected from the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> is <see langword="null" />.</exception>
        /// <remarks>The <see cref="System.Linq.Enumerable.ToLookup{T1,T2,T3}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2},System.Func{T1,T3})" /> method returns a <see cref="System.Linq.Lookup{T1,T2}" />, a one-to-many dictionary that maps keys to collections of values. A <see cref="System.Linq.Lookup{T1,T2}" /> differs from a <see cref="System.Collections.Generic.Dictionary{T1,T2}" />, which performs a one-to-one mapping of keys to single values.
        /// The default equality comparer <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" /> is used to compare keys.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="System.Linq.Enumerable.ToLookup{T1,T2,T3}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2},System.Func{T1,T3})" /> to create a <see cref="System.Linq.Lookup{T1,T2}" /> by using a key selector function and an element selector function.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet107":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet107":::</example>
        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) =>
            ToLookup(source, keySelector, elementSelector, null);

        /// <summary>Creates a <see cref="System.Linq.Lookup{T1,T2}" /> from an <see cref="System.Collections.Generic.IEnumerable{T}" /> according to a specified key selector function, a comparer and an element selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector" />.</typeparam>
        /// <param name="source">The <see cref="System.Collections.Generic.IEnumerable{T}" /> to create a <see cref="System.Linq.Lookup{T1,T2}" /> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="System.Collections.Generic.IEqualityComparer{T}" /> to compare keys.</param>
        /// <returns>A <see cref="System.Linq.Lookup{T1,T2}" /> that contains values of type <typeparamref name="TElement" /> selected from the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> is <see langword="null" />.</exception>
        /// <remarks>The <see cref="System.Linq.Enumerable.ToLookup{T1,T2,T3}(System.Collections.Generic.IEnumerable{T1},System.Func{T1,T2},System.Func{T1,T3},System.Collections.Generic.IEqualityComparer{T2})" /> method returns a <see cref="System.Linq.Lookup{T1,T2}" />, a one-to-many dictionary that maps keys to collections of values. A <see cref="System.Linq.Lookup{T1,T2}" /> differs from a <see cref="System.Collections.Generic.Dictionary{T1,T2}" />, which performs a one-to-one mapping of keys to single values.
        /// If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer <see cref="O:System.Collections.Generic.EqualityComparer{T}.Default" /> is used to compare keys.</remarks>
        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer)
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

            return Lookup<TKey, TElement>.Create(source, keySelector, elementSelector, comparer);
        }
    }

    /// <summary>Defines an indexer, size property, and Boolean search method for data structures that map keys to <see cref="System.Collections.Generic.IEnumerable{T}" /> sequences of values.</summary>
    /// <typeparam name="TKey">The type of the keys in the <see cref="System.Linq.ILookup{T1,T2}" />.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the <see cref="System.Collections.Generic.IEnumerable{T}" /> sequences that make up the values in the <see cref="System.Linq.ILookup{T1,T2}" />.</typeparam>
    /// <remarks>The type <see cref="System.Linq.Lookup{T1,T2}" /> implements the <see cref="System.Linq.ILookup{T1,T2}" /> interface.
    /// The extension method <see cref="O:System.Linq.Enumerable.ToLookup" />, which can be appended to the end of a LINQ query, returns an object of type <see cref="System.Linq.ILookup{T1,T2}" />.</remarks>
    /// <example>The following code example creates an <see cref="System.Linq.ILookup{T1,T2}" /> object and iterates through its contents.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.ILookup/cs/ILookup.cs" id="Snippet1":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.ILookup/vb/ILookup.vb" id="Snippet1":::</example>
    /// <altmember cref="System.Linq.Lookup{T1,T2}"/>
    public interface ILookup<TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>
    {
        /// <summary>Gets the number of key/value collection pairs in the <see cref="System.Linq.ILookup{T1,T2}" />.</summary>
        /// <value>The number of key/value collection pairs in the <see cref="System.Linq.ILookup{T1,T2}" />.</value>
        int Count { get; }

        IEnumerable<TElement> this[TKey key] { get; }

        /// <summary>Determines whether a specified key exists in the <see cref="System.Linq.ILookup{T1,T2}" />.</summary>
        /// <param name="key">The key to search for in the <see cref="System.Linq.ILookup{T1,T2}" />.</param>
        /// <returns><see langword="true" /> if <paramref name="key" /> is in the <see cref="System.Linq.ILookup{T1,T2}" />; otherwise, <see langword="false" />.</returns>
        bool Contains(TKey key);
    }

    /// <summary>Represents a collection of keys each mapped to one or more values.</summary>
    /// <typeparam name="TKey">The type of the keys in the <see cref="System.Linq.Lookup{T1,T2}" />.</typeparam>
    /// <typeparam name="TElement">The type of the elements of each <see cref="System.Collections.Generic.IEnumerable{T}" /> value in the <see cref="System.Linq.Lookup{T1,T2}" />.</typeparam>
    /// <remarks>A <see cref="System.Linq.Lookup{T1,T2}" /> resembles a <see cref="System.Collections.Generic.Dictionary{T1,T2}" />. The difference is that a <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> maps keys to single values, whereas a <see cref="System.Linq.Lookup{T1,T2}" /> maps keys to collections of values.
    /// You can create an instance of a <see cref="System.Linq.Lookup{T1,T2}" /> by calling <see cref="O:System.Linq.Enumerable.ToLookup" /> on an object that implements <see cref="System.Collections.Generic.IEnumerable{T}" />.
    /// <format type="text/markdown"><![CDATA[
    /// > [!NOTE]
    /// >  There is no public constructor to create a new instance of a <xref:System.Linq.Lookup%602>. Additionally, <xref:System.Linq.Lookup%602> objects are immutable, that is, you cannot add or remove elements or keys from a <xref:System.Linq.Lookup%602> object after it has been created.
    /// ]]></format></remarks>
    /// <example>The following example creates a <see cref="System.Linq.Lookup{T1,T2}" /> from a collection of objects. It then enumerates the <see cref="System.Linq.Lookup{T1,T2}" /> and outputs each key and each value in the key's associated collection of values. It also demonstrates how to use the properties <see cref="O:System.Linq.Lookup{T1,T2}.Count" /> and <see cref="O:System.Linq.Lookup{T1,T2}.Item" /> and the methods <see cref="O:System.Linq.Lookup{T1,T2}.Contains" /> and <see cref="O:System.Linq.Lookup{T1,T2}.GetEnumerator" />.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Lookup/CS/lookup.cs" id="Snippet1":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Lookup/VB/Lookup.vb" id="Snippet1":::</example>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(SystemLinq_LookupDebugView<,>))]
    public partial class Lookup<TKey, TElement> : ILookup<TKey, TElement>
    {
        private readonly IEqualityComparer<TKey> _comparer;
        private Grouping<TKey, TElement>[] _groupings;
        private Grouping<TKey, TElement>? _lastGrouping;
        private int _count;

        internal static Lookup<TKey, TElement> Create<TSource>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer)
        {
            Debug.Assert(source != null);
            Debug.Assert(keySelector != null);
            Debug.Assert(elementSelector != null);

            Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(comparer);
            foreach (TSource item in source)
            {
                lookup.GetGrouping(keySelector(item), create: true)!.Add(elementSelector(item));
            }

            return lookup;
        }

        internal static Lookup<TKey, TElement> Create(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            Debug.Assert(source != null);
            Debug.Assert(keySelector != null);

            Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(comparer);
            foreach (TElement item in source)
            {
                lookup.GetGrouping(keySelector(item), create: true)!.Add(item);
            }

            return lookup;
        }

        internal static Lookup<TKey, TElement> CreateForJoin(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(comparer);
            foreach (TElement item in source)
            {
                TKey key = keySelector(item);
                if (key != null)
                {
                    lookup.GetGrouping(key, create: true)!.Add(item);
                }
            }

            return lookup;
        }

        private Lookup(IEqualityComparer<TKey>? comparer)
        {
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
            _groupings = new Grouping<TKey, TElement>[7];
        }

        /// <summary>Gets the number of key/value collection pairs in the <see cref="System.Linq.Lookup{T1,T2}" />.</summary>
        /// <value>The number of key/value collection pairs in the <see cref="System.Linq.Lookup{T1,T2}" />.</value>
        /// <remarks>The value of the <see cref="O:System.Linq.Lookup{T1,T2}.Count" /> property does not change because items cannot be added to or removed from a <see cref="System.Linq.Lookup{T1,T2}" /> object after it has been created.</remarks>
        /// <example>The following example demonstrates how to use <see cref="O:System.Linq.Lookup{T1,T2}.Count" /> to determine the number of key/value collection pairs in a <see cref="System.Linq.Lookup{T1,T2}" />. This code example is part of a larger example provided for the <see cref="System.Linq.Lookup{T1,T2}" /> class.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Lookup/CS/lookup.cs" id="Snippet2":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Lookup/VB/Lookup.vb" id="Snippet2":::</example>
        public int Count => _count;

        public IEnumerable<TElement> this[TKey key]
        {
            get
            {
                Grouping<TKey, TElement>? grouping = GetGrouping(key, create: false);
                return grouping ?? Enumerable.Empty<TElement>();
            }
        }

        /// <summary>Determines whether a specified key is in the <see cref="System.Linq.Lookup{T1,T2}" />.</summary>
        /// <param name="key">The key to find in the <see cref="System.Linq.Lookup{T1,T2}" />.</param>
        /// <returns><see langword="true" /> if <paramref name="key" /> is in the <see cref="System.Linq.Lookup{T1,T2}" />; otherwise, <see langword="false" />.</returns>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to use <see cref="O:System.Linq.Lookup{T1,T2}.Contains" /> to determine whether a <see cref="System.Linq.Lookup{T1,T2}" /> contains a specified key. This code example is part of a larger example provided for the <see cref="System.Linq.Lookup{T1,T2}" /> class.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Lookup/CS/lookup.cs" id="Snippet4":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Lookup/VB/Lookup.vb" id="Snippet4":::</example>
        public bool Contains(TKey key) => GetGrouping(key, create: false) != null;

        /// <summary>Returns a generic enumerator that iterates through the <see cref="System.Linq.Lookup{T1,T2}" />.</summary>
        /// <returns>An enumerator for the <see cref="System.Linq.Lookup{T1,T2}" />.</returns>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to use <see cref="O:System.Linq.Lookup{T1,T2}.GetEnumerator" /> to iterate through the keys and values of a <see cref="System.Linq.Lookup{T1,T2}" />. This code example is part of a larger example provided for the <see cref="System.Linq.Lookup{T1,T2}" /> class.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Lookup/CS/lookup.cs" id="Snippet5":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Lookup/VB/Lookup.vb" id="Snippet5":::</example>
        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;

                    Debug.Assert(g != null);
                    yield return g;
                }
                while (g != _lastGrouping);
            }
        }

        internal List<TResult> ToList<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            List<TResult> list = new List<TResult>(_count);
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;

                    Debug.Assert(g != null);
                    g.Trim();
                    list.Add(resultSelector(g._key, g._elements));
                }
                while (g != _lastGrouping);
            }

            return list;
        }

        /// <summary>Applies a transform function to each key and its associated values and returns the results.</summary>
        /// <typeparam name="TResult">The type of the result values produced by <paramref name="resultSelector" />.</typeparam>
        /// <param name="resultSelector">A function to project a result value from each key and its associated values.</param>
        /// <returns>A collection that contains one value for each key/value collection pair in the <see cref="System.Linq.Lookup{T1,T2}" />.</returns>
        public IEnumerable<TResult> ApplyResultSelector<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;

                    Debug.Assert(g != null);
                    g.Trim();
                    yield return resultSelector(g._key, g._elements);
                }
                while (g != _lastGrouping);
            }
        }

        /// <summary>Returns an enumerator that iterates through the <see cref="System.Linq.Lookup{T1,T2}" />. This class cannot be inherited.</summary>
        /// <returns>An enumerator for the <see cref="System.Linq.Lookup{T1,T2}" />.</returns>
        /// <remarks>This member is an explicit interface member implementation. It can be used only when the <see cref="System.Linq.Lookup{T1,T2}" /> instance is cast to an <see cref="System.Collections.IEnumerable" /> interface.</remarks>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int InternalGetHashCode(TKey key)
        {
            // Handle comparer implementations that throw when passed null
            return (key == null) ? 0 : _comparer.GetHashCode(key) & 0x7FFFFFFF;
        }

        internal Grouping<TKey, TElement>? GetGrouping(TKey key, bool create)
        {
            int hashCode = InternalGetHashCode(key);
            for (Grouping<TKey, TElement>? g = _groupings[hashCode % _groupings.Length]; g != null; g = g._hashNext)
            {
                if (g._hashCode == hashCode && _comparer.Equals(g._key, key))
                {
                    return g;
                }
            }

            if (create)
            {
                if (_count == _groupings.Length)
                {
                    Resize();
                }

                int index = hashCode % _groupings.Length;
                Grouping<TKey, TElement> g = new Grouping<TKey, TElement>(key, hashCode);
                g._hashNext = _groupings[index];
                _groupings[index] = g;
                if (_lastGrouping == null)
                {
                    g._next = g;
                }
                else
                {
                    g._next = _lastGrouping._next;
                    _lastGrouping._next = g;
                }

                _lastGrouping = g;
                _count++;
                return g;
            }

            return null;
        }

        private void Resize()
        {
            int newSize = checked((_count * 2) + 1);
            Grouping<TKey, TElement>[] newGroupings = new Grouping<TKey, TElement>[newSize];
            Grouping<TKey, TElement> g = _lastGrouping!;
            do
            {
                g = g._next!;
                int index = g._hashCode % newSize;
                g._hashNext = newGroupings[index];
                newGroupings[index] = g;
            }
            while (g != _lastGrouping);

            _groupings = newGroupings;
        }
    }
}
