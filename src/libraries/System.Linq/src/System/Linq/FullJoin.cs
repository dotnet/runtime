// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>
        /// Correlates the elements of two sequences based on matching keys, producing a result for each element
        /// in either sequence that has a match as well as for elements that have no match.
        /// A default or specified equality comparer is used to compare keys.
        /// </summary>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}" /> to hash and compare keys, or <see langword="null" /> to use the default equality comparer.</param>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}" /> that has elements of type <typeparamref name="TResult" /> that are obtained by performing a full outer join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <example>
        /// <para>
        /// The following code example demonstrates how to use <see cref="FullJoin{TOuter, TInner, TKey, TResult}(IEnumerable{TOuter}, IEnumerable{TInner}, Func{TOuter, TKey}, Func{TInner, TKey}, Func{TOuter, TInner, TResult}, IEqualityComparer{TKey})" />
        /// to perform a full outer join of two sequences based on a common key.
        /// </para>
        /// <code>
        /// class Person
        /// {
        ///     public string Name { get; set; }
        /// }
        ///
        /// class Pet
        /// {
        ///     public string Name { get; set; }
        ///     public Person Owner { get; set; }
        /// }
        ///
        /// public static void FullJoin()
        /// {
        ///     Person terry = new Person { Name = "Adams, Terry" };
        ///     Person charlotte = new Person { Name = "Weiss, Charlotte" };
        ///     Person tom = new Person { Name = "Chapkin, Tom" };
        ///     Person magnus = new Person { Name = "Hedlund, Magnus" };
        ///
        ///     Pet barley = new Pet { Name = "Barley", Owner = terry };
        ///     Pet boots = new Pet { Name = "Boots", Owner = terry };
        ///     Pet whiskers = new Pet { Name = "Whiskers", Owner = charlotte };
        ///     Pet daisy = new Pet { Name = "Daisy", Owner = magnus };
        ///
        ///     List{Person} people = new List{Person} { terry, charlotte, tom };
        ///     List{Pet} pets = new List{Pet} { barley, boots, whiskers, daisy };
        ///
        ///     var query =
        ///         people.FullJoin(
        ///             pets,
        ///             person => person,
        ///             pet => pet.Owner,
        ///             (person, pet) => new
        ///             {
        ///                 OwnerName = person?.Name ?? "NONE",
        ///                 PetName = pet?.Name ?? "NONE"
        ///             });
        ///
        ///     foreach (var obj in query)
        ///     {
        ///         Console.WriteLine("{0} - {1}", obj.OwnerName, obj.PetName);
        ///     }
        /// }
        ///
        /// /*
        ///  This code produces the following output:
        ///
        ///  Adams, Terry - Barley
        ///  Adams, Terry - Boots
        ///  Weiss, Charlotte - Whiskers
        ///  Chapkin, Tom - NONE
        ///  NONE - Daisy
        /// */
        /// </code>
        /// </example>
        /// <remarks>
        /// <para>
        /// This method is implemented by using deferred execution. The immediate return value is an object that stores
        /// all the information that is required to perform the action. The query represented by this method is not
        /// executed until the object is enumerated either by calling its <c>GetEnumerator</c> method directly or by
        /// using <c>foreach</c> in C# or <c>For Each</c> in Visual Basic.
        /// </para>
        /// <para>If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to hash and compare keys.</para>
        /// <para>
        /// In relational database terms, the <see cref="FullJoin{TOuter, TInner, TKey, TResult}(IEnumerable{TOuter}, IEnumerable{TInner}, Func{TOuter, TKey}, Func{TInner, TKey}, Func{TOuter, TInner, TResult}, IEqualityComparer{TKey})" /> method implements a full outer equijoin.
        /// 'Full outer' means that elements of both sequences are returned regardless of whether matching elements are found in the other sequence.
        /// An 'equijoin' is a join in which the keys are compared for equality.
        /// </para>
        /// </remarks>
        public static IEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter?, TInner?, TResult> resultSelector, IEqualityComparer<TKey>? comparer = null)
        {
            if (outer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outer);
            }

            if (inner is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inner);
            }

            if (outerKeySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outerKeySelector);
            }

            if (innerKeySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.innerKeySelector);
            }

            if (resultSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            return
                IsEmptyArray(outer) ? EmptyOuterIterator(inner, resultSelector) :
                IsEmptyArray(inner) ? EmptyInnerIterator(outer, resultSelector) :
                FullJoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

            static IEnumerable<TResult> EmptyOuterIterator(IEnumerable<TInner> inner, Func<TOuter?, TInner?, TResult> resultSelector)
            {
                foreach (TInner item in inner)
                {
                    yield return resultSelector(default, item);
                }
            }

            static IEnumerable<TResult> EmptyInnerIterator(IEnumerable<TOuter> outer, Func<TOuter?, TInner?, TResult> resultSelector)
            {
                foreach (TOuter item in outer)
                {
                    yield return resultSelector(item, default);
                }
            }
        }

        /// <summary>
        /// Correlates the elements of two sequences based on matching keys, producing a tuple for each element
        /// in either sequence that has a match as well as for elements that have no match.
        /// A default or specified equality comparer is used to compare keys.
        /// </summary>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}" /> to hash and compare keys, or <see langword="null" /> to use the default equality comparer.</param>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}" /> that has elements of type <c>(TOuter?, TInner?)</c> that are obtained by performing a full outer join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or <paramref name="innerKeySelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>
        /// This method is implemented by using deferred execution. The immediate return value is an object that stores
        /// all the information that is required to perform the action. The query represented by this method is not
        /// executed until the object is enumerated either by calling its <c>GetEnumerator</c> method directly or by
        /// using <c>foreach</c> in C# or <c>For Each</c> in Visual Basic.
        /// </para>
        /// <para>If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to hash and compare keys.</para>
        /// </remarks>
        public static IEnumerable<(TOuter? Outer, TInner? Inner)> FullJoin<TOuter, TInner, TKey>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, IEqualityComparer<TKey>? comparer = null) =>
            FullJoin(outer, inner, outerKeySelector, innerKeySelector, static (outer, inner) => (outer, inner), comparer);

        private static IEnumerable<TResult> FullJoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter?, TInner?, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            // FullJoin needs to preserve inner elements with null keys so they can be emitted
            // as unmatched rows, even though null keys still never participate in matches.
            Lookup<TKey, TInner> innerLookup = Lookup<TKey, TInner>.Create(inner, innerKeySelector, comparer);

            HashSet<Grouping<TKey, TInner>>? matchedGroupings = innerLookup.Count != 0
                ? new HashSet<Grouping<TKey, TInner>>()
                : null;

            foreach (TOuter item in outer)
            {
                TKey key = outerKeySelector(item);
                Grouping<TKey, TInner>? g = key is null ? null : innerLookup.GetGrouping(key, create: false);
                if (g is null)
                {
                    yield return resultSelector(item, default);
                }
                else
                {
                    matchedGroupings!.Add(g);
                    int count = g._count;
                    TInner[] elements = g._elements;
                    for (int i = 0; i != count; ++i)
                    {
                        yield return resultSelector(item, elements[i]);
                    }
                }
            }

            // Yield inner elements that had no matching outer element.
            if (matchedGroupings is null || matchedGroupings.Count < innerLookup.Count)
            {
                Grouping<TKey, TInner>? g = innerLookup._lastGrouping;
                if (g is not null)
                {
                    do
                    {
                        g = g._next!;
                        if (matchedGroupings is null || !matchedGroupings.Contains(g))
                        {
                            int count = g._count;
                            TInner[] elements = g._elements;
                            for (int i = 0; i != count; ++i)
                            {
                                yield return resultSelector(default, elements[i]);
                            }
                        }
                    }
                    while (g != innerLookup._lastGrouping);
                }
            }
        }
    }
}
