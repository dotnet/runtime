// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>
        /// Correlates the elements of two sequences based on matching keys. The default equality comparer is used to compare keys.
        /// </summary>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}" /> that has elements of type <typeparamref name="TResult" /> that are obtained by performing an inner join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <example>
        /// <para>
        /// The following code example demonstrates how to use <see cref="Join{TOuter, TInner, TKey, TResult}(IEnumerable{TOuter}, IEnumerable{TInner}, Func{TOuter, TKey}, Func{TInner, TKey}, Func{TOuter, TInner, TResult})" /> to perform an inner join of two sequences based on a common key.
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
        /// public static void JoinEx1()
        /// {
        ///     Person magnus = new Person { Name = "Hedlund, Magnus" };
        ///     Person terry = new Person { Name = "Adams, Terry" };
        ///     Person charlotte = new Person { Name = "Weiss, Charlotte" };
        ///     Person tom = new Person { Name = "Chapkin, Tom" };
        ///
        ///     Pet barley = new Pet { Name = "Barley", Owner = terry };
        ///     Pet boots = new Pet { Name = "Boots", Owner = terry };
        ///     Pet whiskers = new Pet { Name = "Whiskers", Owner = charlotte };
        ///     Pet daisy = new Pet { Name = "Daisy", Owner = magnus };
        ///
        ///     List{Person} people = new List{Person} { magnus, terry, charlotte, tom };
        ///     List{Pet} pets = new List{Pet} { barley, boots, whiskers, daisy };
        ///
        ///     // Create a list of Person-Pet pairs where
        ///     // each element is an anonymous type that contains a
        ///     // Pet's name and the name of the Person that owns the Pet.
        ///     var query =
        ///         people.Join(pets,
        ///             person => person,
        ///             pet => pet.Owner,
        ///             (person, pet) =>
        ///                 new { OwnerName = person.Name, Pet = pet.Name });
        ///
        ///     foreach (var obj in query)
        ///     {
        ///         Console.WriteLine(
        ///             "{0} - {1}",
        ///             obj.OwnerName,
        ///             obj.Pet);
        ///     }
        /// }
        ///
        /// /*
        ///  This code produces the following output:
        ///
        ///  Hedlund, Magnus - Daisy
        ///  Adams, Terry - Barley
        ///  Adams, Terry - Boots
        ///  Weiss, Charlotte - Whiskers
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
        /// <para>
        /// The default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to hash and compare keys.
        /// </para>
        /// <para>
        /// A join refers to the operation of correlating the elements of two sources of information based on a common key.
        /// <see cref="Join{TOuter, TInner, TKey, TResult}(IEnumerable{TOuter}, IEnumerable{TInner}, Func{TOuter, TKey}, Func{TInner, TKey}, Func{TOuter, TInner, TResult})" />
        /// brings the two information sources and the keys by which they are matched together in one method call.
        /// </para>
        /// <para>
        /// In relational database terms, the <see cref="Join{TOuter, TInner, TKey, TResult}(IEnumerable{TOuter}, IEnumerable{TInner}, Func{TOuter, TKey}, Func{TInner, TKey}, Func{TOuter, TInner, TResult})" /> method implements an inner equijoin.
        /// 'Inner' means that only elements that have a match in the other sequence are included in the results.
        /// An 'equijoin' is a join in which the keys are compared for equality.
        /// An left outer join can be performed using the
        /// <see cref="LeftJoin{TOuter, TInner, TKey, TResult}(IEnumerable{TOuter}, IEnumerable{TInner}, Func{TOuter, TKey}, Func{TInner, TKey}, Func{TOuter, TInner, TResult})" /> method,
        /// and a right outer join can be performed using the
        /// <see cref="RightJoin{TOuter, TInner, TKey, TResult}(IEnumerable{TOuter}, IEnumerable{TInner}, Func{TOuter, TKey}, Func{TInner, TKey}, Func{TOuter, TInner, TResult})" /> method,
        /// For more information, see <see href="/dotnet/csharp/linq/standard-query-operators/join-operations">Join operations</see>.
        /// </para>
        /// </remarks>
        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector) =>
            Join(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer: null);

        /// <summary>
        /// Correlates the elements of two sequences based on matching keys. A specified <see cref="IEqualityComparer{T}" /> is used to compare keys.
        /// </summary>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}" /> to hash and compare keys.</param>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}" /> that has elements of type <typeparamref name="TResult" /> that are obtained by performing an inner join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <example>
        /// <para>
        /// The following code example demonstrates how to use <see cref="Join{TOuter, TInner, TKey, TResult}(IEnumerable{TOuter}, IEnumerable{TInner}, Func{TOuter, TKey}, Func{TInner, TKey}, Func{TOuter, TInner, TResult}, IEqualityComparer{TKey})" /> to perform an inner join of two sequences based on a common key.
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
        /// public static void JoinEx1()
        /// {
        ///     Person magnus = new Person { Name = "Hedlund, Magnus" };
        ///     Person terry = new Person { Name = "Adams, Terry" };
        ///     Person charlotte = new Person { Name = "Weiss, Charlotte" };
        ///     Person tom = new Person { Name = "Chapkin, Tom" };
        ///
        ///     Pet barley = new Pet { Name = "Barley", Owner = terry };
        ///     Pet boots = new Pet { Name = "Boots", Owner = terry };
        ///     Pet whiskers = new Pet { Name = "Whiskers", Owner = charlotte };
        ///     Pet daisy = new Pet { Name = "Daisy", Owner = magnus };
        ///
        ///     List{Person} people = new List{Person} { magnus, terry, charlotte, tom };
        ///     List{Pet} pets = new List{Pet} { barley, boots, whiskers, daisy };
        ///
        ///     // Create a list of Person-Pet pairs where
        ///     // each element is an anonymous type that contains a
        ///     // Pet's name and the name of the Person that owns the Pet.
        ///     var query =
        ///         people.Join(pets,
        ///             person => person,
        ///             pet => pet.Owner,
        ///             (person, pet) =>
        ///                 new { OwnerName = person.Name, Pet = pet.Name });
        ///
        ///     foreach (var obj in query)
        ///     {
        ///         Console.WriteLine(
        ///             "{0} - {1}",
        ///             obj.OwnerName,
        ///             obj.Pet);
        ///     }
        /// }
        ///
        /// /*
        ///  This code produces the following output:
        ///
        ///  Hedlund, Magnus - Daisy
        ///  Adams, Terry - Barley
        ///  Adams, Terry - Boots
        ///  Weiss, Charlotte - Whiskers
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
        /// <para>
        /// The default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to hash and compare keys.
        /// </para>
        /// <para>
        /// A join refers to the operation of correlating the elements of two sources of information based on a common key.
        /// <see cref="Join{TOuter, TInner, TKey, TResult}(IEnumerable{TOuter}, IEnumerable{TInner}, Func{TOuter, TKey}, Func{TInner, TKey}, Func{TOuter, TInner, TResult}, IEqualityComparer{TKey})" />
        /// brings the two information sources and the keys by which they are matched together in one method call.
        /// </para>
        /// <para>
        /// In relational database terms, the <see cref="Join{TOuter, TInner, TKey, TResult}(IEnumerable{TOuter}, IEnumerable{TInner}, Func{TOuter, TKey}, Func{TInner, TKey}, Func{TOuter, TInner, TResult}, IEqualityComparer{TKey})" /> method implements an inner equijoin.
        /// 'Inner' means that only elements that have a match in the other sequence are included in the results.
        /// An 'equijoin' is a join in which the keys are compared for equality.
        /// An left outer join can be performed using the
        /// <see cref="LeftJoin{TOuter, TInner, TKey, TResult}(IEnumerable{TOuter}, IEnumerable{TInner}, Func{TOuter, TKey}, Func{TInner, TKey}, Func{TOuter, TInner, TResult}, IEqualityComparer{TKey})" /> method,
        /// and a right outer join can be performed using the
        /// <see cref="RightJoin{TOuter, TInner, TKey, TResult}(IEnumerable{TOuter}, IEnumerable{TInner}, Func{TOuter, TKey}, Func{TInner, TKey}, Func{TOuter, TInner, TResult}, IEqualityComparer{TKey})" /> method,
        /// For more information, see <see href="/dotnet/csharp/linq/standard-query-operators/join-operations">Join operations</see>.
        /// </para>
        /// </remarks>
        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
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

            if (IsEmptyArray(outer))
            {
                return [];
            }

            return JoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }

        private static IEnumerable<TResult> JoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            using IEnumerator<TOuter> e = outer.GetEnumerator();

            if (e.MoveNext())
            {
                Lookup<TKey, TInner> lookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
                if (lookup.Count != 0)
                {
                    do
                    {
                        TOuter item = e.Current;
                        Grouping<TKey, TInner>? g = lookup.GetGrouping(outerKeySelector(item), create: false);
                        if (g is not null)
                        {
                            int count = g._count;
                            TInner[] elements = g._elements;
                            for (int i = 0; i != count; ++i)
                            {
                                yield return resultSelector(item, elements[i]);
                            }
                        }
                    } while (e.MoveNext());
                }
            }
        }
    }
}
